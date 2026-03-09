using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UITrapezoidWarp : BaseMeshEffect
{
    [Range(-1f, 1f)]
    [SerializeField] private float topInsetNormalized = 0.15f;
    // 양수면 위쪽 폭이 좁아짐
    // 음수면 위쪽 폭이 넓어짐

    [Range(-1f, 1f)]
    [SerializeField] private float bottomInsetNormalized = 0f;
    // 필요하면 아래쪽도 따로 조절

    [Range(-1f, 1f)]
    [SerializeField] private float horizontalShearNormalized = 0f;
    // 좌우로 전체가 기울어진 느낌

    public float TopInsetNormalized
    {
        get => topInsetNormalized;
        set
        {
            topInsetNormalized = value;
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount < 4)
            return;

        var verts = new System.Collections.Generic.List<UIVertex>();
        vh.GetUIVertexStream(verts);

        if (verts.Count < 4)
            return;

        float minX = verts[0].position.x;
        float maxX = verts[0].position.x;
        float minY = verts[0].position.y;
        float maxY = verts[0].position.y;

        for (int i = 1; i < verts.Count; i++)
        {
            Vector3 p = verts[i].position;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        float topInset = width * 0.5f * topInsetNormalized;
        float bottomInset = width * 0.5f * bottomInsetNormalized;
        float shear = width * 0.5f * horizontalShearNormalized;

        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            Vector3 p = v.position;

            float y01 = Mathf.InverseLerp(minY, maxY, p.y);

            // 아래쪽 -> bottomInset, 위쪽 -> topInset
            float inset = Mathf.Lerp(bottomInset, topInset, y01);

            // 중앙 기준으로 좌우 압축
            float centerX = (minX + maxX) * 0.5f;

            if (p.x < centerX)
                p.x += inset;
            else
                p.x -= inset;

            // 위로 갈수록 전체를 살짝 옆으로 밀어서 기울기 느낌
            p.x += Mathf.Lerp(-shear, shear, y01);

            v.position = p;
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}