// Copyright © 2026-2030 anshaer.com. All rights reserved.
// Playground prototype: uses WebSocketSharp and Newtonsoft.Json already shipped with Warudo.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using Warudo.Core;
using Warudo.Core.Scenes;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Plugins;
using Warudo.Plugins.Core;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Anshaer.UniversalVMC {
    [PluginType(Id = "com.anshaer.universalvmc", Name = "Anshaer_Web_Vmc",
        Description = "Mobile WebRTC to local Warudo VMC, with swipe blueprint events.",
        Author = "anshaer.com", Version = "0.2.0",
        AssetTypes = new[] { typeof(VMCBridgeAsset) }, NodeTypes = new[] { typeof(OnVMCSwipeNode) })]
    public class UniversalVMCPlugin : Plugin { }

    [AssetType(Id = "a48d67d6-cd5d-442a-bc1c-9657bb5a4891", Title = "Anshaer_Web_Vmc",
        Category = "CATEGORY_MOTION_CAPTURE", Singleton = true)]
    public class VMCBridgeAsset : Asset {
        [DataInput] public bool Enabled = true;
        [DataInput] [Label("Local WebSocket Port")] public int WebSocketPort = 19090;
        [DataInput] [Label("Warudo VMC UDP Port")] public int VMCPort = 39539;
        [DataInput] [Label("Access Key (copy to receiver page)")] public string AccessKey = "";
        [Markdown] public string Status = "Not started.";
        [DataInput] [Label("Bone correction enabled / 骨骼方向校正")] public bool CorrectionEnabled = false;
        [DataInput] [Label("Model profile / 模型校正檔（不會自動推測 VRM）")] public ModelProfile Profile = ModelProfile.Normalized;
        [DataInput] [Label("Bone / 骨骼（LowerArm＝手肘；LowerLeg＝膝蓋）")] public BoneTarget SelectedBone = BoneTarget.LeftUpperArm;
        [DataInput] [Label("Axis map / 軸對應，例 X,Y,Z 或 X,-Y,-Z")] public string AxisMap = "X,Y,Z";
        [DataInput] [Label("Reverse rotation / 反向旋轉")] public bool ReverseRotation = false;
        [DataInput] [Label("X offset / X 固定偏移（度）")] public float OffsetX = 0;
        [DataInput] [Label("Y offset / Y 固定偏移（度）")] public float OffsetY = 0;
        [DataInput] [Label("Z offset / Z 固定偏移（度）")] public float OffsetZ = 0;
        [DataInput] [Label("Saved calibration JSON / 校正存檔（可複製備份）")] public string CalibrationJson = "{}";
        [Markdown] public string CalibrationStatus = "Default is unchanged forwarding. Enable correction after configuring. / 預設原樣轉送。";
        private JObject calibration = new JObject();
        private bool editingCalibration;
        private string calibrationError;
        private double captureAt = -1, rawAt;
        private string captureProfile;
        private readonly Dictionary<string,double[]> rawBones = new Dictionary<string,double[]>();
        private readonly Dictionary<string,BoneCorrection> corrections = new Dictionary<string,BoneCorrection>();
        private readonly Dictionary<string,double[]> neutrals = new Dictionary<string,double[]>();
        private void SetCalibrationStatus(string s){CalibrationStatus=s;BroadcastDataInput(nameof(CalibrationStatus));}
        private void InitCalibration(){
            WatchAll(new[]{nameof(Profile),nameof(SelectedBone)},()=>{if(!editingCalibration){captureAt=-1;LoadCalibration();}});
            WatchAll(new[]{nameof(AxisMap),nameof(ReverseRotation),nameof(OffsetX),nameof(OffsetY),nameof(OffsetZ)},SaveSelection);
            WatchAll(new[]{nameof(CalibrationJson)},()=>{if(!editingCalibration)LoadCalibration();});
            LoadCalibration();
        }
        private JObject ProfileData(){var p=calibration[Profile.ToString()] as JObject;if(p==null){p=new JObject();calibration[Profile.ToString()]=p;}return p;}
        private void LoadCalibration(){
            try{
                var candidate=JObject.Parse(CalibrationJson??"{}");if((CalibrationJson??"").Length>65536)throw new Exception("Calibration JSON exceeds 64 KB");
                var p=candidate[Profile.ToString()] as JObject;var newCorrections=new Dictionary<string,BoneCorrection>();var newNeutrals=new Dictionary<string,double[]>();
                var entries=p?["bones"] as JObject;if(entries!=null)foreach(var entry in entries.Properties()){
                    var c=entry.Value.ToObject<BoneCorrection>();if(c==null)throw new Exception("Invalid bone entry");BoneMath.ParseMap(c.Map);CheckAngle(c.X);CheckAngle(c.Y);CheckAngle(c.Z);newCorrections[entry.Name]=c;
                }
                var n=p?["neutral"] as JObject;if(n!=null)foreach(var entry in n.Properties()){var q=entry.Value.ToObject<double[]>();if(q==null||q.Length!=4)throw new Exception("Invalid neutral quaternion");newNeutrals[entry.Name]=BoneMath.Unit(q);}
                calibration=candidate;corrections.Clear();foreach(var x in newCorrections)corrections[x.Key]=x.Value;neutrals.Clear();foreach(var x in newNeutrals)neutrals[x.Key]=x.Value;
                BoneCorrection selected; if(!corrections.TryGetValue(SelectedBone.ToString(),out selected))selected=new BoneCorrection();
                editingCalibration=true;AxisMap=selected.Map;ReverseRotation=selected.Reverse;OffsetX=(float)selected.X;OffsetY=(float)selected.Y;OffsetZ=(float)selected.Z;
                foreach(string key in new[]{nameof(AxisMap),nameof(ReverseRotation),nameof(OffsetX),nameof(OffsetY),nameof(OffsetZ)})BroadcastDataInput(key);
                calibrationError=null;SetCalibrationStatus("Loaded "+Profile+" / "+SelectedBone+". Changes apply to new frames. / 調整套用於新影格。");
            }catch(Exception e){calibrationError=e.Message;SetCalibrationStatus("Calibration error: "+e.Message+". Correction forwarding paused; fix JSON or reset.");}finally{editingCalibration=false;}
        }
        private static void CheckAngle(double v){if(double.IsNaN(v)||double.IsInfinity(v)||Math.Abs(v)>360)throw new Exception("Offset must be within -360..360 degrees");}
        private void SaveSelection(){if(editingCalibration)return;try{
            BoneMath.ParseMap(AxisMap);CheckAngle(OffsetX);CheckAngle(OffsetY);CheckAngle(OffsetZ);
            // Preserve other profiles and previously saved bone entries.
            var p=ProfileData();var entries=p["bones"] as JObject;if(entries==null){entries=new JObject();p["bones"]=entries;}
            var c=new BoneCorrection{Map=AxisMap.Replace(" ","").ToUpperInvariant(),Reverse=ReverseRotation,X=OffsetX,Y=OffsetY,Z=OffsetZ};entries[SelectedBone.ToString()]=JObject.FromObject(c);PersistCalibration();LoadCalibration();
        }catch(Exception e){calibrationError=e.Message;SetCalibrationStatus("Calibration error: "+e.Message);}}
        private void PersistCalibration(){editingCalibration=true;try{CalibrationJson=calibration.ToString(Newtonsoft.Json.Formatting.None);BroadcastDataInput(nameof(CalibrationJson));}finally{editingCalibration=false;}}
        private double[] CorrectBone(string name,double[] q){
            if(!CorrectionEnabled)return q;BoneCorrection c=null;
            if(!corrections.TryGetValue(name,out c))foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})foreach(string side in new[]{"Left","Right"})if(name.StartsWith(side+finger,StringComparison.Ordinal))corrections.TryGetValue(side+finger,out c);
            double[] n;neutrals.TryGetValue(name,out n);if((c==null||c.Identity)&&n==null)return q;return BoneMath.Apply(q,c,n);
        }
        [Trigger] [Label("Capture T-pose in 3 seconds / 3 秒後記錄 T 姿勢")] public void CaptureTPose(){captureAt=BridgeInbox.Now+3;captureProfile=Profile.ToString();SetCalibrationStatus("Hold a full visible T-pose. Capturing in 3 seconds. / 保持 T 姿勢、關節入鏡。");}
        [Trigger] [Label("Reset selected bone / 重設此骨骼")] public void ResetSelectedBone(){
            var p=ProfileData();var bones=p["bones"] as JObject;bones?.Remove(SelectedBone.ToString());var n=p["neutral"] as JObject;
            if(n!=null){var keys=new List<string>();foreach(var e in n.Properties())if(e.Name==SelectedBone.ToString()||e.Name.StartsWith(SelectedBone.ToString(),StringComparison.Ordinal))keys.Add(e.Name);foreach(var k in keys)n.Remove(k);}
            PersistCalibration();LoadCalibration();
        }
        [Trigger] [Label("Reset current profile / 重設目前模型校正檔")] public void ResetProfile(){captureAt=-1;calibration.Remove(Profile.ToString());PersistCalibration();LoadCalibration();}
        private void UpdateCalibration(){
            if(rawAt>0&&BridgeInbox.Now-rawAt>3){rawBones.Clear();rawAt=0;}
            if(captureAt<0||BridgeInbox.Now<captureAt)return;captureAt=-1;
            if(captureProfile!=Profile.ToString()||rawAt==0||BridgeInbox.Now-rawAt>.5||!rawBones.ContainsKey("LeftUpperArm")||!rawBones.ContainsKey("RightUpperArm")){SetCalibrationStatus("Capture failed: fresh visible arm data required. / 沒有新鮮的雙臂資料，未覆蓋校正。");return;}
            var n=new JObject();foreach(var entry in rawBones){if(entry.Key=="Head")continue;n[entry.Key]=JArray.FromObject(entry.Value);}ProfileData()["neutral"]=n;PersistCalibration();LoadCalibration();SetCalibrationStatus("T-pose recorded for received bones except Head. Enable correction. / 已記錄收到的骨骼（不含頭部），請開校正。");
        }

        public event Action<string> Swipe;
        private WebSocketServer server;
        private UdpClient udp;
        private IPEndPoint target;
        private readonly BridgeInbox inbox = new BridgeInbox();
        private string runningKey;
        private int generation;
        private bool destroyed;
        private double lastStatus;
        private string socketError;

        protected override void OnCreate() {
            base.OnCreate();
            if (string.IsNullOrWhiteSpace(AccessKey)) { AccessKey = Guid.NewGuid().ToString("N"); BroadcastDataInput(nameof(AccessKey)); }
            WatchAll(new[] { nameof(Enabled), nameof(WebSocketPort), nameof(VMCPort), nameof(AccessKey) }, ResetBridge);
            InitCalibration();
            ResetBridge();
        }
        [Trigger] [Label("Restart Bridge")] public void RestartBridge() { ResetBridge(); }
        [Trigger] [Label("Generate New Access Key")] public void NewAccessKey() {
            AccessKey = Guid.NewGuid().ToString("N"); BroadcastDataInput(nameof(AccessKey)); ResetBridge();
        }
        private async void ResetBridge() {
            int ticket = ++generation;
            StopBridge();
            if (!Enabled || destroyed) { SetStatus("Bridge disabled."); return; }
            if (WebSocketPort < 1024 || WebSocketPort > 65535 || VMCPort < 1 || VMCPort > 65535) {
                SetStatus("Invalid port. WebSocket: 1024-65535; VMC UDP: 1-65535."); return;
            }
            if (string.IsNullOrWhiteSpace(AccessKey) || AccessKey.Length < 16 || AccessKey.Length > 128) {
                SetStatus("Access key must contain 16-128 characters."); return;
            }
            try {
                var core = Context.PluginManager.GetPlugin<CorePlugin>();
                await core.BeforeListenToPort();
                if (ticket != generation || destroyed || !Enabled) return;
                runningKey = AccessKey;
                target = new IPEndPoint(IPAddress.Loopback, VMCPort);
                udp = new UdpClient(AddressFamily.InterNetwork);
                server = new WebSocketServer(IPAddress.Loopback, WebSocketPort);
                server.AddWebSocketService<VMCBridgeSession>("/ansha-vmc", session => { session.IgnoreExtensions = true; session.Owner = this; });
                server.Start(); core.AfterListenToPort();
                socketError = null; SetActive(true);
                SetStatus("Listening on ws://127.0.0.1:" + WebSocketPort + "/ansha-vmc → UDP 127.0.0.1:" + VMCPort + ". Waiting for receiver page.");
            } catch (Exception e) { StopBridge(); SetStatus("Bridge failed: " + e.Message + ". Check whether the port is already in use."); }
        }
        private void StopBridge() {
            inbox.Reset(); rawBones.Clear(); rawAt = 0; captureAt = -1;
            var previous = server; server = null;
            if (previous != null) { try { previous.Stop(); } catch { } }
            if (udp != null) { try { udp.Close(); } catch { } udp = null; }
            runningKey = null; target = null; SetActive(false);
        }
        protected override void OnDestroy() { destroyed = true; ++generation; StopBridge(); base.OnDestroy(); }
        internal bool Authenticate(VMCBridgeSession session, string key) {
            return runningKey != null && key == runningKey && inbox.Claim(session);
        }
        internal bool ReceivePacket(VMCBridgeSession session, byte[] bytes) { return inbox.PutPacket(session, bytes); }
        internal bool ReceiveSwipe(VMCBridgeSession session, string direction, long seq) { return inbox.PutSwipe(session, direction, seq); }
        internal void Release(VMCBridgeSession session) { inbox.Release(session); }
        internal void Touch(VMCBridgeSession session) { inbox.Touch(session); }
        internal void ClearMotion(VMCBridgeSession session) { inbox.ClearMotion(session); }
        internal void ReportError(string message) { socketError = message; }
        public override void OnUpdate() {
            base.OnUpdate();
            UpdateCalibration();
            if (server == null || udp == null) return;
            VMCBridgeSession idle = inbox.RemoveTimedOutClient(15);
            if (idle != null) idle.Disconnect("Heartbeat timed out");
            byte[] packet = inbox.TakePacket(3);
            if (packet != null) {
                try {
                    if (CorrectionEnabled && calibrationError != null) throw new Exception("Fix bone calibration: " + calibrationError);
                    rawBones.Clear();
                    byte[] corrected = BonePacket.Transform(packet, CorrectBone, rawBones); rawAt = BridgeInbox.Now;
                    udp.Send(corrected, corrected.Length, target);
                    socketError = null;
                }
                catch (Exception e) { socketError = "UDP send failed: " + e.Message; }
            }
            BridgeSwipe command;
            int remaining = 16;
            while (remaining-- > 0 && inbox.TakeSwipe(out command)) {
                try { Swipe?.Invoke(command.Direction); command.Session.SwipeAcknowledged(command.Direction, command.Sequence); }
                catch (Exception e) { socketError = "Swipe handler failed: " + e.Message; }
            }
            double now = BridgeInbox.Now;
            if (now - lastStatus >= .5) {
                lastStatus = now;
                string state = socketError != null ? socketError : inbox.HasClient
                    ? (inbox.MotionFresh(3) ? "Receiving VMC" : "Browser connected; waiting for mobile tracking")
                    : "Waiting for receiver page";
                SetStatus(state + " · UDP 127.0.0.1:" + VMCPort + " · WebSocket " + WebSocketPort);
            }
        }
        private void SetStatus(string value) { if (Status == value) return; Status = value; BroadcastDataInput(nameof(Status)); }
    }

    public enum ModelProfile { Normalized, VRM0_Custom, VRM1_Custom }
    public enum BoneTarget { Hips, Spine, Chest, Neck, Head, LeftShoulder, RightShoulder, LeftUpperArm, RightUpperArm, LeftLowerArm, RightLowerArm, LeftHand, RightHand, LeftThumb, RightThumb, LeftIndex, RightIndex, LeftMiddle, RightMiddle, LeftRing, RightRing, LeftLittle, RightLittle, LeftUpperLeg, RightUpperLeg, LeftLowerLeg, RightLowerLeg, LeftFoot, RightFoot, LeftToes, RightToes, UpperChest, LeftEye, RightEye, Jaw, LeftThumbProximal, LeftThumbIntermediate, LeftThumbDistal, LeftIndexProximal, LeftIndexIntermediate, LeftIndexDistal, LeftMiddleProximal, LeftMiddleIntermediate, LeftMiddleDistal, LeftRingProximal, LeftRingIntermediate, LeftRingDistal, LeftLittleProximal, LeftLittleIntermediate, LeftLittleDistal, RightThumbProximal, RightThumbIntermediate, RightThumbDistal, RightIndexProximal, RightIndexIntermediate, RightIndexDistal, RightMiddleProximal, RightMiddleIntermediate, RightMiddleDistal, RightRingProximal, RightRingIntermediate, RightRingDistal, RightLittleProximal, RightLittleIntermediate, RightLittleDistal }
    public sealed class BoneCorrection {
        public string Map = "X,Y,Z";
        public bool Reverse;
        public double X, Y, Z;
        public bool Identity { get { return Map == "X,Y,Z" && !Reverse && X == 0 && Y == 0 && Z == 0; } }
    }
    // Local quaternion correction, deliberately independent of Unity/VRM import internals.
    public static class BoneMath {
        public static double[] Unit(double[] q) { double n = Math.Sqrt(q[0]*q[0]+q[1]*q[1]+q[2]*q[2]+q[3]*q[3]); if(n < 1e-9 || double.IsNaN(n) || double.IsInfinity(n))throw new Exception("Invalid bone quaternion"); return new[]{q[0]/n,q[1]/n,q[2]/n,q[3]/n}; }
        public static double[] Inverse(double[] q) { return new[]{-q[0],-q[1],-q[2],q[3]}; }
        public static double[] Multiply(double[] a,double[] b) { return Unit(new[]{a[3]*b[0]+a[0]*b[3]+a[1]*b[2]-a[2]*b[1],a[3]*b[1]-a[0]*b[2]+a[1]*b[3]+a[2]*b[0],a[3]*b[2]+a[0]*b[1]-a[1]*b[0]+a[2]*b[3],a[3]*b[3]-a[0]*b[0]-a[1]*b[1]-a[2]*b[2]}); }
        public static int[] ParseMap(string map) {
            var words=(map??"").ToUpperInvariant().Replace(" ","").Split(',');if(words.Length!=3)throw new Exception("Axis map needs three axes, e.g. X,Y,Z or X,-Y,-Z");
            var result=new int[3];int used=0;
            for(int i=0;i<3;i++){string s=words[i];int sign=1;if(s.StartsWith("-")){sign=-1;s=s.Substring(1);}else if(s.StartsWith("+"))s=s.Substring(1);int axis=s=="X"?1:s=="Y"?2:s=="Z"?3:0;if(axis==0||(used&(1<<axis))!=0)throw new Exception("Use X, Y and Z exactly once");used|=1<<axis;result[i]=axis*sign;}return result;
        }
        public static double[] Apply(double[] q, BoneCorrection c, double[] neutral) {
            if(c==null && neutral==null)return q;
            q=Unit(q);if(neutral!=null)q=Multiply(Inverse(neutral),q);
            if(c==null)return q;
            int[] m=ParseMap(c.Map);int a=Math.Abs(m[0])-1,b=Math.Abs(m[1])-1,d=Math.Abs(m[2])-1;
            int determinant=((b-a)*(d-a)*(d-b)/2)*Math.Sign(m[0])*Math.Sign(m[1])*Math.Sign(m[2]);
            // Quaternion vector is an axial vector: det(M)*M, including reflected axis maps.
            q=new[]{q[a]*Math.Sign(m[0])*determinant,q[b]*Math.Sign(m[1])*determinant,q[d]*Math.Sign(m[2])*determinant,q[3]};
            if(c.Reverse)q=Inverse(q);
            double k=Math.PI/360,x=c.X*k,y=c.Y*k,z=c.Z*k;
            double[] offset=Multiply(Multiply(new[]{0d,0d,Math.Sin(z),Math.Cos(z)},new[]{0d,Math.Sin(y),0d,Math.Cos(y)}),new[]{Math.Sin(x),0d,0d,Math.Cos(x)});
            return Multiply(offset,q);
        }
    }
    public static class BonePacket {
        private static int Int(byte[] b,int p){return (b[p]<<24)|(b[p+1]<<16)|(b[p+2]<<8)|b[p+3];}
        private static string Text(byte[] b,ref int p,int start){int a=p;while(b[p]!=0)p++;string s=Encoding.UTF8.GetString(b,a,p-a);p=start+((p-start+4)&~3);return s;}
        private static float Read(byte[] b,int p){var a=new byte[4];Array.Copy(b,p,a,0,4);if(BitConverter.IsLittleEndian)Array.Reverse(a);return BitConverter.ToSingle(a,0);}
        private static void Write(byte[] b,int p,double value){var a=BitConverter.GetBytes((float)value);if(BitConverter.IsLittleEndian)Array.Reverse(a);Array.Copy(a,0,b,p,4);}
        public static byte[] Transform(byte[] packet, Func<string,double[],double[]> correction, Dictionary<string,double[]> raw) {
            if(!VMCWireValidator.Valid(packet))throw new Exception("Invalid OSC packet");
            var output=(byte[])packet.Clone();Walk(output,0,output.Length,correction,raw);return output;
        }
        private static void Walk(byte[] b,int start,int end,Func<string,double[],double[]> correction,Dictionary<string,double[]> raw){
            int p=start;string address=Text(b,ref p,start);
            if(address=="#bundle"){p=start+16;while(p<end){int n=Int(b,p);p+=4;Walk(b,p,p+n,correction,raw);p+=n;}return;}
            string tags=Text(b,ref p,start);if(address!="/VMC/Ext/Bone/Pos"||tags!=",sfffffff")return;
            string name=Text(b,ref p,start);int qAt=p+12;var q=new double[]{Read(b,qAt),Read(b,qAt+4),Read(b,qAt+8),Read(b,qAt+12)};
            // Do not rewrite untouched rotations, preserving byte-identical forwarding.
            if(raw!=null)raw[name]=BoneMath.Unit(q);var result=correction(name,q);if(object.ReferenceEquals(result,q))return;
            for(int i=0;i<4;i++)Write(b,qAt+4*i,result[i]);
        }
    }

    // Thread-safe, bounded state: only one current frame and at most 16 control events.
    public sealed class BridgeInbox {
        private readonly object gate = new object();
        private VMCBridgeSession client;
        private byte[] pending;
        private double packetAt, activityAt;
        private long lastSwipe = -1;
        private readonly Queue<BridgeSwipe> swipes = new Queue<BridgeSwipe>();
        public static double Now { get { return (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency; } }
        public bool HasClient { get { lock (gate) return client != null; } }
        public bool Claim(VMCBridgeSession session) {
            lock (gate) { if (client != null && client != session) return false; client = session; pending = null; swipes.Clear(); lastSwipe = -1; packetAt = 0; activityAt = Now; return true; }
        }
        public void Touch(VMCBridgeSession session) { lock (gate) { if (client == session) activityAt = Now; } }
        public bool PutPacket(VMCBridgeSession session, byte[] bytes) {
            if (!VMCWireValidator.Valid(bytes)) return false;
            lock (gate) { if (client != session) return false; pending = bytes; packetAt = activityAt = Now; return true; }
        }
        public bool PutSwipe(VMCBridgeSession session, string direction, long seq) {
            if (seq < 0 || seq > 9007199254740991L || (direction != "up" && direction != "down" && direction != "left" && direction != "right")) return false;
            lock (gate) { if (client != session || seq <= lastSwipe || swipes.Count >= 16) return false; lastSwipe = seq; activityAt = Now; swipes.Enqueue(new BridgeSwipe { Session = session, Direction = direction, Sequence = seq }); return true; }
        }
        public byte[] TakePacket(double maxAge) { lock (gate) { byte[] p = Now - packetAt <= maxAge ? pending : null; pending = null; return p; } }
        public bool TakeSwipe(out BridgeSwipe command) { lock (gate) { if (swipes.Count == 0) { command = null; return false; } command = swipes.Dequeue(); return true; } }
        public bool MotionFresh(double maxAge) { lock (gate) return packetAt > 0 && Now - packetAt <= maxAge; }
        public void ClearMotion(VMCBridgeSession session) { lock (gate) { if (client != session) return; pending = null; packetAt = 0; swipes.Clear(); lastSwipe = -1; } }
        public void Release(VMCBridgeSession session) { lock (gate) { if (client != session) return; client = null; pending = null; swipes.Clear(); lastSwipe = -1; packetAt = activityAt = 0; } }
        public VMCBridgeSession RemoveTimedOutClient(double seconds) { lock (gate) { if (client == null || Now - activityAt <= seconds) return null; var old = client; client = null; pending = null; swipes.Clear(); lastSwipe = -1; packetAt = activityAt = 0; return old; } }
        public void Reset() { lock (gate) { client = null; pending = null; swipes.Clear(); lastSwipe = -1; packetAt = activityAt = 0; } }
    }
    public sealed class BridgeSwipe { public VMCBridgeSession Session; public string Direction; public long Sequence; }

    public class VMCBridgeSession : WebSocketBehavior {
        public VMCBridgeAsset Owner { get; set; }
        private bool authenticated;
        protected override void OnMessage(MessageEventArgs e) {
            try {
                if (e.IsBinary) {
                    if (!authenticated) { Disconnect("Authenticate first"); return; }
                    if (!Owner.ReceivePacket(this, e.RawData)) Send("{\"kind\":\"error\",\"message\":\"Rejected VMC packet\"}");
                    return;
                }
                if (!e.IsText || e.Data == null || e.Data.Length > 4096) { Disconnect("Invalid control message"); return; }
                JObject data = JObject.Parse(e.Data);
                string kind = data.Value<string>("kind");
                if (kind == "hello") {
                    if (authenticated) return;
                    if (data.Value<string>("protocol") != "ansha-warudo-bridge/1" || !Owner.Authenticate(this, data.Value<string>("key"))) {
                        Send("{\"kind\":\"error\",\"message\":\"Invalid access key or another browser is connected\"}"); Disconnect("Authentication failed"); return;
                    }
                    authenticated = true;
                    Send("{\"kind\":\"ready\",\"protocol\":\"ansha-warudo-bridge/1\"}"); return;
                }
                if (!authenticated) { Disconnect("Authenticate first"); return; }
                Owner.Touch(this);
                if (kind == "ping") { Send("{\"kind\":\"pong\"}"); return; }
                if (kind == "clear") { Owner.ClearMotion(this); return; }
                if (kind == "bye") { Owner.Release(this); authenticated = false; Disconnect("Browser disconnected"); return; }
                if (kind == "swipe") {
                    var seq = data["seq"];
                    if (seq == null || seq.Type != JTokenType.Integer || !Owner.ReceiveSwipe(this, data.Value<string>("direction"), seq.Value<long>()))
                        Send("{\"kind\":\"error\",\"message\":\"Rejected swipe message\"}");
                }
            } catch { Send("{\"kind\":\"error\",\"message\":\"Invalid bridge message\"}"); }
        }
        protected override void OnClose(CloseEventArgs e) { if (Owner != null) Owner.Release(this); authenticated = false; }
        protected override void OnError(ErrorEventArgs e) { if (Owner != null) Owner.ReportError("WebSocket error: " + e.Message); }
        internal void SwipeAcknowledged(string direction, long sequence) {
            if (!authenticated) return;
            try { Send("{\"kind\":\"swipeAck\",\"direction\":\"" + direction + "\",\"seq\":" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}"); } catch { }
        }
        internal void Disconnect(string reason) { try { Sessions.CloseSession(ID, CloseStatusCode.Normal, reason); } catch { } }
    }

    // Validates the existing mobile OSC bundle without changing or re-encoding it.
    public static class VMCWireValidator {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static bool Valid(byte[] b) {
            if (b == null || b.Length < 16 || b.Length > 32768 || b.Length % 4 != 0) return false;
            try { int count = 0; bool head = false, apply = false; Parse(b, 0, b.Length, 0, ref count, ref head, ref apply); return head && apply; } catch { return false; }
        }
        private static uint UInt(byte[] b, int at) { return ((uint)b[at] << 24) | ((uint)b[at + 1] << 16) | ((uint)b[at + 2] << 8) | b[at + 3]; }
        private static string String(byte[] b, ref int at, int start, int end) {
            int begin = at; while (at < end && b[at] != 0) at++;
            if (at == end || at - begin > 1024) throw new Exception();
            string text = Utf8.GetString(b, begin, at - begin); int next = start + ((at - start + 4) & ~3);
            if (next > end) throw new Exception(); while (at < next) if (b[at++] != 0) throw new Exception(); return text;
        }
        private static float Float(byte[] b, int at) { byte[] temp = new byte[4]; Array.Copy(b, at, temp, 0, 4); if (BitConverter.IsLittleEndian) Array.Reverse(temp); float f = BitConverter.ToSingle(temp, 0); if (float.IsNaN(f) || float.IsInfinity(f)) throw new Exception(); return f; }
        private static void Parse(byte[] b, int start, int end, int depth, ref int count, ref bool head, ref bool apply) {
            if (depth > 3 || start < 0 || end > b.Length || end - start < 4) throw new Exception();
            int at = start; string address = String(b, ref at, start, end);
            if (address == "#bundle") {
                if (end - start < 16) throw new Exception(); at = start + 16;
                while (at < end) { if (at + 4 > end) throw new Exception(); uint n = UInt(b, at); at += 4; if (n == 0 || n > 32768 || n % 4 != 0 || at + n > end) throw new Exception(); Parse(b, at, at + (int)n, depth + 1, ref count, ref head, ref apply); at += (int)n; }
                return;
            }
            if (++count > 128 || (!address.StartsWith("/VMC/Ext/", StringComparison.Ordinal) && !address.StartsWith("/ANSHA/", StringComparison.Ordinal))) throw new Exception();
            string tags = String(b, ref at, start, end); if (tags.Length < 1 || tags[0] != ',') throw new Exception();
            string first = null; float lastFloat = 0;
            for (int i = 1; i < tags.Length; i++) {
                if (tags[i] == 's') { string text = String(b, ref at, start, end); if (i == 1) first = text; }
                else if (tags[i] == 'i' || tags[i] == 'f') { if (at + 4 > end) throw new Exception(); if (tags[i] == 'f') lastFloat = Float(b, at); at += 4; }
                else throw new Exception();
            }
            if (at != end) throw new Exception();
            if (address == "/VMC/Ext/Bone/Pos" && tags == ",sfffffff" && first == "Head") head = true;
            if (address == "/VMC/Ext/Blend/Apply" && tags == ",") apply = true;
            if (address == "/VMC/Ext/Blend/Val" && (tags != ",sf" || lastFloat < 0 || lastFloat > 1)) throw new Exception();
        }
    }

    [NodeType(Id = "2da4cb2a-d735-4d8a-8e68-a3428d408d46", Title = "Anshaer_Web_Vmc On VMC Swipe", Category = "CATEGORY_EXTERNAL_INTEGRATION")]
    public class OnVMCSwipeNode : Node {
        [DataInput] [Label("VMC Bridge")] public VMCBridgeAsset Receiver;
        [FlowOutput] public Continuation Any;
        [FlowOutput] public Continuation Up;
        [FlowOutput] public Continuation Down;
        [FlowOutput] public Continuation Left;
        [FlowOutput] public Continuation Right;
        private VMCBridgeAsset subscribed;
        private string direction = "";
        private double directionAt;
        [DataOutput] public string Direction() { return direction; }
        protected override void OnCreate() { base.OnCreate(); Watch(nameof(Receiver), Subscribe); Subscribe(); }
        private void Subscribe() { if (subscribed != null) subscribed.Swipe -= OnSwipe; subscribed = Receiver; if (subscribed != null) subscribed.Swipe += OnSwipe; }
        protected override void OnDestroy() { if (subscribed != null) subscribed.Swipe -= OnSwipe; subscribed = null; base.OnDestroy(); }
        public override void OnUpdate() { base.OnUpdate(); if (direction.Length > 0 && BridgeInbox.Now - directionAt >= 3) direction = ""; }
        private void OnSwipe(string value) { direction = value; directionAt = BridgeInbox.Now; InvokeFlow(nameof(Any)); switch (value) { case "up": InvokeFlow(nameof(Up)); break; case "down": InvokeFlow(nameof(Down)); break; case "left": InvokeFlow(nameof(Left)); break; case "right": InvokeFlow(nameof(Right)); break; } }
    }
}
