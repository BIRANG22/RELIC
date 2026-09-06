using System.Collections.Generic;
using UnityEngine;

public class RangePreview : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private readonly List<GridCell> directionCells = new();
    private readonly List<GridCell> rangeCells = new();
    private Color currentDirectionPreviewColor = Color.white;
    private Material currentDirectionPreviewMaterial;
    private bool hasCustomDirectionPreviewColor;

    public void ShowDirectionCells(List<int> gridIndices)
    {
        ClearAll();
        hasCustomDirectionPreviewColor = false;
        currentDirectionPreviewMaterial = null;

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

    public void ShowDirectionCells(List<int> gridIndices, Color highlightColor)
    {
        ClearAll();
        currentDirectionPreviewColor = highlightColor;
        currentDirectionPreviewMaterial = null;
        hasCustomDirectionPreviewColor = true;

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetPreview(highlightColor);
            directionCells.Add(cell);
        }
    }

    public void ShowDirectionCells(
        List<int> gridIndices,
        Color highlightColor,
        Material materialOverride)
    {
        ClearAll();
        currentDirectionPreviewColor = highlightColor;
        currentDirectionPreviewMaterial = materialOverride;
        hasCustomDirectionPreviewColor = true;

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetPreview(highlightColor, materialOverride);
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

    public void ShowRangeCells(List<int> gridIndices, Color highlightColor)
    {
        ClearRangeOnly();

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetRangePreview(highlightColor);
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
            if (cell == null)
                continue;

            if (hasCustomDirectionPreviewColor)
                cell.SetPreview(currentDirectionPreviewColor, currentDirectionPreviewMaterial);
            else
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
        currentDirectionPreviewMaterial = null;
        hasCustomDirectionPreviewColor = false;
    }

    public void Clear()
    {
        ClearAll();
    }
}