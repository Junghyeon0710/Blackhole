using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>
    /// UI 에 그리는 행성(+얼굴) 또는 블랙홀. 다음 행성 미리보기, 시작 화면 로고, 결과 창, 도감에 쓴다.
    /// 행성 스프라이트는 반지름 1 유닛 규칙이라 크기 = 스프라이트 크기 ÷ PPU × 반지름.
    /// </summary>
    public sealed class PlanetIcon : MonoBehaviour
    {
        [SerializeField] Image back;   // 행성 또는 블랙홀 빛무리
        [SerializeField] Image middle; // 얼굴 또는 강착원반
        [SerializeField] Image front;  // 블랙홀 코어

        public void SetPlanet(PlanetData data, int tier, float radius, FaceMood? mood)
        {
            if (tier > data.MaxTier)
            {
                SetBlackHole(data, radius, 0.6f);
                return;
            }
            Fit(back, data.tiers[tier].sprite, radius);
            if (mood.HasValue)
            {
                Fit(middle, data.Face(mood.Value), radius);
                middle.rectTransform.localRotation = Quaternion.identity;
            }
            else
            {
                middle.gameObject.SetActive(false);
            }
            front.gameObject.SetActive(false);
        }

        /// <summary>블랙홀. time 은 원반이 돈 시간 (프로토타입 drawBlackHole 의 t).</summary>
        public void SetBlackHole(PlanetData data, float radius, float time)
        {
            Fit(back, data.blackHoleGlow, radius);
            Fit(middle, data.blackHoleDisk, radius);
            middle.rectTransform.localRotation = Quaternion.Euler(0, 0, -time * 2.2f * Mathf.Rad2Deg);
            Fit(front, data.blackHoleCore, radius);
        }

        static void Fit(Image image, Sprite sprite, float radius)
        {
            image.gameObject.SetActive(sprite != null);
            if (sprite == null) return;
            image.sprite = sprite;
            image.rectTransform.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * radius;
        }
    }
}
