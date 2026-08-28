using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum MovePathTileKind
{
    Straight,
    Corner,
    CornerEnd,
    End
}

public enum MovePathTileDirection
{
    Right,
    Left,
    Up,
    Down
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MovePathTileView : MonoBehaviour
{
    private const float CellHalfSize = 0.5f;
    private const float MinimumWidthScale = 0.05f;

    [Header("Renderer")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material pathMaterial;

    [Header("Shape")]
    [SerializeField, Range(0.03f, 0.18f)] private float bodyHalfWidth = 0.085f;
    [SerializeField, Range(0.06f, 0.36f)] private float arrowHeadLength = 0.26f;
    [SerializeField, Range(0.08f, 0.34f)] private float arrowHeadHalfWidth = 0.22f;
    [SerializeField, Range(0f, 0.18f)] private float edgeOverlap = 0.04f;

    [Header("Fallback Color")]
    [SerializeField] private Color pathColor = new(0.16f, 0.95f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private float appliedRotationY;
    [SerializeField] private float appliedRotationZ;

    private Mesh runtimeMesh;
    private Material runtimePathMaterial;

    public MovePathTileKind Kind { get; private set; }
    public float AppliedRotationY => appliedRotationY;
    public float AppliedRotationZ => appliedRotationZ;

    private void Awake()
    {
        EnsureComponents();
    }

    private void OnDestroy()
    {
        DestroyGeneratedObject(runtimeMesh);
        DestroyGeneratedObject(runtimePathMaterial);
    }

    public void ConfigureSprites(Sprite straight, Sprite corner, Sprite cornerEnd, Sprite end)
    {
    }

    public void ConfigureSprites(Sprite straight, Sprite corner, Sprite end)
    {
    }

    public void Apply(MovePathTileKind kind, float rotationZ)
    {
        Apply(kind, 0f, rotationZ);
    }

    public void Apply(MovePathTileKind kind, float rotationY, float rotationZ)
    {
        Apply(kind, rotationY, rotationZ, null, 1f);
    }

    public void Apply(MovePathTileKind kind, float rotationY, float rotationZ, GridCell targetCell, float widthScale)
    {
        MovePathTileDirection direction = RotationToDirection(rotationZ);
        Apply(kind, direction, direction, rotationY, rotationZ, targetCell, widthScale);
    }

    public void Apply(
        MovePathTileKind kind,
        MovePathTileDirection incoming,
        MovePathTileDirection outgoing,
        float rotationY,
        float rotationZ,
        GridCell targetCell,
        float widthScale)
    {
        EnsureComponents();

        Kind = kind;
        appliedRotationY = NormalizeRotation(rotationY);
        appliedRotationZ = NormalizeRotation(rotationZ);

        transform.localEulerAngles = new Vector3(0f, appliedRotationY, appliedRotationZ);

        BuildMesh(kind, incoming, outgoing, targetCell, Mathf.Max(MinimumWidthScale, widthScale));
        ApplyMaterial();
    }

    public void ApplySorting(string sortingLayerName, int sortingOrder)
    {
        EnsureComponents();

        if (meshRenderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
            meshRenderer.sortingLayerName = sortingLayerName;

        meshRenderer.sortingOrder = sortingOrder;
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        SpriteRenderer legacySpriteRenderer = GetComponent<SpriteRenderer>();
        if (legacySpriteRenderer != null)
            legacySpriteRenderer.enabled = false;

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void BuildMesh(
        MovePathTileKind kind,
        MovePathTileDirection incoming,
        MovePathTileDirection outgoing,
        GridCell targetCell,
        float widthScale)
    {
        if (runtimeMesh == null)
        {
            runtimeMesh = new Mesh
            {
                name = "Move Path Tile Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            runtimeMesh.MarkDynamic();
        }
        else
        {
            runtimeMesh.Clear();
        }

        ShapeBuilder shapeBuilder = new();
        AddPathShape(shapeBuilder, kind, incoming, outgoing, widthScale);

        List<Vector3> vertices = new(shapeBuilder.Points.Count);
        List<Vector2> uvs = new(shapeBuilder.Points.Count);
        CellWarp cellWarp = TryGetCellWarp(targetCell, out CellWarp targetCellWarp)
            ? targetCellWarp
            : CellWarp.CreateFlat(transform.position);

        for (int i = 0; i < shapeBuilder.Points.Count; i++)
        {
            Vector2 point = shapeBuilder.Points[i];
            Vector3 worldPoint = cellWarp.Sample(point);

            vertices.Add(transform.InverseTransformPoint(worldPoint));
            uvs.Add(new Vector2(
                Mathf.Clamp01(point.x + 0.5f),
                Mathf.Clamp01(point.y + 0.5f)));
        }

        runtimeMesh.SetVertices(vertices);
        runtimeMesh.SetUVs(0, uvs);
        runtimeMesh.subMeshCount = 1;
        runtimeMesh.SetTriangles(shapeBuilder.Triangles, 0);
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateBounds();

        meshFilter.sharedMesh = runtimeMesh;
    }

    private void AddPathShape(
        ShapeBuilder builder,
        MovePathTileKind kind,
        MovePathTileDirection incoming,
        MovePathTileDirection outgoing,
        float widthScale)
    {
        float scaledBodyHalfWidth = bodyHalfWidth * widthScale;
        float scaledArrowHeadLength = arrowHeadLength * widthScale;
        float scaledArrowHeadHalfWidth = arrowHeadHalfWidth * widthScale;
        Vector2 entryPoint = GetEdgePoint(Opposite(incoming));
        Vector2 exitPoint = GetEdgePoint(outgoing);

        switch (kind)
        {
            case MovePathTileKind.Corner:
                AddCornerBody(builder, entryPoint, exitPoint, scaledBodyHalfWidth);
                break;
            case MovePathTileKind.CornerEnd:
                AddCornerEndBody(
                    builder,
                    entryPoint,
                    exitPoint,
                    scaledBodyHalfWidth,
                    scaledArrowHeadLength,
                    scaledArrowHeadHalfWidth);
                break;
            case MovePathTileKind.End:
                AddArrowSegment(
                    builder,
                    entryPoint,
                    exitPoint,
                    scaledBodyHalfWidth,
                    scaledArrowHeadLength,
                    scaledArrowHeadHalfWidth);
                break;
            default:
                AddSegment(builder, entryPoint, exitPoint, scaledBodyHalfWidth);
                break;
        }
    }

    private void AddCornerBody(
        ShapeBuilder builder,
        Vector2 entryPoint,
        Vector2 exitPoint,
        float halfWidth)
    {
        AddSegment(builder, entryPoint, Vector2.zero, halfWidth);
        AddSegment(builder, Vector2.zero, exitPoint, halfWidth);
        AddJoint(builder, halfWidth);
    }

    private void AddCornerEndBody(
        ShapeBuilder builder,
        Vector2 entryPoint,
        Vector2 exitPoint,
        float halfWidth,
        float headLength,
        float headHalfWidth)
    {
        AddSegment(builder, entryPoint, Vector2.zero, halfWidth);
        AddArrowSegment(builder, Vector2.zero, exitPoint, halfWidth, headLength, headHalfWidth);
        AddJoint(builder, halfWidth);
    }

    private void ApplyMaterial()
    {
        if (meshRenderer == null)
            return;

        runtimePathMaterial = EnsureRuntimeMaterial(runtimePathMaterial, pathMaterial, pathColor, "Move Path");
        meshRenderer.sharedMaterial = runtimePathMaterial;
    }

    private static void AddRect(
        ShapeBuilder builder,
        float minX,
        float minY,
        float maxX,
        float maxY)
    {
        builder.AddQuad(
            new Vector2(minX, minY),
            new Vector2(maxX, minY),
            new Vector2(maxX, maxY),
            new Vector2(minX, maxY));
    }

    private void AddSegment(ShapeBuilder builder, Vector2 from, Vector2 to, float halfWidth)
    {
        Vector2 delta = to - from;

        if (delta.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            float minX = Mathf.Min(from.x, to.x);
            float maxX = Mathf.Max(from.x, to.x);
            AddRect(builder, minX, from.y - halfWidth, maxX, from.y + halfWidth);
            return;
        }

        float minY = Mathf.Min(from.y, to.y);
        float maxY = Mathf.Max(from.y, to.y);
        AddRect(builder, from.x - halfWidth, minY, from.x + halfWidth, maxY);
    }

    private void AddArrowSegment(
        ShapeBuilder builder,
        Vector2 from,
        Vector2 to,
        float halfWidth,
        float headLength,
        float headHalfWidth)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;

        if (length <= Mathf.Epsilon)
            return;

        Vector2 direction = delta / length;
        Vector2 perpendicular = new(-direction.y, direction.x);
        float clampedHeadLength = Mathf.Min(headLength, length);
        Vector2 headBase = to - direction * clampedHeadLength;

        AddSegment(builder, from, headBase, halfWidth);
        builder.AddTriangle(
            headBase - perpendicular * headHalfWidth,
            to,
            headBase + perpendicular * headHalfWidth);
    }

    private static void AddJoint(ShapeBuilder builder, float halfWidth)
    {
        AddRect(builder, -halfWidth, -halfWidth, halfWidth, halfWidth);
    }

    private Vector2 GetEdgePoint(MovePathTileDirection direction)
    {
        float edge = CellHalfSize + Mathf.Max(0f, edgeOverlap);

        return direction switch
        {
            MovePathTileDirection.Left => new Vector2(-edge, 0f),
            MovePathTileDirection.Up => new Vector2(0f, edge),
            MovePathTileDirection.Down => new Vector2(0f, -edge),
            _ => new Vector2(edge, 0f)
        };
    }

    private static MovePathTileDirection Opposite(MovePathTileDirection direction)
    {
        return direction switch
        {
            MovePathTileDirection.Left => MovePathTileDirection.Right,
            MovePathTileDirection.Up => MovePathTileDirection.Down,
            MovePathTileDirection.Down => MovePathTileDirection.Up,
            _ => MovePathTileDirection.Left
        };
    }

    private bool TryGetCellWarp(GridCell targetCell, out CellWarp cellWarp)
    {
        cellWarp = default;

        if (targetCell == null)
            return false;

        MeshFilter targetMeshFilter = targetCell.GetComponent<MeshFilter>();
        Mesh targetMesh = targetMeshFilter != null ? targetMeshFilter.sharedMesh : null;

        if (targetMesh == null || targetMesh.vertexCount < 4)
            return false;

        Vector3[] vertices = targetMesh.vertices;
        Vector2[] uvs = targetMesh.uv;

        if (uvs != null && uvs.Length == vertices.Length)
        {
            Vector3 surfaceOffset = transform.position - targetCell.transform.position;
            cellWarp = new CellWarp(
                targetCell.transform.TransformPoint(FindVertexByUv(vertices, uvs, new Vector2(0f, 0f))) + surfaceOffset,
                targetCell.transform.TransformPoint(FindVertexByUv(vertices, uvs, new Vector2(1f, 0f))) + surfaceOffset,
                targetCell.transform.TransformPoint(FindVertexByUv(vertices, uvs, new Vector2(0f, 1f))) + surfaceOffset,
                targetCell.transform.TransformPoint(FindVertexByUv(vertices, uvs, new Vector2(1f, 1f))) + surfaceOffset);
            return true;
        }

        Bounds bounds = targetMesh.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 offset = transform.position - targetCell.transform.position;
        cellWarp = new CellWarp(
            targetCell.transform.TransformPoint(new Vector3(min.x, min.y, min.z)) + offset,
            targetCell.transform.TransformPoint(new Vector3(max.x, min.y, min.z)) + offset,
            targetCell.transform.TransformPoint(new Vector3(min.x, max.y, max.z)) + offset,
            targetCell.transform.TransformPoint(new Vector3(max.x, max.y, max.z)) + offset);
        return true;
    }

    private static Vector3 FindVertexByUv(Vector3[] vertices, Vector2[] uvs, Vector2 targetUv)
    {
        int bestIndex = 0;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < uvs.Length; i++)
        {
            float distance = (uvs[i] - targetUv).sqrMagnitude;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestIndex = i;
        }

        return vertices[bestIndex];
    }

    private static Material EnsureRuntimeMaterial(
        Material current,
        Material source,
        Color color,
        string materialName)
    {
        if (current != null)
        {
            ApplyColor(current, color);
            return current;
        }

        Shader shader = source != null ? source.shader : FindFallbackShader();
        if (shader == null)
            return source;

        Material material = source != null
            ? new Material(source)
            : new Material(shader);

        material.name = materialName;
        material.hideFlags = HideFlags.HideAndDontSave;
        material.renderQueue = 3000;

        ConfigureMaterial(material, color);
        return material;
    }

    private static Shader FindFallbackShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit") ??
               Shader.Find("Sprites/Default") ??
               Shader.Find("Unlit/Color") ??
               Shader.Find("Standard");
    }

    private static void ConfigureMaterial(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        ApplyColor(material, color);
    }

    private static void ApplyColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private static float NormalizeRotation(float rotationZ)
    {
        float normalized = rotationZ % 360f;

        if (normalized < 0f)
            normalized += 360f;

        return normalized;
    }

    private static MovePathTileDirection RotationToDirection(float rotationZ)
    {
        float normalized = NormalizeRotation(rotationZ);

        if (normalized >= 45f && normalized < 135f)
            return MovePathTileDirection.Down;

        if (normalized >= 135f && normalized < 225f)
            return MovePathTileDirection.Left;

        if (normalized >= 225f && normalized < 315f)
            return MovePathTileDirection.Up;

        return MovePathTileDirection.Right;
    }

    private readonly struct CellWarp
    {
        private readonly Vector3 bottomLeft;
        private readonly Vector3 bottomRight;
        private readonly Vector3 topLeft;
        private readonly Vector3 topRight;

        public CellWarp(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight)
        {
            this.bottomLeft = bottomLeft;
            this.bottomRight = bottomRight;
            this.topLeft = topLeft;
            this.topRight = topRight;
        }

        public static CellWarp CreateFlat(Vector3 center)
        {
            return new CellWarp(
                center + new Vector3(-CellHalfSize, -CellHalfSize, 0f),
                center + new Vector3(CellHalfSize, -CellHalfSize, 0f),
                center + new Vector3(-CellHalfSize, CellHalfSize, 0f),
                center + new Vector3(CellHalfSize, CellHalfSize, 0f));
        }

        public Vector3 Sample(Vector2 point)
        {
            float u = point.x + 0.5f;
            float v = point.y + 0.5f;
            Vector3 bottom = Vector3.LerpUnclamped(bottomLeft, bottomRight, u);
            Vector3 top = Vector3.LerpUnclamped(topLeft, topRight, u);

            return Vector3.LerpUnclamped(bottom, top, v);
        }
    }

    private sealed class ShapeBuilder
    {
        public readonly List<Vector2> Points = new();
        public readonly List<int> Triangles = new();

        public void AddQuad(Vector2 bottomLeft, Vector2 bottomRight, Vector2 topRight, Vector2 topLeft)
        {
            int start = Points.Count;

            Points.Add(bottomLeft);
            Points.Add(bottomRight);
            Points.Add(topRight);
            Points.Add(topLeft);

            Triangles.Add(start);
            Triangles.Add(start + 1);
            Triangles.Add(start + 2);
            Triangles.Add(start);
            Triangles.Add(start + 2);
            Triangles.Add(start + 3);
        }

        public void AddTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            int start = Points.Count;

            Points.Add(a);
            Points.Add(b);
            Points.Add(c);

            Triangles.Add(start);
            Triangles.Add(start + 1);
            Triangles.Add(start + 2);
        }
    }
}
