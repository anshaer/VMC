# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright © 2026-2030 anshaer.com. All rights reserved.
bl_info = {'name': 'Anshaer Web VMC', 'author': 'anshaer.com', 'version': (0, 2, 0),
           'blender': (4, 2, 0), 'location': '3D View > Sidebar > Anshaer VMC', 'category': 'Animation'}

import math
import re
import socket
import time
import bpy
from bpy.props import (BoolProperty, CollectionProperty, EnumProperty, FloatProperty,
                       FloatVectorProperty, IntProperty, PointerProperty, StringProperty)
from bpy.app.handlers import persistent
from mathutils import Matrix, Quaternion, Vector, Euler
from .osc import parse_packet, decode_updates

BONES = ['Hips', 'Spine', 'Chest', 'UpperChest', 'Neck', 'Head', 'LeftEye', 'RightEye', 'Jaw']
for side in ('Left', 'Right'):
    BONES += [side+x for x in ('Shoulder','UpperArm','LowerArm','Hand','UpperLeg','LowerLeg','Foot','Toes')]
    BONES += [side+finger+joint for finger in ('Thumb','Index','Middle','Ring','Little')
              for joint in ('Proximal','Intermediate','Distal')]
BLENDS = ['Blink_L','Blink_R','A','I','U','E','O','Joy','Angry','Sorrow','Fun','Blink']
BLENDS += ['EyeBlinkLeft','EyeBlinkRight','JawOpen','MouthPucker','MouthSmileLeft','MouthSmileRight',
           'BrowInnerUp','BrowOuterUpLeft','BrowOuterUpRight','BrowDownLeft','BrowDownRight',
           'EyeLookInLeft','EyeLookOutLeft','EyeLookUpLeft','EyeLookDownLeft',
           'EyeLookInRight','EyeLookOutRight','EyeLookUpRight','EyeLookDownRight']
AXES = [('X','X',''),('-X','−X',''),('Y','Y',''),('-Y','−Y',''),('Z','Z',''),('-Z','−Z','')]
UNITY = Matrix(((1,0,0),(0,0,-1),(0,1,0)))
EXPR_ALIAS = {'Blink_L':'blinkLeft','EyeBlinkLeft':'blinkLeft','Blink_R':'blinkRight','EyeBlinkRight':'blinkRight',
              'Blink':'blink','A':'aa','JawOpen':'aa','I':'ih','U':'ou','MouthPucker':'ou',
              'E':'ee','O':'oh','Joy':'happy','Angry':'angry','Sorrow':'sad','Fun':'relaxed'}

def norm(name):
    return re.sub('[^a-z0-9]', '', str(name).lower())

def arm_poll(self, ob): return ob.type == 'ARMATURE'
def mesh_poll(self, ob): return ob.type == 'MESH'

def vrm_bones(arm):
    result = {}
    ext = getattr(arm.data, 'vrm_addon_extension', None)
    if not ext: return result
    try:
        if hasattr(ext,'is_vrm0') and not ext.is_vrm0(): raise AttributeError()
        for h in ext.vrm0.humanoid.human_bones:
            if h.node.bone_name: result[norm(h.bone)] = h.node.bone_name
    except (AttributeError, TypeError): pass
    try:
        if hasattr(ext,'is_vrm1') and not ext.is_vrm1(): raise AttributeError()
        for name, h in ext.vrm1.humanoid.human_bones.human_bone_name_to_human_bone().items():
            if h.node.bone_name: result[norm(getattr(name, 'value', name))] = h.node.bone_name
    except (AttributeError, TypeError): pass
    return result

