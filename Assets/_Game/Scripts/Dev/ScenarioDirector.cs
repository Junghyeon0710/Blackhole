#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Blackhole.Dev
{
    /// <summary>
    /// 정해 둔 판을 틱 단위로 그대로 재현한다 (에디터 전용, 녹화용).
    /// 물리가 결정적이라 같은 자리에 행성을 놓고 같은 틱에 쏘면 시뮬레이션과 똑같은 판이 나온다.
    /// GameManager 보다 먼저 FixedUpdate 를 돌아 "놓기·쏘기 → 틱" 순서를 지킨다.
    /// 쏘기 사이에는 발사대를 다음 목표 쪽으로 천천히 돌려 사람이 조준하는 것처럼 보이게 한다 (물리에는 영향 없음).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ScenarioDirector : MonoBehaviour
    {
        public struct Spawn
        {
            public int Tick, Tier;
            public double X, Y;
        }

        public struct Shot
        {
            public int Tick, Tier;
            public double Angle;
            /// <summary>0 이상이면 쏘는 순간 이 단계 행성 쪽으로 조준한다.</summary>
            public int AimAtTier;
        }

        readonly List<Spawn> spawns = new List<Spawn>();
        readonly List<Shot> shots = new List<Shot>();
        GameManager game;
        bool running;

        public int Tick { get; private set; }
        public float AimSpeed = 4f; // rad/s

        public void Play(IEnumerable<Spawn> spawnList, IEnumerable<Shot> shotList)
        {
            game = FindAnyObjectByType<GameManager>();
            spawns.Clear();
            spawns.AddRange(spawnList);
            shots.Clear();
            shots.AddRange(shotList);
            shots.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            Tick = 0;
            running = true;
        }

        public void Stop() => running = false;

        void FixedUpdate()
        {
            if (!running || game == null || game.Paused || game.State != GameState.Playing) return;
            var s = game.Session;
            foreach (var p in spawns)
                if (p.Tick == Tick) s.Spawn(p.Tier, p.X, p.Y);

            Shot? upcoming = null;
            foreach (var shot in shots)
            {
                if (shot.Tick < Tick) continue;
                upcoming = shot;
                break;
            }
            if (upcoming is Shot next)
            {
                double target = TargetAngle(s, next);
                s.Spawner.Current = next.Tier; // 대기 행성을 미리 보여 준다
                if (next.Tick == Tick)
                {
                    s.AimAngle = target;
                    s.Launch();
                }
                else
                {
                    double diff = Math.IEEERemainder(target - s.AimAngle, Math.PI * 2);
                    double step = AimSpeed * Tuning.TickDt;
                    s.AimAngle += Math.Abs(diff) <= step ? diff : Math.Sign(diff) * step;
                }
            }
            Tick++;
        }

        static double TargetAngle(GameSession s, Shot shot)
        {
            if (shot.AimAtTier < 0) return shot.Angle;
            foreach (var b in s.World.Bodies)
                if (b.Tier == shot.AimAtTier) return Math.Atan2(b.Y, b.X);
            return shot.Angle;
        }

#pragma warning disable 0649
        [Serializable] class ParityFile { public ParityScenario[] scenarios; }
        [Serializable] class ParityScenario { public string name; public ParitySpawn[] spawns; public ParityLaunch[] launches; }
        [Serializable] class ParitySpawn { public int tick, tier; public string x, y; }
        [Serializable] class ParityLaunch { public int tick, tier; public string angle; }
#pragma warning restore 0649

        static double Bits(string hex) => BitConverter.Int64BitsToDouble(unchecked((long)Convert.ToUInt64(hex, 16)));

        /// <summary>손맛 비교 테스트의 시나리오(Tools/Parity)를 그대로 재생할 수 있게 읽는다.</summary>
        public static (List<Spawn> spawns, List<Shot> shots) LoadParity(string name)
        {
            var path = Path.Combine(Application.dataPath, "_Game/Tests/EditMode/parity_expected.json");
            var file = JsonUtility.FromJson<ParityFile>(File.ReadAllText(path));
            var sc = Array.Find(file.scenarios, x => x.name == name);
            var spawnList = new List<Spawn>();
            foreach (var p in sc.spawns) spawnList.Add(new Spawn { Tick = p.tick, Tier = p.tier, X = Bits(p.x), Y = Bits(p.y) });
            var shotList = new List<Shot>();
            foreach (var l in sc.launches) shotList.Add(new Shot { Tick = l.tick, Tier = l.tier, Angle = Bits(l.angle), AimAtTier = -1 });
            return (spawnList, shotList);
        }
    }
}
#endif
