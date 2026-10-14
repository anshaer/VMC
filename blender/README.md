# Blender 線上接收 — v1.1 上架版

- 接收頁：<https://anshaer.github.io/VMC/blender/receiver.html>
- 共用手機：<https://anshaer.github.io/VMC/transmit/mobile.html>
- 共用平板：<https://anshaer.github.io/VMC/transmit/tablet.html>
- 本機連接插件（橋接程式）：<https://anshaer.github.io/VMC/downloads/Anshaer_Web_VMC_Bridge_v0.3.zip>

## 預設使用流程

1. 安裝 Blender 必要的模型接收元件／啟用 VMC 接收；步驟見下方原元件說明。
2. 下載本機連接插件 v0.3，解壓並執行 `Anshaer_Web_VMC_Bridge.exe`。這是本機伴隨程式，不是裝在 Blender 裡面的外掛。
3. 它會開出本機授權選擇頁；點 **Blender 線上接收頁**，瀏覽器會開 GitHub，並帶入本次授權碼。
4. 在 GitHub 接收頁按「連接本機插件」。若瀏覽器提示本機／區域網路存取，允許 `anshaer.github.io`；不用關閉整個瀏覽器的安全機制。
5. 在 GitHub 頁設定 UDP 與轉送範圍，按「套用／開始轉送」。軟體端 UDP 埠須一致，預設 `39540`。
6. 手機／平板直接開 GitHub 傳輸頁，掃描／輸入線上接收頁的 12 碼。

不需要下載接收 HTML。一次只開一個接收頁；三種軟體共用單一橋接输出設定。插件重新啟動會換授權碼，請從新的本機授權頁再進線上接收頁。

本機 HTTP 只監聽 `127.0.0.1`，預設 `19091`。授權碼是 64 位十六進位碼，以 URL fragment 帶入後立即清除；不放進查詢字串或遠端伺服器。不開放其他網站 CORS。手機的 12 碼是另外的 WebRTC 配對碼。

以下為原插件／元件的安装、骨骼與表情設定細節；其中舊「本機接收 HTML」流程已由上方 GitHub 線上流程取代。元件算法與版本保持原樣。

---

# Anshaer Web VMC — Blender 插件 v0.2

接收現有網頁橋接器輸出的 OSC/VMC UDP，控制 Blender 骨骼與 Shape Key。無需執行 Warudo 或 VSeeFace，也不需另外安裝或切換 Python。

## Blender 版本會有差嗎？

會。主要差在插件安裝格式、骨架／表情相關 API，以及動畫 Action 的資料結構。4.2 開始使用 Extension manifest，4.4 起動畫 Action 有分層／slot 等變更。Steam 是 Blender 的發佈管道，不需要另做一個 Steam 專用插件。本插件以 `keyframe_insert` 插入關鍵幀，不直接依賴舊的 FCurve 列表。

| Blender                       | 本版狀態                                                                          |
| ----------------------------- | --------------------------------------------------------------------------------- |
| **4.5 LTS**                   | 主要版本；使用真實 **4.5.3 LTS 的 bpy API** 通過功能測試                          |
| **4.2 LTS**                   | 支援；使用真實 **4.2.23 LTS 的 bpy API** 通過同一批測試，建議更新該分支的修補版本 |
| **5.2 LTS（Steam 版也適用）** | 已使用真實 **5.2.2 LTS bpy API** 通過同一批測試與橋接通路                         |
| 4.3／4.4／5.0／5.1            | 宣告範圍內，可安裝；尚未逐版本測試                                                |
| 3.6／4.0／4.1                 | 本包不支援，需另做舊版插件                                                        |
| 5.3 及以後                    | 尚未支援，manifest 限制低於 5.3，待測試後再開放                                   |

你使用的 4.5.11 屬於 4.5 LTS 分支，可先使用此包；**4.5.11 本身尚未逐版實測**。不同模型的骨架軸向差異與 Blender 版本是兩件事，不能只靠 VRM0／VRM1 名稱判斷旋轉方向。

插件的 Python 是 Blender 內建版本。你的 Windows 系統 Python 3.13 不會參與這個插件運作。

## 安裝：Blender 5.2.2 LTS／Steam 版

先下載 **Anshaer_Web_Vmc_Blender_v0.2.zip**。舊的 v0.1 設了低於5.0的上限，不能用在5.2；不要繼續選舊包。插件ZIP不用解壓，也不用放Steam安裝資料夾。

### 1. 開啟偏好設定

