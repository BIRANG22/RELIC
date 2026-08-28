using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MovePathPreviewTests
{
    [Test]
    public void ShowPath_SkipsCasterAndDestinationCellsAndUsesPreDestinationEndTile()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 4, 3);
            SetPrivateField(gridManager, "width", 4);
            SetPrivateField(gridManager, "height", 3);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 0));
            preview.ShowPath(
                startIndex,
                new List<Vector2Int>
                {
                    Vector2Int.right,
                    Vector2Int.right,
                    Vector2Int.up
                });

            Assert.That(rootObject.transform.childCount, Is.EqualTo(2));

            MovePathTileView first = rootObject.transform.GetChild(0).GetComponent<MovePathTileView>();
            MovePathTileView second = rootObject.transform.GetChild(1).GetComponent<MovePathTileView>();

            Assert.That(first.Kind, Is.EqualTo(MovePathTileKind.Straight));
            Assert.That(second.Kind, Is.EqualTo(MovePathTileKind.CornerEnd));
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(second.gameObject.activeSelf, Is.True);
            Assert.That(first.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(first.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(first.GetComponent<SpriteRenderer>(), Is.Null);
            Assert.That(first.transform.position, Is.EqualTo(gridManager.GetWorldPositionByIndex(1)));
            Assert.That(second.transform.position, Is.EqualTo(gridManager.GetWorldPositionByIndex(2)));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    [Test]
    public void Clear_RemovesSpawnedPathTiles()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 3, 1);
            SetPrivateField(gridManager, "width", 3);
            SetPrivateField(gridManager, "height", 1);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            preview.ShowPath(0, new List<Vector2Int> { Vector2Int.right, Vector2Int.right });
            Assert.That(rootObject.transform.childCount, Is.EqualTo(1));

            preview.Clear();

            Assert.That(rootObject.transform.childCount, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    [Test]
    public void Apply_WarpsMeshToGridCellShapeWithoutDepthSubmeshes()
    {
        GameObject tileObject = new("MovePathTile");
        GameObject cellObject = new("WarpedCell");

        try
        {
            GridCell cell = CreateWarpedGridCell(cellObject);
            MovePathTileView tile = tileObject.AddComponent<MovePathTileView>();
            tileObject.transform.position = cellObject.transform.position + new Vector3(0f, 0.03f, 0f);

            tile.Apply(
                MovePathTileKind.End,
                MovePathTileDirection.Right,
                MovePathTileDirection.Right,
                0f,
                0f,
                cell,
                1f);

            MeshFilter meshFilter = tileObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = tileObject.GetComponent<MeshRenderer>();

            Assert.That(tileObject.GetComponent<SpriteRenderer>(), Is.Null);
            Assert.That(meshFilter, Is.Not.Null);
            Assert.That(meshRenderer, Is.Not.Null);

            Mesh mesh = meshFilter.sharedMesh;

            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(mesh.vertexCount, Is.GreaterThan(6));
            Assert.That(meshRenderer.sharedMaterials.Length, Is.EqualTo(1));
            Assert.That(mesh.bounds.size.x, Is.GreaterThan(0.5f));
            Assert.That(mesh.bounds.size.y, Is.GreaterThan(0.5f));
            Assert.That(mesh.bounds.size.z, Is.EqualTo(0f).Within(0.001f));
            AssertContainsWorldVertex(
                tile,
                GetExpectedWarpedPoint(cellObject.transform.position, 1.04f, 0.5f) + new Vector3(0f, 0.03f, 0f));
            AssertRotation(tile, 0f, 0f);
        }
        finally
        {
            Object.DestroyImmediate(cellObject);
            Object.DestroyImmediate(tileObject);
        }
    }

    [Test]
    public void Apply_WarpsCornerMeshThroughEntryAndExitEdges()
    {
        GameObject tileObject = new("MovePathTile");
        GameObject cellObject = new("WarpedCell");

        try
        {
            GridCell cell = CreateWarpedGridCell(cellObject);
            MovePathTileView tile = tileObject.AddComponent<MovePathTileView>();
            tileObject.transform.position = cellObject.transform.position + new Vector3(0f, 0.03f, 0f);

            tile.Apply(
                MovePathTileKind.Corner,
                MovePathTileDirection.Right,
                MovePathTileDirection.Down,
                0f,
                0f,
                cell,
                1f);

            AssertContainsWorldVertex(
                tile,
                GetExpectedWarpedPoint(cellObject.transform.position, -0.04f, 0.415f) + new Vector3(0f, 0.03f, 0f));
            AssertContainsWorldVertex(
                tile,
                GetExpectedWarpedPoint(cellObject.transform.position, 0.415f, -0.04f) + new Vector3(0f, 0.03f, 0f));
            AssertRotation(tile, 0f, 0f);
        }
        finally
        {
            Object.DestroyImmediate(cellObject);
            Object.DestroyImmediate(tileObject);
        }
    }

    [Test]
    public void ShowPath_AppliesRequestedCornerRotationTable()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 4, 4);
            SetPrivateField(gridManager, "width", 4);
            SetPrivateField(gridManager, "height", 4);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 2), Vector2Int.right, Vector2Int.down, 0f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 0), Vector2Int.up, Vector2Int.left, 0f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 0), Vector2Int.up, Vector2Int.right, 90f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 1), Vector2Int.left, Vector2Int.down, 90f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 2), Vector2Int.down, Vector2Int.right, 180f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 0), Vector2Int.left, Vector2Int.up, 180f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 0), Vector2Int.right, Vector2Int.up, 270f);
            AssertCornerRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 2), Vector2Int.down, Vector2Int.left, 270f);
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    [Test]
    public void ShowPath_UsesCornerRotationForCornerEndTile()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 3, 3);
            SetPrivateField(gridManager, "width", 3);
            SetPrivateField(gridManager, "height", 3);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
            preview.ShowPath(
                startIndex,
                new List<Vector2Int>
                {
                    Vector2Int.right,
                    Vector2Int.down
                });

            MovePathTileView cornerEnd = rootObject.transform.GetChild(0).GetComponent<MovePathTileView>();
            Assert.That(cornerEnd.Kind, Is.EqualTo(MovePathTileKind.CornerEnd));
            AssertRotation(cornerEnd, 0f, 0f);
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    [Test]
    public void ShowPath_AppliesRequestedCornerEndRotationTable()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 4, 4);
            SetPrivateField(gridManager, "width", 4);
            SetPrivateField(gridManager, "height", 4);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 2), Vector2Int.right, Vector2Int.down, 0f, 0f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 0), Vector2Int.up, Vector2Int.right, 0f, 90f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 0), Vector2Int.left, Vector2Int.up, 0f, 180f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 2), Vector2Int.down, Vector2Int.left, 0f, 270f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 1), Vector2Int.left, Vector2Int.down, 180f, 0f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(2, 0), Vector2Int.up, Vector2Int.left, 180f, 90f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 0), Vector2Int.right, Vector2Int.up, 180f, 180f);
            AssertCornerEndRotation(preview, rootObject.transform, gridManager, new Vector2Int(0, 2), Vector2Int.down, Vector2Int.right, 180f, 270f);
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    [Test]
    public void ShowPath_UsesVisualDirectionsForCornerRotationWhenGridYIsInverted()
    {
        GameObject previewObject = new("MovePathPreview");
        GameObject rootObject = new("MovePathPreviewRoot");
        GameObject prefabObject = new("MovePathTilePrefab");
        GameObject gridObject = new("MovePathGrid");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreatePositionedGridCells(gridObject.transform, 4, 4, true);
            SetPrivateField(gridManager, "width", 4);
            SetPrivateField(gridManager, "height", 4);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "InitializeCells");

            MovePathTileView prefab = prefabObject.AddComponent<MovePathTileView>();

            MovePathPreview preview = previewObject.AddComponent<MovePathPreview>();
            preview.ConfigureForTest(gridManager, prefab, rootObject.transform);

            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 0));
            preview.ShowPath(
                startIndex,
                new List<Vector2Int>
                {
                    Vector2Int.right,
                    Vector2Int.up,
                    Vector2Int.up
                });

            MovePathTileView corner = rootObject.transform.GetChild(0).GetComponent<MovePathTileView>();
            Assert.That(corner.Kind, Is.EqualTo(MovePathTileKind.Corner));
            AssertRotationZ(corner, 0f);
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(previewObject);
        }
    }

    private static GridCell[] CreatePositionedGridCells(
        Transform parent,
        int width,
        int height,
        bool invertWorldY = false)
    {
        GridCell[] cells = new GridCell[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = x * height + y;
                GameObject cellObject = new($"Cell_{index}");
                cellObject.transform.SetParent(parent);
                cellObject.transform.position = new Vector3(x, invertWorldY ? -y : y, 0f);
                cellObject.AddComponent<MeshRenderer>();
                cellObject.AddComponent<BoxCollider>();
                cells[index] = cellObject.AddComponent<GridCell>();
            }
        }

        return cells;
    }

    private static GridCell CreateWarpedGridCell(GameObject cellObject)
    {
        cellObject.transform.position = new Vector3(10f, 20f, 0f);

        Mesh mesh = new()
        {
            vertices = new[]
            {
                new Vector3(-0.6f, -0.35f, 0f),
                new Vector3(0.7f, -0.25f, 0f),
                new Vector3(-0.45f, 0.4f, 0f),
                new Vector3(0.55f, 0.3f, 0f)
            },
            triangles = new[]
            {
                0, 2, 1,
                2, 3, 1
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            }
        };
        mesh.RecalculateBounds();

        cellObject.AddComponent<MeshRenderer>();
        cellObject.AddComponent<BoxCollider>();
        cellObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        return cellObject.AddComponent<GridCell>();
    }

    private static Vector3 GetExpectedWarpedPoint(Vector3 cellPosition, float u, float v)
    {
        Vector3 bottomLeft = cellPosition + new Vector3(-0.6f, -0.35f, 0f);
        Vector3 bottomRight = cellPosition + new Vector3(0.7f, -0.25f, 0f);
        Vector3 topLeft = cellPosition + new Vector3(-0.45f, 0.4f, 0f);
        Vector3 topRight = cellPosition + new Vector3(0.55f, 0.3f, 0f);
        Vector3 bottom = Vector3.LerpUnclamped(bottomLeft, bottomRight, u);
        Vector3 top = Vector3.LerpUnclamped(topLeft, topRight, u);

        return Vector3.LerpUnclamped(bottom, top, v);
    }

    private static void AssertContainsWorldVertex(MovePathTileView tile, Vector3 expectedWorldPosition)
    {
        Mesh mesh = tile.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPosition = tile.transform.TransformPoint(vertices[i]);
            closestDistance = Mathf.Min(closestDistance, Vector3.Distance(worldPosition, expectedWorldPosition));
        }

        Assert.That(closestDistance, Is.LessThan(0.001f));
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        System.Reflection.MethodInfo method = target.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        method.Invoke(target, null);
    }

    private static void AssertRotationZ(Transform transform, float expectedZ)
    {
        float z = Mathf.Repeat(transform.localEulerAngles.z, 360f);
        Assert.That(z, Is.EqualTo(expectedZ).Within(0.001f));
    }

    private static void AssertRotationZ(MovePathTileView tile, float expectedZ)
    {
        AssertRotationZ(tile.transform, expectedZ);
        Assert.That(tile.AppliedRotationZ, Is.EqualTo(expectedZ).Within(0.001f));
    }

    private static void AssertRotation(MovePathTileView tile, float expectedY, float expectedZ)
    {
        if (Mathf.Approximately(expectedY, 0f))
            AssertRotationZ(tile.transform, expectedZ);

        Assert.That(tile.AppliedRotationY, Is.EqualTo(expectedY).Within(0.001f));
        Assert.That(tile.AppliedRotationZ, Is.EqualTo(expectedZ).Within(0.001f));
    }

    private static void AssertCornerRotation(
        MovePathPreview preview,
        Transform root,
        GridManager gridManager,
        Vector2Int startCoord,
        Vector2Int firstStep,
        Vector2Int secondStep,
        float expectedRotationZ)
    {
        int startIndex = gridManager.CoordToIndex(startCoord);

        preview.ShowPath(
            startIndex,
            new List<Vector2Int>
            {
                firstStep,
                secondStep,
                secondStep
            });

        MovePathTileView corner = root.GetChild(0).GetComponent<MovePathTileView>();
        Assert.That(corner.Kind, Is.EqualTo(MovePathTileKind.Corner));
        AssertRotationZ(corner, expectedRotationZ);
    }

    private static void AssertCornerEndRotation(
        MovePathPreview preview,
        Transform root,
        GridManager gridManager,
        Vector2Int startCoord,
        Vector2Int firstStep,
        Vector2Int secondStep,
        float expectedRotationY,
        float expectedRotationZ)
    {
        int startIndex = gridManager.CoordToIndex(startCoord);

        preview.ShowPath(
            startIndex,
            new List<Vector2Int>
            {
                firstStep,
                secondStep
            });

        MovePathTileView cornerEnd = root.GetChild(0).GetComponent<MovePathTileView>();
        Assert.That(cornerEnd.Kind, Is.EqualTo(MovePathTileKind.CornerEnd));
        AssertRotation(cornerEnd, expectedRotationY, expectedRotationZ);
    }
}
