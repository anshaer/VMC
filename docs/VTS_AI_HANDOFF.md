# AI handoff — Anshaer Web VMC VTube Studio v0.1

## Architecture

Sender → PeerJS Cloud signaling → WebRTC data channel → browser receiver → localhost VTube Studio public WebSocket API. No video streaming and no OSC/UDP server required for this receiver. Existing phone and tablet pages retain protocol `ansha-vmc-osc/2`, peer prefix `ansha-vmc2-`, 12-character pairing code, binary OSC motion packets, scope selection, previews and swipe messages.

Keep `VmcPairLink` and `VmcRegionReceiver` compatible with existing Universal-VMC senders. Max 8 sources; regions all/face/body/torso/leftArm/rightArm/hands/leftHand/rightHand/legs/root; oldest connected fresh owner wins per region. After 3 seconds stale data is released. Swipe sequence IDs are remapped per source and acknowledgements route back to the correct sender. Never acknowledge a configured VTS action before HotkeyTriggerResponse succeeds. Unmapped directions acknowledge receipt only.

VTS client uses `ws://127.0.0.1:8001`, API envelopes `VTubeStudioPublicAPI`, version `1.0`, matched bounded request IDs, timeout and socket epoch guards. Plugin name `Anshaer Web VMC VTube Studio`, developer `anshaer.com`. Persist one authentication token per port, reuse for AuthenticationRequest. Token acquisition is manual; API denial50 blocks automatic auth prompting. No tokens in exported JSON or UI logs. Unexpected disconnect retries after3s; explicit disconnect stops retries.

InputParameterListRequest supplies existing default/custom inputs. HotkeysInCurrentModelRequest supplies real model hotkey IDs. CurrentModelRequest polls4s; do not fetch metadata every frame. Injection uses set, per-parameter weight0..1; faceFound is omitted. One inject request in flight; pending API limit16. FPS15/30/60. Only fresh owned signals may be injected: do not use synthetic neutral bones from the legacy merged packet. `currentSignals()` reads owner source messages directly. Face head orientation uses existing vmcDecode normalized signals; local bone quaternion decomposition is extrinsic XYZ (equivalently intrinsic ZYX): rollX=atan2(2(wx+yz),1-2(x²+y²)), pitchY=asin(2(wy-zx)), yawZ=atan2(2(wz+xy),1-2(y²+z²)), degrees. Angle wrapping/gimbal lock can occur; not a universal 3D→Live2D rig.

Mapping formula clamps normalized (source-inMin)/(inMax-inMin) to0..1, interpolates outMin..outMax, clamps to actual VTS input min/max. Input endpoints may reverse; identical endpoints skip. No duplicate targets; max64 mappings. Imported JSON max64KB, known source keys and finite bounded endpoints required. Standardface preset only adds available input names; eyes invert blink to eyeopen. No raw Live2D output IDs and no custom parameter creation in v0.1.

Set-mode has one API owner per parameter; mixing weight is with native tracking. Stopping injections releases VTS input after its API timeout (~1s), do not send artificial neutral reset. Only genuine current sources control. Model mappings must exist inside VTS. Full body/fingers need model-specific input mappings.

Hotkeys: one request at a time, 200ms minimum spacing, no accumulating queue. Swipe UI/routes expire3s. Store no lifetime frame/command logs. Pagehide clears request timers, reconnect timers, preview references, pairing timers and sources.

## Files and extending

receiver.html is self-contained with embedded third-party libraries and licenses. mobile.html/tablet.html are compatible reusable copies of established Universal-VMC sources. Preserve multitou​​ch, separate face/body spring toggles, collapsed preview, camera model paths and bounded previews. Phone requires HTTPS plus internet for MediaPipe resources; local receiver requires internet for PeerJS signaling. Test real VTS and real hardware before announcing full support. Official API: https://github.com/DenchiSoft/VTubeStudio
