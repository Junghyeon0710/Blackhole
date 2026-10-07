using System.Collections.Generic;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 판 위 행성들을 오브젝트 풀로 그린다. 그리는 순서는 프로토타입처럼 목록 순서(나중에 생긴 행성이 위)이고,
    /// 위험한 행성에는 빨간 테두리를 깜빡인다.
    /// </summary>
    public sealed class PlanetRenderer : MonoBehaviour
    {
        [SerializeField] PlanetView prefab;
        [SerializeField] ShapeBatch dangerRings;
        [SerializeField] int prewarm = 48;

        static readonly Color DangerColor = new Color32(255, 90, 118, 255);

        GameSession session;
        PlanetData data;
        readonly Dictionary<Body, PlanetView> views = new Dictionary<Body, PlanetView>();
        readonly Stack<PlanetView> pool = new Stack<PlanetView>();

        public void Bind(GameSession s, PlanetData planetData)
        {
            session = s;
            data = planetData;
            s.World.BodyAdded += OnAdded;
            s.World.BodyRemoved += OnRemoved;
            for (int i = 0; i < prewarm; i++) pool.Push(Create());
        }

        PlanetView Create()
        {
            var v = Instantiate(prefab, transform);
            v.Init(data);
            v.gameObject.SetActive(false);
            return v;
        }

        void OnAdded(Body b)
        {
            var v = pool.Count > 0 ? pool.Pop() : Create();
            v.SetTier(b.Tier);
            v.Face.SetMood(FaceMood.Normal);
            v.gameObject.SetActive(true);
            views[b] = v;
        }

        void OnRemoved(Body b)
        {
            if (!views.TryGetValue(b, out var v)) return;
            views.Remove(b);
            v.gameObject.SetActive(false);
            pool.Push(v);
        }

        void LateUpdate()
        {
            if (session == null) return;
            float time = Time.unscaledTime;
            var bodies = session.World.Bodies;
            dangerRings.Clear();
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (!views.TryGetValue(b, out var v)) continue;
                var pos = ViewMetrics.WorldToUnity(b.X, b.Y);
                float r = (float)b.R * Tuning.WorldToUnity;
                v.Place(pos, r);
                v.SetSortingOrder(i * 2);
                v.Face.SetMood(PlanetFace.MoodOf(b));
                if (b.DangerT > 0)
                {
                    var c = DangerColor;
                    c.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(time * 12));
                    dangerRings.Ring(pos, r + ViewMetrics.CssToUnity(3), ViewMetrics.CssToUnity(3), c);
                }
            }
            dangerRings.Apply();
        }
    }
}
