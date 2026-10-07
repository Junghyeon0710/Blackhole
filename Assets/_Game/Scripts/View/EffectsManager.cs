using System.Collections.Generic;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 합체 파티클, 퍼지는 고리, 화면 흔들림. 값은 1/60초 틱마다 갱신하고(프로토타입과 같은 감속),
    /// 그리기는 한 메시로 한다. 흔들림은 World 루트를 움직여서 배경은 흔들리지 않는다.
    /// </summary>
    public sealed class EffectsManager : MonoBehaviour
    {
        [SerializeField] ShapeBatch batch;
        [SerializeField] Transform shakeRoot;

        static readonly Color Ink = new Color32(0xff, 0xf7, 0xec, 255);

        struct Particle
        {
            public float X, Y, VX, VY, Life, T, Size;
            public Color Color;
        }

        struct Ring
        {
            public float X, Y, R, Max, Life, T;
            public Color Color;
        }

        readonly List<Particle> particles = new List<Particle>(256);
        readonly List<Ring> rings = new List<Ring>(16);

        /// <summary>흔들림 세기 (CSS px).</summary>
        public float Shake { get; private set; }

        public void AddShake(float amount) => Shake = Mathf.Max(Shake, amount);
        public void SetShake(float amount) => Shake = amount;

        public void Burst(double x, double y, Color color, int count, double spread)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2, s = 60 + Random.value * 160;
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                particles.Add(new Particle
                {
                    X = (float)x + cos * (float)spread * 0.5f,
                    Y = (float)y + sin * (float)spread * 0.5f,
                    VX = cos * s,
                    VY = sin * s,
                    Life = 0.5f + Random.value * 0.4f,
                    Size = 1.5f + Random.value * 3,
                    Color = Random.value < 0.3f ? Ink : color,
                });
            }
        }

        public void Wave(double x, double y, float radius, float maxRadius, Color color)
        {
            rings.Add(new Ring { X = (float)x, Y = (float)y, R = radius, Max = maxRadius, Life = 0.4f, Color = color });
        }

        public void Clear()
        {
            particles.Clear();
            rings.Clear();
            Shake = 0;
        }

        public void Tick(float dt)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.T += dt;
                p.X += p.VX * dt; p.Y += p.VY * dt;
                p.VX *= 0.94f; p.VY *= 0.94f;
                if (p.T >= p.Life) particles.RemoveAt(i);
                else particles[i] = p;
            }
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var r = rings[i];
                r.T += dt;
                if (r.T >= r.Life) rings.RemoveAt(i);
                else rings[i] = r;
            }
            Shake *= Mathf.Pow(0.02f, dt);
        }

        void LateUpdate()
        {
            if (Shake > 0.3f)
                shakeRoot.localPosition = new Vector3(
                    ViewMetrics.CssToUnity((Random.value - 0.5f) * Shake),
                    ViewMetrics.CssToUnity((Random.value - 0.5f) * Shake), 0);
            else
                shakeRoot.localPosition = Vector3.zero;

            batch.Clear();
            foreach (var r in rings)
            {
                float p = r.T / r.Life;
                var c = r.Color; c.a = 1 - p;
                batch.Ring(new Vector2(r.X, r.Y) * Tuning.WorldToUnity, (r.R + (r.Max - r.R) * p) * Tuning.WorldToUnity,
                    ViewMetrics.CssToUnity(3 * (1 - p) + 1), c);
            }
            foreach (var p in particles)
            {
                var c = p.Color; c.a = 1 - p.T / p.Life;
                batch.Disc(new Vector2(p.X, p.Y) * Tuning.WorldToUnity, ViewMetrics.CssToUnity(p.Size), c);
            }
            batch.Apply();
        }
    }
}
