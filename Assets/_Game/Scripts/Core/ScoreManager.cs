using System;

namespace Blackhole
{
    /// <summary>
    /// 점수와 콤보. 직전 합체 후 1.3초 안에 또 합체하면 콤보 +1,
    /// 행성을 쏘면 콤보를 0으로 돌린다 (연타로 콤보를 부풀리지 못하게).
    /// </summary>
    public sealed class ScoreManager
    {
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public double ComboTimer { get; private set; }
        public int Best { get; set; }

        public event Action<int> Changed;

        public void Reset()
        {
            Score = 0;
            Combo = 0;
            ComboTimer = 0;
            Changed?.Invoke(Score);
        }

        public void Add(int points)
        {
            Score += points;
            Changed?.Invoke(Score);
        }

        /// <summary>합체가 일어날 때마다. 콤보 값을 돌려준다.</summary>
        public int RegisterMerge()
        {
            Combo = ComboTimer > 0 ? Combo + 1 : 1;
            ComboTimer = Tuning.ComboWindow;
            return Combo;
        }

        public void ResetCombo()
        {
            Combo = 0;
            ComboTimer = 0;
        }

        public void Tick(double dt)
        {
            if (ComboTimer > 0) ComboTimer -= dt;
        }

        /// <summary>(만든 단계 점수) × (1 + 0.5 × (콤보 − 1)), 반올림. 자바스크립트 Math.round 와 같게 .5 는 올린다.</summary>
        public static int MergePoints(int tier, int combo) =>
            (int)Math.Floor(Tuning.ScoreFor(tier) * (1 + (combo - 1) * Tuning.ComboStep) + 0.5);

        /// <summary>판이 끝났을 때. 최고 점수를 넘었으면 갱신하고 true.</summary>
        public bool CommitBest()
        {
            if (Score <= Best) return false;
            Best = Score;
            return true;
        }
    }
}
