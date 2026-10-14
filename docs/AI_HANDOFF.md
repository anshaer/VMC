# Anshaer_Web_VMC 線上上架版 v1.1

正式預計 https://anshaer.github.io/VMC/。傳輸/六種接收頁均預設GitHub HTTPS；不要改回下載HTML。安裝包只列Warudo插件、Blender插件、Unity元件、共用本機連接插件（橋接程式）。VTS與Web不需额外本機插件。

WebRTC protocol ansha-vmc-osc/2、peerprefix ansha-vmc2-（實際無空格），共用 transmit/mobile.html tablet.html；8來源分區，3秒資料過期，原手機/平板算法不變。Web為診斷而非角色製作器。

Warudo直接連本機WS19090與AccessKey；VTS直接連本機WS8001並以原生API授權，標準面部映射後啟用注入；瀏覽器LNA允許本站。使用ws://127.0.0.1的明確loopback地址，不要無故開放LAN或Cloudproxy。VTS set-mode/資料freshness規則見VTS_AI_HANDOFF。

Blender/Unity/VSeeFaceGitHub接收頁使用fetch http://127.0.0.1:19091/api/*，mode cors、targetAddressSpace loopback、X-Bridge-Key，首次必須使用者按連接，不在初載入輪詢本機。Go橋接v0.3只允許Origin https://anshaer.github.io及本機同源，仍以隨機256-bittoken授權，不使用CORS*，不回傳可由任意來源取得的key API。根/是本機授權選擇頁，點GitHubURL#bridgeKey=token&bridgePort=port帶入；線上receiver立即history.replaceState清除fragment並存於記憶體，不記入公開檔案。

保留本機嵌入receiver作備援，線上流程不依賴它。bridge key啟動更换；UI不自动從localStorage重用失效key。一次一個輸出頁。跨來源preflight只允許固定origin API路徑，API本體必須校驗token與Host。使用瀏覽器本機網路權限，不增加已淘汰PNA特殊標頭。

所有HTML/CSS/JS/JSON/Markdown使用Prettier格式，2空格LF、100字寬目標。第三方内嵌庫保留授權，沒有壓縮成單行。此版只重排程式與改線上橋接，不改辨識算法/插件retargeting。

不能宣稱所有實際應用與手機模型都已驗證。GitHub尚未部署，瀏覽器測試採攔截HTTPS站點內容的精確origin模擬與真实loopback服務。保留测试限制。
