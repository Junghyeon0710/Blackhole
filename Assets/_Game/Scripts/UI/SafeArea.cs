using UnityEngine;

namespace Blackhole
{
    /// <summary>노치와 홈 바를 피하도록 Screen.safeArea 에 맞춰 앵커를 정한다.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void OnEnable() => Apply();
        void Update() => Apply();

        void Apply()
        {
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == applied && size == screen) return;
            applied = safe;
            screen = size;
            if (size.x <= 0 || size.y <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            rt.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
