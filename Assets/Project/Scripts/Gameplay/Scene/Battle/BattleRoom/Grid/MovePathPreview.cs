using System.Collections.Generic;
using UnityEngine;

public class MovePathPreview : MonoBehaviour
{
    private readonly struct MovePathTileRotation
    {
        public MovePathTileRotation(float y, float z)
        {
            Y = y;
            Z = z;
        }

        public float Y { get; }
        public float Z { get; }
    }

    private readonly struct MovePathTileConnection
    {
        public MovePathTileConnection(
            MovePathTileDirection incoming,
            MovePathTileDirection outgoing,
            MovePathTileRotation rotation)
        {
            Incoming = incoming;
            Outgoing = outgoing;
            Rotation = rotation;
        }

        public MovePathTileDirection Incoming { get; }
        public MovePathTileDirection Outgoing { get; }
        public MovePathTileRotation Rotation { get; }
    }

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private MovePathTileView tilePrefab;
    [SerializeField] private Vector3 tileOffset = new(0f, 0.03f, 0f);
    [SerializeField] private float tileScale = 0.7f;
    [SerializeField] private string sortingLayerName = "Unit";
    [SerializeField] private int sortingOrderOffset = 6;
    [SerializeField] private float ySortMultiplier = 100f;

    private readonly List<MovePathTileView> spawnedTiles = new();
    private readonly List<MovePathTileView> pooledTiles = new();
    private int shownStartGridIndex = -1;
    private readonly List<Vector2Int> shownSteps = new();
    private bool reportedMissingTilePrefab;

    public void ShowPath(int startGridIndex, IReadOnlyList<Vector2Int> moveSteps)
    {
        if (gridManager == null ||
            startGridIndex < 0 ||
            moveSteps == null ||
            moveSteps.Count <= 0 ||
            IsSelfFlipPath(moveSteps))
        {
            Clear();
            return;
        }

        if (tilePrefab == null)
        {
            ReportMissingTilePrefab();
            Clear();
            return;
        }

        EnsureSpawnRoot();

        if (IsSamePath(startGridIndex, moveSteps))
            return;

        Clear();

        shownStartGridIndex = startGridIndex;
        shownSteps.AddRange(moveSteps);

        Vector2Int currentCoord = gridManager.IndexToCoord(startGridIndex);

        int pathTileCount = Mathf.Max(0, moveSteps.Count - 1);

        for (int i = 0; i < pathTileCount; i++)
        {
            Vector2Int currentStep = moveSteps[i];

            if (!IsCardinalStep(currentStep))
                continue;

            currentCoord += currentStep;

            if (!gridManager.IsValidCoord(currentCoord))
                continue;

            int gridIndex = gridManager.CoordToIndex(currentCoord);
            GridCell gridCell = gridManager.GetCellByIndex(gridIndex);
            Vector3 worldPosition = gridManager.GetWorldPositionByIndex(gridIndex);
            MovePathTileKind kind = GetTileKind(i, moveSteps);
            MovePathTileConnection connection = GetTileConnection(kind, currentCoord, i, moveSteps);
            float scale = Mathf.Max(0f, tileScale);

            MovePathTileView tile = GetOrCreateTile();
            tile.name = $"Move Path Tile {gridIndex}";
            tile.gameObject.SetActive(true);
            tile.transform.position = worldPosition + tileOffset;
            tile.transform.localScale = Vector3.one * scale;
            tile.Apply(
                kind,
                connection.Incoming,
                connection.Outgoing,
                connection.Rotation.Y,
                connection.Rotation.Z,
                gridCell,
                scale);
            tile.ApplySorting(
                sortingLayerName,
                BattleWorldVfxSortUtility.CalculateSortingOrder(
                    worldPosition.y,
                    ySortMultiplier,
                    sortingOrderOffset));

            spawnedTiles.Add(tile);
        }
    }

    public void Clear()
    {
        for (int i = spawnedTiles.Count - 1; i >= 0; i--)
        {
            MovePathTileView tile = spawnedTiles[i];
            if (tile == null)
                continue;

            tile.gameObject.SetActive(false);
            pooledTiles.Add(tile);
        }

        spawnedTiles.Clear();
        shownStartGridIndex = -1;
        shownSteps.Clear();
    }

