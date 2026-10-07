using UnityEngine;

namespace Blackhole
{
    /// <summary>블랙홀: 빛무리, 도는 강착원반, 검정 코어. 사라질 때는 셋 다 함께 투명해진다.</summary>
    public sealed class BlackHoleView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer glow;
        [SerializeField] SpriteRenderer disk;
        [SerializeField] SpriteRenderer core;
        [SerializeField] GameObject visual;

        GameSession session;

        public void Bind(GameSession s) => session = s;

        void LateUpdate()
        {
            var bh = session?.BlackHole;
            bool on = bh != null && bh.Active;
            if (visual.activeSelf != on) visual.SetActive(on);
            if (!on) return;

            visual.transform.localPosition = ViewMetrics.WorldToUnity(bh.X, bh.Y);
            float r = (float)bh.R * Tuning.WorldToUnity;
            visual.transform.localScale = new Vector3(r, r, 1);
            // 캔버스 rotate(t * 2.2) 는 화면 시계 방향
            disk.transform.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 2.2f * Mathf.Rad2Deg);
            var c = new Color(1, 1, 1, Mathf.Max(0, (float)bh.Fade));
            glow.color = c;
            disk.color = c;
            core.color = c;
        }
    }
}
