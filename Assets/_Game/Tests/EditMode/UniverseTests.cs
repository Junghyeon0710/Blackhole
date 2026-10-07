using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blackhole.Tests
{
    /// <summary>우주 번호 글자("두 번째")와 우주 색 데이터. 첫 번째 우주는 프로토타입 배경과 같아야 한다.</summary>
    public class UniverseTests
    {
        const string DataPath = "Assets/_Game/Data/UniverseData.asset";

        [TestCase(1, "첫 번째")]
        [TestCase(2, "두 번째")]
        [TestCase(3, "세 번째")]
        [TestCase(4, "네 번째")]
        [TestCase(5, "다섯 번째")]
        [TestCase(10, "열 번째")]
        [TestCase(11, "열한 번째")]
        [TestCase(12, "열두 번째")]
        [TestCase(20, "스무 번째")]
        [TestCase(21, "스물한 번째")]
        [TestCase(100, "100번째")]
        public void Ordinal(int n, string expected) => Assert.AreEqual(expected, Korean.Ordinal(n));

        static UniverseData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniverseData>(DataPath);
            Assert.NotNull(data, "우주 색 데이터가 없습니다: " + DataPath);
            return data;
        }

        static void AssertRgb(string expected, Color actual, string what) =>
            Assert.AreEqual(expected, ColorUtility.ToHtmlStringRGB(actual), what);

        [Test]
        public void FirstUniverse_IsThePrototypeBackground()
        {
            var t = Load().Get(0);
            // radial-gradient(#2b1f6b, #1a1446 45%, #0e0a2e)
            AssertRgb("2B1F6B", t.center, "배경 가운데");
            AssertRgb("1A1446", t.middle, "배경 중간");
            AssertRgb("0E0A2E", t.edge, "배경 가장자리");
            // 성운 '120,80,255' '255,110,170' '80,220,200', 별 rgba(255,248,230)
            AssertRgb("7850FF", t.nebula1, "성운 1");
            AssertRgb("FF6EAA", t.nebula2, "성운 2");
            AssertRgb("50DCC8", t.nebula3, "성운 3");
            AssertRgb("FFF8E6", t.star, "별");
            // 경기장 rgba(150,130,255) → rgba(90,70,200) → rgba(170,150,255), 테두리 rgba(201,192,242)
            AssertRgb("9682FF", t.arenaInner, "경기장 가운데");
            AssertRgb("5A46C8", t.arenaMiddle, "경기장 75%");
            AssertRgb("AA96FF", t.arenaOuter, "경기장 테두리 쪽");
            AssertRgb("C9C0F2", t.line, "테두리·흐린 글자");
        }

        [Test]
        public void Themes_StartOverAfterTheLast()
        {
            var data = Load();
            Assert.AreEqual(6, data.Count);
            Assert.AreEqual(data.Get(0).name, data.Get(data.Count).name);
            Assert.AreEqual(data.Get(1).name, data.Get(data.Count + 1).name);
            foreach (var t in data.themes)
                Assert.AreEqual(1f, t.center.a, 1e-4f, t.name + " 배경색이 반투명합니다");
        }
    }
}
