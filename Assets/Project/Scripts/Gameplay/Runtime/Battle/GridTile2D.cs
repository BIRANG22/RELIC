using UnityEngine;

public class GridTile2D : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private GridManager2D gridManager;
    private bool occupied;

    public void Initialize(GridManager2D manager, int x, int y)
    {
        gridManager = manager;
        X = x;
        Y = y;
    }

    public bool IsOccupied()
    {
        return occupied;
    }

    public void SetOccupied(bool value)
    {
        occupied = value;
    }
}