using UnityEngine;

namespace Blackhole
{
    /// <summary>경기장 원, 위험선 점선(위험할수록 굵고 빨갛게 깜빡임), 중심 코어 빛.</summary>
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer fill;
        [SerializeField] SpriteRenderer coreGlow;
        [SerializeField] ShapeBatch lines;

        static readonly Color Outline = new Color32(201, 192, 242, 102);       // rgba(201,192,242,.4)
        static readonly Color DangerIdle = new Color32(255, 247, 236, 71);     // rgba(255,247,236,.28)
        static readonly Color DangerHot = new Color32(255, 90, 118, 255);
        static readonly Color CoreColor = new Color(1f, 247f / 255f, 236f / 255f, 0.9f);

        GameSession session;

        public void Bind(GameSession s) => session = s;

        void LateUpdate()
        {
            float time = Time.unscaledTime;
            float arena = (float)Tuning.ArenaRadius * Tuning.WorldToUnity;
            fill.transform.localScale = Vector3.one * arena;

            lines.Clear();
            lines.Ring(Vector2.zero, arena, ViewMetrics.CssToUnity(2), Outline);

            float dl = session != null ? Mathf.Min(1, (float)session.Danger.Level) : 0;
            Color danger = DangerIdle;
            if (dl > 0)
            {
                danger = DangerHot;
                danger.a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(time * (6 + dl * 10)));
            }
            lines.DashedRing(Vector2.zero, (float)Tuning.DangerRadius * Tuning.WorldToUnity, ViewMetrics.CssToUnity(2 + dl * 2), danger,
                ViewMetrics.CssToUnity(7), ViewMetrics.CssToUnity(9), ViewMetrics.CssToUnity(-time * 14));
            lines.Apply();

            coreGlow.color = CoreColor;
            coreGlow.transform.localScale = Vector3.one * ViewMetrics.CssToUnity(16 + Mathf.Sin(time * 3) * 2);
        }
    }
}
