using System;

namespace Blackhole
{
    /// <summary>
    /// 태양 + 태양 → 블랙홀. 반지름이 12에서 초당 40씩 커져 60에서 멈추고,
    /// 블랙홀 반지름 + 행성 반지름의 30% 안에 들어온 행성을 흡수한다.
    /// 2.6초 뒤 남은 행성까지 모두 흡수하고 0.7초 동안 사라진다.
    /// </summary>
    public sealed class BlackHoleEvent
    {
        public bool Active { get; private set; }
        public bool Done { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double R { get; private set; }
        public double T { get; private set; }
        public double Fade { get; private set; }

        public void Start(double x, double y)
        {
            Active = true;
            Done = false;
            X = x; Y = y;
            R = Tuning.BlackHoleStartRadius;
            T = 0;
            Fade = 1;
        }

        public void Stop() { Active = false; }

        /// <summary>틱마다 한 번. 흡수한 행성마다 onAbsorb 를 부른 뒤 목록에서 뺀다.</summary>
        public void Update(double dt, PhysicsWorld world, Action<Body> onAbsorb)
        {
            if (!Active) return;
            T += dt;
            if (!Done)
            {
                R = Math.Min(Tuning.BlackHoleMaxRadius, Tuning.BlackHoleStartRadius + T * Tuning.BlackHoleGrowSpeed);
                var bodies = world.Bodies;
                for (int i = 0; i < bodies.Count; i++)
                {
                    var b = bodies[i];
                    if (!b.Alive) continue;
                    double d = PhysicsWorld.Hypot(b.X - X, b.Y - Y);
                    if (d < R + b.R * Tuning.BlackHoleAbsorbFactor || T > Tuning.BlackHoleDuration)
                    {
                        b.Alive = false;
                        b.RemoveReason = RemoveReason.Absorbed;
                        onAbsorb?.Invoke(b);
                    }
                }
                world.RemoveDead();
                if (T > Tuning.BlackHoleDuration) Done = true;
            }
            else
            {
                Fade -= dt * Tuning.BlackHoleFadeSpeed;
                if (Fade <= 0) Active = false;
            }
        }
    }
}
