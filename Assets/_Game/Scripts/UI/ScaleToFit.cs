using UnityEngine;

namespace Blackhole
{
    /// <summary>카드가 화면보다 크면(작은 폰, 가로 화면) 통째로 줄인다.</summary>
    public sealed class ScaleToFit : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] float margin = 20;

        void LateUpdate()
        {
            if (content == null || transform.parent is not RectTransform parent) return;
            var area = parent.rect;
            var size = content.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            float s = Mathf.Min(1, (area.width - margin * 2) / size.x, (area.height - margin * 2) / size.y);
            transform.localScale = new Vector3(s, s, 1);
        }
    }
}