def candidate_bone(source, names, vrm):
    # VRM1 names the thumb's first joint metacarpal; VMC uses the VRM0 names.
    key = norm(source)
    if source.endswith('ThumbIntermediate') and key.replace('intermediate','metacarpal') in vrm:
        return vrm.get(key.replace('intermediate','proximal'), '')
    if source.endswith('ThumbProximal') and key.replace('proximal','metacarpal') in vrm:
        return vrm[key.replace('proximal','metacarpal')]
    if key in vrm: return vrm[key]
    aliases = [source, 'mixamorig:'+source]
    side = 'L' if source.startswith('Left') else 'R' if source.startswith('Right') else ''
    part = source[4:] if side == 'L' else source[5:] if side == 'R' else source
    common = {'UpperArm':'upper_arm','LowerArm':'forearm','Hand':'hand','Shoulder':'shoulder',
              'UpperLeg':'thigh','LowerLeg':'shin','Foot':'foot','Toes':'toe','Eye':'eye'}
    if side:
        aliases += [f'J_Bip_{side}_{part}', f'J_Adj_{side}_{part}',
                    f'{common.get(part,part)}.{side}', f'{common.get(part,part)}_{side.lower()}']
    else:
        aliases += [f'J_Bip_C_{part}', part.lower()]
        aliases += {'Hips':['pelvis'], 'Chest':['spine.001'], 'UpperChest':['spine.002']}.get(part,[])
    for alias in aliases:
        if alias in names: return alias
    lookup = {}
    for name in names: lookup.setdefault(norm(name), []).append(name)
    for alias in aliases:
        matches = lookup.get(norm(alias), [])
        if len(matches) == 1: return matches[0]
    return ''

def face_meshes(settings):
    if settings.mesh: return [settings.mesh]
    if not settings.armature: return []
    return [o for o in bpy.context.scene.objects if o.type == 'MESH' and
            (o.parent == settings.armature or o.find_armature() == settings.armature)]

def expression_binds(arm):
    """Optional VRM import add-on metadata. No hard dependency on its package name."""
    result = {}
    ext = getattr(arm.data, 'vrm_addon_extension', None)
    if not ext: return result
    def add(name, binds, scale):
        output = []
        for bind in binds:
            node = getattr(bind, 'node', getattr(bind, 'mesh', None))
            obname = getattr(node, 'mesh_object_name', '')
            ob = bpy.data.objects.get(obname)
            keys = getattr(getattr(ob, 'data', None), 'shape_keys', None)
            if not keys: continue
            key = keys.key_blocks.get(bind.index) if isinstance(bind.index, str) else None
            if not key:
                try: index = int(bind.index)
                except (TypeError, ValueError): continue
                if 0 <= index < len(keys.key_blocks): key=keys.key_blocks[index]
            if key: output.append((ob, key.name, float(bind.weight)*scale))
        if output: result[norm(name)] = output
    try:
        if hasattr(ext,'is_vrm0') and not ext.is_vrm0(): raise AttributeError()
        for exp in ext.vrm0.blend_shape_master.blend_shape_groups:
            add(exp.name, exp.binds, .01)
            if exp.preset_name != 'unknown': add(exp.preset_name, exp.binds, .01)
    except (AttributeError, TypeError): pass
    try:
        if hasattr(ext,'is_vrm1') and not ext.is_vrm1(): raise AttributeError()
        exp = ext.vrm1.expressions
        for name, value in exp.all_name_to_expression_dict().items():
            add(name, value.morph_target_binds, 1.0)
    except (AttributeError, TypeError): pass
    return result

def pose_quat(pb):
    return pb.matrix_basis.to_quaternion().normalized()

def custom_axes(row):
    values = (row.axis_x, row.axis_y, row.axis_z)
    if len({v[-1] for v in values}) != 3: raise ValueError('XYZ 對應必須各用不同軸')
    rows = []
    for axis in values:
        v = [0.,0.,0.];v['XYZ'.index(axis[-1])] = -1. if axis[0] == '-' else 1.;rows.append(v)
    return Matrix(rows)

def converted(q, settings, row=None):
    c = UNITY if settings.coordinates == 'UNITY' else Matrix.Identity(3)
    rotation = c @ q.to_matrix() @ c.transposed()
    if row is not None:
        m = custom_axes(row); rotation = m @ rotation @ m.transposed()
    return rotation.to_quaternion().normalized()

