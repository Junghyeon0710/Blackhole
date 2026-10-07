using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>프로토타입 .btn:active 처럼 누르면 버튼 면이 4px 내려가 그림자에 붙는다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] RectTransform face;
        [SerializeField] float pressDepth = 4;

        Button button;
        Vector2 rest;
        bool pressed;

        void Awake()
        {
            button = GetComponent<Button>();
            if (face != null) rest = face.anchoredPosition;
        }

        void OnDisable() => Release();

        public void OnPointerDown(PointerEventData e)
        {
            if (face == null || !button.IsInteractable()) return;
            pressed = true;
            face.anchoredPosition = rest + Vector2.down * pressDepth;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!pressed || face == null) return;
            pressed = false;
            face.anchoredPosition = rest;
        }
    }
}
