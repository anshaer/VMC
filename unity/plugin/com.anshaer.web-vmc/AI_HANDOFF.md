# Unity VMC v0.1 — AI接手

原創MIT實作，無UniVRM/EVMC4U程式碼依賴。目標Unity2022.3／Unity6，未安裝真實Unity Editor，此階段用.NET8 + UnityEngine/UnityEditor API doubles編譯完整Runtime及Editor並跑真實UDP。不可寫成已做Unity實機驗證。

UDP loopback39540由目前Go網頁橋接器轉出；保留原PeerJS手機／平板端。此Unity元件沒有直接WebRTC、WebSocket或相機辨識。WebGL排除，Editor／Desktop目標。

OscVmcDecoder先完整parse/decode再mutation：UTF8、padding、s/f/i、finite、size65507、128messages、depth3、bone quaternion norm0.1–2。格式錯誤不改目前資料。允許VMC/Ext與ANSHA地址但只消費Bone/Root/BlendVal/BlendApply。

主執行緒LateUpdate、ExecutionOrder10000、bounded nonblocking UDP drain128或6ms。只有latest per-channel dictionaries，128names；pendingBlend跨datagram等Apply。3秒清expired，不回零，不累積總數。

SourceQuaternion就是Unity座標，不轉Blender軸。per-bone Signed Axis是orthogonal matrix，反射以det(M)*M作用到Quaternion向量部。source neutral reference^-1*incoming，再rest-world-relative conjugation，再startLocal與Euler補正。Rootfirstframe作參考。Bone positions忽略。

AutoMap依targetAnimator.avatar.isValid/isHuman與GetBoneTransform；沒Avatar不呼叫避免throw。Manual maps仍可用。Direct meshBlendShape使用weightScale100預設、sourcegain；相同renderer/index由別名驅動取max避免重複加。

沒有VRM0/VRM1 expressionAdapter，只有Mesh BlendShape。UniVRM ControlRig與runtime更新的執行順序、renderedRig選擇需要真實專案確認；不能擅自聲稱自動支援所有VRM方向。

Start/Stop/OnDisable/OnApplicationQuit release sockets。Start拒絕EditMode。ModelRoot刪除或更換停。Restore只還原當次Start snapshots，不停Animator等外部系統。Editor提供按鈕；Standalone需App自行綁UI public void methods。

Package: unitypackage路徑Assets/AnshaerWebVMC；UPM name com.anshaer.web-vmc，版本0.1.0。避免同時安裝兩份，會重複類別。
