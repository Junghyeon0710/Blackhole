using System;
using System.Collections.Generic;

namespace Blackhole
{
    /// <summary>
    /// 위험 판정 (틱마다). 바깥 끝이 249를 넘고, 생긴 지 1.4초가 지났고, 속도가 140 /s 미만이면
    /// 그 행성의 위험 시간이 쌓인다. 조건을 벗어나면 2배 속도로 줄어든다.
    /// 어느 한 행성이라도 2.4초가 되면 게임 오버.
    /// </summary>
    public sealed class DangerMonitor
    {
        /// <summary>가장 위험한 행성의 위험 시간 ÷ 2.4. 1 이상이면 게임 오버.</summary>
        public double Level { get; set; }
        public double Worst { get; private set; }
        public bool Exceeded => Worst >= Tuning.DangerLimit;
        /// <summary>남은 시간 (위험! 1.8 표시용).</summary>
        public double Remaining => Math.Max(0, Tuning.DangerLimit * (1 - Math.Min(1, Level)));

        public void Reset()
        {
            Level = 0;
            Worst = 0;
        }

        public void Update(IReadOnlyList<Body> bodies, double dt, bool blackHoleActive)
        {
            double worst = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                bool outside = PhysicsWorld.Hypot(b.X, b.Y) + b.R > Tuning.DangerRadius + Tuning.DangerMargin;
                double speed = PhysicsWorld.Hypot(b.X - b.PX, b.Y - b.PY) * Tuning.SubSteps * 60;
                if (outside && b.Age > Tuning.DangerMinAge && speed < Tuning.DangerMaxSpeed && !blackHoleActive)
                    b.DangerT += dt;
                else
                    b.DangerT = Math.Max(0, b.DangerT - dt * Tuning.DangerRecoverRate);
                worst = Math.Max(worst, b.DangerT);
            }
            Worst = worst;
            Level = worst / Tuning.DangerLimit;
        }
    }
}
