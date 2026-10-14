# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright © 2026-2030 anshaer.com. All rights reserved.
import math
import struct


def parse_packet(data):
    """Bounded OSC decoder. Returns one atomic packet; no partial mutation on error."""
    if not isinstance(data, bytes) or not 4 <= len(data) <= 65507 or len(data) % 4:
        raise ValueError('OSC 尺寸錯誤')
    messages = []

    def parse(p, depth):
        if depth > 3:
            raise ValueError('OSC 巢狀過深')
        at = 0

        def string():
            nonlocal at
            start = at
            end = p.find(b'\0', start)
            if end < 0 or end - start > 1024:
                raise ValueError('OSC 字串錯誤')
            at = (end + 4) & ~3
            if at > len(p) or any(p[end:at]):
                raise ValueError('OSC padding 錯誤')
            return p[start:end].decode('utf-8', errors='strict')

        address = string()
        if address == '#bundle':
            if len(p) < 16:
                raise ValueError('OSC bundle 錯誤')
            at = 16
            while at < len(p):
                if at + 4 > len(p):
                    raise ValueError('OSC bundle 尺寸錯誤')
                size = struct.unpack_from('>I', p, at)[0]
                at += 4
                if size < 4 or size % 4 or size > len(p) - at:
                    raise ValueError('OSC message 尺寸錯誤')
                parse(p[at:at + size], depth + 1)
                at += size
            return
        if len(messages) >= 128 or not address.startswith(('/VMC/Ext/', '/ANSHA/')):
            raise ValueError('OSC 位址或訊息數錯誤')
        tags = string()
        if not tags.startswith(','):
            raise ValueError('OSC tag 錯誤')
        args = []
        for tag in tags[1:]:
            if tag == 's':
                args.append(string())
            elif tag in 'fi':
                if at + 4 > len(p):
                    raise ValueError('OSC 數值截斷')
                value = struct.unpack_from('>' + tag, p, at)[0]
                at += 4
                if tag == 'f' and not math.isfinite(value):
                    raise ValueError('OSC 非有限數值')
                args.append(value)
            else:
                raise ValueError('OSC tag 不支援')
        if at != len(p):
            raise ValueError('OSC 尾端資料錯誤')
        messages.append((address, tags[1:], args))

    parse(data, 0)
    if not messages:
        raise ValueError('OSC 空封包')
    return messages


def decode_updates(messages):
    bones, blends, root = {}, {}, None
    apply = False
    for address, tags, args in messages:
        if address in ('/VMC/Ext/Bone/Pos', '/VMC/Ext/Root/Pos') and tags == 'sfffffff':
            name = args[0]
            if not name or len(name) > 128:
                raise ValueError('骨骼名稱過長')
            values = args[1:]
            if any(abs(x) > 100000 for x in values):
                raise ValueError('骨骼數值超過範圍')
            q = values[3:]
            norm = math.sqrt(sum(x*x for x in q))
            if not .1 <= norm <= 2:
                raise ValueError('骨骼 quaternion 錯誤')
            item = (tuple(values[:3]), (q[3]/norm, q[0]/norm, q[1]/norm, q[2]/norm))
            if address.endswith('/Bone/Pos'):
                bones[name] = item
            else:
                root = item
        elif address == '/VMC/Ext/Blend/Val' and tags == 'sf':
            if not args[0] or len(args[0]) > 128 or not 0 <= args[1] <= 1:
                raise ValueError('表情名稱或數值錯誤')
            blends[args[0]] = args[1]
        elif address == '/VMC/Ext/Blend/Apply' and tags == '':
            apply = True
    return bones, blends, root, apply
