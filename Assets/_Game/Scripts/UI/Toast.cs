using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>경기장 위쪽 알림 ("새 행성 발견! 지구"). 큰 알림은 분홍색이고 2.2초, 보통은 노랑 1.5초.</summary>
    public sealed class Toast : MonoBehaviour
    {
        [SerializeField] RectTransform box;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image fill;
        [SerializeField] Image shadow;
        [SerializeField] TMP_Text label;

        [SerializeField] Color fillNormal = new Color32(0xff, 0xd8, 0x5e, 255);
        [SerializeField] Color shadowNormal = new Color32(0xe0, 0xa5, 0x26, 255);
        [SerializeField] Color textNormal = new Color32(0x3a, 0x23, 0x00, 255);
        [SerializeField] Color fillBig = new Color32(0xff, 0x8f, 0xb8, 255);
        [SerializeField] Color shadowBig = new Color32(0xc9, 0x50, 0x7f, 255);
        [SerializeField] Color textBig = new Color32(0x3d, 0x06, 0x20, 255);

        bool visible;
        float hideAt;
        float changedAt = -10;
        float fromAlpha, fromOffset = -8, fromScale = 0.9f;
        float alpha, offset = -8, scale = 0.9f;

        void Awake() => Apply();

        public void Show(string message, bool big)
        {
            label.text = message;
            label.fontSize = big ? 20 : 17;
            label.color = big ? textBig : textNormal;
            fill.color = big ? fillBig : fillNormal;
            shadow.color = big ? shadowBig : shadowNormal;
            var size = label.GetPreferredValues(message);
            box.sizeDelta = new Vector2(Mathf.Ceil(size.x) + 32, Mathf.Ceil(label.fontSize * 1.25f) + 16);
            float radius = box.sizeDelta.y / 2;
            fill.pixelsPerUnitMultiplier = shadow.pixelsPerUnitMultiplier = 18f / radius;
            hideAt = Time.unscaledTime + (big ? 2.2f : 1.5f);
            SetVisible(true);
        }

        void SetVisible(bool on)
        {
            if (visible == on) return;
            visible = on;
            fromAlpha = alpha; fromOffset = offset; fromScale = scale;
            changedAt = Time.unscaledTime;
        }

        void Update()
        {
            if (visible && Time.unscaledTime >= hideAt) SetVisible(false);
            float e = Time.unscaledTime - changedAt;
            float a = CubicBezier.Ease.Evaluate(e / 0.2f), t = CubicBezier.Toast.Evaluate(e / 0.25f);
            alpha = Mathf.Lerp(fromAlpha, visible ? 1 : 0, a);
            offset = Mathf.LerpUnclamped(fromOffset, visible ? 0 : -8, t);
            scale = Mathf.LerpUnclamped(fromScale, visible ? 1 : 0.9f, t);
            Apply();
        }

        void Apply()
        {
            // 피벗이 가운데라서 CSS transform 처럼 가운데를 기준으로 커진다
            group.alpha = alpha;
            box.anchoredPosition = new Vector2(0, -10 - box.sizeDelta.y / 2 - offset);
            box.localScale = new Vector3(scale, scale, 1);
        }
    }
}
