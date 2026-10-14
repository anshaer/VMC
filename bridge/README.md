# 本機連接插件（橋接程式）v0.3

適用 Blender、Unity、VSeeFace，Windows x64。它不是裝進軟體的插件，而是網頁到本機 UDP 的伴隨程式。只有本機元件需要下載；傳輸與接收網頁預設都使用 GitHub。

1. 解壓，雙擊 Anshaer_Web_VMC_Bridge.exe。
2. 本機授權選擇頁會開啟；點要用的軟體，進入 https://anshaer.github.io/VMC/ 下的 GitHub 接收頁。
3. 本次 64 位授權碼以網址 fragment 帶入後清除，按線上頁「連接本機插件」並允許瀏覽器本機網路存取。
4. 设定相同 UDP 埠並開始轉送，手機／平板輸入 GitHub 接收頁的 12 碼。

HTTP僅監聽127.0.0.1:19091，UDP預設39540。網頁CORS只允許 https://anshaer.github.io，且每次API仍需有效本次授權碼。未知origin、缺少授權、錯誤主機被拒絕。插件重啟後重新從授權頁進入GitHub。不要把隨機授權碼寫入原始碼或公開README。

一次一個接收頁，共享UDP輸出設定。滑動目前只顯示並確認，不自動換裝。關閉插件用網頁「關閉橋接程式」；關分頁後無心跳約3分鐘退出。

命令列：--http-port 19092 --udp-port 39540 --no-open。其他系統可用Go編譯source，但本包提供Windows amd64執行檔，未做WindowsGUI實機驗證。

保留本機接收HTML作進階備援；它不是預設流程。v0.3新增限定origin CORS及本機授權選擇頁，其餘UDP安全檢查和逾時清理沿用。

更新前請先用舊接收頁關閉舊版本橋接程式，再啟動 v0.3；避免本機 HTTP 埠 19091 被舊程式占用。
