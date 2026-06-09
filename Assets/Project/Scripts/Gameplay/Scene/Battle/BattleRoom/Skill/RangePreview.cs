using System.Collections.Generic;
using UnityEngine;

public class RangePreview : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private readonly List<GridCell> directionCells = new();
    private readonly List<GridCell> rangeCells = new();

    public void ShowDirectionCells(List<int> gridIndices)
    {
        ClearAll();

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetPreview();
            directionCells.Add(cell);
        }
    }

    public void ShowRangeCells(List<int> gridIndices)
    {
        ClearRangeOnly();

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetRangePreview();
            rangeCells.Add(cell);
        }
    }

    public void ClearRangeOnly()
    {
        foreach (GridCell cell in rangeCells)
        {
            if (cell != null)
                cell.SetNormal();
        }

        rangeCells.Clear();

        foreach (GridCell cell in directionCells)
        {
            if (cell != null)
                cell.SetPreview();
        }
    }

    public void ClearAll()
    {
        foreach (GridCell cell in directionCells)
        {
            if (cell != null)
                cell.SetNormal();
        }

        foreach (GridCell cell in rangeCells)
        {
            if (cell != null)
                cell.SetNormal();
        }

        directionCells.Clear();
        rangeCells.Clear();
    }

    public void Clear()
    {
        ClearAll();
    }
}