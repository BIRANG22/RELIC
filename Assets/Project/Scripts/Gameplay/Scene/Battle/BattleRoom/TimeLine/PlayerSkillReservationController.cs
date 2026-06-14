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

    private void EnsureTimelineController()
    {
        if (timelineController != null)
            return;

        timelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
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

        if (currentUserRuntime == null)
        {
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return;
        }

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

        if (!CanUseRangeData())
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

        if (currentMoveSelectableIndices.Count <= 0)
            ShowBattleWarning("선택 가능한 칸이 없습니다.");

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
            ShowBattleWarning("선택할 수 없는 칸입니다.");
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 이동 칸이 아닙니다: {cell.name}");
            return;
        }

        ConfirmMoveReservation(cell.Index);
    }

    private void ConfirmDirectionReservation(BattleDirection direction)
    {
        if (!CanConfirmReservation())
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            currentCasterGridIndex,
            currentSkillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetDirectionResult(direction, rangeIndices, rangeIndices);

        ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();
    }

    private void ConfirmMoveReservation(int selectedGridIndex)
    {
        if (!CanConfirmReservation())
            return;

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

        bool confirmed = ConfirmCommand(command);

        if (confirmed && moveGhostPreview != null)
        {
            moveGhostPreview.Show(
                currentUserRuntime.CharacterId,
                currentCasterSprite,
                selectedGridIndex,
                direction
            );
        }

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
        if (!CanConfirmReservation())
            return;

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();
    }

    private bool ConfirmCommand(PlayerReservedCommand command)
    {
        EnsureTimelineController();

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return false;
        }

        return timelineController.ConfirmPlayerCommand(currentSlotIndex, command);
    }

    private bool CanConfirmReservation()
    {
        if (currentUserRuntime == null)
        {
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (currentSlotIndex < 0)
        {
            ShowBattleWarning("타임라인 슬롯을 먼저 선택해주세요.");
            return false;
        }

        return CanUseRangeData();
    }

    private bool CanUseRangeData()
    {
        if (gridManager == null)
        {
            ShowBattleWarning("전투 그리드를 찾을 수 없습니다.");
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
        {
            ShowBattleWarning("스킬 범위 데이터를 찾을 수 없습니다.");
            return false;
        }

        return true;
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
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
