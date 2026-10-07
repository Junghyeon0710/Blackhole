using UnityEngine;

namespace Blackhole
{
    /// <summary>시작·결과 창. 화면 전체를 어둡게 덮는다. 카드의 튀어나오는 연출은 <see cref="PopIn"/> 이 맡는다.</summary>
    public sealed class Overlay : MonoBehaviour
    {
        public bool IsShown => gameObject.activeSelf;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
