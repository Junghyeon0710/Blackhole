using System;

namespace Blackhole
{
    public readonly struct MergeEvent
    {
        public readonly Body NewBody;
        public readonly int Tier;
        public readonly double X, Y;
        public readonly int Points;
        public readonly int Combo;

        public MergeEvent(Body newBody, int tier, double x, double y, int points, int combo)
        {
            NewBody = newBody; Tier = tier; X = x; Y = y; Points = points; Combo = combo;
        }
    }

    /// <summary>
    /// 한 판의 규칙 전부 (프로토타입의 tick, launch, doMerge, revive …).
    /// Unity 오브젝트를 모르는 순수 C# 이라서 에디트 모드 테스트에서 프로토타입과 수치를 그대로 비교할 수 있다.
    /// 좌표는 월드 단위, y 는 위쪽이 +.
    /// </summary>
    public sealed class GameSession
    {
        public readonly TierTable Tiers;
        public readonly PhysicsWorld World = new PhysicsWorld();
        public readonly ScoreManager Score = new ScoreManager();
        public readonly DangerMonitor Danger = new DangerMonitor();
        public readonly BlackHoleEvent BlackHole = new BlackHoleEvent();
        public readonly SpawnQueue Spawner;

        readonly Func<double> random;
        int nextId;

        /// <summary>판이 진행 중 (물리가 돈다).</summary>
        public bool IsRunning { get; private set; }
        public bool IsOver { get; private set; }
        public bool Ready { get; private set; } = true;
        public double Cooldown { get; private set; }
        /// <summary>이번 판에서 합체로 만든 가장 높은 단계.</summary>
        public int TopTier { get; private set; }
        /// <summary>조준 각도 (라디안, 경기장 중심 기준, 반시계 +). 처음엔 위쪽.</summary>
        public double AimAngle = Math.PI / 2;

        public bool SwapUsed { get; private set; }
        public bool CleanUsed { get; private set; }
        public bool ReviveUsed { get; private set; }

        public bool CanLaunch => IsRunning && !IsOver && Ready && !BlackHole.Active;

        public event Action<Body> Launched;
        public event Action<MergeEvent> Merged;
        public event Action<double, double> BlackHoleStarted;
        public event Action<Body> Absorbed;
        public event Action<Body> Cleared;
        public event Action Ended;

        public GameSession(TierTable tiers, Func<double> random)
        {
            Tiers = tiers;
            this.random = random;
            Spawner = new SpawnQueue(tiers, random);
            World.MergeHandler = OnMerge;
        }

        /// <summary>새 판 (프로토타입 resetGame).</summary>
        public void Reset()
        {
            World.Clear();
            BlackHole.Stop();
            Score.Reset();
            Danger.Reset();
            TopTier = 0;
            SwapUsed = CleanUsed = ReviveUsed = false;
            Ready = true;
            Cooldown = 0;
            IsOver = false;
            IsRunning = true;
            Spawner.Reset(TopTier);
        }

        /// <summary>메뉴로 돌아가는 등 판을 멈출 때.</summary>
        public void Stop() { IsRunning = false; }

        Body CreateBody(int tier, double x, double y)
        {
            double r = Tiers.Radius(tier);
            return new Body
            {
                Id = nextId++,
                Tier = tier,
                X = x, Y = y, PX = x, PY = y,
                R = r, TargetR = r,
                BlinkT = 1 + random() * 4,
            };
        }

        /// <summary>정해진 자리에 행성을 놓는다 (테스트, 디버그용). 발사와 달리 속도 없이 놓인다.</summary>
        public Body Spawn(int tier, double x, double y)
        {
            var b = CreateBody(tier, x, y);
            World.Add(b);
            return b;
        }

        public bool Launch()
        {
            if (!CanLaunch) return false;
            int tier = Spawner.Current;
            double r = Tiers.Radius(tier), l = Tuning.ArenaRadius - r - 1;
            double ux = Math.Cos(AimAngle), uy = Math.Sin(AimAngle);
            var b = CreateBody(tier, ux * l, uy * l);
            double h = 1.0 / 60 / Tuning.SubSteps;
            b.PX = b.X + ux * Tuning.LaunchSpeed * h;
            b.PY = b.Y + uy * Tuning.LaunchSpeed * h;
            World.Add(b);
            Score.ResetCombo(); // 콤보는 한 번의 발사에서 이어지는 연쇄만 센다
            Spawner.Advance(TopTier);
            Ready = false;
            Cooldown = Tuning.LaunchCooldown;
            Launched?.Invoke(b);
            return true;
        }

