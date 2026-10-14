# Unity 線上接收 — v1.1 上架版

- 接收頁：<https://anshaer.github.io/VMC/unity/receiver.html>
- 共用手機：<https://anshaer.github.io/VMC/transmit/mobile.html>
- 共用平板：<https://anshaer.github.io/VMC/transmit/tablet.html>
- 本機連接插件（橋接程式）：<https://anshaer.github.io/VMC/downloads/Anshaer_Web_VMC_Bridge_v0.3.zip>

## 預設使用流程

1. 安裝 Unity 必要的模型接收元件／啟用 VMC 接收；步驟見下方原元件說明。
2. 下載本機連接插件 v0.3，解壓並執行 `Anshaer_Web_VMC_Bridge.exe`。這是本機伴隨程式，不是裝在 Unity 裡面的外掛。
3. 它會開出本機授權選擇頁；點 **Unity 線上接收頁**，瀏覽器會開 GitHub，並帶入本次授權碼。
4. 在 GitHub 接收頁按「連接本機插件」。若瀏覽器提示本機／區域網路存取，允許 `anshaer.github.io`；不用關閉整個瀏覽器的安全機制。
5. 在 GitHub 頁設定 UDP 與轉送範圍，按「套用／開始轉送」。軟體端 UDP 埠須一致，預設 `39540`。
6. 手機／平板直接開 GitHub 傳輸頁，掃描／輸入線上接收頁的 12 碼。

不需要下載接收 HTML。一次只開一個接收頁；三種軟體共用單一橋接输出設定。插件重新啟動會換授權碼，請從新的本機授權頁再進線上接收頁。

本機 HTTP 只監聽 `127.0.0.1`，預設 `19091`。授權碼是 64 位十六進位碼，以 URL fragment 帶入後立即清除；不放進查詢字串或遠端伺服器。不開放其他網站 CORS。手機的 12 碼是另外的 WebRTC 配對碼。

以下為原插件／元件的安装、骨骼與表情設定細節；其中舊「本機接收 HTML」流程已由上方 GitHub 線上流程取代。元件算法與版本保持原樣。

---

# Anshaer Web VMC — Unity v0.1

Unity 用的本機 VMC 接收元件。手機／平板沿用通用 VMC 網頁，資料經既有 Go 橋接器以 UDP 送入 Unity。無需 Warudo、VSeeFace或系統Python。

## 版本與平台

- 目標：**Unity 2022.3 LTS 與 Unity 6**。使用一般 Humanoid Animator、Transform 與 SkinnedMeshRenderer API，沒有第三方Unity套件依賴。
- 模式：Unity Editor **Play Mode**，與Windows／macOS／Linux桌面Standalone。現成橋接EXE是Windows x64，其他系統需要另外編譯Go橋接器。
- 不支援Unity WebGL。瀏覽器無法直接使用本套UDP方式，之後需另做WebRTC／WebSocket資料接收。
- **尚未在真實 Unity Editor 執行。** 已以Unity API測試替身完成Runtime／Editor C#編譯與實際UDP測試，以及Go HTTP→UDP串接。這不是對特定Unity小版本或你的角色視覺效果的實機認證。

## 安裝（建議使用 unitypackage）

1. 從本專案下載 `Anshaer_Web_Vmc_Unity_v0.1.unitypackage`，不用下載網頁ZIP。
2. 在Unity Hub開啟你的Unity專案。
3. Unity上方選 **Assets → Import Package → Custom Package…**。
4. 選下載的 **Anshaer_Web_Vmc_Unity_v0.1.unitypackage**。
5. 匯入視窗保持全部勾選，按 **Import**，等待Unity編譯。
6. Project面板應出現 **Assets/AnshaerWebVMC**。

另一種安裝方式是UPM：使用本區的plugin原始套件，進 **Window → Package Manager → ＋ → Install/Add package from disk…**，選 `plugin/com.anshaer.web-vmc/package.json`。兩種方式擇一，避免同名類別重複。

## 放到角色與設定

1. 把有骨架的角色Prefab放到場景。
2. Hierarchy點角色最外層物件。
3. Inspector按 **Add Component**，搜尋 **Anshaer**，新增 **Anshaer Web Vmc Receiver**。
4. **Target Animator**：拖入角色的Humanoid Animator。**Model Root**：拖入角色的根物件，它應包含臉部SkinnedMeshRenderer與骨架。
5. 在元件底部按 **自動建立 Humanoid／BlendShape 映射**。
6. 展開 **Bone Mappings** 和 **Blend Mappings** 確認目標。Humanoid Avatar須有效；沒有Avatar時骨骼可手動新增映射，臉部BlendShape仍可手動指定。
7. UDP **Port = 39540**；預設接收骨骼／表情，Root位移關閉。
8. 按Unity上方 **▶ Play**。預設 **Start On Play** 勾選，會自動開始接收；也可用元件底部 **開始接收／停止接收** 按鈕。

