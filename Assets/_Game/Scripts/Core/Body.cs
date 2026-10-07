namespace Blackhole
{
    public enum RemoveReason
    {
        None,
        Merged,
        Absorbed,
        Cleared,
        Reset,
    }

    /// <summary>
    /// 판 위의 행성 하나. 버렛 적분이라 속도 대신 이전 위치(PX, PY)를 들고 있다.
    /// 프로토타입과 같은 손맛을 내려고 double 로 계산한다 (자바스크립트 number 와 같은 정밀도).
    /// </summary>
    public sealed class Body
    {
        public int Id;
        public int Tier;
        public double X, Y;
        public double PX, PY;
        public double R;          // 지금 반지름 (합체 직후에는 커지는 중)
        public double TargetR;    // 단계 반지름
        public double Grow;       // 초당 반지름 증가량
        public double Age;
        public bool Alive = true;
        public bool Merging;
        public RemoveReason RemoveReason;

        public double DangerT;
        public double Happy;
        public double BlinkT;
        public double Blinking;
    }
}