class ANSHA_VMC_Bone(bpy.types.PropertyGroup):
    source: StringProperty(name='VMC 名稱')
    target: StringProperty(name='Blender 骨骼')
    enabled: BoolProperty(name='啟用', default=True)
    axis_x: EnumProperty(name='X 取自', items=AXES, default='X')
    axis_y: EnumProperty(name='Y 取自', items=AXES, default='Y')
    axis_z: EnumProperty(name='Z 取自', items=AXES, default='Z')
    inverse: BoolProperty(name='反轉旋轉', default=False)
    offset: FloatVectorProperty(name='局部旋轉補正', size=3, subtype='EULER', default=(0,0,0))
    calibrated: BoolProperty(default=False)
    reference: FloatVectorProperty(size=4, default=(1,0,0,0))

class ANSHA_VMC_Blend(bpy.types.PropertyGroup):
    source: StringProperty(name='VMC 表情名稱')
    target: StringProperty(name='Shape Key／VRM 表情')
    mode: EnumProperty(name='映射', items=[('AUTO','自動','Shape Key 優先，找不到才用 VRM 表情'),
        ('SHAPE','Shape Key',''),('VRM','VRM 表情',''),('OFF','停用','')], default='AUTO')
    gain: FloatProperty(name='強度', min=0,max=4,default=1)

class ANSHA_VMC_Settings(bpy.types.PropertyGroup):
    armature: PointerProperty(name='角色骨架', type=bpy.types.Object, poll=arm_poll)
    mesh: PointerProperty(name='臉部網格（可留空）', type=bpy.types.Object, poll=mesh_poll)
    port: IntProperty(name='UDP 埠', min=1,max=65535,default=39540)
    fps: EnumProperty(name='更新 FPS',items=[('15','15',''),('30','30',''),('60','60','')],default='30')
    coordinates: EnumProperty(name='來源座標',items=[('UNITY','Unity → Blender (X,−Z,Y)',''),('RAW','原始 XYZ（不轉座標）','')],default='UNITY')
    rest_axes: BoolProperty(name='依骨骼靜止軸轉換',default=True)
    smooth: FloatProperty(name='平滑秒數',min=0,max=1,default=.05)
    bones_enabled: BoolProperty(name='接收骨骼',default=True)
    blends_enabled: BoolProperty(name='接收表情',default=True)
    root_enabled: BoolProperty(name='接收 Root 位移／旋轉',default=False)
    root_gain: FloatProperty(name='Root 位移倍率',min=0,max=10,default=1)
    bones: CollectionProperty(type=ANSHA_VMC_Bone)
    blends: CollectionProperty(type=ANSHA_VMC_Blend)
    bone_index: IntProperty(default=0)
    blend_index: IntProperty(default=0)
    show_bones: BoolProperty(name='骨骼映射／方向校正',default=False)
    show_blends: BoolProperty(name='表情映射',default=False)
    status: StringProperty(default='未接收')

