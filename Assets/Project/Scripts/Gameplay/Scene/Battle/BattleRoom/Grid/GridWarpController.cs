using UnityEngine;

[ExecuteAlways]
public class GridQuadWarpController : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int width = 7;
    [SerializeField] private int height = 5;

    [Header("Board Corners - Local Space")]
    [SerializeField] private Vector3 bottomLeft = new Vector3(-3, 0, -2);
    [SerializeField] private Vector3 bottomRight = new Vector3(3, 0, -2);
    [SerializeField] private Vector3 topLeft = new Vector3(-2, 0, 2);
    [SerializeField] private Vector3 topRight = new Vector3(2, 0, 2);

    [Header("Highlight")]
    [SerializeField] private string highlightChildName = "Highlight";
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float highlightYOffset = 0.01f;
    [SerializeField, Range(0f, 0.3f)]
    private float highlightInset = 0.08f;
    private void OnValidate()
    {
        ApplyWarp();
    }

    private void Awake()
    {
        ApplyWarp();
    }

    public void ApplyWarp()
    {
        if (width <= 0 || height <= 0)
            return;

        int requiredCount = width * height;

        for (int i = 0; i < requiredCount; i++)
        {
            if (i >= transform.childCount)
                break;

            int x = i / height;
            int y = height - 1 - (i % height);

            Transform cell = transform.GetChild(i);

            Vector3 p00 = GetPoint(x, y);
            Vector3 p10 = GetPoint(x + 1, y);
            Vector3 p01 = GetPoint(x, y + 1);
            Vector3 p11 = GetPoint(x + 1, y + 1);

            ApplyCellMesh(cell, p00, p10, p01, p11);
            ApplyHighlightMesh(cell, p00, p10, p01, p11);
        }
    }

    private Vector3 GetPoint(int x, int y)
    {
        float u = x / (float)width;
        float v = y / (float)height;

        Vector3 left = Vector3.Lerp(bottomLeft, topLeft, v);
        Vector3 right = Vector3.Lerp(bottomRight, topRight, v);

        return Vector3.Lerp(left, right, u);
    }

    private void ApplyCellMesh(
        Transform cell,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        Vector3 topLeft,
        Vector3 topRight)
    {
        Vector3 center = (bottomLeft + bottomRight + topLeft + topRight) * 0.25f;

        cell.localPosition = center;
        cell.localRotation = Quaternion.identity;
        cell.localScale = Vector3.one;

        MeshFilter meshFilter = cell.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = cell.gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = cell.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = cell.gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = CreateCellMesh(
            bottomLeft - center,
            bottomRight - center,
            topLeft - center,
            topRight - center,
            0f
        );

        mesh.name = "Warped Grid Cell";
        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = cell.GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    private void ApplyHighlightMesh(
        Transform cell,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        Vector3 topLeft,
        Vector3 topRight)
    {
        Transform highlight = cell.Find(highlightChildName);

        if (highlight == null)
        {
            GameObject highlightObject = new GameObject(highlightChildName);
            highlight = highlightObject.transform;
            highlight.SetParent(cell, false);
        }

        highlight.localPosition = new Vector3(0f, highlightYOffset, 0f);
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = Vector3.one;

        MeshFilter meshFilter = highlight.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = highlight.gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = highlight.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = highlight.gameObject.AddComponent<MeshRenderer>();

        if (highlightMaterial != null)
            meshRenderer.sharedMaterial = highlightMaterial;

        Vector3 center = (bottomLeft + bottomRight + topLeft + topRight) * 0.25f;

        Mesh mesh = CreateCellMesh(
            bottomLeft - center,
            bottomRight - center,
            topLeft - center,
            topRight - center,
            highlightInset
        );

        mesh.name = "Warped Grid Highlight";
        meshFilter.sharedMesh = mesh;

        Collider collider = highlight.GetComponent<Collider>();
        if (collider != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(collider);
            else
                Destroy(collider);
#else
            Destroy(collider);
#endif
        }

        highlight.gameObject.SetActive(false);
    }

    private Mesh CreateCellMesh(
    Vector3 bottomLeft,
    Vector3 bottomRight,
    Vector3 topLeft,
    Vector3 topRight,
    float inset = 0f)
    {
        Mesh mesh = new Mesh();

        if (inset > 0f)
        {
            Vector3 center = (bottomLeft + bottomRight + topLeft + topRight) * 0.25f;

            bottomLeft = Vector3.Lerp(bottomLeft, center, inset);
            bottomRight = Vector3.Lerp(bottomRight, center, inset);
            topLeft = Vector3.Lerp(topLeft, center, inset);
            topRight = Vector3.Lerp(topRight, center, inset);
        }

        Vector3[] vertices =
        {
        bottomLeft,
        bottomRight,
        topLeft,
        topRight
    };

        int[] triangles =
        {
        0, 2, 1,
        2, 3, 1
    };

        Vector2[] uvs =
        {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 1),
        new Vector2(1, 1)
    };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}