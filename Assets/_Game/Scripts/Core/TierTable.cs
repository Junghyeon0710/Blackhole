using System;

namespace Blackhole
{
    /// <summary>시뮬레이션이 쓰는 단계별 수치(반지름, 발사 가중치). 그림과 이름은 <see cref="PlanetData"/> 에 있다.</summary>
    public sealed class TierTable
    {
        readonly double[] radii;
        readonly double[] spawnWeights;

        public TierTable(double[] radii, double[] spawnWeights)
        {
            if (radii == null || radii.Length < 2) throw new ArgumentException("단계가 두 개 이상 필요합니다", nameof(radii));
            this.radii = (double[])radii.Clone();
            this.spawnWeights = (double[])spawnWeights.Clone();
        }

        /// <summary>기획서 3장 표.</summary>
        public static TierTable Default => new TierTable(
            new double[] { 11, 15, 20, 26, 32, 39, 47, 56, 66, 78, 92 },
            new double[] { 0.30, 0.27, 0.22, 0.13, 0.08 });

        public int Count => radii.Length;
        /// <summary>태양. 태양 두 개가 합쳐지면 블랙홀이다.</summary>
        public int MaxTier => radii.Length - 1;
        public int SpawnCount => spawnWeights.Length;
        public double Radius(int tier) => radii[tier];
        public double SpawnWeight(int tier) => spawnWeights[tier];
    }
}
