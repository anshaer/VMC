// Copyright © 2026-2030 anshaer.com. All rights reserved.
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Anshaer.WebVMC {
    public struct MotionValue { public Vector3 position; public Quaternion rotation; }
    public sealed class MotionPacket {
        public readonly Dictionary<string, MotionValue> bones = new Dictionary<string, MotionValue>();
        public readonly Dictionary<string, float> blends = new Dictionary<string, float>();
        public MotionValue? root;
        public bool applyBlends;
    }
    public static class OscVmcDecoder {
        [StructLayout(LayoutKind.Explicit)] struct Bits { [FieldOffset(0)] public uint integer; [FieldOffset(0)] public float real; }
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        static uint UInt(byte[] b, int i) { return ((uint)b[i]<<24)|((uint)b[i+1]<<16)|((uint)b[i+2]<<8)|b[i+3]; }
        static string Text(byte[] b, ref int at, int end) {
            int start=at;while(at<end&&b[at]!=0)at++;
            if(at==end||at-start>1024)throw new FormatException("OSC string");
            string s=Utf8.GetString(b,start,at-start);int next=(at+4)&~3;
            if(next>end)throw new FormatException("OSC padding");
            for(int i=at;i<next;i++)if(b[i]!=0)throw new FormatException("OSC padding");at=next;return s;
        }
        static bool Finite(float f) { return !float.IsNaN(f)&&!float.IsInfinity(f); }
        public static MotionPacket Decode(byte[] b) {
            if(b==null||b.Length<4||b.Length>65507||(b.Length&3)!=0)throw new FormatException("OSC length");
            var result=new MotionPacket();int count=0;Parse(b,0,b.Length,0,result,ref count);
            if(count==0)throw new FormatException("Empty OSC");return result;
        }
        static void Parse(byte[] b,int start,int end,int depth,MotionPacket result,ref int count) {
            if(depth>3)throw new FormatException("OSC depth");int at=start;string address=Text(b,ref at,end);
            if(address=="#bundle") {
                if(end-start<16)throw new FormatException("OSC bundle");at=start+16;
                while(at<end){if(at+4>end)throw new FormatException("OSC size");uint size=UInt(b,at);at+=4;
                    if(size<4||(size&3)!=0||size>(uint)(end-at))throw new FormatException("OSC size");
                    Parse(b,at,at+(int)size,depth+1,result,ref count);at+=(int)size;}return;
            }
            if(++count>128||(!address.StartsWith("/VMC/Ext/",StringComparison.Ordinal)&&!address.StartsWith("/ANSHA/",StringComparison.Ordinal)))throw new FormatException("OSC address");
            string tags=Text(b,ref at,end);if(tags.Length<1||tags[0]!=',')throw new FormatException("OSC tags");
            string name=null;float[] values=new float[7];int n=0;
            for(int i=1;i<tags.Length;i++) {
                char tag=tags[i];if(tag=='s'){string s=Text(b,ref at,end);if(i==1)name=s;}
                else if(tag=='f'||tag=='i'){if(at+4>end)throw new FormatException("OSC number");uint bits=UInt(b,at);at+=4;
                    float value=tag=='f'?new Bits{integer=bits}.real:unchecked((int)bits);
                    if(!Finite(value))throw new FormatException("Nonfinite OSC");if(n<7)values[n]=value;n++;
                }else throw new FormatException("Unsupported OSC tag");
            }
            if(at!=end)throw new FormatException("Trailing OSC");
            if((address=="/VMC/Ext/Bone/Pos"||address=="/VMC/Ext/Root/Pos")&&tags==",sfffffff") {
                if(string.IsNullOrEmpty(name)||name.Length>128)throw new FormatException("Bone name");
                for(int i=0;i<7;i++)if(Math.Abs(values[i])>100000)throw new FormatException("Bone range");
                float norm=(float)Math.Sqrt(values[3]*values[3]+values[4]*values[4]+values[5]*values[5]+values[6]*values[6]);
                if(norm<.1f||norm>2f)throw new FormatException("Quaternion norm");
                var value=new MotionValue{position=new Vector3(values[0],values[1],values[2]),rotation=new Quaternion(values[3]/norm,values[4]/norm,values[5]/norm,values[6]/norm)};
                if(address=="/VMC/Ext/Bone/Pos")result.bones[name]=value;else result.root=value;
            }else if(address=="/VMC/Ext/Blend/Val"&&tags==",sf") {
                if(string.IsNullOrEmpty(name)||name.Length>128||values[0]<0||values[0]>1)throw new FormatException("Blend range");result.blends[name]=values[0];
            }else if(address=="/VMC/Ext/Blend/Apply"&&tags==",")result.applyBlends=true;
        }
    }
}
