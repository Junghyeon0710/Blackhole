"""
README "블랙홀 너머엔 또 다른 우주" GIF 만들기.

Unity 에서 FrameRecorder 로 녹화한 장면(Recordings/clip4_universe: 태양 발사 → 블랙홀 → 다음 우주가 퍼짐 → 도착 알림)을
make_highlight.make_gif 와 같은 방식(구간별 MEDIANCUT 팔레트, 30fps)으로 docs/media/universe.gif 로 만든다.

사용법 (프로젝트 루트에서):  python Tools/Video/make_universe_gif.py
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
from make_highlight import make_gif  # noqa: E402

CLIP = os.path.join(ROOT, "Recordings", "clip4_universe")
OUT = os.path.join(ROOT, "docs", "media", "universe.gif")
LEAD_IN = 0.5  # 발사대에 태양이 올라와 있는 모습부터 (초)


def main():
    with open(os.path.join(CLIP, "events.json"), encoding="utf-8") as f:
        log = json.load(f)
    fps = log["fps"]
    drop = next(e["f"] for e in log["events"] if e["t"] == "drop")
    first = max(0, drop - round(LEAD_IN * fps))
    make_gif(CLIP, OUT, fps=fps, width=400, step=max(1, fps // 30), chunk_seconds=1.0, first=first)


if __name__ == "__main__":
    main()
