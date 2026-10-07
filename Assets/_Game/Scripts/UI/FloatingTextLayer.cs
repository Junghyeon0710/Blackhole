using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 경기장 위에 떠오르는 글자: 합체 점수(+15), 콤보("3콤보!"), 블랙홀 +500, 위험 카운트다운("위험! 1.8").
    /// 위치는 월드 좌표를 따라가고(화면 흔들림 포함), 크기는 CSS px 그대로 캔버스 단위로 쓴다.
    /// </summary>
    public sealed class FloatingTextLayer : MonoBehaviour
    {
        [SerializeField] RectTransform stage;
        [SerializeField] CameraRig rig;
        [SerializeField] Transform worldRoot;
        [SerializeField] TMP_Text template;
        [SerializeField] TMP_Text dangerLabel;
        [SerializeField] float rise = 28;

        static readonly Color DangerColor = new Color32(255, 90, 118, 255);

        sealed class Item
        {
            public TMP_Text Text;
            public double X, Y;
            public float Life, T;
            public bool Combo;
        }

        readonly List<Item> items = new List<Item>();
        readonly Stack<TMP_Text> pool = new Stack<TMP_Text>();
        GameManager game;

        public void Bind(GameManager g)
        {
            game = g;
            template.gameObject.SetActive(false);
            dangerLabel.gameObject.SetActive(false);
        }

        public void Add(string text, double x, double y, float life, float size, Color color, bool combo = false)
        {
            if (combo)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    if (items[i].Combo) Release(i);
            }
            var label = pool.Count > 0 ? pool.Pop() : Instantiate(template, template.transform.parent);
            label.gameObject.SetActive(true);
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.transform.SetAsLastSibling();
            dangerLabel.transform.SetAsLastSibling();
            items.Add(new Item { Text = label, X = x, Y = y, Life = life, Combo = combo });
            Place(items[items.Count - 1]);
        }

        public void Tick(float dt)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                items[i].T += dt;
                if (items[i].T >= items[i].Life) Release(i);
            }
        }

        public void Clear()
        {
            for (int i = items.Count - 1; i >= 0; i--) Release(i);
        }

        void Release(int index)
        {
            var item = items[index];
            items.RemoveAt(index);
            item.Text.gameObject.SetActive(false);
            pool.Push(item.Text);
        }

        Vector2 WorldToStage(double x, double y)
        {
            var world = worldRoot.TransformPoint(ViewMetrics.WorldToUnity(x, y));
            var screen = rig.Camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(stage, screen, null, out var local);
            return local;
        }

        void Place(Item item)
        {
            float p = item.T / item.Life;
            var pos = WorldToStage(item.X, item.Y);
            item.Text.rectTransform.anchoredPosition = pos + new Vector2(0, p * rise);
            var c = item.Text.color;
            c.a = p < 0.7f ? 1 : 1 - (p - 0.7f) / 0.3f;
            item.Text.color = c;
        }

        void LateUpdate()
        {
            foreach (var item in items) Place(item);
            UpdateDanger();
        }

        void UpdateDanger()
        {
            var session = game != null ? game.Session : null;
            float dl = session != null ? Mathf.Min(1, (float)session.Danger.Level) : 0;
            bool show = game != null && game.State == GameState.Playing && dl > 0.15f;
            if (dangerLabel.gameObject.activeSelf != show) dangerLabel.gameObject.SetActive(show);
            if (!show) return;

            float time = Time.time;
            dangerLabel.text = $"위험! {session.Danger.Remaining:0.0}";
            var c = DangerColor;
            c.a = 0.6f + 0.4f * Mathf.Sin(time * 10);
            dangerLabel.color = c;

            // 경기장 위 14px, 단 Stage 위쪽에서 16px 보다 위로는 가지 않는다
            var top = WorldToStage(0, Tuning.ArenaRadius);
            float stageTop = stage.rect.yMax;
            dangerLabel.rectTransform.anchoredPosition = new Vector2(top.x, Mathf.Min(stageTop - 16, top.y + 14));
        }
    }
}
