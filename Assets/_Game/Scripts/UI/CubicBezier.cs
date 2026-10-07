using UnityEngine;

namespace Blackhole
{
    /// <summary>CSS cubic-bezier(x1, y1, x2, y2) 이징. 프로토타입의 튀어나오는 팝 애니메이션을 그대로 쓰려고 둔다.</summary>
    public readonly struct CubicBezier
    {
        readonly float x1, y1, x2, y2;

        public CubicBezier(float x1, float y1, float x2, float y2)
        {
            this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2;
        }

        public static readonly CubicBezier Ease = new CubicBezier(0.25f, 0.1f, 0.25f, 1f);
        public static readonly CubicBezier Toast = new CubicBezier(0.3f, 1.6f, 0.5f, 1f);
        public static readonly CubicBezier Pop = new CubicBezier(0.3f, 1.5f, 0.5f, 1f);

        static float Bez(float t, float a, float b) => 3 * a * (1 - t) * (1 - t) * t + 3 * b * (1 - t) * t * t + t * t * t;
        static float BezDx(float t, float a, float b) => 3 * a * (1 - t) * (1 - t) + 6 * (b - a) * (1 - t) * t + 3 * (1 - b) * t * t;

        public float Evaluate(float x)
        {
            x = Mathf.Clamp01(x);
            float t = x;
            for (int i = 0; i < 8; i++)
            {
                float err = Bez(t, x1, x2) - x;
                if (Mathf.Abs(err) < 1e-5f) break;
                float d = BezDx(t, x1, x2);
                if (Mathf.Abs(d) < 1e-6f) break;
                t = Mathf.Clamp01(t - err / d);
            }
            return Bez(t, y1, y2);
        }
    }
}
