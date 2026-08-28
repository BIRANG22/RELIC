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
            prefabObject.AddComponent<SpriteRenderer>();

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
            prefabObject.AddComponent<SpriteRenderer>();

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
            prefabObject.AddComponent<SpriteRenderer>();

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
            prefabObject.AddComponent<SpriteRenderer>();

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
            prefabObject.AddComponent<SpriteRenderer>();

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
            prefabObject.AddComponent<SpriteRenderer>();

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
