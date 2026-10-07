using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 행성 하나의 모습 (기획서의 Planet). 시뮬레이션 값은 <see cref="Body"/> 에 있고 여기서는 그리기만 한다.
    /// 스프라이트는 반지름 1 유닛 규칙이라 localScale = 반지름(유닛).
    /// </summary>
    public sealed class PlanetView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] PlanetFace face;

        PlanetData data;
        int tier = -1;
        float alpha = 1;

        public PlanetFace Face => face;

        public void Init(PlanetData planetData)
        {
            data = planetData;
            face.Init(planetData);
        }

        public void SetTier(int t)
        {
            if (t == tier) return;
            tier = t;
            body.sprite = data.tiers[t].sprite;
        }

        public void SetSortingOrder(int order)
        {
            body.sortingOrder = order;
            face.Renderer.sortingOrder = order + 1;
        }

        public void SetAlpha(float a)
        {
            if (Mathf.Approximately(a, alpha)) return;
            alpha = a;
            var c = new Color(1, 1, 1, a);
            body.color = c;
            face.Renderer.color = c;
        }

        public void Place(Vector3 localPosition, float radiusUnity)
        {
            transform.localPosition = localPosition;
            transform.localScale = new Vector3(radiusUnity, radiusUnity, 1);
        }
    }
}
