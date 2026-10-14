// Copyright © 2026-2030 anshaer.com. All rights reserved.
// SPDX-License-Identifier: MIT
using UnityEditor;
using UnityEngine;
using Anshaer.WebVMC;

[CustomEditor(typeof(AnshaerWebVmcReceiver))]
public sealed class AnshaerWebVmcReceiverEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var receiver=(AnshaerWebVmcReceiver)target;
        EditorGUILayout.Space();EditorGUILayout.HelpBox(receiver.Status,MessageType.Info);
        EditorGUI.BeginDisabledGroup(receiver.IsReceiving);
        if(GUILayout.Button("自動建立 Humanoid／BlendShape 映射")){Undo.RecordObject(receiver,"Map VMC");receiver.AutoMap();EditorUtility.SetDirty(receiver);}
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if(GUILayout.Button(receiver.IsReceiving?"停止接收":"開始接收")){if(receiver.IsReceiving)receiver.StopReceiver();else receiver.StartReceiver();}
        if(GUILayout.Button("3 秒後中立校正"))receiver.CalibrateNeutral();
        if(GUILayout.Button("清除中立校正"))receiver.ClearCalibration();
        if(GUILayout.Button("停止並還原開始姿態"))receiver.RestoreStartPose();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.LabelField("Copyright © 2026-2030 anshaer.com.");EditorGUILayout.LabelField("All rights reserved.");
        if(Application.isPlaying)Repaint();
    }
}
