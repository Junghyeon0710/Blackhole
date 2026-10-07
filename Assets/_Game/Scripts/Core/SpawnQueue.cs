using System;

namespace Blackhole
{
    /// <summary>
    /// 지금 쏠 행성과 다음 행성. 발사로 나오는 건 0~4단계뿐이고,
    /// 이번 판에서 금성(4단계)을 합체로 만들기 전까지는 0~3단계만 가중치를 다시 나눠 뽑는다.
    /// </summary>
    public sealed class SpawnQueue
    {
        const int VenusTier = 4;

        readonly TierTable tiers;
        readonly Func<double> random;

        public int Current { get; set; }
        public int Next { get; private set; }

        public event Action Changed;

        public SpawnQueue(TierTable tiers, Func<double> random)
        {
            this.tiers = tiers;
            this.random = random;
        }

        public int Roll(int topTier)
        {
            double x = random();
            int maxI = topTier < VenusTier ? VenusTier - 1 : Math.Min(VenusTier, tiers.SpawnCount - 1);
            double sum = 0;
            for (int k = 0; k <= maxI; k++) sum += tiers.SpawnWeight(k);
            x *= sum;
            int i = 0;
            for (; i < maxI; i++)
            {
                x -= tiers.SpawnWeight(i);
                if (x < 0) break;
            }
            return i;
        }

        public void Reset(int topTier)
        {
            Current = Roll(topTier);
            Next = Roll(topTier);
            Changed?.Invoke();
        }

        public void Advance(int topTier)
        {
            Current = Next;
            Next = Roll(topTier);
            Changed?.Invoke();
        }

        /// <summary>행성 교체 보상: 대기 중인 행성을 다른 0~4단계로 바꾼다.</summary>
        public int SwapCurrent()
        {
            int t;
            do { t = (int)Math.Floor(random() * (Tuning.SwapMaxTier + 1)); } while (t == Current);
            Current = t;
            Changed?.Invoke();
            return t;
        }
    }
}