元件不在Edit Mode接收，請先進入Play Mode。

## 接手機／平板

1. 解壓本包 `../downloads/Anshaer_Web_VMC_Bridge_v0.3.zip`，雙擊裡面的 `Anshaer_Web_VMC_Bridge.exe`。
2. 程式開授權選擇頁；選 Unity，進入 GitHub 線上接收頁。
3. 網頁UDP埠同樣設 **39540**。
4. 先按網頁 **測試轉頭／閉眼／張嘴（3秒）**，確認Unity元件狀態與角色反應。
5. 手機／平板開既有通用VMC頁，輸入或掃描本機接收頁的連接碼。
6. 需要頭部／面部：橋接頁選面部；需要手臂：手機辨識與橋接頁都選上半身；需要腿：兩端都選全身。

不要讓其他程式同時占用39540的UDP接收埠。需要改埠時，先停止Unity接收，修改Port，再開始；橋接網頁也改成相同值。

## 功能

- Humanoid標準骨骼、手掌、手指、眼／下顎與腿部映射。
- BlendShape source → renderer／shapeName映射。`Blink_L`可對EyeBlinkLeft，`A`可對JawOpen，`U`可對MouthPucker。
- source值0–1，**Weight Scale預設100**，對應常見Unity BlendShape權重；特殊Mesh可自行改倍率。
- 每骨骼Signed Axis、旋轉反轉、Rotation Offset（度數）。XYZ要選三個不同軸；不合法組合略過該映射。
- **Use Rest Axes**：依開始時的骨骼軸向轉換Normalized VMC旋轉。不同Rig可能要切換並調整各骨骼補正。
- **3秒後中立校正**：記錄當時新鮮VMC旋轉，對應開始時的姿態；不是自動辨認Tpose。清除校正移除參考訊號。
- **Receive Root**預設關閉，開啟後以第一次收到的Root作為位置與旋轉參考。
- 忽略各骨骼來源位置，避免拉長／縮短模型肢體。
- 更新15–60FPS、Smooth Seconds平滑。
- 停止釋放UDP，不強制讓模型歸零；**停止並還原開始姿態**可恢復元件開始時的骨骼、Root及BlendShape值。Animator／constraints等其他系統仍可繼續改變姿態。

Play Mode內變更與中立校正通常在退出Play後被Unity還原；請依Unity專案的設定保存流程管理。映射與方向補正可在Edit Mode設定並隨場景／Prefab保存。

## VRM注意

本元件不匯入VRM，需使用專案自己的UniVRM／VRM載入器。

骨骼可由Humanoid Animator自動找。表情直接控制Mesh BlendShape，不直接調用VRM0 BlendShapeProxy或VRM1 Expression API；VRM表情Clip、材質綁定與組合權重不會自動展開，請指定實際Mesh shapeName。

VRM1 Control Rig、UniVRM Expression更新、Animator／IK／constraints等可能再覆寫Transform或BlendShape。元件以LateUpdate、ExecutionOrder 10000套用，但不保證在每個自訂系統後執行。需依你專案調整執行順序與目標Rig，不能只因VRM0／VRM1版本就全部轉180度。

## 資源與限制

只監聽127.0.0.1，不直接收另一台手機的UDP；跨網路仍交給原網頁WebRTC方案。

只保留目前骨骼／表情值，名稱數最多128、封包最多65507bytes／128messages／3層bundle。每個更新最多128datagrams或6ms接收時間。不儲存傳輸歷史，不累計幾張／幾次。訊號3秒過期後清除快取，停止寫入該訊號，保持最後姿態；其他動畫系統仍可以驅動模型。

沒有連續錄製AnimationClip／Timeline，沒有服裝或表情快捷鍵事件映射。滑動方向目前仍在橋接網頁顯示，不送到Unity元件。

獨立桌面程式沒有Editor的Inspector。可自行用UI Button的OnClick綁元件：`StartListening()`、`StopReceiver()`、`CalibrateNeutral()`、`ClearCalibration()`、`RestoreStartPose()`。

## 授權

原創Unity程式採MIT，完整授權見LICENSE.txt。原橋接器的第三方授權在其ZIP內。底部版權文字不增加MIT之外的限制。

Copyright © 2026-2030 anshaer.com. All rights reserved.

更新前請先用舊接收頁關閉舊版本橋接程式，再啟動 v0.3；避免本機 HTTP 埠 19091 被舊程式占用。
