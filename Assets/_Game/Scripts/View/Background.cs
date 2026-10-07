using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 별 배경. 우주색 원형 그라디언트와 성운 세 개를 화면 크기에 맞춰 텍스처로 굽고,
    /// 별 130개 + 반짝이는 별 22개를 그린다 (프로토타입 buildBg. 첫 번째 우주는 같은 난수 시드 42).
    /// 다음 우주로 넘어갈 때는 새 배경을 한 장 더 구워 원형 마스크(<see cref="UniverseView"/>) 안에서만 보이게 하고,
    /// 별도 그 원 안팎으로 나눠 그린다.
    /// 화면 흔들림을 받지 않도록 World 바깥에 둔다.
    /// </summary>
    public sealed class Background : MonoBehaviour
    {
        [SerializeField] CameraRig rig;
        [SerializeField] SpriteRenderer gradient;
        [Tooltip("다음 우주 배경. 원형 마스크 안에서만 보인다")]
        [SerializeField] SpriteRenderer revealGradient;
        [SerializeField] ShapeBatch stars;
        [SerializeField] int maxTextureSide = 512;

        static readonly Vector2[] NebulaSpots = { new Vector2(0.20f, 0.25f), new Vector2(0.85f, 0.70f), new Vector2(0.50f, 0.95f) };
        static readonly Color RingColor = new Color32(255, 244, 220, 255);

        struct Star { public float x, y, r, a; }
        struct Twinkle { public float x, y, phase, speed; }

        enum Side { All, Outside, Inside }

        /// <summary>한 우주의 배경: 색, 별 배치, 구운 텍스처.</summary>
        sealed class Layer
        {
            public UniverseData.Theme Theme;
            public bool HasTheme;
            public readonly Star[] Stars = new Star[130];
            public readonly Twinkle[] Twinkles = new Twinkle[22];
            public Texture2D Texture;
            public Sprite Sprite;
            public bool Baked;
        }

        Layer current = new Layer(), next = new Layer();
        bool nextInUse, revealing;
        Vector2 revealCenter;
        float revealRadius, ringAlpha;
        Vector2Int builtScreen;
        RectInt builtStage;

        void OnDestroy()
        {
            Release(current);
            Release(next);
        }

        static void Release(Layer layer)
        {
            if (layer.Sprite) Destroy(layer.Sprite);
            if (layer.Texture) Destroy(layer.Texture);
        }

        /// <summary>바로 이 우주 배경으로 바꾼다.</summary>
        public void SetTheme(UniverseData.Theme theme, int universe)
        {
            Assign(current, theme, universe);
            nextInUse = revealing = false;
            revealGradient.enabled = false;
        }

        /// <summary>다음 우주 배경을 미리 구워 둔다 (전환이 시작될 때 끊기지 않게).</summary>
        public void Prepare(UniverseData.Theme theme, int universe)
        {
            Assign(next, theme, universe);
            nextInUse = true;
            if (builtScreen.x > 0) Bake(next);
        }

        /// <summary>전환 중: center 에서 반지름 radius 안은 다음 우주. 경계에 밝은 고리를 그린다.</summary>
        public void SetReveal(Vector2 center, float radius, float ring)
        {
            revealing = true;
            revealCenter = center;
            revealRadius = radius;
            ringAlpha = ring;
            revealGradient.enabled = true;
        }

        /// <summary>준비했거나 진행 중인 전환을 접는다. 지금 우주는 그대로.</summary>
        public void CancelReveal()
        {
            nextInUse = revealing = false;
            revealGradient.enabled = false;
        }

        /// <summary>전환이 끝났다: 다음 우주가 지금 우주가 된다.</summary>
        public void FinishReveal()
        {
            (current, next) = (next, current);
            nextInUse = revealing = false;
            revealGradient.enabled = false;
            if (current.Baked) gradient.sprite = current.Sprite;
        }

        static void Assign(Layer layer, UniverseData.Theme theme, int universe)
        {
            layer.Theme = theme;
            layer.HasTheme = true;
            layer.Baked = false;
            var rnd = new Mulberry32(42 + universe * 1013);
            for (int i = 0; i < layer.Stars.Length; i++)
            {
                float a = 0.15f + rnd.NextFloat() * 0.5f;
                layer.Stars[i] = new Star { a = a, x = rnd.NextFloat(), y = rnd.NextFloat(), r = rnd.NextFloat() * 1.3f + 0.3f };
            }
            for (int i = 0; i < layer.Twinkles.Length; i++)
                layer.Twinkles[i] = new Twinkle { x = rnd.NextFloat(), y = rnd.NextFloat(), phase = rnd.NextFloat() * 6, speed = 0.8f + rnd.NextFloat() * 2 };
        }

        void LateUpdate()
        {
            var stage = ViewMetrics.StageRect;
            var stageInt = new RectInt(Mathf.RoundToInt(stage.x), Mathf.RoundToInt(stage.y), Mathf.RoundToInt(stage.width), Mathf.RoundToInt(stage.height));
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != builtScreen || !stageInt.Equals(builtStage))
            {
                builtScreen = screen;
                builtStage = stageInt;
                current.Baked = next.Baked = false;
            }
            if (!current.HasTheme) return;
            if (!current.Baked) Bake(current);
            if (nextInUse && !next.Baked) Bake(next);
            if (gradient.sprite != current.Sprite) gradient.sprite = current.Sprite;
            FitToCamera(gradient);
            if (nextInUse)
            {
                if (revealGradient.sprite != next.Sprite) revealGradient.sprite = next.Sprite;
                FitToCamera(revealGradient);
            }
            DrawStars(stage);
        }

        void Bake(Layer layer)
        {
            var stage = ViewMetrics.StageRect;
            float k = Mathf.Max(1f, Mathf.Max(builtScreen.x, builtScreen.y) / (float)maxTextureSide);
            int w = Mathf.Max(2, Mathf.CeilToInt(builtScreen.x / k)), h = Mathf.Max(2, Mathf.CeilToInt(builtScreen.y / k));

            if (layer.Texture == null || layer.Texture.width != w || layer.Texture.height != h)
            {
                if (layer.Texture) Destroy(layer.Texture);
                layer.Texture = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
                {
                    name = "Background",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            var theme = layer.Theme;
            Color[] nebulae = { theme.nebula1, theme.nebula2, theme.nebula3 };
            var pixels = new Color32[w * h];
            float sw = builtScreen.x, sh = builtScreen.y;
            float nebulaR = Mathf.Max(stage.width, stage.height) * 0.55f;
            uint hash = 2166136261;
            for (int py = 0; py < h; py++)
            {
                float y = (py + 0.5f) / h * sh;
                float cssY = sh - y; // 위에서부터
                for (int px = 0; px < w; px++)
                {
                    float x = (px + 0.5f) / w * sw;
                    // radial-gradient(120% 80% at 50% 45%, 가운데 0%, 중간 45%, 가장자리 100%)
                    float dx = (x - sw * 0.5f) / (sw * 1.2f), dy = (cssY - sh * 0.45f) / (sh * 0.8f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = d < 0.45f ? Color.Lerp(theme.center, theme.middle, d / 0.45f) : Color.Lerp(theme.middle, theme.edge, (d - 0.45f) / 0.55f);
                    for (int i = 0; i < NebulaSpots.Length; i++)
                    {
                        float nx = stage.xMin + stage.width * NebulaSpots[i].x, ny = stage.yMax - stage.height * NebulaSpots[i].y;
                        float nd = Mathf.Sqrt((x - nx) * (x - nx) + (y - ny) * (y - ny));
                        if (nd >= nebulaR) continue;
                        c = Color.Lerp(c, nebulae[i], 0.16f * (1 - nd / nebulaR));
                    }
                    // 어두운 그라디언트의 띠 무늬를 없애는 약한 디더
                    hash = (hash ^ (uint)(px * 73856093 ^ py * 19349663)) * 16777619;
                    float noise = ((hash >> 8) & 0xff) / 255f - 0.5f;
                    c.r += noise / 255f; c.g += noise / 255f; c.b += noise / 255f;
                    c.a = 1;
                    pixels[py * w + px] = c;
                }
            }
            layer.Texture.SetPixels32(pixels);
            layer.Texture.Apply(false);

            if (layer.Sprite) Destroy(layer.Sprite);
            layer.Sprite = Sprite.Create(layer.Texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
            layer.Baked = true;
        }

        void FitToCamera(SpriteRenderer sr)
        {
            if (sr.sprite == null) return;
            var cam = rig.Camera;
            float height = cam.orthographicSize * 2, width = height * cam.aspect;
            var size = sr.sprite.bounds.size;
            sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
            sr.transform.localScale = new Vector3(width / size.x, height / size.y, 1);
        }

        void DrawStars(Rect stage)
        {
            float time = Time.time;
            stars.Clear();
            float px = ViewMetrics.UiScale / ViewMetrics.PxPerUnity; // CSS px → 유닛
            DrawLayer(current, stage, time, px, revealing ? Side.Outside : Side.All);
            if (revealing)
            {
                DrawLayer(next, stage, time, px, Side.Inside);
                if (ringAlpha > 0)
                {
                    var ring = RingColor;
                    ring.a = ringAlpha;
                    stars.Ring(revealCenter, revealRadius, ViewMetrics.CssToUnity(3), ring);
                }
            }
            stars.Apply();
        }

        void DrawLayer(Layer layer, Rect stage, float time, float px, Side side)
        {
            foreach (var s in layer.Stars)
            {
                var p = rig.ScreenToUnity(new Vector2(stage.xMin + s.x * stage.width, stage.yMax - s.y * stage.height));
                if (!Visible(p, side)) continue;
                var c = layer.Theme.star; c.a = s.a;
                stars.Disc(p, s.r * px, c);
            }
            foreach (var t in layer.Twinkles)
            {
                var p = rig.ScreenToUnity(new Vector2(stage.xMin + t.x * stage.width, stage.yMax - t.y * stage.height));
                if (!Visible(p, side)) continue;
                var c = layer.Theme.star; c.a = 0.25f + 0.55f * (0.5f + 0.5f * Mathf.Sin(time * t.speed + t.phase));
                stars.Disc(p, 1.2f * px, c);
            }
        }

        bool Visible(Vector2 p, Side side)
        {
            if (side == Side.All) return true;
            bool inside = (p - revealCenter).sqrMagnitude <= revealRadius * revealRadius;
            return inside == (side == Side.Inside);
        }
    }
}
