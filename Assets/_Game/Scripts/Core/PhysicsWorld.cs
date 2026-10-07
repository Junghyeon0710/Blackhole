using System;
using System.Collections.Generic;

namespace Blackhole
{
    /// <summary>
    /// 기획서 4장의 직접 계산 물리. 원끼리만 계산하고, 마찰·반발 없이 겹친 만큼 밀어낸다.
    /// 프로토타입 step() 을 줄 단위로 옮겼다. 연산 순서까지 같게 두어야 손맛이 같다.
    /// </summary>
    public sealed class PhysicsWorld
    {
        readonly List<Body> bodies = new List<Body>(128);
        readonly List<Body> mergeA = new List<Body>(8);
        readonly List<Body> mergeB = new List<Body>(8);

        public IReadOnlyList<Body> Bodies => bodies;

        /// <summary>같은 단계 두 행성이 닿았을 때. 서브스텝의 경계 처리까지 끝난 뒤 부른다.</summary>
        public Action<Body, Body> MergeHandler;

        public event Action<Body> BodyAdded;
        public event Action<Body> BodyRemoved;

        public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

        public void Add(Body body)
        {
            bodies.Add(body);
            BodyAdded?.Invoke(body);
        }

        /// <summary>서브스텝 한 번: 적분 → 충돌·합체 감지 → 경계 → 합체 처리.</summary>
        public void Step(double h, double gx, double gy, double gStr, bool allowMerge)
        {
            int n = bodies.Count;
            for (int i = 0; i < n; i++)
            {
                var b = bodies[i];
                if (!b.Alive) continue;
                double dx = gx - b.X, dy = gy - b.Y, d = Hypot(dx, dy);
                if (d == 0) d = 1e-6;
                double gs = gStr * Math.Min(1, d / Tuning.GravitySoftRadius);
                double vx = (b.X - b.PX) * Tuning.Damping, vy = (b.Y - b.PY) * Tuning.Damping;
                double sp = Hypot(vx, vy), lim = Tuning.MaxSpeed * h;
                if (sp > lim) { vx *= lim / sp; vy *= lim / sp; }
                b.PX = b.X; b.PY = b.Y;
                b.X += vx + dx / d * gs * h * h;
                b.Y += vy + dy / d * gs * h * h;
                if (b.Grow > 0)
                {
                    b.R = Math.Min(b.TargetR, b.R + b.Grow * h);
                    if (b.R >= b.TargetR) b.Grow = 0;
                }
            }

            mergeA.Clear();
            mergeB.Clear();
            for (int i = 0; i < n; i++)
            {
                var a = bodies[i];
                if (!a.Alive) continue;
                for (int j = i + 1; j < n; j++)
                {
                    var b = bodies[j];
                    if (!b.Alive) continue;
                    double dx = b.X - a.X, dy = b.Y - a.Y, rr = a.R + b.R, d2 = dx * dx + dy * dy;
                    if (d2 >= rr * rr) continue;
                    if (allowMerge && a.Tier == b.Tier && !a.Merging && !b.Merging)
                    {
                        a.Merging = b.Merging = true;
                        mergeA.Add(a);
                        mergeB.Add(b);
                        continue;
                    }
                    double d = Math.Sqrt(d2);
                    if (d == 0) d = 1e-4;
                    double nx = dx / d, ny = dy / d, ov = (rr - d) * Tuning.CollisionCorrection;
                    double ma = a.R * a.R, mb = b.R * b.R, m = ma + mb;
                    a.X -= nx * ov * mb / m; a.Y -= ny * ov * mb / m;
                    b.X += nx * ov * ma / m; b.Y += ny * ov * ma / m;
                }
            }

            for (int i = 0; i < n; i++)
            {
                var b = bodies[i];
                if (!b.Alive) continue;
                double d = Hypot(b.X, b.Y);
                if (d + b.R > Tuning.ArenaRadius)
                {
                    double k = (Tuning.ArenaRadius - b.R) / d;
                    b.X *= k; b.Y *= k;
                }
            }

            if (mergeA.Count > 0)
            {
                for (int i = 0; i < mergeA.Count; i++) MergeHandler?.Invoke(mergeA[i], mergeB[i]);
                RemoveDead();
            }
        }

        /// <summary>Alive 가 꺼진 행성을 순서를 지키며 뺀다. 그리는 순서가 목록 순서라서 순서가 중요하다.</summary>
        public void RemoveDead()
        {
            int w = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b.Alive) { bodies[w++] = b; continue; }
                BodyRemoved?.Invoke(b);
            }
            if (w < bodies.Count) bodies.RemoveRange(w, bodies.Count - w);
        }

        public void Clear()
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Alive = false;
                bodies[i].RemoveReason = RemoveReason.Reset;
            }
            RemoveDead();
        }
    }
}