        /// <summary>1/60초 한 틱. 서브스텝 8회 뒤 블랙홀, 쿨다운, 콤보, 표정 타이머, 위험 판정 순서.</summary>
        public void Tick(double dt)
        {
            if (!IsRunning || IsOver) return;

            double h = dt / Tuning.SubSteps;
            for (int s = 0; s < Tuning.SubSteps; s++)
            {
                bool bh = BlackHole.Active;
                World.Step(h,
                    bh ? BlackHole.X : 0,
                    bh ? BlackHole.Y : 0,
                    bh ? Tuning.BlackHoleGravity : Tuning.Gravity,
                    !bh);
            }
            BlackHole.Update(dt, World, OnAbsorb);

            if (!Ready)
            {
                Cooldown -= dt;
                if (Cooldown <= 0) Ready = true;
            }
            Score.Tick(dt);

            var bodies = World.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                b.Age += dt;
                if (b.Happy > 0) b.Happy -= dt;
                b.BlinkT -= dt;
                if (b.BlinkT < 0)
                {
                    b.Blinking = 0.13;
                    b.BlinkT = 2 + random() * 4;
                }
                if (b.Blinking > 0) b.Blinking -= dt;
            }
            Danger.Update(bodies, dt, BlackHole.Active);
            if (Danger.Exceeded) EndGame();
        }

        void OnMerge(Body a, Body b)
        {
            a.Alive = b.Alive = false;
            a.RemoveReason = b.RemoveReason = RemoveReason.Merged;
            double ma = a.R * a.R, mb = b.R * b.R, m = ma + mb;
            double x = (a.X * ma + b.X * mb) / m, y = (a.Y * ma + b.Y * mb) / m;
            double vx = ((a.X - a.PX) * ma + (b.X - b.PX) * mb) / m, vy = ((a.Y - a.PY) * ma + (b.Y - b.PY) * mb) / m;
            int nt = a.Tier + 1;
            int combo = Score.RegisterMerge();
            if (nt > Tiers.MaxTier)
            {
                StartBlackHole(x, y);
                return;
            }
            var nb = CreateBody(nt, x, y);
            nb.R = Math.Max(a.R, b.R);
            nb.Grow = (nb.TargetR - nb.R) / Tuning.MergeGrowTime;
            nb.PX = x - vx * Tuning.MergeVelocityKeep;
            nb.PY = y - vy * Tuning.MergeVelocityKeep;
            nb.Age = Tuning.MergedBodyAge;
            nb.Happy = Tuning.HappyTime;
            World.Add(nb);
            int pts = ScoreManager.MergePoints(nt, combo);
            Score.Add(pts);
            if (nt > TopTier) TopTier = nt;
            Merged?.Invoke(new MergeEvent(nb, nt, x, y, pts, combo));
        }

        void StartBlackHole(double x, double y)
        {
            BlackHole.Start(x, y);
            Score.Add(Tuning.BlackHoleBonus);
            BlackHoleStarted?.Invoke(x, y);
        }

        void OnAbsorb(Body b)
        {
            Score.Add(Tuning.ScoreFor(b.Tier) * Tuning.AbsorbScoreMultiplier);
            Absorbed?.Invoke(b);
        }

        void EndGame()
        {
            IsOver = true;
            Ended?.Invoke();
        }

        /// <summary>이어하기 보상: 바깥 끝이 165를 넘는 행성을 모두 없애고 위험 시간을 0으로.</summary>
        public void Revive()
        {
            ReviveUsed = true;
            var bodies = World.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (PhysicsWorld.Hypot(b.X, b.Y) + b.R > Tuning.ReviveClearRadius) Clear(b);
                b.DangerT = 0;
            }
            World.RemoveDead();
            IsOver = false;
            Ready = true;
            Danger.Reset();
        }

        public bool HasSmallBodies()
        {
            var bodies = World.Bodies;
            for (int i = 0; i < bodies.Count; i++)
                if (bodies[i].Tier <= 1) return true;
            return false;
        }

        /// <summary>운석 청소 보상: 운석과 달을 모두 없앤다.</summary>
        public int CleanSmallBodies()
        {
            CleanUsed = true;
            int count = 0;
            var bodies = World.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Tier > 1) continue;
                Clear(bodies[i]);
                count++;
            }
            World.RemoveDead();
            return count;
        }

        /// <summary>행성 교체 보상.</summary>
        public int SwapCurrent()
        {
            SwapUsed = true;
            return Spawner.SwapCurrent();
        }

        void Clear(Body b)
        {
            b.Alive = false;
            b.RemoveReason = RemoveReason.Cleared;
            Cleared?.Invoke(b);
        }
    }
}
