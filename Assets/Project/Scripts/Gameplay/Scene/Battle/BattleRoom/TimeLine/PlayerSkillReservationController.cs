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

    private readonly List<int> currentMoveSelectableIndices = new();

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

        if (currentSkillData.RangeType == RangeType.Direction)
        {
            ConfirmDirectionReservation(currentUserRuntime.Direction);
            return;
        }

        if (currentSkillData.RangeType == RangeType.Selection)
        {
            PreviewMoveSelectableCells();
            return;
        }

        ConfirmDirectReservation();
    }

    private void PreviewMoveSelectableCells()
    {
        currentMoveSelectableIndices.Clear();

        Vector2Int casterCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        TryAddMoveCell(casterCoord + Vector2Int.right);
        TryAddMoveCell(casterCoord + Vector2Int.left);
        TryAddMoveCell(casterCoord + Vector2Int.up);
        TryAddMoveCell(casterCoord + Vector2Int.down);

        if (rangePreview != null)
            rangePreview.ShowDirectionCells(currentMoveSelectableIndices);
    }

    private void TryAddMoveCell(Vector2Int coord)
    {
        if (gridManager == null)
            return;

        if (!gridManager.IsValidCoord(coord))
            return;

        currentMoveSelectableIndices.Add(gridManager.CoordToIndex(coord));
    }

    private void HandleCellClicked(GridCell cell)
    {
        if (cell == null || currentSkillData == null)
            return;

        if (currentSkillData.RangeType != RangeType.Selection)
            return;

        if (!currentMoveSelectableIndices.Contains(cell.Index))
        {
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 이동 칸이 아님: {cell.name}");
            return;
        }

        ConfirmMoveReservation(cell.Index);
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

    private void ConfirmMoveReservation(int selectedGridIndex)
    {
        BattleDirection direction = GetDirectionFromMove(
            currentCasterGridIndex,
            selectedGridIndex
        );

        currentUserRuntime.Direction = direction;

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionResult(direction, selectedGridIndex, new List<int> { selectedGridIndex });

        if (timelineController != null)
            timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        if (moveGhostPreview != null)
            moveGhostPreview.Show(currentCasterSprite, selectedGridIndex, direction);

        ClearPreview();
    }

    private BattleDirection GetDirectionFromMove(int casterGridIndex, int selectedGridIndex)
    {
        Vector2Int caster = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);

        if (selected.x < caster.x)
            return BattleDirection.Left;

        if (selected.x > caster.x)
            return BattleDirection.Right;

        return currentUserRuntime != null
            ? currentUserRuntime.Direction
            : BattleDirection.Right;
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
        currentMoveSelectableIndices.Clear();

        if (rangePreview != null)
            rangePreview.Clear();
    }
}