#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Blackhole.Dev
{
    /// <summary>
    /// 자동 플레이 (에디터 전용). 사람처럼 발사대를 목표 각도까지 돌린 뒤 쏜다.
    /// Targeted 면 지금 행성과 같은 단계 행성 쪽을 노리고, 아니면 아무 데나 쏜다.
    /// 합체·블랙홀 연출 확인, 녹화, 판 길이 재기에 쓴다.
    /// </summary>
    public sealed class DemoPlayer : MonoBehaviour
    {
        public bool Targeted = true;
        [Tooltip("발사 사이 최소 시간 (게임 시간, 초)")]
        public float MinInterval = 0.7f;
        [Tooltip("발사대가 도는 빠르기 (rad/s)")]
        public float TurnSpeed = 6f;

        System.Random random = new System.Random();
        GameManager game;
        double target = double.NaN;
        float nextShot;

        public int Launches { get; private set; }

        public void Seed(int seed) => random = new System.Random(seed);

        void Update()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            if (game == null || !game.AcceptsAimInput) return;
            var s = game.Session;
            if (double.IsNaN(target)) target = PickTarget(s);

            double diff = Math.IEEERemainder(target - s.AimAngle, Math.PI * 2);
            double step = TurnSpeed * Time.deltaTime;
            bool arrived = Math.Abs(diff) <= step;
            s.AimAngle += arrived ? diff : Math.Sign(diff) * step;

            if (arrived && s.CanLaunch && Time.time >= nextShot)
            {
                game.Launch();
                Launches++;
                nextShot = Time.time + MinInterval;
                target = double.NaN;
            }
        }

        double PickTarget(GameSession s)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            if (!Targeted) return angle;
            int tier = s.Spawner.Current;
            double best = double.MinValue;
            foreach (var b in s.World.Bodies)
            {
                if (b.Tier != tier) continue;
                // 같은 단계 중 가장자리에 가까운 행성이 맞히기 쉽다
                double d = Math.Sqrt(b.X * b.X + b.Y * b.Y);
                if (d > best) { best = d; angle = Math.Atan2(b.Y, b.X); }
            }
            return angle + (random.NextDouble() - 0.5) * 0.05;
        }
    }
}
#endif
