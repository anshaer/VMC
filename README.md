# Anshaer_Web_VMC

手機動捕、平板多點控制與 WebRTC 傳輸。**傳輸與接收預設都直接在 GitHub 開啟，只有必要的本機插件／元件需要下載。**

預計網站：<https://anshaer.github.io/VMC/>。正式上架檔案包 **v1.1**；尚未代為部署。

Copyright © 2026-2030 anshaer.com. All rights reserved.

## 線上入口

| 區域         | 網頁                                                             | 本機需要                  |
| ------------ | ---------------------------------------------------------------- | ------------------------- |
| 手機傳輸     | [手機鏡頭](https://anshaer.github.io/VMC/transmit/mobile.html)   | 無；允許相機              |
| 平板傳輸     | [多點滑桿](https://anshaer.github.io/VMC/transmit/tablet.html)   | 無；不用鏡頭              |
| Web          | [接收診斷](https://anshaer.github.io/VMC/web/receiver.html)      | 無                        |
| Warudo       | [接收](https://anshaer.github.io/VMC/warudo/receiver.html)       | Warudo插件                |
| Blender      | [接收](https://anshaer.github.io/VMC/blender/receiver.html)      | Blender插件＋本機連接插件 |
| Unity        | [接收](https://anshaer.github.io/VMC/unity/receiver.html)        | Unity元件＋本機連接插件   |
| VSeeFace     | [接收](https://anshaer.github.io/VMC/vseeface/receiver.html)     | 開啟原生VMC＋本機連接插件 |
| VTube Studio | [接收](https://anshaer.github.io/VMC/vtube-studio/receiver.html) | 開啟原生API；不需額外插件 |
| 說明         | [全部說明](https://anshaer.github.io/VMC/docs/)                  | 各版本安裝／操作          |

網頁都開在執行目標軟體的電腦上。手機與平板各共用一份。Web是資料診斷，不含2D製作器或角色渲染；VTS映射Live2D輸入，不直接套用3D骨架。

## 只下載必要元件

| 項目                                  | 安裝檔                                                                                          |
| ------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Warudo插件v0.2                        | [ZIP](https://anshaer.github.io/VMC/downloads/Anshaer_Web_Vmc_Warudo_v0.2.zip)                  |
| Blender插件v0.2                       | [ZIP直接安裝](https://anshaer.github.io/VMC/downloads/Anshaer_Web_Vmc_Blender_v0.2.zip)         |
| Unity元件v0.1                         | [unitypackage](https://anshaer.github.io/VMC/downloads/Anshaer_Web_Vmc_Unity_v0.1.unitypackage) |
| 本機連接插件／橋接v0.3（Windows x64） | [ZIP](https://anshaer.github.io/VMC/downloads/Anshaer_Web_VMC_Bridge_v0.3.zip)                  |

本機連接插件是獨立伴隨程式，將線上網頁收到的VMC資料交給本機UDP，不是安裝在Blender/Unity/VSeeFace裡的外掛。網頁無法直接送UDP，所以這三種接收仍需要它。无需下載HTML、各軟體網頁包或手機／平板包。

## 使用

1. 開啟目標軟體、安裝必要元件或開原生接收／API。
2. 直接開上方GitHub接收頁。Blender/Unity/VSeeFace先執行本機連接插件，從它的授權頁選軟體，會開GitHub並帶入本次授權碼。
3. 按連接軟體／本機插件；瀏覽器提示本機網路存取時允許本站。不要關閉整個瀏覽器安全機制。
4. VTS首次在VTS允許插件，再按「加入標準面部對應」並勾選「啟用參數注入」。
5. 手機／平板直接開GitHub傳輸頁，輸入或掃描接收頁12碼，啟用辨識或操作滑桿。

Warudo預設WS19090、AccessKey與UDP39539；VTS預設WS8001；本機連接插件HTTP19091、目標UDP39540。依軟體實際埠號調整。授權碼與WebRTC12碼不同。

## 瀏覽器與資源

以目前Chrome／Edge的HTTPS頁面使用；需要本機連線的接收頁請在執行軟體的同一台電腦開啟。瀏覽器若已封鎖本站本機網路，從本站權限設定重新允許。不同瀏覽器對本機HTTP／WS的政策可能不同，不能保證所有瀏覽器可用。

共用手機首次下載MediaPipe模型與資源，配對依賴PeerJS Cloud，不是完全離線方案。最多8台分區，同區先連且資料新鮮者優先，3秒逾時釋放；不是多鏡頭三維融合。方向事件：Warudo接藍圖，VTS對應快捷鍵，UDP橋接目前只顯示／確認，不自動換裝。

目前資料與滑動方向不累積歷史張數／次數。平板面部與身體放開歸零獨立，預覽預設收合。原插件算法與版本保留。

## 網頁原始碼

HTML、CSS、JavaScript、JSON與Markdown已整理縮排與完整換行，使用LF、2空格與100字寬目標；保留第三方授權註記。自帶函式庫也有換行，便於後續AI維護。

目錄：web/、warudo/、blender/、unity/、vseeface/、vtube-studio/、transmit/、docs/、shared/、bridge/、downloads/。

## 上傳 GitHub Pages

解壓 GitHub 整合包後，根目錄直接有 `index.html`、`404.html`、`README.md` 與各資料夾，不需要建置前端或執行 npm。

- 使用 `anshaer.github.io` 使用者站點儲存庫：將所有內容放入網站發佈來源下的 **`VMC` 資料夾**。
- 使用獨立 **`VMC` 儲存庫**：將所有內容放入該儲存庫的 Pages 發佈來源根目錄。
- 保持大小寫；所有路徑資料夾使用小寫 ASCII。
- 若未開啟 Pages：Repository **Settings → Pages → Build and deployment → Source → Deploy from a branch**，選包含本包的分支與發佈根目錄，再按 Save。若你已有 GitHub Actions 發佈流程，沿用原設定。
- 在使用者站點的子資料夾部署時，Pages 的發佈來源仍是網站根；不要把本包的 `docs` 誤選成唯一網站發佈來源。
- 本包未提供 `CNAME`，保留預計 `anshaer.github.io/VMC/` 網址。
- 上傳與 Pages 設定需由你操作；本次只是準備檔案，沒有替你推送或部署。

### 404

`404.html` 使用絕對網址返回本專案，不會因為錯誤路徑深度而走錯連結。

GitHub Pages 自動錯誤頁需放在 **網站發佈根目錄**。如果本包放在使用者站點的 `VMC` 子目錄，可將這份 404 複製／整合到網站根；根目錄已有其他專案的 404 時請保留並自行整合，避免直接覆蓋。

官方設定文件：

- [GitHub Pages 發佈來源](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)
- [GitHub Pages 自訂 404](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-custom-404-page-for-your-github-pages-site)

## 測試與版本

GitHub上架包v1.1，共用本機連接插件v0.3；原Warudo/Blender/Unity/VTS元件版本不因網頁重排而一起升版。測試與實機限制見 [TESTING.md](https://anshaer.github.io/VMC/docs/TESTING.md)。

Chrome官方本機網路說明：<https://github.com/GoogleChrome/modern-web-guidance/blob/main/skills/modern-web-guidance/guides/security/local-network-access.md>
