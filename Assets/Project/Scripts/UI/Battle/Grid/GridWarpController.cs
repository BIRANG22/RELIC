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

            int x = i % width;
            int y = i / width;

            Transform cell = transform.GetChild(i);

            Vector3 p00 = GetPoint(x, y);
            Vector3 p10 = GetPoint(x + 1, y);
            Vector3 p01 = GetPoint(x, y + 1);
            Vector3 p11 = GetPoint(x + 1, y + 1);

            ApplyCellMesh(cell, p00, p10, p01, p11);
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

    private void ApplyCellMesh(Transform cell, Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight)
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

        Mesh mesh = new Mesh();
        mesh.name = "Warped Grid Cell";

        Vector3[] vertices =
        {
            bottomLeft - center,
            bottomRight - center,
            topLeft - center,
            topRight - center
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

        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = cell.GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
}