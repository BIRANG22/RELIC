using System.Collections.Generic;
using UnityEngine;

public class RangePreview : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private readonly List<GridCell> previewCells = new();

    public void Show(List<int> gridIndices)
    {
        Clear();

        if (gridManager == null || gridIndices == null)
            return;

        foreach (int index in gridIndices)
        {
            GridCell cell = gridManager.GetCellByIndex(index);

            if (cell == null)
                continue;

            cell.SetPreview();
            previewCells.Add(cell);
        }
    }

    public void Clear()
    {
        foreach (GridCell cell in previewCells)
        {
            if (cell != null)
                cell.SetNormal();
        }

        previewCells.Clear();
    }
}