1. 從 Steam 啟動 Blender。
2. 若有啟動畫面，點畫面外將它關閉。
3. 點主視窗最上方 **Edit（編輯）**。
4. 選 **Preferences…（偏好設定）**，會開啟一個設定視窗。

### 2. 從 ZIP 安裝

1. 設定視窗左側點 **Get Extensions（擴充套件）**。中文翻譯可能不同，英文名稱最容易對照。
2. 看設定視窗**右上角的小下拉箭頭／選單**，點開。
3. 選 **Install from Disk…（從磁碟安裝）**。
4. 在檔案選擇視窗左側選 Downloads（下載），或到你保存ZIP的資料夾。
5. 點選 **Anshaer_Web_Vmc_Blender_v0.2.zip**，不要選前版橋接器ZIP或裡面的 `.exe`。
6. 若右側看得到 **Enable on Install（安裝後啟用）**，保持勾選。
7. 按檔案視窗的 **Install from Disk／從磁碟安裝** 完成。
8. 回到設定視窗左側的 **Add-ons（附加元件）**，在搜尋框輸入 **Anshaer**。
9. 找到 **Anshaer Web VMC**，確認前面的啟用勾選有打勾，再關閉偏好設定。

若 Get Extensions 初次顯示線上存取／官方套件庫介紹，本地 ZIP 不需要先登入或搜尋官方套件庫。仍使用右上選單的 Install from Disk。

另一個官方支援的入口是將插件 ZIP 從檔案總管拖進 Blender，確認安裝提示與 Enable on Install。建議初次先用上面的偏好設定流程。

### 3. 找到插件面板

1. 回到 Blender 主視窗，選 **Layout（佈局）** 工作區。
2. 將滑鼠放到中央**顯示模型／格線的 3D 視窗**裡，按鍵盤 **N**。
3. 3D 視窗右側會展開側欄；右邊的直向頁籤找到 **Anshaer VMC**，點它。
4. 面板標題應是 **Anshaer Web VMC v0.2**。

如果按 N 沒看到，確認滑鼠不是停在右上的物件清單、時間軸、檔案選擇視窗或文字編輯器。這個面板只放在3D Viewport。

### 4. 指定要控制的角色

1. 先打開有角色的 `.blend`；若只有 `.vrm`，先安裝 **VRM format / VRM Add-on for Blender** 再匯入。本插件本身不匯入VRM。
2. 面板的 **角色骨架** 下拉選角色的 Armature，不能選只有網格的Mesh物件。
3. **臉部網格** 可指定有眨眼／嘴型Shape Key的Mesh；留空會找骨架子網格與Armature modifier關聯網格。
4. 按 **自動建立映射**。
5. 展開 **骨骼映射／方向校正**，確認 `Head` 有對到角色頭骨、左右手臂有對到正確骨骼。空白代表沒自動找到，可以在目標欄自行選。
6. UDP 埠保持 **39540**，按 **開始接收**。狀態應變成「等待本機 UDP」或「等待資料」。

只安裝插件並不會自動開始接收。骨架、網格與映射需要先選好。

## 配合現有網頁橋接器

1. 雙擊 `Anshaer_Web_VMC_Bridge.exe`；選 Blender，進入 GitHub 線上接收頁。
2. Blender 插件 UDP 埠設為 **39540**，按「開始接收」。橋接網頁也使用 **39540**。
3. 在橋接頁按「測試轉頭／閉眼／張嘴（3秒）」，先確認 Blender 有收到。
4. 手機／平板沿用既有 VMC 頁，掃描／輸入橋接頁的連接碼。
5. 面捕只用「面部／頭部」；需要手臂時手機辨識與橋接頁都選上半身；腿部則兩端都選全身。

同一台電腦的同一個 UDP 接收埠不要同時交給 VSeeFace 與 Blender。若 VSeeFace 已占用 39540，先關閉它的 VMC receiver，或將橋接網頁與 Blender 都改成另一個埠，例如 39541。

本版 Blender 插件只監聽 **127.0.0.1**，必須與網頁橋接器在同一台電腦。手機仍走 WebRTC，無需直接向 Blender 的 UDP 埠連線。

## 骨骼、方向與校正

