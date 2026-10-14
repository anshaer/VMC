# 驗證範圍

2026-10-14，Unity套件v0.1。

完整Runtime與Editor C#使用.NET8 Roslyn和Unity API測試替身編譯；Quaternion等替身以System.Numerics實作。沒有真實Unity Editor／Unity官方runtime二進位參與這項編譯。

已驗證：

- 真正UdpClient socket接收；自動Humanoid／BlendShape map。
- HeadQuaternion、閉眼0.8→權重80、嘴0.7→權重70。
- BlendVal等待BlendApply；NaN／malformed packet原子拒絕。
- SignedAxis反射、重複軸拒絕、3秒中立校正邏輯。
- Root第一次位置參考與後續相對位移。
- 3秒過期／保持姿態、停止、還原初始權重／pose、埠再bind、重新開始。
- EditMode拒絕開始，目標更換停止。
- 另以現有Go橋接器重建Linux版本，實際HTTP→Go→UDP→Unity接收C#邏輯測試。
- unitypackage GUID/meta/pathname與ZIP/tar完整性。

未驗證：真實Unity2022.3／Unity6的Editor匯入、Unity人物渲染、Prefab序列化與Inspector實際排版、UniVRM0／1真實角色、桌面Player/IL2CPPBuild。此包是可供實機測試的源碼套件。
