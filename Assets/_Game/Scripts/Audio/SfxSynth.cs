using System;
using UnityEngine;

namespace Blackhole
{
    public enum Wave
    {
        Sine,
        Triangle,
        Square,
        Sawtooth,
    }

    /// <summary>
    /// 프로토타입의 Web Audio 합성음(Sound.tone)을 그대로 PCM 으로 굽는다.
    /// 주파수는 지수 곡선으로 미끄러지고, 소리 크기는 0.012초에 올라갔다가 dur 에 꺼진다.
    /// 출시 전에는 AudioManager 의 덮어쓰기 칸에 실제 효과음을 넣으면 된다.
    /// </summary>
    public sealed class SfxBuilder
    {
        const double Floor = 0.0001;
        const double Attack = 0.012;
        const double StopTail = 0.03;

        readonly int rate;
        float[] buffer = Array.Empty<float>();

        public SfxBuilder(int sampleRate) { rate = sampleRate; }

        public SfxBuilder Tone(double freq, double dur, Wave wave = Wave.Sine, double vol = 0.15, double slide = 1, double delay = 0)
        {
            int start = (int)Math.Round(delay * rate);
            int length = (int)Math.Ceiling((dur + StopTail) * rate);
            Ensure(start + length);
            double f0 = freq, f1 = Math.Max(30, freq * slide);
            double phase = 0;
            for (int i = 0; i < length; i++)
            {
                double t = (double)i / rate;
                double f = slide == 1 ? f0 : t < dur ? f0 * Math.Pow(f1 / f0, t / dur) : f1;
                double g = t < Attack
                    ? Floor * Math.Pow(vol / Floor, t / Attack)
                    : t < dur ? vol * Math.Pow(Floor / vol, (t - Attack) / (dur - Attack)) : Floor;
                double dt = f / rate;
                buffer[start + i] += (float)(Sample(wave, phase, dt) * g);
                phase += dt;
                if (phase >= 1) phase -= 1;
            }
            return this;
        }

        void Ensure(int length)
        {
            if (buffer.Length >= length) return;
            Array.Resize(ref buffer, length);
        }

        static double Sample(Wave wave, double p, double dt)
        {
            switch (wave)
            {
                case Wave.Triangle:
                    return 1 - 4 * Math.Abs(p - 0.5);
                case Wave.Square:
                    return (p < 0.5 ? 1 : -1) + PolyBlep(p, dt) - PolyBlep((p + 0.5) % 1, dt);
                case Wave.Sawtooth:
                    return 2 * p - 1 - PolyBlep(p, dt);
                default:
                    return Math.Sin(2 * Math.PI * p);
            }
        }

        // 톱니·사각파의 계단에서 생기는 앨리어싱을 줄인다 (Web Audio 발진기는 대역 제한이라 맑게 들린다)
        static double PolyBlep(double t, double dt)
        {
            if (t < dt) { t /= dt; return t + t - t * t - 1; }
            if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
            return 0;
        }

        public AudioClip Build(string name)
        {
            var clip = AudioClip.Create(name, Math.Max(1, buffer.Length), 1, rate, false);
            clip.SetData(buffer, 0);
            return clip;
        }
    }
}
