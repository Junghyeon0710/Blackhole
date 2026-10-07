using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 직교 카메라를 경기장에 맞춘다. HUD 와 아래 도구 사이의 Stage 영역 안에 경기장(반지름 300 + 여백 8px)이 꽉 차게
    /// 크기를 정하고, 경기장 중심이 Stage 가운데에 오게 카메라를 옮긴다 (프로토타입 resize()).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] RectTransform stage;
        [SerializeField] Canvas canvas;
        [SerializeField] float marginCss = 8;

        Camera cam;
        readonly Vector3[] corners = new Vector3[4];

        public Camera Camera => cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            Fit();
        }

        void LateUpdate() => Fit();

        void Fit()
        {
            if (stage == null || canvas == null) return;
            stage.GetWorldCorners(corners); // 오버레이 캔버스라서 화면 px
            var rect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            if (rect.width < 1 || rect.height < 1) rect = new Rect(0, 0, Screen.width, Screen.height);

            float ui = canvas.scaleFactor;
            float pxPerWorld = Mathf.Max(0.05f * ui, (Mathf.Min(rect.width, rect.height) / 2f - marginCss * ui) / (float)Tuning.ArenaRadius);
            float pxPerUnity = pxPerWorld / Tuning.WorldToUnity;

            cam.orthographicSize = Screen.height / 2f / pxPerUnity;
            var offset = rect.center - new Vector2(Screen.width / 2f, Screen.height / 2f);
            transform.position = new Vector3(-offset.x / pxPerUnity, -offset.y / pxPerUnity, -10);

            ViewMetrics.PxPerWorld = pxPerWorld;
            ViewMetrics.UiScale = ui;
            ViewMetrics.StageRect = rect;
        }

        public Vector2 ScreenToUnity(Vector2 screen)
        {
            var p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            return new Vector2(p.x, p.y);
        }
    }
}
