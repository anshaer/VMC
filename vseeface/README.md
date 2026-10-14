# VSeeFace 線上接收 — v1.1 上架版

- 接收頁：<https://anshaer.github.io/VMC/vseeface/receiver.html>
- 共用手機：<https://anshaer.github.io/VMC/transmit/mobile.html>
- 共用平板：<https://anshaer.github.io/VMC/transmit/tablet.html>
- 本機連接插件（橋接程式）：<https://anshaer.github.io/VMC/downloads/Anshaer_Web_VMC_Bridge_v0.3.zip>

## 預設使用流程

1. 安裝 VSeeFace 必要的模型接收元件／啟用 VMC 接收；步驟見下方原元件說明。
2. 下載本機連接插件 v0.3，解壓並執行 `Anshaer_Web_VMC_Bridge.exe`。這是本機伴隨程式，不是裝在 VSeeFace 裡面的外掛。
3. 它會開出本機授權選擇頁；點 **VSeeFace 線上接收頁**，瀏覽器會開 GitHub，並帶入本次授權碼。
4. 在 GitHub 接收頁按「連接本機插件」。若瀏覽器提示本機／區域網路存取，允許 `anshaer.github.io`；不用關閉整個瀏覽器的安全機制。
5. 在 GitHub 頁設定 UDP 與轉送範圍，按「套用／開始轉送」。軟體端 UDP 埠須一致，預設 `39540`。
6. 手機／平板直接開 GitHub 傳輸頁，掃描／輸入線上接收頁的 12 碼。

不需要下載接收 HTML。一次只開一個接收頁；三種軟體共用單一橋接输出設定。插件重新啟動會換授權碼，請從新的本機授權頁再進線上接收頁。

本機 HTTP 只監聽 `127.0.0.1`，預設 `19091`。授權碼是 64 位十六進位碼，以 URL fragment 帶入後立即清除；不放進查詢字串或遠端伺服器。不開放其他網站 CORS。手機的 12 碼是另外的 WebRTC 配對碼。

以下為原插件／元件的安装、骨骼與表情設定細節；其中舊「本機接收 HTML」流程已由上方 GitHub 線上流程取代。元件算法與版本保持原樣。

---

# Anshaer Web VMC → VSeeFace v0.1

Windows 10／11 x64 免安裝測試版。將手機或平板網頁的 VMC 資料直接轉送給 VSeeFace，不需要 Warudo、Python、.NET 安裝或管理員權限。

## 快速開始

1. 解壓 ZIP，雙擊 `Anshaer_Web_VMC_Bridge.exe`。預設瀏覽器會開啟 `http://127.0.0.1:19091/`。從授權選擇頁點 VSeeFace，進入 GitHub 接收頁並按連接本機插件。
2. 開 VSeeFace，載入 **VRM0** 模型。在 **General settings** 開啟 **VMC protocol receiver**，接收埠設為 **39540**。
3. 初次測試先取消 VMC 接收區域內所有 **Track…** 勾選，避免 VSeeFace 自己的辨識覆蓋收到的資料；也先關閉 VMC sender。那些 Track 選項表示「追加 VSeeFace 本機辨識」，不是「允許接收」。
4. 接收網頁預設「面部／頭部」與 UDP 埠 39540。按 **測試轉頭／閉眼／張嘴（3秒）**，檢查角色是否有反應。測試結束送一次中立面部值；如果手機已有新鮮動捕資料，測試不會搶占它。
5. 手機開原本通用 VMC 手機網頁，直接使用 https://anshaer.github.io/VMC/transmit/mobile.html。掃描／輸入電腦接收頁的連接碼，按開啟辨識。
6. 平板同樣使用原本 Warudo 用途的通用 VMC 平板端，或 https://anshaer.github.io/VMC/transmit/tablet.html，不需要開相機。
7. 需要手臂時，手機辨識模式與電腦轉送範圍都選上半身；需要腿部時兩端都選全身。

既有手機／平板頁可沿用，網址名稱包含 warudo 也不代表需要執行 Warudo：

