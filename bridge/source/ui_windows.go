package main

import (
	"syscall"
	"unsafe"
)

func showError(text string) {
	message, _ := syscall.UTF16PtrFromString(text)
	title, _ := syscall.UTF16PtrFromString("Anshaer Web VMC Bridge")
	proc := syscall.NewLazyDLL("user32.dll").NewProc("MessageBoxW")
	proc.Call(0, uintptr(unsafe.Pointer(message)), uintptr(unsafe.Pointer(title)), 0x10)
}
