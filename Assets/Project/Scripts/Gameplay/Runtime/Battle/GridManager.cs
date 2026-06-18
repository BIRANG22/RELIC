using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int width = 7;
    [SerializeField] private int height = 5;

    [Header("Cells")]
    [SerializeField] private GridCell[] cells;

    private GridCell[,] cellMap;
    private bool isGridVisible = true;

    public event Action<GridCell> OnCellClicked;
    public event Action<GridCell> OnCellHovered;
    public event Action<GridCell> OnCellHoverExited;

    public int Width => width;
    public int Height => height;
    public bool IsGridVisible => isGridVisible;

    private void Awake()
    {
        InitializeCells();
    }

    private void InitializeCells()
    {
        if (cells == null || cells.Length == 0)
            cells = GetComponentsInChildren<GridCell>(true);

        cellMap = new GridCell[width, height];

        for (int i = 0; i < cells.Length; i++)
        {
            GridCell cell = cells[i];

            if (cell == null)
                continue;

            int x = i / height;
            int y = i % height;

            if (!IsValidCoord(x, y))
                continue;

            cell.Initialize(this, x, y, i);
            cellMap[x, y] = cell;
        }

        ApplyGridVisibilityToCells();
    }

    public void SetGridVisible(bool visible)
    {
        isGridVisible = visible;
        ApplyGridVisibilityToCells();
    }

    private void ApplyGridVisibilityToCells()
    {
        if (cells == null || cells.Length == 0)
            cells = GetComponentsInChildren<GridCell>(true);

        if (cells == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            GridCell cell = cells[i];

            if (cell == null)
                continue;

            Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (renderers[rendererIndex] != null)
                    renderers[rendererIndex].enabled = isGridVisible;
            }

            Collider[] colliders = cell.GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                if (colliders[colliderIndex] != null)
                    colliders[colliderIndex].enabled = isGridVisible;
            }

            Collider2D[] colliders2D = cell.GetComponentsInChildren<Collider2D>(true);
            for (int colliderIndex = 0; colliderIndex < colliders2D.Length; colliderIndex++)
            {
                if (colliders2D[colliderIndex] != null)
                    colliders2D[colliderIndex].enabled = isGridVisible;
            }
        }
    }

    public void NotifyCellClicked(GridCell cell)
    {
        OnCellClicked?.Invoke(cell);
    }

    public void NotifyCellHovered(GridCell cell)
    {
        OnCellHovered?.Invoke(cell);
    }

    public void NotifyCellHoverExited(GridCell cell)
    {
        OnCellHoverExited?.Invoke(cell);
    }

    public GridCell GetCell(int x, int y)
    {
        if (!IsValidCoord(x, y))
            return null;

        return cellMap[x, y];
    }

    public GridCell GetCell(Vector2Int coord)
    {
        return GetCell(coord.x, coord.y);
    }

    public GridCell GetCellByIndex(int index)
    {
        Vector2Int coord = IndexToCoord(index);
        return GetCell(coord);
    }

    public bool IsValidCoord(int x, int y)
    {
        return x >= 0 && x < width &&
               y >= 0 && y < height;
    }

    public bool IsValidCoord(Vector2Int coord)
    {
        return IsValidCoord(coord.x, coord.y);
    }

    public int CoordToIndex(Vector2Int coord)
    {
        return coord.x * height + coord.y;
    }

    public Vector2Int IndexToCoord(int index)
    {
        int x = index / height;
        int y = index % height;

        return new Vector2Int(x, y);
    }

    public Vector3 GetWorldPositionByIndex(int index)
    {
        GridCell cell = GetCellByIndex(index);

        if (cell == null)
            return Vector3.zero;

        return cell.transform.position;
    }

    public List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new();

        if (cell == null)
            return neighbors;

        int x = cell.X;
        int y = cell.Y;

        TryAddCell(x + 1, y, neighbors);
        TryAddCell(x - 1, y, neighbors);
        TryAddCell(x, y + 1, neighbors);
        TryAddCell(x, y - 1, neighbors);

        TryAddCell(x + 1, y + 1, neighbors);
        TryAddCell(x + 1, y - 1, neighbors);
        TryAddCell(x - 1, y + 1, neighbors);
        TryAddCell(x - 1, y - 1, neighbors);

        return neighbors;
    }

    private void TryAddCell(int x, int y, List<GridCell> list)
    {
        GridCell cell = GetCell(x, y);

        if (cell != null)
            list.Add(cell);
    }
}
