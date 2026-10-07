using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 경기장 원, 위험선 점선(위험할수록 굵고 빨갛게 깜빡임), 중심 코어 빛.
    /// 바닥 그라디언트와 테두리는 우주 색을 따른다. 다음 우주로 넘어갈 때는 새 바닥을 원형 마스크 안에만 보이고
    /// 테두리 색은 같은 진행도로 섞는다.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer fill;
        [Tooltip("다음 우주 바닥. 원형 마스크 안에서만 보인다")]
        [SerializeField] SpriteRenderer revealFill;
        [SerializeField] SpriteRenderer coreGlow;
        [SerializeField] ShapeBatch lines;
        [Tooltip("바닥 텍스처의 원 반지름(px). 스프라이트는 이 원이 1 유닛이 되게 만든다")]
        [SerializeField] int fillRadius = 255;

        const float OutlineAlpha = 0.4f;                                       // rgba(201,192,242,.4)
        static readonly Color DangerIdle = new Color32(255, 247, 236, 71);     // rgba(255,247,236,.28)
        static readonly Color DangerHot = new Color32(255, 90, 118, 255);
        static readonly Color CoreColor = new Color(1f, 247f / 255f, 236f / 255f, 0.9f);

        GameSession session;
        Texture2D fillTexture, revealTexture;
        Sprite fillSprite, revealSprite;
        Color outline, revealOutline;
        float blend;

        public void Bind(GameSession s) => session = s;

        void OnDestroy()
        {
            foreach (var o in new Object[] { fillSprite, revealSprite, fillTexture, revealTexture })
                if (o) Destroy(o);
        }

        /// <summary>바로 이 우주 색으로 바꾼다.</summary>
        public void SetTheme(UniverseData.Theme theme)
        {
            Bake(theme, ref fillTexture, ref fillSprite);
            fill.sprite = fillSprite;
            outline = WithAlpha(theme.line, OutlineAlpha);
            blend = 0;
            revealFill.enabled = false;
        }

        /// <summary>다음 우주 바닥을 미리 구워 둔다.</summary>
        public void Prepare(UniverseData.Theme theme)
        {
            Bake(theme, ref revealTexture, ref revealSprite);
            revealFill.sprite = revealSprite;
            revealOutline = WithAlpha(theme.line, OutlineAlpha);
        }

        /// <summary>전환 중: 새 바닥을 보이고 테두리 색을 t 만큼 섞는다.</summary>
        public void SetReveal(float t)
        {
            revealFill.enabled = true;
            blend = t;
        }

        public void CancelReveal()
        {
            blend = 0;
            revealFill.enabled = false;
        }

        public void FinishReveal()
        {
            (fillTexture, revealTexture) = (revealTexture, fillTexture);
            (fillSprite, revealSprite) = (revealSprite, fillSprite);
            fill.sprite = fillSprite;
            revealFill.sprite = revealSprite;
            outline = revealOutline;
            blend = 0;
            revealFill.enabled = false;
        }

        void LateUpdate()
        {
            float time = Time.time;
            float arena = (float)Tuning.ArenaRadius * Tuning.WorldToUnity;
            fill.transform.localScale = Vector3.one * arena;
            revealFill.transform.localScale = Vector3.one * arena;

            lines.Clear();
            lines.Ring(Vector2.zero, arena, ViewMetrics.CssToUnity(2), Color.Lerp(outline, revealOutline, blend));

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

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>프로토타입 경기장 그라디언트: 가운데 α .14 → 75% α .07 → 테두리 α .16, 가장자리 1px 부드럽게.</summary>
        void Bake(UniverseData.Theme theme, ref Texture2D texture, ref Sprite sprite)
        {
            int r = fillRadius, size = r * 2 + 4;
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
                {
                    name = "ArenaFill",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }
            Color inner = WithAlpha(theme.arenaInner, 0.14f), middle = WithAlpha(theme.arenaMiddle, 0.07f), outer = WithAlpha(theme.arenaOuter, 0.16f);
            var pixels = new Color32[size * size];
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            {
                float dy = y + 0.5f - c;
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float cover = Mathf.Clamp01(r + 0.5f - dist);
                    if (cover <= 0) continue;
                    float d = dist / r;
                    var col = d < 0.75f ? Color.Lerp(inner, middle, d / 0.75f) : Color.Lerp(middle, outer, (d - 0.75f) / 0.25f);
                    col.a *= cover;
                    pixels[y * size + x] = col;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            if (sprite == null) sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), r);
        }
    }
}
