using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 별 배경. 우주색 원형 그라디언트와 성운 세 개를 화면 크기에 맞춰 텍스처로 굽고,
    /// 별 130개 + 반짝이는 별 22개를 그린다 (프로토타입 buildBg, 같은 난수 시드 42).
    /// 화면 흔들림을 받지 않도록 World 바깥에 둔다.
    /// </summary>
    public sealed class Background : MonoBehaviour
    {
        [SerializeField] CameraRig rig;
        [SerializeField] SpriteRenderer gradient;
        [SerializeField] ShapeBatch stars;
        [SerializeField] int maxTextureSide = 512;

        static readonly Color Center = new Color32(0x2b, 0x1f, 0x6b, 255);
        static readonly Color Middle = new Color32(0x1a, 0x14, 0x46, 255);
        static readonly Color Edge = new Color32(0x0e, 0x0a, 0x2e, 255);
        static readonly (float x, float y, Color c)[] Nebulae =
        {
            (0.20f, 0.25f, new Color32(120, 80, 255, 255)),
            (0.85f, 0.70f, new Color32(255, 110, 170, 255)),
            (0.50f, 0.95f, new Color32(80, 220, 200, 255)),
        };
        static readonly Color StarColor = new Color32(255, 248, 230, 255);

        struct Star { public float x, y, r, a; }
        struct Twinkle { public float x, y, phase, speed; }

        Star[] staticStars;
        Twinkle[] twinkles;
        Texture2D texture;
        Sprite sprite;
        Vector2Int builtScreen;
        RectInt builtStage;

        void Awake()
        {
            var rnd = new Mulberry32(42);
            staticStars = new Star[130];
            for (int i = 0; i < staticStars.Length; i++)
            {
                float a = 0.15f + rnd.NextFloat() * 0.5f;
                staticStars[i] = new Star { a = a, x = rnd.NextFloat(), y = rnd.NextFloat(), r = rnd.NextFloat() * 1.3f + 0.3f };
            }
            twinkles = new Twinkle[22];
            for (int i = 0; i < twinkles.Length; i++)
                twinkles[i] = new Twinkle { x = rnd.NextFloat(), y = rnd.NextFloat(), phase = rnd.NextFloat() * 6, speed = 0.8f + rnd.NextFloat() * 2 };
        }

        void OnDestroy()
        {
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
        }

        void LateUpdate()
        {
            var stage = ViewMetrics.StageRect;
            var stageInt = new RectInt(Mathf.RoundToInt(stage.x), Mathf.RoundToInt(stage.y), Mathf.RoundToInt(stage.width), Mathf.RoundToInt(stage.height));
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != builtScreen || !stageInt.Equals(builtStage)) Rebuild(screen, stage, stageInt);
            FitToCamera();
            DrawStars(stage);
        }

        void Rebuild(Vector2Int screen, Rect stage, RectInt stageInt)
        {
            builtScreen = screen;
            builtStage = stageInt;
            float k = Mathf.Max(1f, Mathf.Max(screen.x, screen.y) / (float)maxTextureSide);
            int w = Mathf.Max(2, Mathf.CeilToInt(screen.x / k)), h = Mathf.Max(2, Mathf.CeilToInt(screen.y / k));

            if (texture == null || texture.width != w || texture.height != h)
            {
                if (texture) Destroy(texture);
                texture = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
                {
                    name = "Background",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            var pixels = new Color32[w * h];
            float sw = screen.x, sh = screen.y;
            float nebulaR = Mathf.Max(stage.width, stage.height) * 0.55f;
            uint hash = 2166136261;
            for (int py = 0; py < h; py++)
            {
                float y = (py + 0.5f) / h * sh;
                float cssY = sh - y; // 위에서부터
                for (int px = 0; px < w; px++)
                {
                    float x = (px + 0.5f) / w * sw;
                    // radial-gradient(120% 80% at 50% 45%, #2b1f6b 0%, #1a1446 45%, #0e0a2e 100%)
                    float dx = (x - sw * 0.5f) / (sw * 1.2f), dy = (cssY - sh * 0.45f) / (sh * 0.8f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = d < 0.45f ? Color.Lerp(Center, Middle, d / 0.45f) : Color.Lerp(Middle, Edge, (d - 0.45f) / 0.55f);
                    foreach (var n in Nebulae)
                    {
                        float nx = stage.xMin + stage.width * n.x, ny = stage.yMax - stage.height * n.y;
                        float nd = Mathf.Sqrt((x - nx) * (x - nx) + (y - ny) * (y - ny));
                        if (nd >= nebulaR) continue;
                        float a = 0.16f * (1 - nd / nebulaR);
                        c = Color.Lerp(c, n.c, a);
                    }
                    // 어두운 그라디언트의 띠 무늬를 없애는 약한 디더
                    hash = (hash ^ (uint)(px * 73856093 ^ py * 19349663)) * 16777619;
                    float noise = ((hash >> 8) & 0xff) / 255f - 0.5f;
                    c.r += noise / 255f; c.g += noise / 255f; c.b += noise / 255f;
                    pixels[py * w + px] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);

            if (sprite) Destroy(sprite);
            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
            gradient.sprite = sprite;
        }

        void FitToCamera()
        {
            var cam = rig.Camera;
            float height = cam.orthographicSize * 2, width = height * cam.aspect;
            var size = sprite.bounds.size;
            gradient.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
            gradient.transform.localScale = new Vector3(width / size.x, height / size.y, 1);
        }

        void DrawStars(Rect stage)
        {
            float time = Time.time;
            stars.Clear();
            float px = ViewMetrics.UiScale / ViewMetrics.PxPerUnity; // CSS px → 유닛
            foreach (var s in staticStars)
            {
                var p = rig.ScreenToUnity(new Vector2(stage.xMin + s.x * stage.width, stage.yMax - s.y * stage.height));
                var c = StarColor; c.a = s.a;
                stars.Disc(p, s.r * px, c);
            }
            foreach (var t in twinkles)
            {
                var p = rig.ScreenToUnity(new Vector2(stage.xMin + t.x * stage.width, stage.yMax - t.y * stage.height));
                var c = StarColor; c.a = 0.25f + 0.55f * (0.5f + 0.5f * Mathf.Sin(time * t.speed + t.phase));
                stars.Disc(p, 1.2f * px, c);
            }
            stars.Apply();
        }
    }
}
