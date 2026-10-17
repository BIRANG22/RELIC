using UnityEngine;
using UnityEngine.UI;

/// <summary>Warps a UGUI Graphic's mesh to a four-corner quad in its local space.</summary>
[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public sealed class UIGridQuadMeshEffect : BaseMeshEffect, ICanvasRaycastFilter
{
    [SerializeField, HideInInspector] private Vector2 bottomLeft;
    [SerializeField, HideInInspector] private Vector2 bottomRight;
    [SerializeField, HideInInspector] private Vector2 topLeft;
    [SerializeField, HideInInspector] private Vector2 topRight;
    [SerializeField] private bool filterRaycasts = true;

    public void SetQuad(Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr)
    {
        bottomLeft = bl;
        bottomRight = br;
        topLeft = tl;
        topRight = tr;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || helper.currentVertCount == 0) return;
        var rect = ((RectTransform)transform).rect;
        if (rect.width < 0.0001f || rect.height < 0.0001f) return;

        UIVertex vertex = new UIVertex();
        for (int i = 0; i < helper.currentVertCount; i++)
        {
            helper.PopulateUIVertex(ref vertex, i);
            float u = (vertex.position.x - rect.xMin) / rect.width;
            float v = (vertex.position.y - rect.yMin) / rect.height;
            Vector2 a = Vector2.LerpUnclamped(bottomLeft, bottomRight, u);
            Vector2 b = Vector2.LerpUnclamped(topLeft, topRight, u);
            Vector2 target = Vector2.LerpUnclamped(a, b, v);
            vertex.position = new Vector3(target.x, target.y, vertex.position.z);
            helper.SetUIVertex(vertex, i);
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!filterRaycasts) return true;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, eventCamera, out Vector2 p))
            return false;
        return InsideTriangle(p, bottomLeft, bottomRight, topLeft) ||
               InsideTriangle(p, topLeft, bottomRight, topRight);
    }

    private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(b - a, p - a);
        float d2 = Cross(c - b, p - b);
        float d3 = Cross(a - c, p - c);
        return !(d1 < -0.001f || d2 < -0.001f || d3 < -0.001f) &&
               !(d1 > 0.001f || d2 > 0.001f || d3 > 0.001f);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
