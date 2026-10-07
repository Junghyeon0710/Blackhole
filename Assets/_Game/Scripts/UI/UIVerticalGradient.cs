using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>UI 그래픽에 위→아래 세로 그라디언트를 입힌다 (카드 배경 #3a2a8a → #2b1f6b).</summary>
    public sealed class UIVerticalGradient : BaseMeshEffect
    {
        [SerializeField] Color top = Color.white;
        [SerializeField] Color bottom = Color.white;

        public void SetColors(Color topColor, Color bottomColor)
        {
            top = topColor;
            bottom = bottomColor;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var rect = graphic.rectTransform.rect;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float t = Mathf.InverseLerp(rect.yMin, rect.yMax, v.position.y);
                v.color *= Color.Lerp(bottom, top, t);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
