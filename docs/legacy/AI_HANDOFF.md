> 本文件保留既有協定／功能細節；整合版入口與下載以根 README.md 及 docs/AI_HANDOFF.md 為準。

# AI 接手說明

目標：ANSHA 網頁 VMC 方案，2D Web 與 Warudo 分開發佈；使用者希望提供可下載 HTML/ZIP，未授權部署。

## 權威檔案

- transmit/mobile.html：手機 v1.1 的面部限定版。保留共用解算程式，但 UI 停用身體模式／手指。
- web/receiver.html：通用接收診斷頁，不是角色播放器。勿聲稱它能載入角色 ZIP。
- transmit/mobile.html：手機 v1.1，面部／上半身／全身。
- warudo/receiver.html：接收與插件橋接頁 v0.1。
- warudo/plugin/Playground/Anshaer_Web_Vmc.cs：插件 v0.2，保持既有 Plugin/Asset/Node ID。
- transmit/tablet.html、transmit/tablet.html：無相機平板控制臺 v1.1。Pointer Events 自訂滑桿、每控件 pointer capture，多按鈕暫時覆蓋，放開還原底值。Warudo 輸出51骨骼，四槽任選骨骼，指節握拳快捷及 Root 位移。詳見 TABLET_GUIDE.md。
- 本包不含 2D 製作器，等協定確認後再接入。

## 改動範圍

按用途拆分頁面並新增兩種平板合成資料來源，沿用既有 WebRTC 協定／命名空間。
兩套 HTML 的底層協定仍共用；沒有完整能力協商。新版已加入 VmcRegionReceiver，多台平板同碼分區控制，最多8台；每來源一份訊息、3秒逾時、先連線新鮮者優先，按區重建OSC。詳見 MULTI_TABLET.md。
後續若要真正隔離 namespace，手機與對應接收器（包含角色匯出頁）須一起變更，避免失配。

## 已知限制

手機深度為相機模型估計；遮擋關節可能保留舊姿態。VMC 骨骼封包沒有每關節可見度。
VRM0/1 版本不是自動方向判斷依據；插件提供手動每骨骼校正。不要預設全部轉 180 度。
模型表情須對照；標準預設與自訂名稱可同時存在。
Warudo 插件目前只是一份 Playground .cs，不是假裝已製成 .warudo Mod。
C# API 測試用替身，不能宣稱在實際 Warudo 中已完成全模型相容測試。

## 後續順序

先完成兩種接收端與平板控制臺的實體設備測試。
已支援區域來源與控制權，下一步是實體設備測試及能力協商，最後才做多鏡頭融合。
細節見 PROTOCOL.md 與 WARUDO_AI_HANDOFF.md。

Copyright © 2026-2030 anshaer.com. All rights reserved.

## GitHub Pages 發佈資訊

預設網站固定為 https://anshaer.github.io/VMC/ 。
首頁是 index.html；404.html 使用絕對網址，錯誤路徑不影響返回與下載。
README 的頁面與下載路徑使用此基底。Warudo 接收端提供下載提示，主要在本機開啟。
本包是靜態檔案，已整理換行；改動時保留嵌入第三方程式庫與授權。
若放在使用者網站子目錄，GitHub 自動404需要發佈根目錄的一份404；不得擅自覆蓋既有網站的404。
發佈包v1.3沒有變更骨架算法、分區接收與平板版本。
