using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width = 5;
    public int height = 4;
    public float tileSize = 1f;
    public GameObject tilePrefab;

    private GridTile[,] tiles;

    private void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        tiles = new GridTile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = transform.position +
                              new Vector3(x * tileSize, 0.1f, y * tileSize);

                GameObject obj = Instantiate(tilePrefab, pos, tilePrefab.transform.rotation, transform);

                GridTile tile = obj.GetComponent<GridTile>();
                tile.Initialize(this, x, y);

                tiles[x, y] = tile;
            }
        }
    }

    public GridTile GetTile(int x, int y)
    {
        return tiles[x, y];
    }

    public List<GridTile> GetNeighbors(GridTile tile)
    {
        List<GridTile> neighbors = new List<GridTile>();

        int x = tile.X;
        int y = tile.Y;

        // 직선
        TryAdd(x + 1, y, neighbors);
        TryAdd(x - 1, y, neighbors);
        TryAdd(x, y + 1, neighbors);
        TryAdd(x, y - 1, neighbors);

        // 대각선 (코너컷 방지)
        TryAddDiagonal(x, y, 1, 1, neighbors);
        TryAddDiagonal(x, y, 1, -1, neighbors);
        TryAddDiagonal(x, y, -1, 1, neighbors);
        TryAddDiagonal(x, y, -1, -1, neighbors);

        return neighbors;
    }

    void TryAdd(int x, int y, List<GridTile> list)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            list.Add(tiles[x, y]);
        }
    }

    void TryAddDiagonal(int x, int y, int dx, int dy, List<GridTile> list)
    {
        int nx = x + dx;
        int ny = y + dy;

        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            return;

        GridTile horizontal = tiles[x + dx, y];
        GridTile vertical = tiles[x, y + dy];

        if (horizontal.IsOccupied() || vertical.IsOccupied())
            return;

        list.Add(tiles[nx, ny]);
    }
}