- [現有手機端](https://anshaer.github.io/VMC/transmit/mobile.html)
- [現有平板端](https://anshaer.github.io/VMC/transmit/tablet.html)
- [VSeeFace 官方 VMC 說明](https://www.vseeface.icu/#vmc)

手機頁如需使用相機，請使用 HTTPS。電腦接收頁預設在 GitHub 直接開啟，EXE只提供授權選擇與UDP橋接；不需要下載接收HTML。

## 電腦接收頁

| 設定           | 功能                                                               |
| -------------- | ------------------------------------------------------------------ |
| VMC UDP 埠     | 對應 VSeeFace 的接收埠，預設 39540；目標固定為同一台電腦 127.0.0.1 |
| 面部／頭部     | 只轉送 Head、眼／下顎骨與 BlendShape；不送身體、手指或 Root        |
| 上半身／手指   | 轉送上半身、手指與面部；排除腿、腳與 Root                          |
| 全身／手指     | 轉送整個 VMC 身體、手指、Root 與面部                               |
| 轉送 FPS       | 15／30／60，預設 30；以來源更新率為上限                            |
| 套用／開始轉送 | 套用 UDP 埠並啟用轉送                                              |
| 停止轉送       | 停止 UDP；網頁可繼續配對／預覽，不退出程式                         |
| 關閉橋接程式   | 關閉本機伺服器與 UDP socket，釋放埠，之後可刪除 EXE                |

「最近有資料送出」只表示橋接器成功送出 UDP，**無法確認 VSeeFace 已接收或模型已套用**。

只關瀏覽器分頁時，程式在沒有網頁心跳約 3 分鐘後自動退出；需要立即結束請按「關閉橋接程式」。重複雙擊同一個 EXE 會開啟既有接收頁，不會新增第二個橋接程序。接收頁不會記住 UDP 埠：重新啟動預設回到 39540；當次頁面重新整理會從目前橋接器取得埠。

## 多台手機／平板

最多 8 台共用同一接收碼。沿用通用 VMC 的分區：面部、身體、左右手臂、手、腿等。

同區域先連線且資料新鮮的來源優先，3 秒未更新可切備援。這是控制分工，並非多相機融合。每個來源只保留目前資料；不累積已傳幾張、滑幾次或歷史封包。

手機上下左右滑動會在接收頁顯示，3 秒後清除，並回覆手機已收到。**本版不會自動觸發 VSeeFace 服裝／表情快捷鍵。**

## 常見情況

- **頁面沒有開啟**：手動以 Chrome／Edge 開 `http://127.0.0.1:19091/`。若程式有提示埠被占用，使用下面的命令參數改埠。
- **有資料送出但角色不動**：確認 VSeeFace receiver 已開、接收埠一致，以及 Track… 選項沒有覆蓋資料。可先用 3 秒測試，排除手機辨識問題。
- **頭動但眼睛／嘴沒動**：確認 VRM0 模型內有對應 BlendShape。標準閉眼／張嘴使用 `Blink_L`、`Blink_R`、`A`，嘟嘴 `U`，微笑 `Joy`；額外眉毛／眼神依模型映射。此方案不會替模型新增表情。
- **手臂／手指方向錯誤**：本版轉送現有通用 VMC 的旋轉資料，沒有加入 Warudo 插件的每骨骼方向校正 UI；需要依實際 VRM0 再測試調整。
- **不同網路無法配對**：PeerJS Cloud 負責配對，資料走 WebRTC。沿用原方案的 STUN 設定，沒有自行架 TURN；某些網路可能無法直連。
- **停止後角色留在原姿勢**：停止只停止傳輸，不替 VSeeFace 重置角色。

進階命令列（非必要）：

```powershell
.\Anshaer_Web_VMC_Bridge.exe --http-port 19092 --udp-port 39541
```

本版固定本機 UDP 目標，不直接控制其他電腦。外部網站也不能直接使用本機 API；接收網頁要從 EXE 提供的網址開啟。

## 包含檔案

- `Anshaer_Web_VMC_Bridge.exe`：內嵌接收網頁的獨立 Windows x64 橋接器。
- `web/mobile.html`、`web/tablet.html`：可放 HTTPS 網站的手機／平板端。
- `source/`：Go 原始碼、內嵌接收網頁與編譯說明。
- `docs/AI_HANDOFF.md`、`docs/TESTING.md`：後續維護資訊與測試範圍。
- `THIRD_PARTY_NOTICES.txt`：使用的第三方授權。

Windows EXE 已交叉編譯；HTTP→UDP、實際接收頁及兩台平板合併資料均已測試。此環境無法執行 Windows VSeeFace，**尚未在 VSeeFace 實機確認模型動作外觀**。

Copyright © 2026-2030 anshaer.com. All rights reserved.

更新前請先用舊接收頁關閉舊版本橋接程式，再啟動 v0.3；避免本機 HTTP 埠 19091 被舊程式占用。
