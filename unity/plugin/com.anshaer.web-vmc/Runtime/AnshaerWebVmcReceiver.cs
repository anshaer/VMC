// Copyright © 2026-2030 anshaer.com. All rights reserved.
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Anshaer.WebVMC {
    public enum SignedAxis { X, NegativeX, Y, NegativeY, Z, NegativeZ }
    [Serializable] public sealed class BoneMapping {
        public string source;
        public Transform target;
        public bool enabled=true;
        public SignedAxis x=SignedAxis.X,y=SignedAxis.Y,z=SignedAxis.Z;
        public bool invertRotation;
        public Vector3 rotationOffset;
        [HideInInspector] public bool calibrated;
        [HideInInspector] public Quaternion reference=Quaternion.identity;
        [NonSerialized] internal Quaternion startLocal,restRotation;
    }
    [Serializable] public sealed class BlendMapping {
        public string source;
        public SkinnedMeshRenderer renderer;
        public string shapeName;
        [Range(0,4)] public float gain=1;
        public float weightScale=100;
        public bool enabled=true;
        [NonSerialized] internal int index=-1;
    }
    [DisallowMultipleComponent,DefaultExecutionOrder(10000)]
    public sealed class AnshaerWebVmcReceiver : MonoBehaviour {
        [Header("Target / 角色")]
        public Animator targetAnimator;
        public Transform modelRoot;
        [Header("Local UDP / 本機接收")]
        [Range(1,65535)] public int port=39540;
        public bool startOnPlay=true;
        [Range(15,60)] public int updateFPS=30;
        [Range(0,1)] public float smoothSeconds=.05f;
        [Header("Channels / 控制區域")]
        public bool receiveBones=true,receiveBlends=true;
        public bool useRestAxes=true;
        public bool receiveRoot=false;
        [Range(0,10)] public float rootPositionScale=1;
        [Header("Mapping / 映射與補正")]
        public List<BoneMapping> boneMappings=new List<BoneMapping>();
        public List<BlendMapping> blendMappings=new List<BlendMapping>();
        public bool IsReceiving { get { return socket!=null; } }
        public string Status { get; private set; }="尚未接收";
        struct BoneSample { public MotionValue value;public double at; }
        struct BlendSample { public float value;public double at; }
        struct PoseStart { public Quaternion rotation;public Vector3 position,scale; }
        struct ShapeKey : IEquatable<ShapeKey> {
            public SkinnedMeshRenderer renderer;public int index;
            public bool Equals(ShapeKey other){return renderer==other.renderer&&index==other.index;}
            public override bool Equals(object other){return other is ShapeKey&&Equals((ShapeKey)other);}
            public override int GetHashCode(){return (renderer==null?0:renderer.GetHashCode())*397^index;}
        }
        UdpClient socket;int boundPort;double nextTick,lastData,calibrateAt;
        readonly Dictionary<string,BoneSample> bones=new Dictionary<string,BoneSample>();
        readonly Dictionary<string,BlendSample> blends=new Dictionary<string,BlendSample>();
        readonly Dictionary<string,float> pending=new Dictionary<string,float>();
        readonly Dictionary<Transform,PoseStart> initialBones=new Dictionary<Transform,PoseStart>();
        readonly Dictionary<ShapeKey,float> initialBlends=new Dictionary<ShapeKey,float>();
        readonly Dictionary<ShapeKey,float> desiredBlends=new Dictionary<ShapeKey,float>();
        readonly List<string> expired=new List<string>();
        MotionValue? root,rootReference;double rootAt;
        Transform capturedRoot;Vector3 startRootPosition;Quaternion startRootRotation;
        static double Now { get { return Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency; } }
        void OnEnable(){if(Application.isPlaying&&startOnPlay)StartReceiver();}
        void OnDisable(){StopReceiver();}
        void OnApplicationQuit(){StopReceiver();}
        void Reset(){targetAnimator=GetComponentInChildren<Animator>();if(targetAnimator!=null)modelRoot=targetAnimator.transform;}
        [ContextMenu("Auto Map Humanoid and BlendShapes")]
        public void AutoMap() {
            if(IsReceiving){Status="請先停止接收再建立映射";return;}
            if(targetAnimator==null)targetAnimator=GetComponentInChildren<Animator>();
            if(modelRoot==null)modelRoot=targetAnimator!=null?targetAnimator.transform:transform;
            boneMappings.Clear();blendMappings.Clear();
            if(targetAnimator!=null&&targetAnimator.avatar!=null&&targetAnimator.avatar.isValid&&targetAnimator.avatar.isHuman){
                foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones))){
                    if(bone==HumanBodyBones.LastBone)continue;Transform target=targetAnimator.GetBoneTransform(bone);
                    if(target!=null)boneMappings.Add(new BoneMapping{source=bone.ToString(),target=target});
                }
            }
            var renderers=modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var renderer in renderers){var mesh=renderer.sharedMesh;if(mesh==null)continue;
                foreach(string source in FaceNames){int found=FindShape(mesh,source);if(found>=0)blendMappings.Add(new BlendMapping{source=source,renderer=renderer,shapeName=mesh.GetBlendShapeName(found)});}
            }
            Status="映射建立：骨骼 "+boneMappings.Count+"／表情 "+blendMappings.Count+"；找不到的名稱請手動新增。";
        }
        [ContextMenu("Start VMC Receiver (Play Mode)")]
        public void StartListening(){StartReceiver();}
        public bool StartReceiver() {
            if(!Application.isPlaying){Status="請先按 Unity Play，再開始接收";return false;}
            StopReceiver();
            if(port<1||port>65535){Status="UDP 埠必須在 1–65535";return false;}
            if(targetAnimator==null)targetAnimator=GetComponentInChildren<Animator>();
            if(modelRoot==null)modelRoot=targetAnimator!=null?targetAnimator.transform:transform;
            if(boneMappings.Count==0&&blendMappings.Count==0)AutoMap();
            if(boneMappings.Count==0&&blendMappings.Count==0){Status="沒有可控制的映射，請指定角色或手動新增目標";return false;}
            try{socket=new UdpClient(new IPEndPoint(IPAddress.Loopback,port));socket.Client.Blocking=false;boundPort=port;}
            catch(SocketException e){if(socket!=null)socket.Close();socket=null;Status="無法開啟 UDP："+e.Message;return false;}
            initialBones.Clear();initialBlends.Clear();capturedRoot=modelRoot;
            startRootPosition=modelRoot.localPosition;startRootRotation=modelRoot.localRotation;
            foreach(var row in boneMappings){if(row.target==null)continue;
                row.startLocal=row.target.localRotation;row.restRotation=Quaternion.Inverse(modelRoot.rotation)*row.target.rotation;
                if(!initialBones.ContainsKey(row.target))initialBones[row.target]=new PoseStart{rotation=row.target.localRotation,position=row.target.localPosition,scale=row.target.localScale};
            }
            foreach(var row in blendMappings){row.index=row.renderer==null||row.renderer.sharedMesh==null?-1:FindExactShape(row.renderer.sharedMesh,row.shapeName);if(row.index<0)continue;
                var key=new ShapeKey{renderer=row.renderer,index=row.index};if(!initialBlends.ContainsKey(key))initialBlends[key]=row.renderer.GetBlendShapeWeight(row.index);
            }
            nextTick=lastData=calibrateAt=0;rootReference=null;Status="等待 UDP 127.0.0.1:"+boundPort;return true;
        }
        [ContextMenu("Stop VMC Receiver")]
        public void StopReceiver(){if(socket!=null){socket.Close();socket=null;}bones.Clear();blends.Clear();pending.Clear();desiredBlends.Clear();root=null;rootReference=null;calibrateAt=0;lastData=0;Status="接收已停止，保留目前姿態";}
        [ContextMenu("Calibrate Neutral After 3 Seconds")]
        public void CalibrateNeutral(){if(!IsReceiving){Status="請先開始接收";return;}calibrateAt=Now+3;}
        [ContextMenu("Clear Neutral Calibration")]
        public void ClearCalibration(){foreach(var row in boneMappings)row.calibrated=false;rootReference=null;}
        [ContextMenu("Stop and Restore Start Pose")]
        public void RestoreStartPose(){StopReceiver();foreach(var pair in initialBones){if(pair.Key==null)continue;pair.Key.localRotation=pair.Value.rotation;pair.Key.localPosition=pair.Value.position;pair.Key.localScale=pair.Value.scale;}
            foreach(var pair in initialBlends){if(pair.Key.renderer!=null&&pair.Key.renderer.sharedMesh!=null&&pair.Key.index<pair.Key.renderer.sharedMesh.blendShapeCount)pair.Key.renderer.SetBlendShapeWeight(pair.Key.index,pair.Value);}
            if(capturedRoot!=null){capturedRoot.localPosition=startRootPosition;capturedRoot.localRotation=startRootRotation;}
            Status="已停止並還原開始姿態";
        }
        void LateUpdate(){Tick();}
        public void Tick() {
            if(!IsReceiving)return;
            if(modelRoot==null||modelRoot!=capturedRoot){StopReceiver();Status="目標被刪除或更換，請重新開始";return;}
            double now=Now;if(now<nextTick)return;nextTick=now+1.0/Math.Max(15,Math.Min(60,updateFPS));
            double until=Now+.006;
            try{for(int i=0;i<128&&socket.Available>0;i++){IPEndPoint remote=new IPEndPoint(IPAddress.Any,0);byte[] bytes=socket.Receive(ref remote);
                    try{AcceptPacket(bytes,now);}catch(FormatException e){Status="忽略無效封包："+e.Message;}catch(DecoderFallbackException e){Status="忽略無效文字："+e.Message;}
                    if(Now>=until)break;
                }}catch(SocketException e){if(e.SocketErrorCode!=SocketError.WouldBlock){StopReceiver();Status="接收中斷："+e.Message;return;}}
            Expire(now);
            if(calibrateAt>0&&now>=calibrateAt){calibrateAt=0;if(bones.Count>0){foreach(var row in boneMappings){BoneSample sample;if(bones.TryGetValue(row.source??"",out sample)){row.reference=sample.value.rotation;row.calibrated=true;}}if(root.HasValue)rootReference=root.Value;Status="已校正";}else Status="校正失敗：沒有新鮮骨骼資料";}
            ApplyMotion(1f/Math.Max(15,Math.Min(60,updateFPS)));
            if(calibrateAt>0)Status="校正倒數 "+Math.Ceiling(calibrateAt-now)+" 秒";
            else if(bones.Count>0||blends.Count>0||root.HasValue)Status="接收中 · 目前骨骼 "+bones.Count+"／表情 "+blends.Count;
            else Status="等待資料，逾 3 秒保持姿態";
        }
        // Parser finishes before state mutation. Public to support external/native tests.
        public void AcceptPacket(byte[] bytes,double now) {
            var p=OscVmcDecoder.Decode(bytes);
            foreach(var pair in p.bones)if(bones.ContainsKey(pair.Key)||bones.Count<128)bones[pair.Key]=new BoneSample{value=pair.Value,at=now};
            foreach(var pair in p.blends)if(pending.ContainsKey(pair.Key)||pending.Count<128)pending[pair.Key]=pair.Value;
            if(p.applyBlends){foreach(var pair in pending)if(blends.ContainsKey(pair.Key)||blends.Count<128)blends[pair.Key]=new BlendSample{value=pair.Value,at=now};pending.Clear();}
            if(p.root.HasValue){root=p.root;rootAt=now;}if(p.bones.Count>0||p.blends.Count>0||p.root.HasValue)lastData=now;
        }
        void Expire(double now){expired.Clear();foreach(var p in bones)if(now-p.Value.at>=3)expired.Add(p.Key);foreach(string key in expired)bones.Remove(key);
            expired.Clear();foreach(var p in blends)if(now-p.Value.at>=3)expired.Add(p.Key);foreach(string key in expired)blends.Remove(key);expired.Clear();
            if(root.HasValue&&now-rootAt>=3)root=null;if(now-lastData>=3)pending.Clear();}
        public void ApplyMotion(float dt){float alpha=smoothSeconds<=0?1:1-(float)Math.Exp(-dt/smoothSeconds);
            if(receiveBones)foreach(var row in boneMappings){BoneSample sample;if(!row.enabled||row.target==null||!bones.TryGetValue(row.source??"",out sample))continue;
                Quaternion delta=Quaternion.Inverse(row.calibrated?row.reference:Quaternion.identity)*sample.value.rotation;
                if(row.invertRotation)delta=Quaternion.Inverse(delta);Quaternion mapped;if(!MapAxes(delta,row,out mapped))continue;
                if(useRestAxes)mapped=Quaternion.Inverse(row.restRotation)*mapped*row.restRotation;
                Quaternion wanted=row.startLocal*mapped*Quaternion.Euler(row.rotationOffset);row.target.localRotation=Quaternion.Slerp(row.target.localRotation,wanted,alpha);
            }
            desiredBlends.Clear();
            if(receiveBlends)foreach(var row in blendMappings){BlendSample sample;if(!row.enabled||row.renderer==null||row.renderer.sharedMesh==null||row.index<0||row.index>=row.renderer.sharedMesh.blendShapeCount||!blends.TryGetValue(row.source??"",out sample))continue;
                float value=Math.Max(0,Math.Min(1,sample.value*row.gain))*row.weightScale;var key=new ShapeKey{renderer=row.renderer,index=row.index};float old;
                if(!desiredBlends.TryGetValue(key,out old)||value>old)desiredBlends[key]=value;
            }
            foreach(var pair in desiredBlends){float current=pair.Key.renderer.GetBlendShapeWeight(pair.Key.index);pair.Key.renderer.SetBlendShapeWeight(pair.Key.index,current+(pair.Value-current)*alpha);}
            if(receiveRoot&&root.HasValue){if(!rootReference.HasValue)rootReference=root;MotionValue value=root.Value,reference=rootReference.Value;
                modelRoot.localPosition=Vector3.Lerp(modelRoot.localPosition,startRootPosition+(value.position-reference.position)*rootPositionScale,alpha);
                modelRoot.localRotation=Quaternion.Slerp(modelRoot.localRotation,startRootRotation*Quaternion.Inverse(reference.rotation)*value.rotation,alpha);
            }
        }
        static int AxisIndex(SignedAxis axis){return ((int)axis)/2;}
        static int AxisSign(SignedAxis axis){return ((int)axis)%2==0?1:-1;}
        static float Component(Quaternion q,int index){return index==0?q.x:index==1?q.y:q.z;}
        public static bool MapAxes(Quaternion q,BoneMapping row,out Quaternion mapped){int a=AxisIndex(row.x),b=AxisIndex(row.y),c=AxisIndex(row.z);
            mapped=q;if(a==b||a==c||b==c)return false;int inversions=(a>b?1:0)+(a>c?1:0)+(b>c?1:0);
            int sx=AxisSign(row.x),sy=AxisSign(row.y),sz=AxisSign(row.z),det=(inversions%2==0?1:-1)*sx*sy*sz;
            mapped=new Quaternion(det*sx*Component(q,a),det*sy*Component(q,b),det*sz*Component(q,c),q.w);return true;
        }
        static string Normal(string text){var s=new StringBuilder();foreach(char c in text??"")if(char.IsLetterOrDigit(c))s.Append(char.ToLowerInvariant(c));return s.ToString();}
        static int FindExactShape(Mesh mesh,string name){for(int i=0;i<mesh.blendShapeCount;i++)if(mesh.GetBlendShapeName(i)==name)return i;return -1;}
        static int FindShape(Mesh mesh,string source){foreach(string alias in Aliases(source)){int exact=FindExactShape(mesh,alias);if(exact>=0)return exact;
                int match=-1;for(int i=0;i<mesh.blendShapeCount;i++)if(Normal(mesh.GetBlendShapeName(i))==Normal(alias)){if(match>=0){match=-1;break;}match=i;}if(match>=0)return match;}
            return -1;
        }
        static string[] Aliases(string name){switch(name){case "Blink_L":return new[]{name,"EyeBlinkLeft","blinkLeft"};case "Blink_R":return new[]{name,"EyeBlinkRight","blinkRight"};case "A":return new[]{name,"JawOpen","aa"};case "U":return new[]{name,"MouthPucker","ou"};case "Joy":return new[]{name,"happy"};default:return new[]{name};}}
        static readonly string[] FaceNames={"Blink_L","Blink_R","A","I","U","E","O","Joy","Angry","Sorrow","Fun","Blink","EyeBlinkLeft","EyeBlinkRight","JawOpen","MouthPucker","MouthSmileLeft","MouthSmileRight","BrowInnerUp","BrowOuterUpLeft","BrowOuterUpRight","BrowDownLeft","BrowDownRight","EyeLookInLeft","EyeLookOutLeft","EyeLookUpLeft","EyeLookDownLeft","EyeLookInRight","EyeLookOutRight","EyeLookUpRight","EyeLookDownRight"};
    }
}
