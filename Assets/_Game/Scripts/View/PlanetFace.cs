using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 표정 4종: 기본, 깜빡임(2~6초마다 0.13초), 웃음 ^^(합체 직후 0.7초), 겁먹음 &gt;&lt;(위험 시간 0.3초 이상).
    /// 얼굴은 행성과 따로 그린 스프라이트라서 행성이 어떻게 놓여도 화면 기준으로 똑바로 선다.
    /// </summary>
    public sealed class PlanetFace : MonoBehaviour
    {
        [SerializeField] SpriteRenderer face;

        PlanetData data;
        FaceMood mood = (FaceMood)(-1);

        public SpriteRenderer Renderer => face;

        public void Init(PlanetData planetData)
        {
            data = planetData;
            mood = (FaceMood)(-1);
            SetMood(FaceMood.Normal);
        }

        public static FaceMood MoodOf(Body b)
        {
            if (b.Happy > 0) return FaceMood.Happy;
            if (b.DangerT > Tuning.ScaredThreshold) return FaceMood.Scared;
            return b.Blinking > 0 ? FaceMood.Blink : FaceMood.Normal;
        }

        public void SetMood(FaceMood next)
        {
            if (next == mood || data == null) return;
            mood = next;
            face.sprite = data.Face(next);
        }
    }
}
