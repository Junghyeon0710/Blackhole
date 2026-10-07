"""
Blender(백그라운드)에서 실행: make_highlight.py 가 만든 plan.json 대로 비디오 시퀀스 편집기(VSE)에 컷을 깔고
크로스페이드, 자막, 엔딩 카드, 효과음을 붙여 MP4 와 GIF 용 JPG 프레임을 렌더한다.
blender --background --factory-startup --python blender_edit.py -- plan.json
"""
import json
import os
import sys

import bpy

plan_path = sys.argv[sys.argv.index("--") + 1]
with open(plan_path, encoding="utf-8") as f:
    plan = json.load(f)

BUTTER = (1.0, 0.847, 0.369, 1.0)
INK = (1.0, 0.969, 0.925, 1.0)
INK_SOFT = (0.788, 0.753, 0.949, 1.0)

scene = bpy.context.scene
scene.render.resolution_x = plan["width"]
scene.render.resolution_y = plan["height"]
scene.render.resolution_percentage = 100
scene.render.fps = plan["fps"]
scene.frame_start = 1
scene.frame_end = plan["total"]
scene.view_settings.view_transform = "Standard"  # 녹화한 색 그대로
se = scene.sequence_editor_create()
font = bpy.data.fonts.load(plan["font"])
fade = plan["crossfade"]
S = plan["width"] / 720  # 크기는 720px 기준으로 적고 해상도에 맞춰 키운다


def fade_alpha(strip, start, end, length=6, peak=1.0):
    strip.blend_alpha = 0.0
    strip.keyframe_insert("blend_alpha", frame=start)
    strip.blend_alpha = peak
    strip.keyframe_insert("blend_alpha", frame=start + length)
    strip.keyframe_insert("blend_alpha", frame=end - length)
    strip.blend_alpha = 0.0
    strip.keyframe_insert("blend_alpha", frame=end)


def text(name, body, channel, start, length, size, color, y, box=False, shadow=False):
    t = se.strips.new_effect(name, "TEXT", channel, start, length=length)
    t.text = body
    t.font = font
    t.font_size = size
    t.color = color
    t.location = (0.5, y)
    t.anchor_x = "CENTER"
    t.anchor_y = "CENTER"
    t.alignment_x = "CENTER"
    t.use_outline = True
    t.outline_color = (0.05, 0.04, 0.16, 1.0)
    t.outline_width = 0.12
    if box:
        t.use_box = True
        t.box_color = (0.07, 0.05, 0.2, 0.86)
        t.box_margin = 0.03
        t.box_roundness = 0.6
    if shadow:
        t.use_shadow = True
        t.shadow_color = (0.54, 0.31, 0.0, 1.0)
        t.shadow_offset = 0.06
        t.shadow_angle = 1.57
    t.blend_type = "ALPHA_OVER"
    return t


# 1) 녹화 구간들: 채널 1·2 를 번갈아 쓰고, 겹치는 부분에 크로스페이드
prev = None
for i, seg in enumerate(plan["segments"]):
    files = seg["files"]
    strip = se.strips.new_image(f"seg{i}", os.path.join(seg["dir"], files[0]), seg["channel"], seg["start"])
    for name in files[1:]:
        strip.elements.append(name)
    if prev is not None:
        se.strips.new_effect(f"fade{i}", "CROSS", 3, seg["start"], length=fade, input1=prev, input2=strip)
    prev = strip
    if seg["caption"]:
        cap_start = seg["start"] + (fade if i > 0 else 0)
        cap_end = seg["start"] + len(files) - (fade if i < len(plan["segments"]) - 1 else 0)
        cap = text(f"cap{i}", seg["caption"], 6, cap_start, cap_end - cap_start, 50 * S, BUTTER, 0.055, box=True)
        fade_alpha(cap, cap_start, cap_end, length=round(6 * plan["fps"] / 30))

# 2) 엔딩 카드: 배경색 + 시작 화면과 같은 로고(화성, 웃는 태양, 지구) + 제목
end_start, end_len = plan["end_start"], plan["end_frames"]
bg = se.strips.new_image("end_bg", plan["end_bg"], 7, end_start)
bg.duration = end_len
bg.blend_type = "ALPHA_OVER"
bg.blend_alpha = 0.0
bg.keyframe_insert("blend_alpha", frame=end_start)
bg.blend_alpha = 1.0
bg.keyframe_insert("blend_alpha", frame=end_start + fade)

art = plan["art"]
logo = [  # (그림, 얼굴, x 오프셋, y 오프셋, 반지름 px)
    ("Planets/planet_03.png", "Faces/face_normal.png", -150, 170, 46),
    ("Planets/planet_10.png", "Faces/face_happy.png", 0, 205, 84),
    ("Planets/planet_05.png", "Faces/face_normal.png", 150, 165, 52),
]
ch = 8
for k, (body, face, ox, oy, radius) in enumerate(logo):
    for path, unit in ((body, None), (face, 128)):
        img = se.strips.new_image(f"logo{k}_{ch}", os.path.join(art, path), ch, end_start + 4)
        img.duration = end_len - 4
        img.blend_type = "ALPHA_OVER"
        w = img.elements[0].orig_width
        # 행성 그림은 반지름 = PPU(px) 규칙: 크기 비율 = 원하는 반지름 / 그림 반지름
        src_radius = unit if unit else {"planet_03.png": 78, "planet_10.png": 276, "planet_05.png": 117}[os.path.basename(path)]
        s = radius / src_radius
        img.transform.scale_x = img.transform.scale_y = s * S
        img.transform.offset_x = ox * S
        img.transform.offset_y = oy * S
        fade_alpha(img, end_start + 4, end_start + end_len, length=8)
        ch += 1

title = text("end_title", plan["end_card"]["title"], ch, end_start + 6, end_len - 6, 104 * S, BUTTER, 0.47, shadow=True)
fade_alpha(title, end_start + 6, end_start + end_len, length=8)
sub = text("end_sub", plan["end_card"]["subtitle"], ch + 1, end_start + 10, end_len - 10, 40 * S, INK_SOFT, 0.38)
fade_alpha(sub, end_start + 10, end_start + end_len, length=8)

# 3) 효과음
se.strips.new_sound("audio", plan["audio"], 30, 1)

# 4) MP4 (H.264 + AAC)
r = scene.render
r.image_settings.media_type = "VIDEO"
r.image_settings.file_format = "FFMPEG"
r.ffmpeg.format = "MPEG4"
r.ffmpeg.codec = "H264"
r.ffmpeg.constant_rate_factor = "CUSTOM"
r.ffmpeg.custom_constant_rate_factor = 17  # 거의 무손실
r.ffmpeg.ffmpeg_preset = "BEST"
r.ffmpeg.audio_codec = "AAC"
r.ffmpeg.audio_bitrate = 192
r.ffmpeg.audio_channels = "MONO"
r.filepath = plan["mp4"]
bpy.ops.render.render(animation=True)

# 5) GIF 용 프레임 (절반 크기 JPG)
r.image_settings.media_type = "IMAGE"
r.image_settings.file_format = "JPEG"
r.image_settings.quality = 92
r.resolution_percentage = 50
r.filepath = plan["frames"]
bpy.ops.render.render(animation=True)
print("BLENDER_EDIT_DONE")
