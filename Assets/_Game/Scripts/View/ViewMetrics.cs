using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 화면 크기 정보. <see cref="CameraRig"/> 가 매 프레임 채운다.
    /// 프로토타입은 선 두께와 글자 크기를 CSS px 로 정했으니, 여기서도 "CSS px" 를 캔버스 단위(UI 배율 1)로 보고 바꿔 쓴다.
    /// </summary>
    public static class ViewMetrics
    {
        /// <summary>월드 1 단위가 화면 몇 px 인지.</summary>
        public static float PxPerWorld = 1;
        /// <summary>UI 캔버스 배율 (CSS px 1 = 화면 몇 px).</summary>
        public static float UiScale = 1;
        /// <summary>경기장이 들어가는 영역 (화면 px, 왼쪽 아래 원점).</summary>
        public static Rect StageRect;

        public static float PxPerUnity => PxPerWorld / Tuning.WorldToUnity;
        public static float CssToWorld(float css) => css * UiScale / PxPerWorld;
        public static float CssToUnity(float css) => CssToWorld(css) * Tuning.WorldToUnity;
        public static float PixelToUnity(float px) => px / PxPerUnity;

        public static Vector3 WorldToUnity(double x, double y) =>
            new Vector3((float)x * Tuning.WorldToUnity, (float)y * Tuning.WorldToUnity, 0);
    }
}
