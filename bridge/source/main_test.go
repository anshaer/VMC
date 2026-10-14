package main

import (
	"encoding/binary"
	"math"
	"testing"
)

func oscText(s string) []byte { b := make([]byte, (len(s)+4)&^3); copy(b, s); return b }
func oscNumber(value float32) []byte {
	p := append(oscText("/VMC/Ext/T"), oscText(",f")...)
	b := make([]byte, 4)
	binary.BigEndian.PutUint32(b, math.Float32bits(value))
	return append(p, b...)
}
func oscPack(packets ...[]byte) []byte {
	b := make([]byte, 16)
	copy(b, "#bundle")
	for _, p := range packets {
		n := make([]byte, 4)
		binary.BigEndian.PutUint32(n, uint32(len(p)))
		b = append(b, n...)
		b = append(b, p...)
	}
	return b
}
func TestPacketLimits(t *testing.T) {
	valid := oscNumber(.5)
	if validateOSC(valid) != nil || validateOSC(oscPack(valid)) != nil {
		t.Fatal("valid OSC rejected")
	}
	bad := [][]byte{nil, make([]byte, 65508), oscNumber(float32(math.NaN())), oscNumber(float32(math.Inf(1))), valid[:len(valid)-1], append(append([]byte{}, valid...), 0, 0, 0, 0), oscPack()}
	deep := valid
	for i := 0; i < 5; i++ {
		deep = oscPack(deep)
	}
	bad = append(bad, deep)
	many := make([][]byte, 129)
	for i := range many {
		many[i] = valid
	}
	bad = append(bad, oscPack(many...))
	padding := append([]byte{}, valid...)
	padding[11] = 1
	bad = append(bad, padding)
	for i, p := range bad {
		if validateOSC(p) == nil {
			t.Fatalf("bad OSC accepted %d", i)
		}
	}
}