class Runtime:
    def __init__(self):
        self.sock=None;self.scene=None;self.arm=None;self.running=False
        self.bones={};self.blends={};self.pending={};self.root=None
        self.started={};self.started_keys={};self.object_start=None;self.root_reference=None
        self.expression_map={};self.changed_bones=set();self.changed_keys=set()
        self.last=time.monotonic();self.cal_at=0.;self.last_data=0.;self.last_redraw=0.
    def start(self, scene):
        self.stop(clear=True)
        settings=scene.ansha_vmc
        if not settings.armature: raise ValueError('請先指定角色骨架')
        if not settings.bones and not settings.blends: auto_map(settings)
        sock=socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
        try: sock.bind(('127.0.0.1',settings.port));sock.setblocking(False)
        except Exception: sock.close();raise
        self.sock=sock;self.scene=scene;self.arm=settings.armature;self.running=True
        self.started={p.name:(p.matrix_basis.copy(),p.rotation_mode) for p in self.arm.pose.bones}
        self.object_start=(self.arm.location.copy(),self.arm.rotation_mode,self.arm.rotation_euler.copy(),self.arm.rotation_quaternion.copy())
        self.expression_map=expression_binds(self.arm)
        self.started_keys={(ob.name,k.name):float(k.value) for ob in face_meshes(settings) if ob.data.shape_keys for k in ob.data.shape_keys.key_blocks}
        self.last=time.monotonic();settings.status=f'等待本機 UDP 127.0.0.1:{settings.port}'
        if not bpy.app.timers.is_registered(timer): bpy.app.timers.register(timer,first_interval=.01)
    def stop(self,clear=False):
        if self.sock: self.sock.close()
        self.sock=None;self.running=False;self.bones.clear();self.blends.clear();self.pending.clear();self.root=None;self.cal_at=0.;self.last_data=0.
        if bpy.app.timers.is_registered(timer): bpy.app.timers.unregister(timer)
        if self.scene:
            try: self.scene.ansha_vmc.status='接收已停止（保留目前姿態）'
            except (ReferenceError,AttributeError): pass
        if clear:
            self.started.clear();self.started_keys.clear();self.object_start=None;self.arm=None;self.scene=None;self.root_reference=None;self.expression_map.clear();self.changed_bones.clear();self.changed_keys.clear()
    def ingest(self, data, now):
        bones,blends,root,apply=decode_updates(parse_packet(data))
        for name,value in bones.items():
            if name in self.bones or len(self.bones)<128: self.bones[name]=(value,now)
        for name,value in blends.items():
            if name in self.pending or len(self.pending)<128: self.pending[name]=value
        if apply:
            for name,value in self.pending.items():
                if name in self.blends or len(self.blends)<128: self.blends[name]=(value,now)
            self.pending.clear()
        if root: self.root=(root,now)
        if bones or blends or root or apply: self.last_data=now
    def restore(self):
        if not self.arm or self.arm.name not in bpy.data.objects: return
        for name,(basis,mode) in self.started.items():
            pb=self.arm.pose.bones.get(name)
            if pb: pb.rotation_mode=mode;pb.matrix_basis=basis
        for (obname,key),value in self.started_keys.items():
            ob=bpy.data.objects.get(obname)
            if ob and ob.data.shape_keys and key in ob.data.shape_keys.key_blocks: ob.data.shape_keys.key_blocks[key].value=value
        if self.object_start:
            pos,mode,euler,quat=self.object_start;self.arm.location=pos;self.arm.rotation_mode=mode;self.arm.rotation_euler=euler;self.arm.rotation_quaternion=quat
    def tick(self):
        if not self.running: return None
        now=time.monotonic();dt=min(.2,max(.001,now-self.last));self.last=now
        try:
            if not self.scene or self.scene.name not in bpy.data.scenes or not self.arm or self.arm.name not in bpy.data.objects:
                self.stop(clear=True);return None
            s=self.scene.ansha_vmc
            if s.armature != self.arm: self.stop();s.status='骨架已更換，請重新開始接收';return None
            until=time.monotonic()+.006
            for _ in range(128):
                try: packet,_=self.sock.recvfrom(65508)
                except BlockingIOError: break
                try: self.ingest(packet,now)
                except (ValueError,UnicodeError) as err: s.status='忽略無效封包：'+str(err)
                if time.monotonic()>=until: break
            self.bones={k:v for k,v in self.bones.items() if now-v[1]<3}
            self.blends={k:v for k,v in self.blends.items() if now-v[1]<3}
            if self.root and now-self.root[1]>=3: self.root=None
            if now-self.last_data>=3: self.pending.clear()
            if self.cal_at and now>=self.cal_at:
                self.cal_at=0.
                if self.bones:
                    for row in s.bones:
                        if row.source in self.bones: row.reference=self.bones[row.source][0][1];row.calibrated=True
                    if self.root: self.root_reference=self.root[0]
                    s.status='已記錄本次中立訊號，對應開始接收時的姿態'
                else: s.status='校正失敗：沒有新鮮骨骼資料'
            self.apply(s,dt)
            if self.cal_at: s.status=f'中立校正倒數 {max(0,math.ceil(self.cal_at-now))} 秒'
            elif self.bones or self.blends or self.root: s.status=f'接收中 · 目前骨骼 {len(self.bones)}／表情 {len(self.blends)}'
            elif now-self.last_data>=3: s.status='等待資料（逾 3 秒停止更新，保持姿態）'
            if now-self.last_redraw>.2:
                self.last_redraw=now
                for window in bpy.context.window_manager.windows:
                    for area in window.screen.areas:
                        if area.type=='VIEW_3D': area.tag_redraw()
            return 1/int(s.fps)
        except (ReferenceError,OSError,RuntimeError,ValueError) as err:
            self.stop()
            try: self.scene.ansha_vmc.status='接收已停止：'+str(err)
            except (ReferenceError,AttributeError): pass
            return None
    def apply(self,s,dt):
        alpha=1 if s.smooth==0 else 1-math.exp(-dt/s.smooth)
        self.changed_bones.clear();self.changed_keys.clear()
        if s.bones_enabled:
            for row in s.bones:
                if not row.enabled or not row.target or row.source not in self.bones: continue
                pb=self.arm.pose.bones.get(row.target)
                if not pb: continue
                incoming=Quaternion(self.bones[row.source][0][1])
                reference=Quaternion(row.reference) if row.calibrated else Quaternion((1,0,0,0))
                delta=reference.inverted() @ incoming
                if row.inverse: delta.invert()
                try: q=converted(delta,s,row)
                except ValueError: continue
                if s.rest_axes:
                    rest=pb.bone.matrix_local.to_quaternion();q=rest.inverted() @ q @ rest
                baseline=self.started.get(pb.name,(pb.matrix_basis.copy(),pb.rotation_mode))[0].to_quaternion()
                q=baseline @ q @ Euler(row.offset,'XYZ').to_quaternion()
                pb.rotation_mode='QUATERNION';pb.rotation_quaternion=pose_quat(pb).slerp(q.normalized(),alpha)
                self.changed_bones.add(pb.name)
        if s.blends_enabled:
            targets={}
            meshes=face_meshes(s)
            for row in s.blends:
                if row.mode=='OFF' or row.source not in self.blends: continue
                value=max(0,min(1,self.blends[row.source][0]*row.gain))
                target=row.target or row.source
                matched=False
                if row.mode in ('AUTO','SHAPE'):
                    for ob in meshes:
                        keys=getattr(ob.data,'shape_keys',None)
                        if not keys: continue
                        exact=keys.key_blocks.get(target)
                        found=[exact] if exact else [k for k in keys.key_blocks if norm(k.name)==norm(target)]
                        if len(found)!=1: continue
                        k=found[0];matched=True;pair=(ob.name,k.name);targets[pair]=max(targets.get(pair,0),value)
                if row.mode=='VRM' or (row.mode=='AUTO' and not matched):
                    name=norm(row.target or EXPR_ALIAS.get(row.source,row.source))
                    # VRM0 presets use a / blink_l; VRM1 uses aa / blinkLeft.
                    aliases=[name,norm(row.source)]
                    aliases += {'blinkleft':['blinkl'],'blinkright':['blinkr'],'aa':['a'],'ih':['i'],'ou':['u'],'ee':['e'],'oh':['o'],'happy':['joy'],'sad':['sorrow'],'relaxed':['fun']}.get(name,[])
                    binds=next((self.expression_map[n] for n in aliases if n in self.expression_map),[])
                    for ob,key,weight in binds:
                        pair=(ob.name,key);targets[pair]=max(targets.get(pair,0),min(1,value*weight))
            for (obname,key),value in targets.items():
                ob=bpy.data.objects.get(obname)
                if not ob or not ob.data.shape_keys: continue
                k=ob.data.shape_keys.key_blocks.get(key)
                if k: k.value += (value-k.value)*alpha;self.changed_keys.add((obname,key))
        if s.root_enabled and self.root and self.object_start:
            data=self.root[0]
            if self.root_reference is None: self.root_reference=data
            pos,q=data;refpos,refq=self.root_reference
            c=UNITY if s.coordinates=='UNITY' else Matrix.Identity(3)
            wanted=self.object_start[0]+c@(Vector(pos)-Vector(refpos))*s.root_gain
            self.arm.location=self.arm.location.lerp(wanted,alpha)
            delta=Quaternion(refq).inverted() @ Quaternion(q)
            base=self.object_start[3] if self.object_start[1]=='QUATERNION' else self.object_start[2].to_quaternion()
            wanted=base @ converted(delta,s)
            current=self.arm.matrix_basis.to_quaternion();self.arm.rotation_mode='QUATERNION';self.arm.rotation_quaternion=current.slerp(wanted,alpha)

