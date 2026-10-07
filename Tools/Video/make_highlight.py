"""
하이라이트 영상 만들기.

1) Unity 에서 FrameRecorder 로 녹화한 장면(Recordings/<clip>/00000.jpg …, events.json)을
2) edit.json 의 대본(구간, 속도, 자막)대로 이어 붙일 프레임 목록과 효과음 트랙(WAV)으로 바꾸고
3) Blender(백그라운드)로 컷·크로스페이드·자막·엔딩 카드를 붙여 MP4 를 만든 뒤
4) README 에 바로 재생되는 GIF 를 Pillow 로 만든다.

사용법 (프로젝트 루트에서):  python Tools/Video/make_highlight.py
Blender 위치가 다르면 BLENDER 환경 변수로 지정한다.
"""
import json
import os
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


def resolve(value, events, frames):
    """'over-150', 'end', 숫자를 프레임 번호로."""
    if isinstance(value, int):
        return value
    if value == "end":
        return frames
    over = next(e["f"] for e in events if e["t"] == "over")
    if value.startswith("over"):
        return over + int(value[4:] or 0)
    raise ValueError(value)


def build_plan(edit):
    fps, fade = edit["fps"], edit["crossfade"]
    segments, start = [], 1
    track_events = []
    for i, seg in enumerate(edit["segments"]):
        folder = os.path.join(REC, seg["clip"])
        log = sfx.load_events(folder)
        a = resolve(seg["from"], log["events"], log["frames"])
        b = resolve(seg["to"], log["events"], log["frames"])
        repeat = max(1, round(1 / seg["speed"]))
        files = [f"{f:05d}.jpg" for f in range(a, b) for _ in range(repeat)]
        if i > 0:
            start -= fade  # 앞 구간과 겹쳐서 크로스페이드
        segments.append({
            "dir": folder, "files": files, "start": start, "channel": 1 + i % 2,
            "caption": seg["caption"],
        })
        for e in log["events"]:
            if a <= e["f"] < b:
                track_events.append(((start - 1) / fps + (e["f"] - a) * repeat / fps, e))
        start += len(files)
    end_frames = int(edit["end_card"]["seconds"] * fps)
    end_start = start - fade
    total = end_start + end_frames - 1
    return segments, track_events, end_start, end_frames, total


def main():
    with open(os.path.join(HERE, "edit.json"), encoding="utf-8") as f:
        edit = json.load(f)
    os.makedirs(WORK, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    segments, events, end_start, end_frames, total = build_plan(edit)
    fps = edit["fps"]

    # 효과음 + 잔잔한 배경음
    track = sfx.Track(total / fps + 1)
    track.pad(0, total / fps)
    for at, e in events:
        sfx.play_event(track, at, e)
    wav = os.path.join(WORK, "audio.wav")
    track.save(wav)

    plan = {
        "fps": fps, "width": edit["width"], "height": edit["height"], "crossfade": edit["crossfade"],
        "font": os.path.join(ROOT, edit["font"]), "art": os.path.join(ROOT, "Assets", "_Game", "Art"),
        "segments": segments, "end_start": end_start, "end_frames": end_frames, "end_card": edit["end_card"],
        "total": total, "audio": wav,
        "mp4": os.path.join(OUT_DIR, "highlight.mp4"),
        "frames": os.path.join(WORK, "gif_frames") + os.sep,
    }
    plan_path = os.path.join(WORK, "plan.json")
    with open(plan_path, "w", encoding="utf-8") as f:
        json.dump(plan, f, ensure_ascii=False)
    print(f"구간 {len(segments)}개, 총 {total}프레임 ({total / fps:.1f}초), 효과음 {len(events)}개")

    subprocess.run([BLENDER, "--background", "--factory-startup", "--python",
                    os.path.join(HERE, "blender_edit.py"), "--", plan_path], check=True)

    make_gif(plan["frames"], os.path.join(OUT_DIR, "highlight.gif"))


def make_gif(frame_dir, path, width=270, step=3):
    """10fps GIF. 모든 프레임이 팔레트 하나(256색)를 같이 써야 프레임 사이 압축이 잘 되어 용량이 절반이 된다."""
    files = sorted(f for f in os.listdir(frame_dir) if f.endswith(".jpg"))[::step]
    frames = []
    for name in files:
        im = Image.open(os.path.join(frame_dir, name)).convert("RGB")
        frames.append(im.resize((width, round(im.height * width / im.width)), Image.LANCZOS))
    samples = frames[::3]
    h = frames[0].height
    strip = Image.new("RGB", (width, h * len(samples)))
    for i, im in enumerate(samples):
        strip.paste(im, (0, i * h))
    palette = strip.quantize(colors=256, method=Image.Quantize.MAXCOVERAGE)
    q = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in frames]
    q[0].save(path, save_all=True, append_images=q[1:], duration=round(1000 * step / 30), loop=0, optimize=True)
    print(f"GIF {len(q)}프레임 → {path} ({os.path.getsize(path) / 1e6:.1f} MB)")


if __name__ == "__main__":
    main()
