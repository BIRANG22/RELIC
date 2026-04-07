using UnityEngine;
using System.Collections.Generic;

public class GridManager2D : MonoBehaviour
{
    public int width = 5;
    public int height = 4;
    public float tileSize = 1f;
    public GameObject tilePrefab;

    private GridTile2D[,] tiles;

    private void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        tiles = new GridTile2D[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = transform.position +
                              new Vector3(x * tileSize, y * tileSize, 0f);

                GameObject obj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                GridTile2D tile = obj.GetComponent<GridTile2D>();
                tile.Initialize(this, x, y);

                tiles[x, y] = tile;
            }
        }
    }

    public GridTile2D GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return tiles[x, y];
    }

    public List<GridTile2D> GetNeighbors(GridTile2D tile)
    {
        List<GridTile2D> neighbors = new List<GridTile2D>();

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

    void TryAdd(int x, int y, List<GridTile2D> list)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            list.Add(tiles[x, y]);
        }
    }

    void TryAddDiagonal(int x, int y, int dx, int dy, List<GridTile2D> list)
    {
        int nx = x + dx;
        int ny = y + dy;

        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            return;

        GridTile2D horizontal = tiles[x + dx, y];
        GridTile2D vertical = tiles[x, y + dy];

        if (horizontal.IsOccupied() || vertical.IsOccupied())
            return;

        list.Add(tiles[nx, ny]);
    }
}