RT=Runtime()
def timer(): return RT.tick()

@persistent
def on_load(_): RT.stop(clear=True)

def auto_map(s):
    arm=s.armature
    if not arm: raise ValueError('請先指定角色骨架')
    vrm=vrm_bones(arm);names=[b.name for b in arm.pose.bones]
    s.bones.clear()
    used=set()
    for source in BONES:
        row=s.bones.add();row.source=source;target=candidate_bone(source,names,vrm)
        if target not in used: row.target=target;used.add(target)
    shapes={norm(k.name):k.name for ob in face_meshes(s) if ob.data.shape_keys for k in ob.data.shape_keys.key_blocks}
    expressions=expression_binds(arm);s.blends.clear()
    for name in BLENDS:
        row=s.blends.add();row.source=name
        aliases=[name,EXPR_ALIAS.get(name,name)]
        aliases += {'Blink_L':['blink_l','blinkLeft','EyeBlinkLeft'],'Blink_R':['blink_r','blinkRight','EyeBlinkRight'],'A':['JawOpen','aa'],'U':['MouthPucker','ou'],'Joy':['happy']}.get(name,[])
        for alias in aliases:
            if norm(alias) in shapes: row.target=shapes[norm(alias)];row.mode='SHAPE';break
        else:
            row.mode='VRM' if any(norm(a) in expressions for a in aliases) else 'AUTO'
    s.bone_index=s.blend_index=0;s.status='映射已建立；空白骨骼代表沒有自動找到，請自行指定'

