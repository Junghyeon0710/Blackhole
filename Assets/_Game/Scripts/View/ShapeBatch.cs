using System.Collections.Generic;
using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 선, 고리, 점선, 작은 원(파티클·별)을 한 메시에 모아 한 번에 그린다.
    /// 프로토타입이 캔버스 stroke 로 그리던 것들이라 두께는 화면 px 기준이고, 가장자리를 1px 흐리게 해서 계단이 안 보이게 한다.
    /// 텍스처는 흰 원(disc.png) 하나: 원은 전체 UV, 선은 가운데 UV(불투명 흰색)를 쓴다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ShapeBatch : MonoBehaviour
    {
        const float TwoPi = Mathf.PI * 2;
        const float DiscQuadScale = 32f / 30f; // disc.png 는 64px 안에 반지름 30px 원
        static readonly Vector2 SolidUv = new Vector2(0.5f, 0.5f);

        [SerializeField] int sortingOrder;

        Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>(1024);
        readonly List<Color32> colors = new List<Color32>(1024);
        readonly List<Vector2> uvs = new List<Vector2>(1024);
        readonly List<int> tris = new List<int>(2048);
        bool linear;

        public int SortingOrder
        {
            get => sortingOrder;
            set { sortingOrder = value; GetComponent<MeshRenderer>().sortingOrder = value; }
        }

        void Awake()
        {
            mesh = new Mesh { name = name + " Shapes" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        public void Clear()
        {
            verts.Clear();
            colors.Clear();
            uvs.Clear();
            tris.Clear();
        }

        public void Apply()
        {
            mesh.Clear(false);
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1));
        }

        Color32 Convert(Color c) => linear ? new Color(c.linear.r, c.linear.g, c.linear.b, c.a) : c;

        static float Feather => ViewMetrics.PixelToUnity(1f);

        void Vertex(Vector2 p, Color32 c, Vector2 uv)
        {
            verts.Add(new Vector3(p.x, p.y, 0));
            colors.Add(c);
            uvs.Add(uv);
        }

        void Quad(int a, int b, int c, int d)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }

        // 두께 w 의 단면을 네 점으로: 바깥 0 → 안쪽 1 → 안쪽 1 → 바깥 0. 1px 보다 얇으면 가운데 알파를 줄인다.
        static void Profile(float w, float f, byte alpha, out float o0, out float o1, out float o2, out float o3, out byte inner)
        {
            if (w >= f)
            {
                float hw = w * 0.5f, hf = f * 0.5f;
                o0 = -hw - hf; o1 = -hw + hf; o2 = hw - hf; o3 = hw + hf;
                inner = alpha;
            }
            else
            {
                float hf = f * 0.5f;
                o0 = -hf; o1 = 0; o2 = 0; o3 = hf;
                inner = (byte)(alpha * w / f);
            }
        }

        /// <summary>호. 각도는 라디안, 반시계가 +.</summary>
        public void Arc(Vector2 center, float radius, float width, Color color, float a0, float a1)
        {
            if (radius <= 0 || width <= 0) return;
            Color32 col = Convert(color);
            Profile(width, Feather, col.a, out float o0, out float o1, out float o2, out float o3, out byte inner);
            float r0 = Mathf.Max(0, radius + o0), r1 = Mathf.Max(0, radius + o1), r2 = radius + o2, r3 = radius + o3;
            var edge = col; edge.a = 0;
            var mid = col; mid.a = inner;

            float span = a1 - a0;
            float radiusPx = radius * ViewMetrics.PxPerUnity;
            int seg = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(span) * radiusPx / 6f), 1, 720);
            int start = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = a0 + span * i / seg;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vertex(center + dir * r0, edge, SolidUv);
                Vertex(center + dir * r1, mid, SolidUv);
                Vertex(center + dir * r2, mid, SolidUv);
                Vertex(center + dir * r3, edge, SolidUv);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 4, b = a + 4;
                Quad(a, a + 1, b + 1, b);
                Quad(a + 1, a + 2, b + 2, b + 1);
                Quad(a + 2, a + 3, b + 3, b + 2);
            }
        }

        public void Ring(Vector2 center, float radius, float width, Color color) =>
            Arc(center, radius, width, color, 0, TwoPi);

        /// <summary>
        /// 캔버스 setLineDash 점선 원. 캔버스 arc 는 오른쪽에서 시작해 화면 시계 방향으로 돌고,
        /// lineDashOffset 만큼 무늬가 밀린다.
        /// </summary>
        public void DashedRing(Vector2 center, float radius, float width, Color color, float dash, float gap, float dashOffset)
        {
            if (radius <= 0) return;
            float period = dash + gap, length = TwoPi * radius;
            float p = Mathf.Repeat(dashOffset, period), s = 0;
            int guard = 0;
            while (s < length && guard++ < 4096)
            {
                if (p < dash)
                {
                    float end = Mathf.Min(length, s + dash - p);
                    Arc(center, radius, width, color, -s / radius, -end / radius);
                    s = end;
                    p = dash;
                }
                else
                {
                    s += period - p;
                    p = 0;
                }
            }
        }

        /// <summary>a 에서 시작하는 점선 (끝은 평평하게, 캔버스 lineCap butt).</summary>
        public void DashedLine(Vector2 a, Vector2 b, float width, Color color, float dash, float gap)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 1e-6f) return;
            var u = d / len;
            var n = new Vector2(-u.y, u.x);
            Color32 col = Convert(color);
            Profile(width, Feather, col.a, out float o0, out float o1, out float o2, out float o3, out byte inner);
            var edge = col; edge.a = 0;
            var mid = col; mid.a = inner;
            for (float s = 0; s < len; s += dash + gap)
            {
                var p0 = a + u * s;
                var p1 = a + u * Mathf.Min(len, s + dash);
                int i = verts.Count;
                Vertex(p0 + n * o0, edge, SolidUv); Vertex(p0 + n * o1, mid, SolidUv); Vertex(p0 + n * o2, mid, SolidUv); Vertex(p0 + n * o3, edge, SolidUv);
                Vertex(p1 + n * o0, edge, SolidUv); Vertex(p1 + n * o1, mid, SolidUv); Vertex(p1 + n * o2, mid, SolidUv); Vertex(p1 + n * o3, edge, SolidUv);
                Quad(i, i + 1, i + 5, i + 4);
                Quad(i + 1, i + 2, i + 6, i + 5);
                Quad(i + 2, i + 3, i + 7, i + 6);
            }
        }

        /// <summary>속이 찬 원 (파티클, 별).</summary>
        public void Disc(Vector2 center, float radius, Color color)
        {
            if (radius <= 0 || color.a <= 0) return;
            Color32 col = Convert(color);
            float h = radius * DiscQuadScale;
            int i = verts.Count;
            Vertex(new Vector2(center.x - h, center.y - h), col, new Vector2(0, 0));
            Vertex(new Vector2(center.x - h, center.y + h), col, new Vector2(0, 1));
            Vertex(new Vector2(center.x + h, center.y + h), col, new Vector2(1, 1));
            Vertex(new Vector2(center.x + h, center.y - h), col, new Vector2(1, 0));
            Quad(i, i + 1, i + 2, i + 3);
        }
    }
}
