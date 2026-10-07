"""
하이라이트 영상 만들기.

1) Unity 에서 FrameRecorder 로 녹화한 장면(Recordings/<clip>/00000.jpg …, events.json)을
2) edit.json 의 대본(구간, 속도, 자막)대로 이어 붙일 프레임 목록과 효과음 트랙(WAV)으로 바꾸고
3) Blender(백그라운드)로 컷·크로스페이드·자막·엔딩 카드를 붙여 MP4(docs/media/highlight.mp4)를 만들고
4) README 맨 위에 넣는 고화질 GIF(docs/media/highlight.gif)를 만든다.

대본의 시점은 초 단위 숫자, "end", 또는 이벤트 기준 "click+0.8", "combo-1.8", "launch", "blackhole+3.4", "over-4.0" 처럼 쓴다.
(combo = 그 장면에서 콤보가 가장 높았던 합체)

사용법 (프로젝트 루트에서):  python Tools/Video/make_highlight.py
Blender 위치가 다르면 BLENDER 환경 변수로 지정한다.
"""
import json
import os
import re
import subprocess
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
import sfx  # noqa: E402

REC = os.path.join(ROOT, "Recordings")
WORK = os.path.join(REC, "highlight")
OUT_DIR = os.path.join(ROOT, "docs", "media")
BLENDER = os.environ.get("BLENDER", r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe")


def marker_frame(name, events):
    if name == "combo":
        merges = [e for e in events if e["t"] == "merge"]
        best = max(e["combo"] for e in merges)
        return next(e["f"] for e in merges if e["combo"] == best)
    kind = {"click": "click", "launch": "drop", "blackhole": "blackhole", "over": "over"}[name]
    return next(e["f"] for e in events if e["t"] == kind)


def resolve(value, log):
    """시점을 녹화 프레임 번호로."""
    fps = log["fps"]
    if isinstance(value, (int, float)):
        return round(value * fps)
    if value == "end":
        return log["frames"]
    m = re.fullmatch(r"([a-z]+)([+-][0-9.]+)?", value)
    base = marker_frame(m.group(1), log["events"])
    return base + round(float(m.group(2) or 0) * fps)


def build_plan(edit):
    out_fps = edit["fps"]
    fade = round(edit["crossfade"] * out_fps)
    segments, track_events, start = [], [], 1
    for i, seg in enumerate(edit["segments"]):
        folder = os.path.join(REC, seg["clip"])
        log = sfx.load_events(folder)
        a, b = resolve(seg["from"], log), resolve(seg["to"], log)
        a, b = max(0, a), min(log["frames"], b)
        repeat = max(1, round(out_fps / (log["fps"] * seg["speed"])))
        files = [f"{f:05d}.jpg" for f in range(a, b) for _ in range(repeat)]
        if i > 0:
            start -= fade  # 앞 구간과 겹쳐서 크로스페이드
        segments.append({"dir": folder, "files": files, "start": start, "channel": 1 + i % 2, "caption": seg["caption"]})
        for e in log["events"]:
            if a <= e["f"] < b:
                track_events.append(((start - 1) / out_fps + (e["f"] - a) * repeat / out_fps, e))
        start += len(files)
    end_frames = round(edit["end_card"]["seconds"] * out_fps)
    end_start = start - fade
    total = end_start + end_frames - 1
    return segments, track_events, fade, end_start, end_frames, total


def make_endcard_bg(path, w, h):
    """엔딩 카드 배경: 게임 배경과 같은 우주색 원형 그라디언트 + 별 (Blender 5.2 단색 스트립은 백그라운드 렌더에서 비어 나온다)."""
    import random
    from PIL import ImageDraw

    def lerp(a, b, t):
        return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))

    center, middle, edge = (0x2b, 0x1f, 0x6b), (0x1a, 0x14, 0x46), (0x0e, 0x0a, 0x2e)
    sw, sh = w // 4, h // 4
    small = Image.new("RGB", (sw, sh))
    px = small.load()
    for y in range(sh):
        for x in range(sw):
            dx = (x + 0.5 - sw * 0.5) / (sw * 1.2)
            dy = (y + 0.5 - sh * 0.45) / (sh * 0.8)
            d = (dx * dx + dy * dy) ** 0.5
            px[x, y] = lerp(center, middle, d / 0.45) if d < 0.45 else lerp(middle, edge, min(1, (d - 0.45) / 0.55))
    img = small.resize((w, h), Image.BICUBIC)
    draw = ImageDraw.Draw(img, "RGBA")
    rnd = random.Random(42)
    for _ in range(170):
        x, y, r = rnd.random() * w, rnd.random() * h, (rnd.random() * 1.3 + 0.3) * w / 400
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(255, 248, 230, int(255 * (0.15 + rnd.random() * 0.5))))
    img.save(path)


