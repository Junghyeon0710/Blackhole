using System;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 11단계 행성의 이름, 반지름, 색, 발사 가중치, 스프라이트. 기획서 3장 표.
    /// 스프라이트는 "반지름 1 유닛" 규칙으로 가져온다 (PPU = 그림의 행성 반지름 px).
    /// </summary>
    [CreateAssetMenu(menuName = "Blackhole/Planet Data", fileName = "PlanetData")]
    public sealed class PlanetData : ScriptableObject
    {
        [Serializable]
        public struct Tier
        {
            public string name;
            [Tooltip("월드 단위 반지름 (경기장 반지름 = 300)")]
            public float radius;
            public Color light;
            public Color dark;
            [Tooltip("발사 가중치. 0~4단계만 쓴다")]
            public float spawnWeight;
            public Sprite sprite;
        }

        public Tier[] tiers = new Tier[11];

        [Header("얼굴")]
        public Sprite faceNormal;
        public Sprite faceBlink;
        public Sprite faceHappy;
        public Sprite faceScared;

        [Header("블랙홀")]
        public string blackHoleName = "블랙홀";
        public Sprite blackHoleGlow;
        public Sprite blackHoleDisk;
        public Sprite blackHoleCore;

        public int Count => tiers.Length;
        public int MaxTier => tiers.Length - 1;

        public string NameOf(int tier) => tier > MaxTier ? blackHoleName : tiers[tier].name;

        public TierTable ToTierTable()
        {
            var radii = new double[tiers.Length];
            for (int i = 0; i < tiers.Length; i++) radii[i] = tiers[i].radius;
            int spawnCount = 0;
            while (spawnCount < tiers.Length && tiers[spawnCount].spawnWeight > 0) spawnCount++;
            var weights = new double[spawnCount];
            for (int i = 0; i < spawnCount; i++) weights[i] = tiers[i].spawnWeight;
            return new TierTable(radii, weights);
        }

        public Sprite Face(FaceMood mood) => mood switch
        {
            FaceMood.Blink => faceBlink,
            FaceMood.Happy => faceHappy,
            FaceMood.Scared => faceScared,
            _ => faceNormal,
        };
    }

    public enum FaceMood
    {
        Normal,
        Blink,
        Happy,
        Scared,
    }
}
