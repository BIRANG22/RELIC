using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIQuadWarp : BaseMeshEffect
{
    [System.Serializable]
    public struct CornerOffset
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomRight;
        public Vector2 bottomLeft;
    }

    [Header("Normalized Corner Offsets")]
    [SerializeField] private CornerOffset normalizedOffset;

    public CornerOffset Offset
    {
        get => normalizedOffset;
        set
        {
            normalizedOffset = value;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    }

    public void SetCorners(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
    {
        normalizedOffset.topLeft = topLeft;
        normalizedOffset.topRight = topRight;
        normalizedOffset.bottomRight = bottomRight;
        normalizedOffset.bottomLeft = bottomLeft;

        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        List<UIVertex> verts = new List<UIVertex>();
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
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;

        if (Mathf.Approximately(width, 0f) || Mathf.Approximately(height, 0f))
            return;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        Vector2 srcBL = new Vector2(minX, minY);
        Vector2 srcTL = new Vector2(minX, maxY);
        Vector2 srcTR = new Vector2(maxX, maxY);
        Vector2 srcBR = new Vector2(maxX, minY);

        Vector2 dstTL = srcTL + new Vector2(normalizedOffset.topLeft.x * halfW, normalizedOffset.topLeft.y * halfH);
        Vector2 dstTR = srcTR + new Vector2(normalizedOffset.topRight.x * halfW, normalizedOffset.topRight.y * halfH);
        Vector2 dstBR = srcBR + new Vector2(normalizedOffset.bottomRight.x * halfW, normalizedOffset.bottomRight.y * halfH);
        Vector2 dstBL = srcBL + new Vector2(normalizedOffset.bottomLeft.x * halfW, normalizedOffset.bottomLeft.y * halfH);

        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            Vector3 p = v.position;

            float u = Mathf.InverseLerp(minX, maxX, p.x);
            float t = Mathf.InverseLerp(minY, maxY, p.y);

            Vector2 left = Vector2.Lerp(dstBL, dstTL, t);
            Vector2 right = Vector2.Lerp(dstBR, dstTR, t);
            Vector2 warped = Vector2.Lerp(left, right, u);

            v.position = new Vector3(warped.x, warped.y, p.z);
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}