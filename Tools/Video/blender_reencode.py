"""
Blender(백그라운드)에서 실행: 완성된 MP4 를 GitHub README 에 올릴 크기(720×1280, 60fps, 10MB 이하)로 다시 인코딩한다.
blender --background --factory-startup --python blender_reencode.py -- src.mp4 dst.mp4 [crf]
"""
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
src, dst = args[0], args[1]
crf = int(args[2]) if len(args) > 2 else 20

scene = bpy.context.scene
scene.render.resolution_x, scene.render.resolution_y = 720, 1280
scene.render.resolution_percentage = 100
scene.render.fps = 60
scene.view_settings.view_transform = "Standard"
se = scene.sequence_editor_create()
movie = se.strips.new_movie("video", src, 1, 1, fit_method="FIT")
se.strips.new_sound("audio", src, 2, 1)
scene.frame_start, scene.frame_end = 1, movie.frame_final_duration

r = scene.render
r.image_settings.media_type = "VIDEO"
r.image_settings.file_format = "FFMPEG"
r.ffmpeg.format = "MPEG4"
r.ffmpeg.codec = "H264"
r.ffmpeg.constant_rate_factor = "CUSTOM"
r.ffmpeg.custom_constant_rate_factor = crf
r.ffmpeg.ffmpeg_preset = "BEST"
r.ffmpeg.audio_codec = "AAC"
r.ffmpeg.audio_bitrate = 128
r.ffmpeg.audio_channels = "MONO"
r.filepath = dst
bpy.ops.render.render(animation=True)