class ANSHA_VMC_OT_Start(bpy.types.Operator):
    bl_idname='ansha_vmc.start';bl_label='開始接收'
    def execute(self,context):
        try: RT.start(context.scene)
        except (ValueError,OSError) as e: self.report({'ERROR'},str(e));return {'CANCELLED'}
        return {'FINISHED'}
class ANSHA_VMC_OT_Stop(bpy.types.Operator):
    bl_idname='ansha_vmc.stop';bl_label='停止接收'
    def execute(self,context): RT.stop();return {'FINISHED'}
class ANSHA_VMC_OT_Map(bpy.types.Operator):
    bl_idname='ansha_vmc.auto_map';bl_label='自動建立映射'
    bl_options={'REGISTER','UNDO'}
    def execute(self,context):
        if RT.running: self.report({'ERROR'},'請先停止接收再重建映射');return {'CANCELLED'}
        try: auto_map(context.scene.ansha_vmc)
        except ValueError as e: self.report({'ERROR'},str(e));return {'CANCELLED'}
        return {'FINISHED'}
class ANSHA_VMC_OT_Calibrate(bpy.types.Operator):
    bl_idname='ansha_vmc.calibrate';bl_label='3 秒後中立校正'
    def execute(self,context):
        if not RT.running: self.report({'ERROR'},'請先開始接收');return {'CANCELLED'}
        RT.cal_at=time.monotonic()+3;return {'FINISHED'}
