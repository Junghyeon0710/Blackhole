using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 켜질 때 scale .85 → 1, 투명 → 불투명으로 튀어나온다 (프로토타입 @keyframes pop, cubic-bezier(.3,1.5,.5,1)).
    /// 카드는 0.35초, "최고 기록 경신!" 은 0.2초 뒤 0.5초.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopIn : MonoBehaviour
    {
        [SerializeField] float duration = 0.35f;
        [SerializeField] float delay;

        CanvasGroup group;
        float startedAt;

        void Awake() => group = GetComponent<CanvasGroup>();

        void OnEnable()
        {
            startedAt = Time.time;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            float p = Mathf.Clamp01((Time.time - startedAt - delay) / duration);
            float e = CubicBezier.Pop.Evaluate(p);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, e);
            group.alpha = Mathf.Clamp01(e);
        }
    }
}
