using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class PlayerSkillReservationController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleTimelineController timelineController;

    private CharacterRuntimeData currentUserRuntime;
    private SkillMasterData currentSkillData;
    private int currentSlotIndex = -1;
    private int currentCasterGridIndex = -1;
    private Sprite currentCasterSprite;

    private readonly List<int> currentDirectionSelectIndices = new();

    private void OnEnable()
    {
        if (gridManager != null)
            gridManager.OnCellClicked += HandleCellClicked;
    }

    private void OnDisable()
    {
        if (gridManager != null)
            gridManager.OnCellClicked -= HandleCellClicked;
    }

    public void StartReservation(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int casterGridIndex,
        int slotIndex,
        Sprite casterSprite = null)
    {
        ClearPreview();

        currentUserRuntime = userRuntime;
        currentSkillData = skillData;
        currentCasterGridIndex = casterGridIndex;
        currentSlotIndex = slotIndex;
        currentCasterSprite = casterSprite;

        if (currentUserRuntime == null || currentSkillData == null)
            return;

        if (currentSkillData.RangeType == RangeType.Direction ||
            currentSkillData.RangeType == RangeType.Selection)
        {
            PreviewDirectionSelectCells();
            return;
        }

        ConfirmDirectReservation();
    }

    private void PreviewDirectionSelectCells()
    {
        currentDirectionSelectIndices.Clear();

        Vector2Int casterCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        TryAddDirectionCell(casterCoord + Vector2Int.right);
        TryAddDirectionCell(casterCoord + Vector2Int.left);
        TryAddDirectionCell(casterCoord + Vector2Int.up);
        TryAddDirectionCell(casterCoord + Vector2Int.down);

        if (rangePreview != null)
            rangePreview.Show(currentDirectionSelectIndices);
    }

    private void TryAddDirectionCell(Vector2Int coord)
    {
        if (gridManager == null)
            return;

        if (!gridManager.IsValidCoord(coord))
            return;

        currentDirectionSelectIndices.Add(gridManager.CoordToIndex(coord));
    }

    private void HandleCellClicked(GridCell cell)
    {
        if (cell == null || currentSkillData == null)
            return;

        if (!currentDirectionSelectIndices.Contains(cell.Index))
        {
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 방향 칸이 아님: {cell.name}");
            return;
        }

        BattleDirection direction = GetDirectionFromSelectedGrid(
            currentCasterGridIndex,
            cell.Index
        );

        if (currentSkillData.RangeType == RangeType.Direction)
        {
            ConfirmDirectionReservation(direction);
            return;
        }

        if (currentSkillData.RangeType == RangeType.Selection)
        {
            ConfirmMoveReservation(cell.Index, direction);
            return;
        }
    }

    private void ConfirmDirectionReservation(BattleDirection direction)
    {
        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            currentCasterGridIndex,
            currentSkillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetDirectionResult(direction, rangeIndices, rangeIndices);

        if (timelineController != null)
            timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        ClearPreview();
    }

    private void ConfirmMoveReservation(int selectedGridIndex, BattleDirection direction)
    {
        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            currentCasterGridIndex,
            currentSkillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionResult(selectedGridIndex, rangeIndices);

        if (timelineController != null)
            timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        if (moveGhostPreview != null)
            moveGhostPreview.Show(currentCasterSprite, selectedGridIndex);

        ClearPreview();
    }

    private BattleDirection GetDirectionFromSelectedGrid(int casterGridIndex, int selectedGridIndex)
    {
        Vector2Int caster = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);

        Vector2Int diff = selected - caster;

        if (diff == Vector2Int.right)
            return BattleDirection.Right;

        if (diff == Vector2Int.left)
            return BattleDirection.Left;

        if (diff == Vector2Int.up)
            return BattleDirection.Up;

        if (diff == Vector2Int.down)
            return BattleDirection.Down;

        return BattleDirection.Right;
    }

    private void ConfirmDirectReservation()
    {
        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        if (timelineController != null)
            timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        ClearPreview();
    }

    public void ClearPreview()
    {
        currentUserRuntime = null;
        currentSkillData = null;
        currentSlotIndex = -1;
        currentCasterGridIndex = -1;
        currentCasterSprite = null;
        currentDirectionSelectIndices.Clear();

        if (rangePreview != null)
            rangePreview.Clear();
    }
}