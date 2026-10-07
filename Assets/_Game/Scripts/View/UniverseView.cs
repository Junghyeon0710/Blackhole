using System;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 우주 색 전환. 블랙홀이 판을 다 삼키면 그 자리에서 다음 우주 색이 원을 그리며 화면 끝까지 퍼진다.
    /// 배경과 경기장 바닥은 원형 마스크로 나누고, 경기장 테두리와 UI 색은 같은 진행도로 섞는다.
    /// 그리기만 바꾸고 물리와 점수는 건드리지 않는다.
    /// </summary>
    public sealed class UniverseView : MonoBehaviour
    {
        [SerializeField] UniverseData data;
        [SerializeField] CameraRig rig;
        [SerializeField] Background background;
        [SerializeField] ArenaView arena;
        [Tooltip("원 반지름 1 유닛짜리 흰 원 스프라이트를 쓴다 (disc.png)")]
        [SerializeField] SpriteMask mask;
        [SerializeField] float revealTime = 1.6f;
        [SerializeField] float ringAlpha = 0.75f;

        int shown = -1, prepared = -1, target = -1;
        float elapsed;
        Vector2 center;
        UniversePalette from, to;

        /// <summary>UI 가 따라갈 색. 전환 중에는 매 프레임, 끝나면 한 번 더 부른다.</summary>
        public event Action<UniversePalette> PaletteChanged;
        /// <summary>전환이 끝나 새 우주에 도착했다 (인자: 우주 번호, 0부터).</summary>
        public event Action<int> Arrived;

        public UniverseData Data => data;
        public int Shown => shown;
        public bool Revealing => target >= 0;

        void Awake()
        {
            mask.gameObject.SetActive(false);
            Show(0);
        }

        /// <summary>바로 이 우주로 바꾼다 (새 판).</summary>
        public void Show(int universe)
        {
            target = prepared = -1;
            mask.gameObject.SetActive(false);
            var theme = data.Get(universe);
            if (shown == universe)
            {
                // 같은 우주면 다시 굽지 않고, 진행 중이던 전환만 접는다
                background.CancelReveal();
                arena.CancelReveal();
            }
            else
            {
                background.SetTheme(theme, universe);
                arena.SetTheme(theme);
                shown = universe;
            }
            PaletteChanged?.Invoke(UniversePalette.Of(theme));
        }

        /// <summary>다음 우주를 미리 구워 둔다. 블랙홀이 생길 때 불러 두면 전환이 시작될 때 끊기지 않는다.</summary>
        public void Prepare(int universe)
        {
            if (prepared == universe) return;
            var theme = data.Get(universe);
            background.Prepare(theme, universe);
            arena.Prepare(theme);
            prepared = universe;
        }

        /// <summary>unityCenter(블랙홀 자리)에서 universe 우주가 퍼져 나온다.</summary>
        public void Reveal(int universe, Vector2 unityCenter)
        {
            if (Revealing) Finish();
            Prepare(universe);
            target = universe;
            elapsed = 0;
            center = unityCenter;
            from = UniversePalette.Of(data.Get(shown));
            to = UniversePalette.Of(data.Get(universe));
            mask.transform.position = new Vector3(center.x, center.y, 0);
            mask.gameObject.SetActive(true);
            Step(0);
        }

        void Update()
        {
            if (!Revealing) return;
            elapsed += Time.deltaTime;
            if (elapsed >= revealTime) Finish();
            else Step(elapsed / revealTime);
        }

        void Step(float p)
        {
            // 천천히 열렸다가 빨라지고 끝에서 다시 느려진다 (easeInOutQuad)
            float e = p < 0.5f ? 2 * p * p : 1 - (2 - 2 * p) * (2 - 2 * p) / 2;
            float radius = Mathf.Max(0.001f, e * CoverRadius());
            mask.transform.localScale = new Vector3(radius, radius, 1);
            background.SetReveal(center, radius, ringAlpha * (1 - p));
            arena.SetReveal(e);
            PaletteChanged?.Invoke(UniversePalette.Lerp(from, to, e));
        }

        void Finish()
        {
            background.FinishReveal();
            arena.FinishReveal();
            mask.gameObject.SetActive(false);
            shown = target;
            target = prepared = -1;
            PaletteChanged?.Invoke(to);
            Arrived?.Invoke(shown);
        }

        /// <summary>원이 화면 네 귀퉁이까지 덮는 반지름.</summary>
        float CoverRadius()
        {
            var cam = rig.Camera;
            float h = cam.orthographicSize, w = h * cam.aspect;
            Vector2 c = cam.transform.position;
            float best = 0;
            for (int i = 0; i < 4; i++)
            {
                var corner = c + new Vector2(i % 2 == 0 ? -w : w, i < 2 ? -h : h);
                best = Mathf.Max(best, (corner - center).magnitude);
            }
            return best + 0.05f;
        }
    }
}
