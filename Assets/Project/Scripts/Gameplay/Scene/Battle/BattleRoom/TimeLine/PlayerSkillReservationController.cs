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
    private BattleDirection currentCasterDirection = BattleDirection.Right;
    private Sprite currentCasterSprite;

    private readonly List<int> currentMoveSelectableIndices = new();
    private readonly Dictionary<int, List<List<Vector2Int>>> currentMovePathCandidatesByTargetIndex = new();

    private int currentMoveDistancePerCommand = 1;
    private int currentMoveReservationCapacity = 1;

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
        BattleDirection casterDirection = userRuntime != null
            ? userRuntime.Direction
            : BattleDirection.Right;

        StartReservation(
            userRuntime,
            skillData,
            casterGridIndex,
            slotIndex,
            casterDirection,
            casterSprite
        );
    }

    public void StartReservation(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int casterGridIndex,
        int slotIndex,
        BattleDirection casterDirection,
        Sprite casterSprite = null)
    {
        ClearPreview();

        currentUserRuntime = userRuntime;
        currentSkillData = skillData;
        currentCasterGridIndex = casterGridIndex;
        currentSlotIndex = slotIndex;
        currentCasterDirection = casterDirection;
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
            ConfirmDirectionReservation(currentCasterDirection);
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
        currentMovePathCandidatesByTargetIndex.Clear();

        if (!CanUseRangeData())
            return;

        currentMoveDistancePerCommand = GetMoveDistancePerCommand();
        currentMoveReservationCapacity = GetMoveReservationCapacity();

        if (currentMoveReservationCapacity <= 0)
        {
            ShowBattleWarning("이 슬롯에는 더 이상 스킬을 예약할 수 없습니다.");
            return;
        }

        List<int> rangeIndices = GetMoveRangeIndices(
            currentCasterGridIndex,
            currentMoveReservationCapacity,
            currentMoveDistancePerCommand,
            gridManager
        );

        Vector2Int casterCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        for (int i = 0; i < rangeIndices.Count; i++)
        {
            int index = rangeIndices[i];
            Vector2Int selectedCoord = gridManager.IndexToCoord(index);
            Vector2Int moveOffset = selectedCoord - casterCoord;
            List<List<Vector2Int>> pathCandidates =
                BuildMoveReservationPathCandidates(
                    moveOffset,
                    currentMoveDistancePerCommand
                );

            pathCandidates.RemoveAll(path =>
                path == null ||
                path.Count <= 0 ||
                path.Count > currentMoveReservationCapacity
            );

            if (pathCandidates.Count <= 0)
                continue;

            currentMovePathCandidatesByTargetIndex[index] = pathCandidates;

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

        if (!currentMovePathCandidatesByTargetIndex.TryGetValue(
            selectedGridIndex,
            out List<List<Vector2Int>> pathCandidates))
        {
            Vector2Int caster = gridManager.IndexToCoord(currentCasterGridIndex);
            Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);
            Vector2Int moveOffset = selected - caster;

            pathCandidates = BuildMoveReservationPathCandidates(
                moveOffset,
                currentMoveDistancePerCommand
            );
        }

        List<Vector2Int> moveOffsets = GetFirstReservableMovePath(pathCandidates);

        if (moveOffsets == null || moveOffsets.Count <= 0)
        {
            ShowBattleWarning("이동할 수 없는 위치입니다.");
            return;
        }

        if (moveOffsets.Count > currentMoveReservationCapacity)
        {
            ShowBattleWarning("선택한 위치까지 이동할 슬롯이 부족합니다.");
            return;
        }

        List<PlayerReservedCommand> commands = BuildMoveReservationCommands(moveOffsets);

        if (commands.Count <= 0)
        {
            ShowBattleWarning("이동 예약을 만들 수 없습니다.");
            return;
        }

        bool confirmed = ConfirmCommands(commands);
        BattleDirection finalDirection = commands[commands.Count - 1].Direction;

        if (confirmed && moveGhostPreview != null)
        {
            moveGhostPreview.Show(
                currentUserRuntime.CharacterId,
                currentCasterSprite,
                selectedGridIndex,
                finalDirection
            );
        }

        KeepSkillListOpenForThisClick();
        ClearPreview();
    }

    private List<Vector2Int> GetFirstReservableMovePath(List<List<Vector2Int>> pathCandidates)
    {
        if (pathCandidates == null)
            return null;

        for (int i = 0; i < pathCandidates.Count; i++)
        {
            List<Vector2Int> path = pathCandidates[i];

            if (path == null || path.Count <= 0)
                continue;

            if (path.Count > currentMoveReservationCapacity)
                continue;

            return path;
        }

        return null;
    }

    private int GetMoveReservationCapacity()
    {
        EnsureTimelineController();

        if (timelineController == null)
            return 1;

        return timelineController.GetRemainingPlayerCommandCapacity(currentSlotIndex);
    }

    private int GetMoveDistancePerCommand()
    {
        int distance = Mathf.Max(0, currentSkillData != null ? currentSkillData.GridMove : 0);

        if (distance > 0)
            return distance;

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null || currentSkillData == null)
            return 1;

        if (!DataManager.Instance.RangeDatabase.TryGet(currentSkillData.RangeId, out SkillRangeData rangeData))
            return 1;

        if (rangeData == null || rangeData.Positions == null)
            return 1;

        for (int i = 0; i < rangeData.Positions.Count; i++)
        {
            Vector2Int offset = rangeData.Positions[i];
            distance = Mathf.Max(distance, GetMoveDistance(offset));
        }

        return Mathf.Max(1, distance);
    }

    private List<PlayerReservedCommand> BuildMoveReservationCommands(List<Vector2Int> moveOffsets)
    {
        List<PlayerReservedCommand> commands = new();

        if (moveOffsets == null || gridManager == null)
            return commands;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentCasterGridIndex);
        BattleDirection direction = currentCasterDirection;

        for (int i = 0; i < moveOffsets.Count; i++)
        {
            Vector2Int moveOffset = moveOffsets[i];
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                return new List<PlayerReservedCommand>();

            int targetGridIndex = gridManager.CoordToIndex(targetCoord);
            direction = GetDirectionAfterMove(direction, moveOffset);

            PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
            command.SetSelectionResult(
                direction,
                targetGridIndex,
                new List<int> { targetGridIndex },
                moveOffset
            );

            commands.Add(command);
            currentCoord = targetCoord;
        }

        ApplyVisualMovePath(commands);

        return commands;
    }

    private void ApplyVisualMovePath(List<PlayerReservedCommand> commands)
    {
        if (commands == null || commands.Count <= 1 || gridManager == null)
            return;

        Vector2Int visualCurrentCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand current = commands[i];

            if (current == null)
                continue;

            Vector2Int visualOffset = current.MoveOffset;

            if (i + 1 < commands.Count)
            {
                PlayerReservedCommand next = commands[i + 1];

                if (CanMergeToDiagonal(current.MoveOffset, next.MoveOffset))
                {
                    visualOffset = current.MoveOffset + next.MoveOffset;

                    next.SetSkipMoveVisual(true);

                    i++;
                }
            }

            Vector2Int visualTargetCoord = visualCurrentCoord + visualOffset;

            if (!gridManager.IsValidCoord(visualTargetCoord))
                continue;

            int visualTargetGridIndex = gridManager.CoordToIndex(visualTargetCoord);

            current.SetVisualMoveResult(
                visualTargetGridIndex,
                visualOffset
            );

            visualCurrentCoord = visualTargetCoord;
        }
    }

    private bool CanMergeToDiagonal(Vector2Int a, Vector2Int b)
    {
        bool aHorizontal = a.x != 0 && a.y == 0;
        bool aVertical = a.x == 0 && a.y != 0;

        bool bHorizontal = b.x != 0 && b.y == 0;
        bool bVertical = b.x == 0 && b.y != 0;

        return (aHorizontal && bVertical) || (aVertical && bHorizontal);
    }

    public static List<int> GetMoveRangeIndices(
        int casterGridIndex,
        int maxMoveDistance,
        GridManager gridManager)
    {
        return GetMoveRangeIndices(casterGridIndex, maxMoveDistance, 1, gridManager);
    }

    public static List<int> GetMoveRangeIndices(
        int casterGridIndex,
        int reservationCapacity,
        int moveDistancePerCommand,
        GridManager gridManager)
    {
        List<int> result = new();

        if (gridManager == null || reservationCapacity <= 0)
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);

        if (!gridManager.IsValidCoord(casterCoord))
            return result;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                Vector2Int offset = coord - casterCoord;

                if (GetRequiredMoveReservationCount(offset, safeDistancePerCommand) > reservationCapacity)
                    continue;

                result.Add(gridManager.CoordToIndex(coord));
            }
        }

        return result;
    }

    public static int GetRequiredMoveReservationCount(
        Vector2Int moveOffset,
        int moveDistancePerCommand)
    {
        if (moveOffset == Vector2Int.zero)
            return 1;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        int xSteps = Mathf.CeilToInt(Mathf.Abs(moveOffset.x) / (float)safeDistancePerCommand);
        int ySteps = Mathf.CeilToInt(Mathf.Abs(moveOffset.y) / (float)safeDistancePerCommand);

        return xSteps + ySteps;
    }

    public static List<Vector2Int> BuildMoveReservationOffsets(
        Vector2Int moveOffset,
        int moveDistancePerCommand)
    {
        List<Vector2Int> result = new();

        if (moveOffset == Vector2Int.zero)
        {
            result.Add(Vector2Int.zero);
            return result;
        }

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        AddAxisMoveOffsets(result, moveOffset.x, safeDistancePerCommand, true);
        AddAxisMoveOffsets(result, moveOffset.y, safeDistancePerCommand, false);

        return result;
    }

    private static void AddAxisMoveOffsets(
        List<Vector2Int> result,
        int amount,
        int moveDistancePerCommand,
        bool horizontal)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int stepMagnitude = Mathf.Min(Mathf.Abs(remaining), moveDistancePerCommand);
            int step = remaining > 0 ? stepMagnitude : -stepMagnitude;
            Vector2Int offset = horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step);

            result.Add(offset);
            remaining -= step;
        }
    }

    private static int GetMoveDistance(Vector2Int moveOffset)
    {
        return Mathf.Abs(moveOffset.x) + Mathf.Abs(moveOffset.y);
    }

    private static BattleDirection GetDirectionAfterMove(
        BattleDirection currentDirection,
        Vector2Int moveOffset)
    {
        if (moveOffset.x < 0)
            return BattleDirection.Left;

        if (moveOffset.x > 0)
            return BattleDirection.Right;

        if (moveOffset == Vector2Int.zero)
            return GetOppositeDirection(currentDirection);

        return currentDirection;
    }
    private BattleDirection GetDirectionFromMove(int casterGridIndex, int selectedGridIndex)
    {
        Vector2Int caster = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);

        return GetDirectionAfterMove(currentCasterDirection, selected - caster);
    }

    private static BattleDirection GetOppositeDirection(BattleDirection direction)
    {
        return direction == BattleDirection.Right
            ? BattleDirection.Left
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

    private bool ConfirmCommands(IReadOnlyList<PlayerReservedCommand> commands)
    {
        EnsureTimelineController();

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return false;
        }

        return timelineController.ConfirmPlayerCommands(currentSlotIndex, commands);
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
        currentCasterDirection = BattleDirection.Right;
        currentCasterSprite = null;
        currentMoveSelectableIndices.Clear();
        currentMovePathCandidatesByTargetIndex.Clear();
        currentMoveDistancePerCommand = 1;
        currentMoveReservationCapacity = 1;

        if (rangePreview != null)
            rangePreview.Clear();
    }

    public static List<List<Vector2Int>> BuildMoveReservationPathCandidates(
    Vector2Int moveOffset,
    int moveDistancePerCommand)
    {
        List<List<Vector2Int>> candidates = new();

        if (moveOffset == Vector2Int.zero)
        {
            candidates.Add(new List<Vector2Int> { Vector2Int.zero });
            return candidates;
        }

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);

        List<Vector2Int> xFirst = new();
        AddAxisMoveOffsets(xFirst, moveOffset.x, safeDistancePerCommand, true);
        AddAxisMoveOffsets(xFirst, moveOffset.y, safeDistancePerCommand, false);

        List<Vector2Int> yFirst = new();
        AddAxisMoveOffsets(yFirst, moveOffset.y, safeDistancePerCommand, false);
        AddAxisMoveOffsets(yFirst, moveOffset.x, safeDistancePerCommand, true);

        if (xFirst.Count > 0)
            candidates.Add(xFirst);

        if (yFirst.Count > 0 && !IsSamePath(xFirst, yFirst))
            candidates.Add(yFirst);

        return candidates;
    }

    private static bool IsSamePath(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null)
            return false;

        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}