    private MovePathTileView GetOrCreateTile()
    {
        while (pooledTiles.Count > 0)
        {
            int lastIndex = pooledTiles.Count - 1;
            MovePathTileView pooled = pooledTiles[lastIndex];
            pooledTiles.RemoveAt(lastIndex);

            if (pooled == null)
                continue;

            if (pooled.transform.parent != spawnRoot)
                pooled.transform.SetParent(spawnRoot, false);

            return pooled;
        }

        return Instantiate(tilePrefab, spawnRoot);
    }

    public void ConfigureForTest(
        GridManager targetGridManager,
        MovePathTileView targetTilePrefab,
        Transform targetSpawnRoot)
    {
        gridManager = targetGridManager;
        tilePrefab = targetTilePrefab;
        spawnRoot = targetSpawnRoot;
    }

    public void BindGridManager(GridManager targetGridManager)
    {
        gridManager = targetGridManager;
    }

    private void EnsureSpawnRoot()
    {
        if (spawnRoot != null)
            return;

        GameObject rootObject = new("Move Path Preview Root");
        rootObject.transform.SetParent(transform, false);
        spawnRoot = rootObject.transform;
    }

    private void ReportMissingTilePrefab()
    {
        if (reportedMissingTilePrefab)
            return;

        reportedMissingTilePrefab = true;
        Debug.LogWarning(
            "[MovePathPreview] tilePrefab is not assigned. Assign MovePathTile.prefab in the inspector.",
            this);
    }

    private bool IsSamePath(int startGridIndex, IReadOnlyList<Vector2Int> moveSteps)
    {
        if (shownStartGridIndex != startGridIndex || shownSteps.Count != moveSteps.Count)
            return false;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            if (shownSteps[i] != moveSteps[i])
                return false;
        }

