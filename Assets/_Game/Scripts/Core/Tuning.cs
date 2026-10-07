namespace Blackhole
{
    /// <summary>
    /// 기획서 4·5장 수치. 프로토타입(blackhole.html)과 같은 값이다.
    /// 길이는 월드 단위(경기장 반지름 300)이고, Unity 유닛으로는 <see cref="WorldToUnity"/> 를 곱한다.
    /// </summary>
    public static class Tuning
    {
        public const float WorldToUnity = 0.01f;

        // 물리
        public const double ArenaRadius = 300;
        public const double DangerRadius = 246;
        public const double Gravity = 1150;
        public const double GravitySoftRadius = 18;   // 중심에서 이 거리 안은 거리/18 비율로 약해진다
        public const int SubSteps = 8;
        public const double Damping = 0.9965;
        public const double MaxSpeed = 900;
        public const double CollisionCorrection = 0.8;
        public const double TickDt = 1.0 / 60.0;
        public const int MaxTicksPerFrame = 4;

        // 발사
        public const double LaunchSpeed = 300;
        public const double LaunchCooldown = 0.45;

        // 합체
        public const double MergeGrowTime = 0.16;
        public const double MergeVelocityKeep = 0.5;
        public const double MergedBodyAge = 5;
        public const double HappyTime = 0.7;

        // 위험
        public const double DangerMargin = 3;          // 바깥 끝이 246 + 3 = 249 를 넘으면
        public const double DangerMinAge = 1.4;
        public const double DangerMaxSpeed = 140;
        public const double DangerLimit = 2.4;
        public const double DangerRecoverRate = 2;
        public const double ScaredThreshold = 0.3;

        // 점수
        public const double ComboWindow = 1.3;
        public const double ComboStep = 0.5;

        // 블랙홀
        public const double BlackHoleGravity = 2600;
        public const double BlackHoleStartRadius = 12;
        public const double BlackHoleGrowSpeed = 40;
        public const double BlackHoleMaxRadius = 60;
        public const double BlackHoleAbsorbFactor = 0.3;
        public const double BlackHoleDuration = 2.6;
        public const double BlackHoleFadeSpeed = 1.4;  // 1 / 1.4 ≒ 0.7초 동안 사라진다
        public const int BlackHoleBonus = 500;
        public const int AbsorbScoreMultiplier = 2;

        // 이어하기
        public const double ReviveClearRadius = ArenaRadius * 0.55; // 165

        // 진행
        public const double GameOverDelay = 0.45;
        public const int InterstitialEvery = 3;
        public const int SwapMaxTier = 4;

        public static int ScoreFor(int tier) => (tier + 1) * (tier + 2) / 2;
    }
}