class ANSHA_VMC_OT_ResetCal(bpy.types.Operator):
    bl_idname='ansha_vmc.reset_calibration';bl_label='清除中立校正'
    def execute(self,context):
        for row in context.scene.ansha_vmc.bones: row.calibrated=False
        RT.root_reference=None;return {'FINISHED'}
class ANSHA_VMC_OT_Restore(bpy.types.Operator):
    bl_idname='ansha_vmc.restore';bl_label='還原開始姿態'
    bl_options={'REGISTER','UNDO'}
    def execute(self,context): RT.stop();RT.restore();return {'FINISHED'}
class ANSHA_VMC_OT_Keyframe(bpy.types.Operator):
    bl_idname='ansha_vmc.keyframe';bl_label='記錄目前影格'
    bl_options={'REGISTER','UNDO'}
    def execute(self,context):
        if not RT.arm or RT.arm!=context.scene.ansha_vmc.armature: self.report({'ERROR'},'請先接收動捕資料');return {'CANCELLED'}
        if not RT.changed_bones and not RT.changed_keys and not (context.scene.ansha_vmc.root_enabled and RT.root): self.report({'ERROR'},'目前沒有可記錄的動捕資料');return {'CANCELLED'}
        frame=context.scene.frame_current
        for name in RT.changed_bones:
            pb=RT.arm.pose.bones.get(name)
            if pb: pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=name)
        for obname,key in RT.changed_keys:
            ob=bpy.data.objects.get(obname)
            if ob and ob.data.shape_keys and key in ob.data.shape_keys.key_blocks:
                ob.data.shape_keys.key_blocks[key].keyframe_insert(data_path='value',frame=frame)
        if context.scene.ansha_vmc.root_enabled:
            RT.arm.keyframe_insert(data_path='location',frame=frame);RT.arm.keyframe_insert(data_path='rotation_quaternion',frame=frame)
        return {'FINISHED'}
class ANSHA_VMC_OT_BlendAdd(bpy.types.Operator):
    bl_idname='ansha_vmc.blend_add';bl_label='新增表情映射'
    def execute(self,context):
        s=context.scene.ansha_vmc
        if len(s.blends)>=128: self.report({'ERROR'},'表情映射最多128項');return {'CANCELLED'}
        row=s.blends.add();row.source='Custom';s.blend_index=len(s.blends)-1;return {'FINISHED'}
class ANSHA_VMC_OT_BlendDelete(bpy.types.Operator):
    bl_idname='ansha_vmc.blend_delete';bl_label='刪除表情映射'
    def execute(self,context):
        s=context.scene.ansha_vmc
        if s.blends: s.blends.remove(min(s.blend_index,len(s.blends)-1));s.blend_index=max(0,min(s.blend_index,len(s.blends)-1))
        return {'FINISHED'}
class ANSHA_VMC_UL_Bones(bpy.types.UIList):
    def draw_item(self,context,layout,data,item,icon,active_data,active_propname,index):
        row=layout.row(align=True);row.prop(item,'enabled',text='');row.label(text=item.source);row.label(text=item.target or '—')
class ANSHA_VMC_UL_Blends(bpy.types.UIList):
    def draw_item(self,context,layout,data,item,icon,active_data,active_propname,index):
        row=layout.row();row.label(text=item.source);row.label(text=item.target or '自動／VRM')
