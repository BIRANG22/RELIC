using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public PlayerUnit playerUnitPrefab;
    public GridManager playerGrid;

    private List<PlayerUnit> playerUnits = new List<PlayerUnit>();
    private PlayerUnit selectedUnit;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < 3; i++)
        {
            PlayerUnit unit = Instantiate(playerUnitPrefab);

            GridTile startTile = playerGrid.GetTile(i, 0);

            unit.currentGrid = playerGrid;

            unit.transform.position = new Vector3(
                startTile.transform.position.x,
                0.5f,
                startTile.transform.position.z
            );

            unit.SetStartTile(startTile);

            playerUnits.Add(unit);
        }

        selectedUnit = playerUnits[0];
        selectedUnit.SetSelected(true);
    }

    public void OnTileClicked(GridTile tile)
    {
        if (selectedUnit == null)
            return;

        selectedUnit.MoveTo(tile);
    }

    public void SelectUnit(PlayerUnit unit)
    {
        if (selectedUnit != null)
            selectedUnit.SetSelected(false);

        selectedUnit = unit;
        selectedUnit.SetSelected(true);
    }

    // BFS °æ·Î Å½»ö
    public List<GridTile> FindPath(GridTile start, GridTile target)
    {
        Queue<GridTile> queue = new Queue<GridTile>();
        Dictionary<GridTile, GridTile> cameFrom = new Dictionary<GridTile, GridTile>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            GridTile current = queue.Dequeue();

            if (current == target)
                break;

            foreach (var neighbor in current.GetGrid().GetNeighbors(current))
            {
                if (neighbor.IsOccupied() && neighbor != target)
                    continue;

                if (cameFrom.ContainsKey(neighbor))
                    continue;

                queue.Enqueue(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        if (!cameFrom.ContainsKey(target))
            return null;

        List<GridTile> path = new List<GridTile>();
        GridTile temp = target;

        while (temp != null)
        {
            path.Add(temp);
            temp = cameFrom[temp];
        }

        path.Reverse();
        return path;
    }
}