def main():
    with open(os.path.join(HERE, "edit.json"), encoding="utf-8") as f:
        edit = json.load(f)
    os.makedirs(WORK, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    segments, events, fade, end_start, end_frames, total = build_plan(edit)
    fps = edit["fps"]

    # 효과음 + 잔잔한 배경음
    track = sfx.Track(total / fps + 1)
    track.pad(0, total / fps)
    for at, e in events:
        sfx.play_event(track, at, e)
    wav = os.path.join(WORK, "audio.wav")
    track.save(wav)

    end_bg = os.path.join(WORK, "endcard_bg.png")
    make_endcard_bg(end_bg, edit["width"], edit["height"])

    frames_dir = os.path.join(WORK, "gif_frames")
    if os.path.isdir(frames_dir):
        for name in os.listdir(frames_dir):
            os.remove(os.path.join(frames_dir, name))
    plan = {
        "fps": fps, "width": edit["width"], "height": edit["height"], "crossfade": fade,
        "font": os.path.join(ROOT, edit["font"]), "art": os.path.join(ROOT, "Assets", "_Game", "Art"),
        "segments": segments, "end_start": end_start, "end_frames": end_frames, "end_card": edit["end_card"],
        "total": total, "audio": wav, "end_bg": end_bg,
        "mp4": os.path.join(OUT_DIR, "highlight.mp4"),
        "frames": frames_dir + os.sep,
    }
    plan_path = os.path.join(WORK, "plan.json")
    with open(plan_path, "w", encoding="utf-8") as f:
        json.dump(plan, f, ensure_ascii=False)
    print(f"구간 {len(segments)}개, 총 {total}프레임 ({total / fps:.1f}초, {fps}fps), 효과음 {len(events)}개")

    subprocess.run([BLENDER, "--background", "--factory-startup", "--python",
                    os.path.join(HERE, "blender_edit.py"), "--", plan_path], check=True)

    make_gif(frames_dir, os.path.join(OUT_DIR, "highlight.gif"), fps=fps, step=max(1, fps // 30))


def make_gif(frame_dir, path, fps=60, width=480, step=2, chunk_seconds=2.0):
    """
    README 용 GIF (가로 480px, 30fps).
    - 팔레트는 MEDIANCUT 으로 2초 구간마다 새로 뽑는다. MAXCOVERAGE 는 우주 배경 그라디언트를 얼룩지게 뭉갠다.
    - 한 구간 안의 프레임은 팔레트를 같이 써서, 앞 프레임과 같은 부분은 다시 저장하지 않는다 (용량 절약).
    - 디더링은 쓰지 않는다. 프레임마다 점무늬가 바뀌어 용량이 4배가 되고 화면이 지글거린다.
    """
    files = sorted(f for f in os.listdir(frame_dir) if f.endswith(".jpg"))[::step]
    frames = []
    for name in files:
        im = Image.open(os.path.join(frame_dir, name)).convert("RGB")
        frames.append(im.resize((width, round(im.height * width / im.width)), Image.LANCZOS))

    out_fps = fps / step
    chunk = max(1, round(chunk_seconds * out_fps))
    quantized = []
    for c in range(0, len(frames), chunk):
        part = frames[c:c + chunk]
        samples = part[::2]
        h = part[0].height
        strip = Image.new("RGB", (width, h * len(samples)))
        for i, im in enumerate(samples):
            strip.paste(im, (0, i * h))
        palette = strip.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
        quantized += [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in part]

    # GIF 프레임 시간은 10ms 단위라 30fps 는 30·30·40ms 를 번갈아 써서 실제 속도를 맞춘다
    frame_ms = 1000 / out_fps
    durations, t = [], 0.0
    for i in range(len(quantized)):
        nxt = round((t + frame_ms) / 10) * 10
        durations.append(max(10, nxt - round(t / 10) * 10))
        t += frame_ms
    quantized[0].save(path, save_all=True, append_images=quantized[1:], duration=durations, loop=0, optimize=True)
    print(f"GIF {len(quantized)}프레임 {width}px {out_fps:.0f}fps → {path} ({os.path.getsize(path) / 1e6:.1f} MB)")


if __name__ == "__main__":
    main()
