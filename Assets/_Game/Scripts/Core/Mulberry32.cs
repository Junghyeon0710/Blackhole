namespace Blackhole
{
    /// <summary>프로토타입의 rng(seed) 와 같은 수열을 내는 난수기. 별 배경 배치를 똑같이 맞출 때 쓴다.</summary>
    public sealed class Mulberry32
    {
        int seed;

        public Mulberry32(int seed) { this.seed = seed; }

        public double Next()
        {
            unchecked
            {
                seed += 0x6D2B79F5;
                int t = (seed ^ (int)((uint)seed >> 15)) * (1 | seed);
                t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }

        public float NextFloat() => (float)Next();
    }
}
