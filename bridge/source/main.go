// Copyright © 2026-2030 anshaer.com. All rights reserved.
package main

import (
	"bytes"
	"context"
	"crypto/rand"
	"crypto/subtle"
	_ "embed"
	"encoding/binary"
	"encoding/hex"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"io"
	"math"
	"net"
	"net/http"
	"os"
	"os/exec"
	"os/signal"
	"runtime"
	"strings"
	"sync"
	"syscall"
	"time"
	"unicode/utf8"
)

//go:embed receiver.html
var receiverHTML string

//go:embed blender.html
var blenderHTML string

//go:embed unity.html
var unityHTML string

//go:embed launcher.html
var launcherHTML string

const product = "Anshaer_Web_VMC_Bridge_v0.3"

type bridge struct {
	mu          sync.Mutex
	udp         *net.UDPConn
	port        int
	enabled     bool
	lastFrame   time.Time
	lastBrowser time.Time
	lastError   string
	token       string
	host        string
	quit        chan struct{}
	once        sync.Once
}

func (b *bridge) stop() { b.once.Do(func() { close(b.quit) }) }
func (b *bridge) auth(r *http.Request) bool {
	if r.Host != b.host {
		return false
	}
	origin := r.Header.Get("Origin")
	if origin != "" && origin != "http://"+b.host && origin != "https://anshaer.github.io" {
		return false
	}
	return subtle.ConstantTimeCompare([]byte(r.Header.Get("X-Bridge-Key")), []byte(b.token)) == 1
}
func respond(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	json.NewEncoder(w).Encode(v)
}
func (b *bridge) handler() http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Cache-Control", "no-store")
		w.Header().Set("X-Content-Type-Options", "nosniff")
		w.Header().Set("Referrer-Policy", "no-referrer")
		w.Header().Set("X-Frame-Options", "DENY")
		if r.Host != b.host {
			http.Error(w, "Wrong host", 403)
			return
		}
		origin := r.Header.Get("Origin")
		if origin == "https://anshaer.github.io" {
			w.Header().Set("Access-Control-Allow-Origin", origin)
			w.Header().Set("Vary", "Origin")
			w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "X-Bridge-Key, Content-Type")
		}
		if r.Method == "OPTIONS" {
			if origin != "https://anshaer.github.io" || !strings.HasPrefix(r.URL.Path, "/api/") {
				http.Error(w, "Origin not allowed", 403)
				return
			}
			w.WriteHeader(204)
			return
		}
		if r.URL.Path == "/" && r.Method == "GET" {
			w.Header().Set("Content-Type", "text/html; charset=utf-8")
			io.WriteString(w, strings.ReplaceAll(strings.ReplaceAll(launcherHTML, "__BRIDGE_KEY__", b.token), "__BRIDGE_HOST__", b.host))
			return
		}
		if r.URL.Path == "/health" && r.Method == "GET" {
			respond(w, map[string]string{"product": product})
			return
		}
		page := ""
		switch r.URL.Path {
		case "/", "/receiver.html", "/vseeface/receiver.html":
			page = receiverHTML
		case "/blender/receiver.html":
			page = blenderHTML
		case "/unity/receiver.html":
			page = unityHTML
		}
		if page != "" && r.Method == "GET" {
			w.Header().Set("Content-Type", "text/html; charset=utf-8")
			io.WriteString(w, strings.Replace(page, "__BRIDGE_KEY__", b.token, 1))
			return
		}
		if !strings.HasPrefix(r.URL.Path, "/api/") {
			http.NotFound(w, r)
			return
		}
		if !b.auth(r) {
			http.Error(w, "Unauthorized local request", 403)
			return
		}
		b.mu.Lock()
		b.lastBrowser = time.Now()
		b.mu.Unlock()
		switch r.URL.Path {
		case "/api/status":
			if r.Method != "GET" {
				http.Error(w, "GET required", 405)
				return
			}
			b.mu.Lock()
			v := map[string]any{"enabled": b.enabled, "udpPort": b.port, "fresh": !b.lastFrame.IsZero() && time.Since(b.lastFrame) < 3*time.Second, "error": b.lastError}
			b.mu.Unlock()
			respond(w, v)
		case "/api/config":
			if r.Method != "POST" {
				http.Error(w, "POST required", 405)
				return
			}
			var c struct {
				UDPPort *int  `json:"udpPort"`
				Enabled *bool `json:"enabled"`
			}
			dec := json.NewDecoder(http.MaxBytesReader(w, r.Body, 1024))
			dec.DisallowUnknownFields()
			if dec.Decode(&c) != nil || dec.Decode(&struct{}{}) != io.EOF || (c.UDPPort != nil && (*c.UDPPort < 1 || *c.UDPPort > 65535)) {
				http.Error(w, "Invalid config", 400)
				return
			}
			b.mu.Lock()
			if c.UDPPort != nil {
				b.port = *c.UDPPort
				b.lastFrame = time.Time{}
			}
			if c.Enabled != nil {
				b.enabled = *c.Enabled
				if !b.enabled {
					b.lastFrame = time.Time{}
				}
			}
			b.lastError = ""
			b.mu.Unlock()
			respond(w, map[string]bool{"ok": true})
		case "/api/frame":
			if r.Method != "POST" {
				http.Error(w, "POST required", 405)
				return
			}
			if r.Header.Get("Content-Type") != "application/octet-stream" {
				http.Error(w, "OSC binary required", 415)
				return
			}
			packet, err := io.ReadAll(http.MaxBytesReader(w, r.Body, 65507))
			if err != nil || validateOSC(packet) != nil {
				http.Error(w, "Invalid or oversized OSC/VMC packet", 400)
				return
			}
			b.mu.Lock()
			defer b.mu.Unlock()
			if !b.enabled {
				http.Error(w, "Forwarding is stopped", 409)
				return
			}
			// No queue or history: forward the current request, retaining only its timestamp.
			if err = b.udp.SetWriteDeadline(time.Now().Add(time.Second)); err == nil {
				_, err = b.udp.WriteToUDP(packet, &net.UDPAddr{IP: net.IPv4(127, 0, 0, 1), Port: b.port})
			}
			if err != nil {
				b.lastError = "UDP: " + err.Error()
				http.Error(w, "UDP send failed", 502)
				return
			}
			b.lastFrame = time.Now()
			b.lastError = ""
			w.WriteHeader(204)
		case "/api/quit":
			if r.Method != "POST" {
				http.Error(w, "POST required", 405)
				return
			}
			respond(w, map[string]bool{"ok": true})
			go func() { time.Sleep(100 * time.Millisecond); b.stop() }()
		case "/api/tab-close":
			if r.Method != "POST" {
				http.Error(w, "POST required", 405)
				return
			}
			w.WriteHeader(204)
		default:
			http.NotFound(w, r)
		}
	})
}
func validateOSC(data []byte) error {
	if len(data) < 4 || len(data) > 65507 || len(data)%4 != 0 {
		return errors.New("length")
	}
	count := 0
	var parse func([]byte, int) error
	parse = func(p []byte, depth int) error {
		if depth > 3 {
			return errors.New("depth")
		}
		at := 0
		str := func() (string, error) {
			start := at
			for at < len(p) && p[at] != 0 {
				at++
			}
			if at >= len(p) || at-start > 1024 {
				return "", errors.New("string")
			}
			s := p[start:at]
			if !utf8.Valid(s) {
				return "", errors.New("utf8")
			}
			end := (at + 4) &^ 3
			if end > len(p) {
				return "", errors.New("padding")
			}
			for _, c := range p[at:end] {
				if c != 0 {
					return "", errors.New("padding")
				}
			}
			at = end
			return string(s), nil
		}
		address, err := str()
		if err != nil {
			return err
		}
		if address == "#bundle" {
			if len(p) < 16 {
				return errors.New("bundle")
			}
			at = 16
			for at < len(p) {
				if at+4 > len(p) {
					return errors.New("size")
				}
				n := int(binary.BigEndian.Uint32(p[at : at+4]))
				at += 4
				if n < 4 || n%4 != 0 || n > len(p)-at {
					return errors.New("size")
				}
				if err = parse(p[at:at+n], depth+1); err != nil {
					return err
				}
				at += n
			}
			return nil
		}
		count++
		if count > 128 || !strings.HasPrefix(address, "/VMC/Ext/") {
			return errors.New("address")
		}
		tags, err := str()
		if err != nil || !strings.HasPrefix(tags, ",") {
			return errors.New("tags")
		}
		for _, t := range tags[1:] {
			switch t {
			case 's':
				if _, err = str(); err != nil {
					return err
				}
			case 'f', 'i':
				if at+4 > len(p) {
					return errors.New("number")
				}
				if t == 'f' {
					v := math.Float32frombits(binary.BigEndian.Uint32(p[at : at+4]))
					if math.IsNaN(float64(v)) || math.IsInf(float64(v), 0) {
						return errors.New("finite")
					}
				}
				at += 4
			default:
				return errors.New("type")
			}
		}
		if at != len(p) {
			return errors.New("trailing")
		}
		return nil
	}
	err := parse(data, 0)
	if err == nil && count == 0 {
		return errors.New("empty")
	}
	return err
}
func openBrowser(url string) {
	switch runtime.GOOS {
	case "windows":
		exec.Command("rundll32.exe", "url.dll,FileProtocolHandler", url).Start()
	case "darwin":
		exec.Command("open", url).Start()
	default:
		exec.Command("xdg-open", url).Start()
	}
}
func runningBridge(host string) bool {
	client := http.Client{Timeout: time.Second}
	res, err := client.Get("http://" + host + "/health")
	if err != nil {
		return false
	}
	defer res.Body.Close()
	p, err := io.ReadAll(io.LimitReader(res.Body, 1024))
	return err == nil && bytes.Contains(p, []byte(product))
}
func run() error {
	httpPort := flag.Int("http-port", 19091, "Local receiver webpage port")
	udpPort := flag.Int("udp-port", 39540, "Target VMC receiver UDP port")
	noOpen := flag.Bool("no-open", false, "Do not launch the default browser")
	flag.Parse()
	if *httpPort < 1024 || *httpPort > 65535 || *udpPort < 1 || *udpPort > 65535 {
		return errors.New("invalid port")
	}
	host := fmt.Sprintf("127.0.0.1:%d", *httpPort)
	listener, err := net.Listen("tcp4", host)
	if err != nil {
		if runningBridge(host) {
			if !*noOpen {
				openBrowser("http://" + host + "/")
			}
			return nil
		}
		return fmt.Errorf("本機網頁埠 %d 已被其他程式使用；可用 --http-port 19092 指定另一個埠", *httpPort)
	}
	defer listener.Close()
	udp, err := net.ListenUDP("udp4", &net.UDPAddr{IP: net.IPv4(127, 0, 0, 1), Port: 0})
	if err != nil {
		return err
	}
	defer udp.Close()
	token := make([]byte, 32)
	if _, err = rand.Read(token); err != nil {
		return err
	}
	b := &bridge{udp: udp, port: *udpPort, enabled: true, lastBrowser: time.Now(), token: hex.EncodeToString(token), host: host, quit: make(chan struct{})}
	server := &http.Server{Handler: b.handler(), ReadHeaderTimeout: 3 * time.Second, ReadTimeout: 5 * time.Second, WriteTimeout: 5 * time.Second, IdleTimeout: 15 * time.Second, MaxHeaderBytes: 8192}
	errs := make(chan error, 1)
	go func() { errs <- server.Serve(listener) }()
	defer func() {
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		server.Shutdown(ctx)
	}()
	if !*noOpen {
		openBrowser("http://" + host + "/")
	}
	fmt.Println(product, "http://"+host+"/", "→ UDP 127.0.0.1:", *udpPort)
	signals := make(chan os.Signal, 1)
	signal.Notify(signals, os.Interrupt, syscall.SIGTERM)
	defer signal.Stop(signals)
	tick := time.NewTicker(time.Second)
	defer tick.Stop()
	for {
		select {
		case <-b.quit:
			return nil
		case <-signals:
			return nil
		case err = <-errs:
			if err == http.ErrServerClosed {
				return nil
			}
			return err
		case <-tick.C:
			b.mu.Lock()
			idle := time.Since(b.lastBrowser) > 3*time.Minute
			if time.Since(b.lastFrame) >= 3*time.Second {
				b.lastFrame = time.Time{}
				b.lastError = ""
			}
			b.mu.Unlock()
			if idle {
				return nil
			}
		}
	}
}
func main() {
	if err := run(); err != nil {
		showError(err.Error())
		os.Exit(1)
	}
}
