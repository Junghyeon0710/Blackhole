using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Blackhole.Tests
{
    /// <summary>
    /// C# 이식본이 프로토타입과 같은 손맛인지 수치로 확인한다.
    /// parity_expected.json 은 Tools/Parity/parity.mjs 가 프로토타입 코드를 그대로 돌려 만든 기준값이다.
    /// 같은 시나리오(행성 놓기, 발사)를 GameSession 으로 돌려 매 기록 시점의 행성 위치·반지름, 점수, 콤보,
    /// 블랙홀, 위험도, 게임 오버가 비트 단위로 같은지 본다.
    /// 실수는 IEEE 비트(16진수 문자열)로 저장되어 있다 (JsonUtility 는 17자리 실수를 정확히 못 읽는다).
    /// </summary>
    public class PhysicsParityTests
    {
#pragma warning disable 0649
        [Serializable] class ParityFile { public Scenario[] scenarios; }
        [Serializable] class Scenario
        {
            public string name;
            public int ticks;
            public SpawnStep[] spawns;
            public LaunchStep[] launches;
            public bool[] launched;
            public LaunchState[] launchStates;
            public Snapshot[] snapshots;
        }
        [Serializable] class SpawnStep { public int tick, tier; public string x, y; }
        [Serializable] class LaunchStep { public int tick, tier; public string angle; }
        [Serializable] class LaunchState { public string x, y, px, py; }
        [Serializable] class Snapshot
        {
            public int tick, score, combo, topTier;
            public bool over, blackHole;
            public string danger, bhX, bhY, bhR, bhFade;
            public BodyState[] bodies;
        }
        [Serializable] class BodyState { public int tier; public string x, y, r; }
#pragma warning restore 0649

        static ParityFile s_File;

        static ParityFile Load()
        {
            if (s_File != null) return s_File;
            var path = Path.Combine(Application.dataPath, "_Game/Tests/EditMode/parity_expected.json");
            s_File = JsonUtility.FromJson<ParityFile>(File.ReadAllText(path));
            return s_File;
        }

        static double D(string hex) => BitConverter.Int64BitsToDouble(unchecked((long)Convert.ToUInt64(hex, 16)));

        static IEnumerable<string> ScenarioNames() => new[] { "launches", "blackhole", "overflow" };

        [TestCaseSource(nameof(ScenarioNames))]
        public void MatchesPrototype(string name)
        {
            var sc = Array.Find(Load().scenarios, s => s.name == name);
            Assert.NotNull(sc, "시나리오가 없습니다: " + name);

            var session = new GameSession(TierTable.Default, new System.Random(1).NextDouble);
            session.Reset();
            var launched = new List<bool>();
            int next = 0, launchIndex = 0;
            double maxLaunchDiff = 0;
            for (int k = 0; k < sc.ticks; k++)
            {
                foreach (var s in sc.spawns) if (s.tick == k) session.Spawn(s.tier, D(s.x), D(s.y));
                foreach (var l in sc.launches)
                {
                    if (l.tick != k) continue;
                    session.AimAngle = D(l.angle);
                    session.Spawner.Current = l.tier;
                    bool ok = session.Launch();
                    launched.Add(ok);
                    if (!ok) continue;
                    // 자바스크립트와 .NET 의 cos·sin 은 마지막 자리가 다를 수 있다. 차이를 재 두고 프로토타입 값에서 출발한다
                    var b = session.World.Bodies[session.World.Bodies.Count - 1];
                    var js = sc.launchStates[launchIndex++];
                    double x = D(js.x), y = D(js.y);
                    maxLaunchDiff = Math.Max(maxLaunchDiff, Math.Max(Math.Abs(x - b.X), Math.Abs(y - b.Y)));
                    b.X = x; b.Y = y; b.PX = D(js.px); b.PY = D(js.py);
                }
                session.Tick(1.0 / 60);
                if (next < sc.snapshots.Length && sc.snapshots[next].tick == k + 1)
                    Compare(sc.snapshots[next++], session, name);
            }
            Assert.AreEqual(sc.snapshots.Length, next, "확인한 시점 수");
            CollectionAssert.AreEqual(sc.launched, launched, "발사 성공 여부");
            Assert.Less(maxLaunchDiff, 1e-9, "발사 위치 (cos·sin 마지막 자리 차이만 허용)");
        }

        static void Compare(Snapshot want, GameSession got, string scenario)
        {
            string at = $"[{scenario} @ {want.tick}틱]";
            var bodies = got.World.Bodies;
            Assert.AreEqual(want.bodies.Length, bodies.Count, at + " 행성 수");
            for (int i = 0; i < bodies.Count; i++)
            {
                var w = want.bodies[i];
                var b = bodies[i];
                Assert.AreEqual(w.tier, b.Tier, $"{at} {i}번 행성 단계");
                Assert.AreEqual(D(w.x), b.X, $"{at} {i}번 행성 x");
                Assert.AreEqual(D(w.y), b.Y, $"{at} {i}번 행성 y");
                Assert.AreEqual(D(w.r), b.R, $"{at} {i}번 행성 반지름");
            }
            Assert.AreEqual(want.score, got.Score.Score, at + " 점수");
            Assert.AreEqual(want.combo, got.Score.Combo, at + " 콤보");
            Assert.AreEqual(want.topTier, got.TopTier, at + " 최고 단계");
            Assert.AreEqual(want.over, got.IsOver, at + " 게임 오버");
            Assert.AreEqual(D(want.danger), got.Danger.Level, at + " 위험도");
            Assert.AreEqual(want.blackHole, got.BlackHole.Active, at + " 블랙홀");
            if (want.blackHole)
            {
                Assert.AreEqual(D(want.bhX), got.BlackHole.X, at + " 블랙홀 x");
                Assert.AreEqual(D(want.bhY), got.BlackHole.Y, at + " 블랙홀 y");
                Assert.AreEqual(D(want.bhR), got.BlackHole.R, at + " 블랙홀 반지름");
                Assert.AreEqual(D(want.bhFade), got.BlackHole.Fade, at + " 블랙홀 사라짐");
            }
        }
    }
}