class ANSHA_VMC_PT_Main(bpy.types.Panel):
    bl_label='Anshaer Web VMC v0.2';bl_idname='ANSHA_VMC_PT_main';bl_space_type='VIEW_3D';bl_region_type='UI';bl_category='Anshaer VMC'
    def draw(self,context):
        l=self.layout;s=context.scene.ansha_vmc
        target=l.column();target.enabled=not RT.running;target.prop(s,'armature');target.prop(s,'mesh');target.prop(s,'port');target.operator('ansha_vmc.auto_map')
        row=l.row(align=True);row.operator('ansha_vmc.stop' if RT.running else 'ansha_vmc.start');row.operator('ansha_vmc.restore')
        l.label(text=s.status)
        row=l.row(align=True);row.prop(s,'bones_enabled');row.prop(s,'blends_enabled')
        l.prop(s,'coordinates');l.prop(s,'rest_axes');l.prop(s,'smooth');l.prop(s,'fps');l.prop(s,'root_enabled')
        if s.root_enabled:l.prop(s,'root_gain')
        row=l.row(align=True);row.operator('ansha_vmc.calibrate');row.operator('ansha_vmc.reset_calibration')
        l.operator('ansha_vmc.keyframe')
        l.prop(s,'show_bones',icon='TRIA_DOWN' if s.show_bones else 'TRIA_RIGHT',emboss=False)
        if s.show_bones:
            box=l.box();box.template_list('ANSHA_VMC_UL_Bones','',s,'bones',s,'bone_index',rows=5)
            if 0<=s.bone_index<len(s.bones):
                row=s.bones[s.bone_index];box.prop(row,'source');
                if s.armature:box.prop_search(row,'target',s.armature.data,'bones')
                else:box.prop(row,'target')
                box.prop(row,'inverse');axes=box.row(align=True);axes.prop(row,'axis_x');axes.prop(row,'axis_y');axes.prop(row,'axis_z');box.prop(row,'offset')
                if len({a[-1] for a in (row.axis_x,row.axis_y,row.axis_z)})<3:box.label(text='XYZ 不可重複使用同一軸',icon='ERROR')
                item=RT.bones.get(row.source)
                box.label(text='目前有訊號' if item else '目前無訊號')
        l.prop(s,'show_blends',icon='TRIA_DOWN' if s.show_blends else 'TRIA_RIGHT',emboss=False)
        if s.show_blends:
            box=l.box();box.template_list('ANSHA_VMC_UL_Blends','',s,'blends',s,'blend_index',rows=5)
            row=box.row(align=True);row.operator('ansha_vmc.blend_add');row.operator('ansha_vmc.blend_delete')
            if 0<=s.blend_index<len(s.blends):
                row=s.blends[s.blend_index];box.prop(row,'source');box.prop(row,'target');box.prop(row,'mode');box.prop(row,'gain')
                box.label(text=f'目前值：{RT.blends.get(row.source,(0,0))[0]:.3f}')
        l.separator();l.label(text='Copyright © 2026-2030 anshaer.com.',icon='NONE')
        l.label(text='All rights reserved.')

CLASSES=(ANSHA_VMC_Bone,ANSHA_VMC_Blend,ANSHA_VMC_Settings,ANSHA_VMC_OT_Start,ANSHA_VMC_OT_Stop,
    ANSHA_VMC_OT_Map,ANSHA_VMC_OT_Calibrate,ANSHA_VMC_OT_ResetCal,ANSHA_VMC_OT_Restore,ANSHA_VMC_OT_Keyframe,
    ANSHA_VMC_OT_BlendAdd,ANSHA_VMC_OT_BlendDelete,ANSHA_VMC_UL_Bones,ANSHA_VMC_UL_Blends,ANSHA_VMC_PT_Main)
def register():
    for cls in CLASSES:bpy.utils.register_class(cls)
    bpy.types.Scene.ansha_vmc=PointerProperty(type=ANSHA_VMC_Settings)
    if on_load not in bpy.app.handlers.load_pre:bpy.app.handlers.load_pre.append(on_load)
def unregister():
    RT.stop(clear=True)
    if on_load in bpy.app.handlers.load_pre:bpy.app.handlers.load_pre.remove(on_load)
    if hasattr(bpy.types.Scene,'ansha_vmc'):del bpy.types.Scene.ansha_vmc
    for cls in reversed(CLASSES):bpy.utils.unregister_class(cls)