        return true;
    }

    private static bool IsSelfFlipPath(IReadOnlyList<Vector2Int> moveSteps)
    {
        return moveSteps.Count == 1 && moveSteps[0] == Vector2Int.zero;
    }

    private static bool IsCardinalStep(Vector2Int step)
    {
        return Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;
    }

    private static MovePathTileKind GetTileKind(int stepIndex, IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int currentStep = moveSteps[stepIndex];
        Vector2Int nextStep = moveSteps[stepIndex + 1];

        if (stepIndex >= moveSteps.Count - 2)
        {
            return currentStep == nextStep
                ? MovePathTileKind.End
                : MovePathTileKind.CornerEnd;
        }

        return currentStep == nextStep
            ? MovePathTileKind.Straight
            : MovePathTileKind.Corner;
    }

    private MovePathTileConnection GetTileConnection(
        MovePathTileKind kind,
        Vector2Int currentCoord,
        int stepIndex,
        IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int currentStep = moveSteps[stepIndex];
        Vector2Int nextStep = moveSteps[stepIndex + 1];
        Vector2Int previousCoord = currentCoord - currentStep;
        Vector2Int nextCoord = currentCoord + nextStep;

        if (!TryGetVisualDirection(previousCoord, currentCoord, out MovePathTileDirection incoming) &&
            !TryGetLogicalDirection(currentStep, out incoming))
        {
            incoming = MovePathTileDirection.Right;
        }

        MovePathTileDirection outgoing;

        if (kind == MovePathTileKind.Corner || kind == MovePathTileKind.CornerEnd)
        {
            if (!TryGetVisualDirection(currentCoord, nextCoord, out outgoing) &&
                !TryGetLogicalDirection(nextStep, out outgoing))
            {
                outgoing = incoming;
            }
        }
        else
        {
            outgoing = incoming;
        }

        return new MovePathTileConnection(
            incoming,
            outgoing,
            GetRotation(kind, incoming, outgoing, currentStep));
    }

    private static MovePathTileRotation GetRotation(
        MovePathTileKind kind,
        MovePathTileDirection incoming,
        MovePathTileDirection outgoing,
        Vector2Int currentStep)
    {
        if (kind == MovePathTileKind.CornerEnd)
            return CornerEndToRotation(incoming, outgoing);

        if (kind == MovePathTileKind.Corner)
            return new MovePathTileRotation(0f, CornerToRotation(incoming, outgoing));

        return new MovePathTileRotation(0f, DirectionToRotation(currentStep));
    }

    private static float DirectionToRotation(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return 270f;

        if (direction == Vector2Int.left)
            return 180f;

        if (direction == Vector2Int.down)
            return 90f;

        return 0f;
    }

    private bool TryGetVisualDirection(
        Vector2Int fromCoord,
        Vector2Int toCoord,
        out MovePathTileDirection direction)
    {
        direction = MovePathTileDirection.Right;

        if (gridManager == null ||
            !gridManager.IsValidCoord(fromCoord) ||
            !gridManager.IsValidCoord(toCoord))
        {
            return false;
        }

        Vector3 from = gridManager.GetWorldPositionByIndex(gridManager.CoordToIndex(fromCoord));
        Vector3 to = gridManager.GetWorldPositionByIndex(gridManager.CoordToIndex(toCoord));
        Vector3 delta = to - from;

        if (Mathf.Approximately(delta.x, 0f) &&
            Mathf.Approximately(delta.y, 0f))
        {
            return false;
        }

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            direction = delta.x >= 0f
                ? MovePathTileDirection.Right
                : MovePathTileDirection.Left;
            return true;
        }

        direction = delta.y >= 0f
            ? MovePathTileDirection.Up
            : MovePathTileDirection.Down;
        return true;
    }

    private static bool TryGetLogicalDirection(
        Vector2Int step,
        out MovePathTileDirection direction)
    {
        if (step == Vector2Int.right)
        {
            direction = MovePathTileDirection.Right;
            return true;
        }

        if (step == Vector2Int.left)
        {
            direction = MovePathTileDirection.Left;
            return true;
        }

        if (step == Vector2Int.up)
        {
            direction = MovePathTileDirection.Up;
            return true;
        }

        if (step == Vector2Int.down)
        {
            direction = MovePathTileDirection.Down;
            return true;
        }

        direction = MovePathTileDirection.Right;
        return false;
    }

    private static float CornerToRotation(MovePathTileDirection incoming, MovePathTileDirection outgoing)
    {
        if (incoming == MovePathTileDirection.Right && outgoing == MovePathTileDirection.Down)
            return 0f;

        if (incoming == MovePathTileDirection.Up && outgoing == MovePathTileDirection.Left)
            return 0f;

        if (incoming == MovePathTileDirection.Up && outgoing == MovePathTileDirection.Right)
            return 90f;

        if (incoming == MovePathTileDirection.Left && outgoing == MovePathTileDirection.Down)
            return 90f;

        if (incoming == MovePathTileDirection.Down && outgoing == MovePathTileDirection.Right)
            return 180f;

        if (incoming == MovePathTileDirection.Left && outgoing == MovePathTileDirection.Up)
            return 180f;

        if (incoming == MovePathTileDirection.Right && outgoing == MovePathTileDirection.Up)
            return 270f;

        if (incoming == MovePathTileDirection.Down && outgoing == MovePathTileDirection.Left)
            return 270f;

        return 0f;
    }

    private static MovePathTileRotation CornerEndToRotation(
        MovePathTileDirection incoming,
        MovePathTileDirection outgoing)
    {
        if (incoming == MovePathTileDirection.Right && outgoing == MovePathTileDirection.Down)
            return new MovePathTileRotation(0f, 0f);

        if (incoming == MovePathTileDirection.Up && outgoing == MovePathTileDirection.Right)
            return new MovePathTileRotation(0f, 90f);

        if (incoming == MovePathTileDirection.Left && outgoing == MovePathTileDirection.Up)
            return new MovePathTileRotation(0f, 180f);

        if (incoming == MovePathTileDirection.Down && outgoing == MovePathTileDirection.Left)
            return new MovePathTileRotation(0f, 270f);

        if (incoming == MovePathTileDirection.Left && outgoing == MovePathTileDirection.Down)
            return new MovePathTileRotation(180f, 0f);

        if (incoming == MovePathTileDirection.Up && outgoing == MovePathTileDirection.Left)
            return new MovePathTileRotation(180f, 90f);

        if (incoming == MovePathTileDirection.Right && outgoing == MovePathTileDirection.Up)
            return new MovePathTileRotation(180f, 180f);

        if (incoming == MovePathTileDirection.Down && outgoing == MovePathTileDirection.Right)
            return new MovePathTileRotation(180f, 270f);

        return new MovePathTileRotation(0f, 0f);
    }
}
