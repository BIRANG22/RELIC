using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class UIPerspectiveSkew : BaseMeshEffect
{
    [Header("Skew")]
    [SerializeField] private float topSkewX = 0f;
    [SerializeField] private float bottomSkewX = 0f;

    [Header("Scale By Height")]
    [SerializeField] private float topScaleX = 1f;
    [SerializeField] private float bottomScaleX = 1f;

    public void SetSkew(float topX, float bottomX)
    {
        topSkewX = topX;
        bottomSkewX = bottomX;

        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public void SetScale(float topScale, float bottomScale)
    {
        topScaleX = topScale;
        bottomScaleX = bottomScale;

        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        List<UIVertex> verts = new();
        vh.GetUIVertexStream(verts);

        if (verts.Count == 0)
            return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float centerX = 0f;

        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 p = verts[i].position;
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
            centerX += p.x;
        }

        centerX /= verts.Count;
        float height = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            Vector3 p = v.position;

            float t = (p.y - minY) / height;

            float skew = Mathf.Lerp(bottomSkewX, topSkewX, t);
            float scaleX = Mathf.Lerp(bottomScaleX, topScaleX, t);

            p.x = centerX + (p.x - centerX) * scaleX + skew;

            v.position = p;
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}