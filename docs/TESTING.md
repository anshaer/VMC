# 線上上架版 v1.1 驗證

## 本次通過

- 實際三種 GitHub 接收頁控制器：沒有按連接前不請求本機網路，fragment授權碼帶入後立即移除、有效埠和key驗證、CORS/loopback fetch、VMC篩選、設定套用、pagehide清理。
- 實際 Go 本機連接插件：固定GitHub origin的CORS/preflight、仍需有效每次啟動token、拒絕未知origin/錯誤key/錯誤host、OSC格式與容量限制、三個備援接收路由與授權啟動頁。
- HTTP真正跨來源請求：Origin=https://anshaer.github.io → 本機Go插件 → 實際UDP，封包內容一致。
- 真實Chromium153（Linux）：以本機攔截內容模擬精確的 https://anshaer.github.io/VMC/ 頁面origin，授予瀏覽器本機網路權限後，實際GitHub頁JavaScript → 本機CORS HTTP → UDP成功。按連接前沒有本機請求、URL片段已清除。未關閉web-security/CORS檢查。
- 真實Chromium153：GitHub HTTPS origin → 本機WebSocket；Warudo握手和二進位資料成功，VTS原生API授權、參數查詢、參數注入和閉眼到EyeOpen反向成功。兩個服務端是協定模擬，不是真實應用。
- Web/Warudo實際接收腳本＋三台實際共用平板腳本：面部/身體分工、手臂優先、3秒逾時備援、區域切換、独立放開歸零與清理。
- VTS實際接收腳本＋兩台平板：授權重用/拒絕、head/eye/mouth/body mapping、來源逾時不補假零、在途注入限制、滑動路由與重連。
- 所有網頁/樣式/腳本/JSON/Markdown由Prettier整理換行與縮排；Node檢查所有嵌入JS語法，檢查站內網址/插件下載/ZIP完整性。
- 新Windows x64橋接成功交叉編譯；插件安裝包的Warudo/Blender/Unity程式未改算法。

## 限制

GitHub沒有代為部署，因此HTTPS瀏覽器測試是精確origin＋本機內容攔截，不是公開站點已上線驗收。Chrome本機網路權限以自動化授予；尚未在使用者Windows手動驗證權限提示及本機EXE GUI。

Warudo/VTS服務端使用模擬API，不代表真正Warudo/VTS與模型全部實機測試通過。手機鏡頭、實際PeerJS Cloud配對與NAT穿透、長時間效能未重新測試。Blender/Unity插件原有驗證與限制見各README。

不同瀏覽器的HTTPS到本機HTTP/WS政策可能不同。此上架版使用Chrome/Edge本機網路權限與固定origin CORS，不要求使用者停用瀏覽器安全機制。
