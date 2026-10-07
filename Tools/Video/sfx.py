"""
하이라이트 영상용 효과음 합성. 게임의 SfxSynth(= 프로토타입 Web Audio 합성음)와 같은 공식이다.
FrameRecorder 가 남긴 events.json 의 시점에 효과음을 놓아 WAV 하나로 만든다.
"""
import json
import math
import struct
import wave

RATE = 44100
FLOOR = 0.0001
ATTACK = 0.012


def _blep(t, dt):
    if t < dt:
        t /= dt
        return t + t - t * t - 1
    if t > 1 - dt:
        t = (t - 1) / dt
        return t * t + t + t + 1
    return 0.0


def _sample(wave_type, p, dt):
    if wave_type == "triangle":
        return 1 - 4 * abs(p - 0.5)
    if wave_type == "square":
        return (1 if p < 0.5 else -1) + _blep(p, dt) - _blep((p + 0.5) % 1, dt)
    if wave_type == "sawtooth":
        return 2 * p - 1 - _blep(p, dt)
    return math.sin(2 * math.pi * p)


class Track:
    def __init__(self, seconds):
        self.buf = [0.0] * int(seconds * RATE + RATE)

    def tone(self, at, freq, dur, wave_type="sine", vol=0.15, slide=1.0, delay=0.0):
        start = int((at + delay) * RATE)
        n = int((dur + 0.03) * RATE)
        f0, f1 = freq, max(30.0, freq * slide)
        phase = 0.0
        for i in range(n):
            j = start + i
            if j < 0 or j >= len(self.buf):
                continue
            t = i / RATE
            f = f0 if slide == 1 else (f0 * (f1 / f0) ** (t / dur) if t < dur else f1)
            if t < ATTACK:
                g = FLOOR * (vol / FLOOR) ** (t / ATTACK)
            elif t < dur:
                g = vol * (FLOOR / vol) ** ((t - ATTACK) / (dur - ATTACK))
            else:
                g = FLOOR
            dt = f / RATE
            self.buf[j] += _sample(wave_type, phase, dt) * g
            phase += dt
            if phase >= 1:
                phase -= 1

    # 게임과 같은 소리들 (AudioManager)
    def drop(self, at):
        self.tone(at, 620, 0.09, "triangle", 0.07, 0.55)

    def click(self, at):
        self.tone(at, 760, 0.05, "square", 0.03)

    def merge(self, at, tier, combo):
        k = max(0, min(18, tier + min(combo - 1, 8)))
        f = 300 * 1.1225 ** k
        self.tone(at, f, 0.16, "sine", 0.17, 1.6)
        self.tone(at, f * 1.5, 0.22, "triangle", 0.05, 1.2, 0.03)

    def discover(self, at):
        for i in range(3):
            self.tone(at, 523 * 1.26 ** i, 0.22, "triangle", 0.09, 1, i * 0.09)

    def over(self, at):
        self.tone(at, 330, 0.7, "sawtooth", 0.06, 0.3)

    def blackhole(self, at):
        self.tone(at, 70, 1.8, "sawtooth", 0.11, 0.3)
        self.tone(at, 140, 1.4, "sine", 0.1, 5)

    def pad(self, start, length, root=110.0, vol=0.022):
        """잔잔한 우주 느낌 배경음: 느리게 숨 쉬는 화음."""
        notes = [1.0, 1.5, 2.0, 2.52, 3.0]
        s, e = int(start * RATE), int((start + length) * RATE)
        for j in range(max(0, s), min(len(self.buf), e)):
            t = (j - s) / RATE
            env = min(1.0, t / 1.5, (length - t) / 1.5)
            breath = 0.75 + 0.25 * math.sin(2 * math.pi * t / 6.0)
            v = 0.0
            for i, m in enumerate(notes):
                v += math.sin(2 * math.pi * root * m * t + i) * (0.6 if i == 0 else 0.35)
            self.buf[j] += v * vol * env * breath

    def save(self, path, gain=1.6):
        peak = max(1e-6, max(abs(x) for x in self.buf))
        k = min(gain, 0.95 / peak)
        with wave.open(path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x * k)) * 32767)) for x in self.buf))


def play_event(track, at, ev):
    kind = ev["t"]
    if kind == "drop":
        track.drop(at)
    elif kind == "click":
        track.click(at)
    elif kind == "merge":
        track.merge(at, ev["tier"], ev["combo"])
    elif kind == "discover":
        track.discover(at)
    elif kind == "blackhole":
        track.blackhole(at)
    elif kind == "over":
        track.over(at)


def load_events(folder):
    with open(folder + "/events.json", encoding="utf-8") as f:
        return json.load(f)
