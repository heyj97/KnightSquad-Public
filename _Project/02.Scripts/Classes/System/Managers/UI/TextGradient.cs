using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.yellow;

    public void SetBottomColor(Color color)
    {
        if (bottomColor == color) return;
        bottomColor = color;
        if (graphic != null) graphic.SetVerticesDirty();
    }
    
    public void SetTopColor(Color color)
    {
        if (topColor == color) return;
        topColor = color;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        UIVertex vertex = new UIVertex();

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // 텍스트의 상단과 하단 좌표 계산
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }

        float height = Mathf.Max(maxY - minY, 0.0001f);

        // 각 정점에 그라데이션 색상 적용
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float t = (vertex.position.y - minY) / height;
            Color gradientColor = Color.Lerp(bottomColor, topColor, t);

            // 기존 텍스트 투명도 유지
            gradientColor.a *= vertex.color.a;
            vertex.color = gradientColor;

            vh.SetUIVertex(vertex, i);
        }
    }
}
