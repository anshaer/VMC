> 本文件保留既有協定／功能細節；整合版入口與下載以根 README.md 及 docs/AI_HANDOFF.md 為準。

# 資料協定與後續整合

## 現有分工

2D：手機 face → WebRTC → 2D 接收診斷端／相容角色頁。
Warudo：手機 face/upper/full → WebRTC → Warudo 接收頁 → WebSocket → 插件 → UDP → 原生 VMC Receiver。

兩端文件分開，連線核心共用。配對是接收端產生 12 碼、傳送端掃描／輸入。使用 PeerJS Cloud 訊號服務。
現有兩種用途共用配對 namespace，不是協定隔離；請勿誤用另一接收頁的連接碼。

## 線路

- WebRTC protocol: `ansha-vmc-osc/2`；Peer ID prefix: `ansha-vmc2-`。
- 動作：`vmc`，帶 `seq` 與原始 OSC binary `packet`。
- 點線診斷：`preview`，帶 `seq` 與精簡關鍵點資料。
- 滑動：`swipe`，direction 為 up/down/left/right；使用 swipeAck 確認。
- 心跳與握手控制：hello、ping、pong。
- 插件 WebSocket：`ws://127.0.0.1:19090/ansha-vmc`，握手 protocol `ansha-warudo-bridge/1` + Access Key。
- 插件 UDP 目標：`127.0.0.1:39539`（可改埠）。

OSC 包含狀態、相對時間、Head、BlendShape、依模式的骨骼與 Root，以及 ANSHA 自訂深度／辨識模式欄位。
滑動與點線預覽是自訂外層訊息，不是標準 VMC 骨骼欄位。Warudo 滑動需自行連接 Anshaer_Web_Vmc On VMC Swipe 藍圖節點。

## 多來源已實作項目

最多 8 個來源，同碼連線；平板握手後送 `scope` 訊息與 region。接收端按來源維護當前資料及 3 秒逾時，先連線且新鮮的來源取得指定區域。
合併成一份 VMC；方向事件全域重編號，回覆時恢復原來源的 seq。
具體區域及限制見 [MULTI_TABLET.md](https://anshaer.github.io/VMC/docs/MULTI_TABLET.md)。

## 後續規則（尚未完成）

1. 每個來源附裝置 ID、用途與可用訊號，接收端回報能力。
2. 面部／頭部／身體／左右手／左右腿分開指定控制權。
3. 手機與平板同時連線時只允許指定來源寫入指定範圍。
4. 按住覆蓋、放開恢復、來源逾時的回退行為須明確且平滑。
5. 多相機融合需關鍵點可信度、拍攝時間、時鐘對齊與相機校正。
6. 最終只輸出一份融合後的骨架／表情資料給角色端。

目前可接多來源並按區分工；不能把不同相機的旋轉平均當成多視角融合。能力協商、跨相機時鐘／座標校正與融合尚未完成。
