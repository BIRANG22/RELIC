using UnityEngine;

public class GridTile : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private GridManager ownerGrid;

    public PlayerUnit OccupiedUnit { get; private set; }

    public void Initialize(GridManager grid, int x, int y)
    {
        ownerGrid = grid;
        X = x;
        Y = y;
    }

    public bool IsOccupied()
    {
        return OccupiedUnit != null;
    }

    public void SetOccupant(PlayerUnit unit)
    {
        OccupiedUnit = unit;
    }

    public void ClearOccupant()
    {
        OccupiedUnit = null;
    }

    private void OnMouseDown()
    {
        BattleManager.Instance.OnTileClicked(this);
    }

    public GridManager GetGrid()
    {
        return ownerGrid;
    }
}