- 支援標準 humanoid 軀幹、頭部、眼／下顎、雙臂、手掌、手指、雙腿與腳。
- 自動映射優先讀 VRM Add-on 的 humanoid 資訊，再找 VMC 同名、VRoid J_Bip、部分 Mixamo／Rigify 名稱。
- Rigify 的 IK 控制骨不等同於 deform 骨；複雜 rig 請自行選對目標，現有 constraints／drivers 仍可能影響最終姿勢。
- 預設來源座標為 Unity → Blender：`(X, -Z, Y)`，並依骨骼靜止軸轉換旋轉。不同來源不適合此設定時可選「原始 XYZ」，或取消「依骨骼靜止軸轉換」。
- 每個骨骼可獨立指定 X／Y／Z 取自哪個軸、反號、旋轉反轉、局部旋轉補正。XYZ 要選三個不同的軸。
- **3 秒後中立校正**：記錄當時新鮮的 VMC 骨骼旋轉，對應開始接收時的姿態。校正後訊號差值驅動角色；不是自動辨認 T 姿勢，也不是自動修好所有骨架方向。
- **清除中立校正**：移除訊號參考，不改手動軸向補正。
- 預設只套用骨骼旋轉，**忽略來源的骨骼位置**，避免改變角色手腳長度。
- Root 位移／旋轉預設關閉。開啟後以第一次收到的新鮮 Root 為參考，控制整個骨架物件；位移倍率可調。父物件與非標準縮放可能仍需要專案自己的處理。

停止接收會釋放 UDP 埠／timer，保留當下姿態；「還原開始姿態」會先停止，再恢復開始時的骨骼、Root 與已選臉部網格的 Shape Key 值。

## 表情

可直接對應 Shape Key，或使用已匯入 VRM0／VRM1 的表情 morph bind 資訊。

- `Blink_L`／`Blink_R`：左右閉眼，可自動對應 `EyeBlinkLeft`／`EyeBlinkRight` 等名稱。
- `A`／`JawOpen`：張嘴。
- `U`／`MouthPucker`：嘟嘴。
- `Joy`：微笑。
- 可以新增自訂 VMC 表情名稱、設定目標名稱與強度。

同一 Shape Key 被多個別名驅動時，使用目前最大的映射值，避免 Blink_L 與 EyeBlinkLeft 疊加兩次。表情值要收到 `/VMC/Ext/Blend/Apply` 才套用。

本版只處理 morph／Shape Key，不處理 VRM 材質色彩、透明度、貼圖切換等表情綁定，也不會為模型生成不存在的 Shape Key。VRM metadata adapter 以資料結構測試驗證；各版本 VRM 匯入插件與實際模型仍需確認。

## 記錄動作

「**記錄目前影格**」會把目前已套用的骨骼 quaternion 與 Shape Key 插入當前影格，Root 開啟時也記錄骨架物件的位置／旋轉。骨架與 Shape Key 各使用自己的動畫資料。

本版是單影格記錄，**尚未提供連續錄製／自動推進時間軸**。沒有新鮮資料時不會假裝記錄成功。

## 資源與狀態

- Blender 主執行緒 timer 接收，沒有背景執行緒修改 bpy，也沒有額外 pip 套件。
- 更新 15／30／60 FPS。每個 timer 接收數與時間有上限。
- 只保留目前骨骼／表情，限制名稱與封包大小；不累計已傳輸幾張、幾次。
- 3 秒沒更新就清除訊號快取，角色保持姿態，不突然歸零。
- 開新 `.blend`、停用插件或目標骨架被刪除時會停止接收並釋放資源。
- 映射／強度／中立參考存於 Scene，可隨 `.blend` 保存；執行中的 socket 不會存入檔案，也不會隨載入自動開始。

## 已驗證與限制

使用真實 Blender bpy 4.2.23／4.5.3／5.2.2 API 建立測試骨架與網格，完成 UDP 接收、旋轉、閉眼／嘴型、Blend Apply、方向映射、校正、骨架／Shape Key 關鍵幀、逾時保持姿態、卸載／重啟及刪除目標等測試。

另使用現有 Go 橋接器實際 HTTP → UDP → Blender 4.5／5.2 的骨架／表情通路。**尚未在 Windows Blender GUI 與你的真人／角色模型做視覺校模**；測試版的方向與表情映射需依實際角色確認。

## 授權與版權

插件原始碼採 **GPL-3.0-or-later**，完整授權在 `LICENSE.txt`，可依該授權使用與修改。底部版權文字不增加 GPL 以外的限制。

Copyright © 2026-2030 anshaer.com. All rights reserved.

## v0.2 更新

開放 5.2 系列，最高版本界線為5.3；驗證5.2.2 API與橋接通路，補上Steam／中文介面的逐步安裝說明。現有映射與插件ID沿用，沒有新增外部Python依賴。

更新前請先用舊接收頁關閉舊版本橋接程式，再啟動 v0.3；避免本機 HTTP 埠 19091 被舊程式占用。
