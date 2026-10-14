# Warudo 線上接收

預設直接開 GitHub 接收頁，不下載 HTML。僅下載 Warudo 插件，放入 Playground，啟動後填 WebSocket 埠與 Access Key；浏览器詢問本機網路存取時允許本站。

# Anshaer_Web_Vmc v0.2 — 骨骼方向校正

這是 Playground C# 插件原型，不是 .warudo 封裝模組。電腦接收網頁為 v0.2（線上流程）；手機／平板統一使用 transmit/ 共用頁。

## 更新安裝

1. 關閉 Warudo，先移走舊的 `ANSHA_VMC_Bridge.cs`，再放入 `Anshaer_Web_Vmc.cs`。不要同時留下兩份 .cs。
2. 正確位置：Warudo\Warudo_Data\StreamingAssets\Playground\Anshaer_Web_Vmc.cs。
   也可在 Warudo 選單開資料夾，再進 Playground。不是 Warudo.exe 旁邊的 Playground。
3. 開 Warudo，等待編譯；資產 Anshaer_Web_Vmc 的 ID 沿用，原場景通常可保留連接設定。
4. 若未新增資產，Assets → Add Asset → 搜尋 Anshaer_Web_Vmc。
5. 面部模板選 VMC；要身體時動作模板也選 VMC。Warudo 原生 VMC Receiver UDP 接收埠 39539。
6. 在執行 Warudo 的電腦開 https://anshaer.github.io/VMC/warudo/receiver.html，填 19090 與插件 Access Key。
7. 手機端請用 HTTPS 網址，使用 transmit/mobile.html（共用手機頁），掃描接收網頁配對碼，開辨識。

## 新增設定

- Bone correction enabled：方向校正開關，預設關。關閉時原樣轉送。
- Model profile：Normalized／VRM0_Custom／VRM1_Custom。三份設定彼此獨立。
  這些是手動設定檔，不會偵測模型、套用假設的 VRM0/1 旋轉，也不會跟角色自動切換。
- Bone：選要調的骨骼。左右分開。
- Axis map：輸出旋轉軸對應，預設 X,Y,Z。每個軸只能出現一次，可加負號。
- Reverse rotation：旋轉反向。適合彎曲方向整體相反；不是固定轉 180 度。
- X/Y/Z offset：固定角度偏移 -360..360 度。用於姿勢底角、掌心朝向等。
- Saved calibration JSON：全部設定檔的備份文字，隨場景儲存；可複製／貼回。
- Capture T-pose in 3 seconds：3 秒後記錄收到的局部骨骼旋轉為中性基準，不含 Head。
  需要近期收到雙上臂資料。插件不知道人體是否真的站成 T，也不知道相機關節是否被遮擋；
  請自行確認雙肩、手肘、手腕可見，手臂平伸、掌心朝下，腿部也要校正就全身入鏡。
  只校正收到的骨骼，不補猜未收到的骨骼。正常零旋轉骨架可略過這一步。
- Reset selected bone：重設選中骨骼，包含它的中性基準；整條手指選項重設該指三節。
- Reset current profile：只清空目前模型校正檔，不改其他設定檔及連線設定。

## 對照

| 範圍           | Bone 選項                                                         |
| -------------- | ----------------------------------------------------------------- |
| 身體           | Hips、Spine、Chest、UpperChest                                    |
| 脖子／頭       | Neck、Head                                                        |
| 肩／上臂       | Left/RightShoulder、Left/RightUpperArm                            |
| 手肘／前臂     | Left/RightLowerArm                                                |
| 手掌／手腕     | Left/RightHand                                                    |
| 整條手指       | Left/RightThumb、Index、Middle、Ring、Little                      |
| 單獨指節       | 上述手指 + Proximal（根節）、Intermediate（中節）、Distal（末節） |
| 大腿           | Left/RightUpperLeg                                                |
| 膝蓋／小腿     | Left/RightLowerLeg                                                |
| 腳／腳趾       | Left/RightFoot、Left/RightToes                                    |
| 模型眼球／下巴 | LeftEye、RightEye、Jaw（僅有相應 Bone 資料才作用）                |

VMC 的眼睛閉合與嘴型通常是 BlendShape，不是 LeftEye/Jaw 的骨骼旋轉；本次不改面部數值。
只有輸入封包有的骨骼能作用。例如手機目前沒有 UpperChest/Jaw 骨骼資料，調它不會產生動作。
整條手指設定套用三節；若某節有獨立設定，該節獨立設定優先。

## 建議調整順序

1. 手機先關手指，Warudo 選模型對應的 Custom 設定檔，開方向校正。
2. 先測 T 姿勢；模型底角有偏差再記錄 T 姿勢。歸零只能修底角，不能自動修錯軸。
3. 分別修 UpperArm → LowerArm → Hand；腿是 UpperLeg → LowerLeg → Foot。
4. 每次只調一項、只測一邊，另一邊確認方向後再設。不要同時開手機手掌翻轉與亂加插件偏移。
5. 彎曲整體相反，先試該骨骼 Reverse rotation；若只一個旋轉軸相反，再試 Axis map。
   X,-Y,-Z 表示繞 X 方向的座標基底翻轉，影響 Y/Z 旋轉；不是通用的前後反向答案。
   X,Z,Y 交換 Y/Z；-X,Y,Z 是反射軸對應，其四元數依旋轉的軸向量規則轉換。
6. 掌心固定朝向偏 90/180 度，試 Hand 的 X/Y/Z offset，一次 90 度，錯了重設此骨骼。
7. 開手指後先測整條手指，再按單獨指節細調。拇指和四指方向可能不同。
8. 完成後儲存 Warudo 場景並複製 JSON 備份。換模型時手動換 Profile。

固定偏移會影響下游肢體姿態，因為骨架有父子關係。上臂修完再調手肘，不能把所有關節都轉 180 度。
校正處理順序：中性基準移除 → 旋轉軸對應 → 可選反向 → 固定偏移。
無資料／錯誤深度／辨識遮擋／錯誤骨架父子關係不能只靠這些設定修復。

## 驗證與限制

已通過 C# 語法編譯（Warudo API 使用測試替身，沒有真實 Warudo DLL）、
48 種正負軸對應、反向、90 度偏移、T 姿勢校正、獨立模型檔、指節優先與重設測試。
實際本機 WebSocket → C# → UDP 測試確認只改指定肘關節，其他封包內容保持。
原樣轉送、配對、滑動事件、確認、重連、錯誤驗證與資料清理測試通過。
尚未在真實 Warudo 載入新版，也未用實體手機／VRM0／VRM1 模型驗證。

VRM0／VRM1 都可能正常，也都可能因匯出方式或轉接座標異常。
Warudo VMC 要求 T 姿勢骨骼旋轉正規化，官方說明：https://docs.warudo.app/docs/mocap/vmc

Copyright © 2026-2030 anshaer.com. All rights reserved.

## 名稱更新

Warudo 插件與資產顯示名稱統一為 `Anshaer_Web_Vmc`，原始碼為 `Anshaer_Web_Vmc.cs`。Plugin／Asset／Node ID、骨骼校正、WebSocket 埠與通訊協定維持原設定；版本仍為 v0.2。舊場景可沿用原資產連接。

## 整合版入口

接收頁：https://anshaer.github.io/VMC/warudo/receiver.html

共用手機／平板：https://anshaer.github.io/VMC/transmit/

插件下載：https://anshaer.github.io/VMC/downloads/Anshaer_Web_Vmc_Warudo_v0.2.zip
