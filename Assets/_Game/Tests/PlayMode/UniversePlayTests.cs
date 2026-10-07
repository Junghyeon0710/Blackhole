using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Blackhole.Tests
{
    /// <summary>
    /// Main 씬에서 블랙홀이 판을 다 삼키면 다음 우주로 넘어가는지 (점수 아래 표시, 가장 멀리 간 우주 저장),
    /// 새 판은 다시 첫 번째 우주에서 시작하는지 확인한다.
    /// </summary>
    public class UniversePlayTests
    {
        int savedBest, savedDiscovered, savedBlackHoles, savedFarthest;

        [SetUp]
        public void SaveRecords()
        {
            savedBest = SaveData.Best;
            savedDiscovered = SaveData.Discovered;
            savedBlackHoles = SaveData.BlackHoles;
            savedFarthest = SaveData.FarthestUniverse;
        }

        [TearDown]
        public void RestoreRecords()
        {
            SaveData.Best = savedBest;
            SaveData.Discovered = savedDiscovered;
            SaveData.BlackHoles = savedBlackHoles;
            SaveData.FarthestUniverse = savedFarthest;
        }

        [UnityTest]
        public IEnumerator BlackHole_OpensNextUniverse_AndNewGameStartsAtFirst()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            var game = Object.FindAnyObjectByType<GameManager>();
            var view = Object.FindAnyObjectByType<UniverseView>();
            Assert.NotNull(game);
            Assert.NotNull(view);

            game.StartGame();
            Assert.AreEqual(0, game.Universe);
            Assert.AreEqual(0, view.Shown);
            Assert.IsNull(GameObject.Find("Universe"), "첫 번째 우주에서는 점수 아래 우주 표시를 숨겨야 합니다");

            // 태양 두 개를 맞붙여 블랙홀을 만든다
            game.Session.Spawn(10, -90, 0);
            game.Session.Spawn(10, 90, 0);

            // 블랙홀이 판을 다 삼키면(2.6초) 그 자리에서 다음 우주가 퍼지기 시작한다
            float until = Time.realtimeSinceStartup + 8;
            while (game.Universe == 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(1, game.Universe, "블랙홀이 다 삼켰는데 다음 우주로 넘어가지 않았습니다");
            Assert.IsTrue(view.Revealing, "다음 우주가 퍼지는 연출이 시작되지 않았습니다");
            Assert.GreaterOrEqual(SaveData.FarthestUniverse, 1, "가장 멀리 간 우주가 저장되지 않았습니다");

            until = Time.realtimeSinceStartup + 5;
            while (view.Revealing && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(1, view.Shown, "다음 우주로 바뀌는 연출이 끝나지 않았습니다");
            var row = GameObject.Find("Universe");
            Assert.NotNull(row, "점수 아래 우주 표시가 보이지 않습니다");
            StringAssert.Contains("두 번째 우주", row.GetComponentInChildren<TMP_Text>().text);

            // 새 판은 첫 번째 우주부터
            game.StartGame();
            Assert.AreEqual(0, game.Universe);
            Assert.AreEqual(0, view.Shown);
            Assert.IsNull(GameObject.Find("Universe"), "새 판인데 우주 표시가 남아 있습니다");
        }
    }
}
