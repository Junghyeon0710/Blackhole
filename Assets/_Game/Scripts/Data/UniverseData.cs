using System;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 우주 색. 블랙홀을 하나 만들 때마다 다음 우주로 넘어간다 (판마다 첫 번째 우주부터).
    /// 색이 다 떨어지면 처음 색부터 다시 쓰고, 번호는 계속 오른다. 첫 번째 우주는 프로토타입 배경색 그대로다.
    /// </summary>
    [CreateAssetMenu(menuName = "Blackhole/Universe Data", fileName = "UniverseData")]
    public sealed class UniverseData : ScriptableObject
    {
        [Serializable]
        public struct Theme
        {
            public string name;

            [Header("배경: 가운데 → 45% → 가장자리 원형 그라디언트")]
            public Color center;
            public Color middle;
            public Color edge;

            [Header("성운 (왼쪽 위, 오른쪽, 아래쪽)과 별")]
            public Color nebula1;
            public Color nebula2;
            public Color nebula3;
            public Color star;

            [Header("경기장 바닥: 가운데 → 75% → 테두리")]
            public Color arenaInner;
            public Color arenaMiddle;
            public Color arenaOuter;

            [Tooltip("경기장 테두리, 흐린 글자, 버튼·도감 테두리")]
            public Color line;
        }

        public Theme[] themes = new Theme[6];

        public int Count => themes.Length;

        /// <summary>universe 번째(0부터) 우주의 색.</summary>
        public Theme Get(int universe)
        {
            int n = themes.Length;
            return themes[(universe % n + n) % n];
        }
    }

    /// <summary>UI 와 경기장 선이 따라가는 우주 색 세 가지. 전환 중에는 두 우주 사이를 섞는다.</summary>
    public readonly struct UniversePalette
    {
        /// <summary>흐린 글자, 테두리.</summary>
        public readonly Color Line;
        /// <summary>버튼 바탕 (배경 가운데 색).</summary>
        public readonly Color Panel;
        /// <summary>도감 바탕 (배경 가장자리 색).</summary>
        public readonly Color Deep;

        public UniversePalette(Color line, Color panel, Color deep)
        {
            Line = line; Panel = panel; Deep = deep;
        }

        public static UniversePalette Of(UniverseData.Theme t) => new UniversePalette(t.line, t.center, t.edge);

        public static UniversePalette Lerp(UniversePalette a, UniversePalette b, float t) =>
            new UniversePalette(Color.Lerp(a.Line, b.Line, t), Color.Lerp(a.Panel, b.Panel, t), Color.Lerp(a.Deep, b.Deep, t));
    }
}
