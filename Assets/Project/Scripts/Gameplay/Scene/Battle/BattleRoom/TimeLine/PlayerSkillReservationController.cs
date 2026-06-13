using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class PlayerSkillReservationController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Skill List Panel")]
    [SerializeField] private SkillListPanel skillListPanel;
    [SerializeField] private bool keepSkillListOpenAfterReservationClick = true;
    [SerializeField] private int keepSkillListOpenIgnoreFrames = 1;

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

    private void EnsureSkillListPanel()
    {
        if (skillListPanel != null)
            return;

        skillListPanel = FindFirstObjectByType<SkillListPanel>(FindObjectsInactive.Include);
    }

    private void KeepSkillListOpenForThisClick()
    {
        if (!keepSkillListOpenAfterReservationClick)
            return;

        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        skillListPanel.IgnoreOutsideCloseForFrames(keepSkillListOpenIgnoreFrames);
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

        if (gridManager == null || currentSkillData == null)
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            currentCasterGridIndex,
            currentSkillData.RangeId,
            currentUserRuntime != null ? currentUserRuntime.Direction : BattleDirection.Right,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        for (int i = 0; i < rangeIndices.Count; i++)
        {
            int index = rangeIndices[i];

            if (index == currentCasterGridIndex)
                continue;

            if (!currentMoveSelectableIndices.Contains(index))
                currentMoveSelectableIndices.Add(index);
        }

        if (rangePreview != null)
            rangePreview.ShowDirectionCells(currentMoveSelectableIndices);
    }
    private void HandleCellClicked(GridCell cell)
    {
        if (cell == null || currentSkillData == null)
            return;

        if (currentSkillData.RangeType != RangeType.Selection)
            return;

        if (!currentMoveSelectableIndices.Contains(cell.Index))
        {
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 이동 칸이 아닙니다: {cell.name}");
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

        KeepSkillListOpenForThisClick();
        ClearPreview();
    }

    private void ConfirmMoveReservation(int selectedGridIndex)
    {
        BattleDirection direction = GetDirectionFromMove(
            currentCasterGridIndex,
            selectedGridIndex
        );

        currentUserRuntime.Direction = direction;

        Vector2Int caster = gridManager.IndexToCoord(currentCasterGridIndex);
        Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);
        Vector2Int moveOffset = selected - caster;

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionResult(
            direction,
            selectedGridIndex,
            new List<int> { selectedGridIndex },
            moveOffset
        );

        if (timelineController != null)
            timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        if (moveGhostPreview != null)
            moveGhostPreview.Show(
                currentUserRuntime.CharacterId,
                currentCasterSprite,
                selectedGridIndex,
                direction
            );

        KeepSkillListOpenForThisClick();
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

        KeepSkillListOpenForThisClick();
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