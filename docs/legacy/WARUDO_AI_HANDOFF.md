> 本文件保留既有協定／功能細節；整合版入口與下載以根 README.md 及 docs/AI_HANDOFF.md 為準。

# Anshaer_Web_Vmc v0.2 handoff

Phone v1.1 -> PeerJS protocol ansha-vmc-osc/2 -> desktop receiver v0.1 -> local WebSocket /ansha-vmc on 19090 -> plugin -> loopback UDP 39539 -> Warudo native VMC Receiver. Both face and body use native VMC templates; swipes use Anshaer_Web_Vmc On VMC Swipe blueprint node.

Plugin/asset/node IDs unchanged from v0.1. Keep only one .cs in StreamingAssets/Playground. No extra DLLs. Uses shipped Newtonsoft.Json and WebSocketSharp.

Bone correction optional/default false. Manually select Normalized/VRM0_Custom/VRM1_Custom; no model selection link or VRM detection. Profiles persist as CalibrationJson in the Warudo scene. Bone picker includes standard humanoid bones, 10 whole-finger groups and individual finger joints. Matching exact bone overrides finger prefix. No invented bone data, no BlendShape edits, no root/position edits.

Math is local quaternion xyzw. Apply inverse(neutral)*q, signed axis map det(M)*M on quaternion vector part (w unchanged), optional quaternion inverse, then left multiply offset. Euler offset convention explicitly qz*qy*qx, degrees. This is stream correction, not a model bind-pose/hierarchy repair. No automatic per-version 180 degree guesses.

OSC wire validated first (bounded parser). BonePacket clones frame, walks nested bundles, rewrites only matching /VMC/Ext/Bone/Pos sfffffff quaternion bytes. Untouched data remains byte-identical. Raw quaternions tracked in one current dictionary, cleared after 3s. Correction caches are bounded by 64KB JSON. Invalid active calibration stops forwarding until fixed/reset; disabled correction bypasses configuration errors. Input zero quaternions cannot be normalized.

T capture trigger waits 3s, requires recent current arm data, stores all received bones except Head for selected profile. It cannot verify actual T pose or tracking confidence because VMC contains no per-bone landmark visibility. No history/counters. Profile changes cancel pending capture; reset profile cancels pending capture. Single-bone reset clears its neutral; finger-prefix reset clears all its joint neutrals.

C# compiled with Warudo API test doubles only; real WebSocket/UDP tested with actual C# code and WebSocketSharp; active correction validated. Real Warudo loading and actual VRM0/1 retargeting are still unverified. Follow the user's screenshots for further iteration; do not claim universal automatic direction correction.

Copyright © 2026-2030 anshaer.com. All rights reserved.

## 名稱更新

Warudo 插件與資產顯示名稱統一為 `Anshaer_Web_Vmc`，原始碼為 `Anshaer_Web_Vmc.cs`。Plugin／Asset／Node ID、骨骼校正、WebSocket 埠與通訊協定維持原設定；版本仍為 v0.2。舊場景可沿用原資產連接。
