using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
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
    private bool isMoveTargetMonsterVisualActive;

    private int currentMoveDistancePerCommand = 1;
    private int currentMoveReservationCapacity = 1;

    private const string MoveSkillLevelOneId = "S_Move_1";
    private const string MoveSkillLevelTwoId = "S_Move_2";

    private void OnEnable()
    {
        if (gridManager != null)
            gridManager.OnCellClicked += HandleCellClicked;
    }

    private void OnDisable()
    {
        SetMoveTargetMonsterVisualActive(false);

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
        MonsterUnit.HideAllTemporaryHUDs();
        ClearPreview();

        ResolveTimelinePreviewCasterState(
            userRuntime,
            slotIndex,
            ref casterGridIndex,
            ref casterDirection);

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
            SetMoveTargetMonsterVisualActive(false);
            ConfirmDirectionReservation(currentCasterDirection);
            return;
        }

        if (currentSkillData.RangeType == RangeType.Selection)
        {
            SetMoveTargetMonsterVisualActive(IsMoveSkill(currentSkillData));
            PreviewMoveSelectableCells();
            return;
        }

        SetMoveTargetMonsterVisualActive(false);
        ConfirmDirectReservation();
    }

    private void ResolveTimelinePreviewCasterState(
        CharacterRuntimeData userRuntime,
        int slotIndex,
        ref int casterGridIndex,
        ref BattleDirection casterDirection)
    {
        if (userRuntime == null || slotIndex < 0)
            return;

        EnsureTimelineController();

        if (timelineController == null)
            return;

        int previewGridIndex =
            timelineController.GetPreviewGridIndexAtSlotEnd(userRuntime, slotIndex);

        if (previewGridIndex >= 0)
            casterGridIndex = previewGridIndex;

        casterDirection = timelineController.GetPreviewDirection(userRuntime, slotIndex);
    }

    private void RefreshCurrentCasterStateFromTimelinePreview()
    {
        if (currentUserRuntime == null || currentSlotIndex < 0)
            return;

        EnsureTimelineController();

        if (timelineController == null)
            return;

        int previewGridIndex =
            timelineController.GetPreviewGridIndexAtSlotEnd(currentUserRuntime, currentSlotIndex);

        if (previewGridIndex >= 0)
            currentCasterGridIndex = previewGridIndex;

        currentCasterDirection =
            timelineController.GetPreviewDirection(currentUserRuntime, currentSlotIndex);
    }

    private void PreviewMoveSelectableCells()
    {
        currentMoveSelectableIndices.Clear();
        currentMovePathCandidatesByTargetIndex.Clear();

        if (!CanUseRangeData())
            return;

        RefreshCurrentCasterStateFromTimelinePreview();

        currentMoveDistancePerCommand = GetMoveDistancePerCommand();
        currentMoveReservationCapacity = GetMoveReservationCapacity();

        HashSet<int> blockedDestinationGridIndices = BuildKnownOtherPlayerDestinationGridIndices();

        AddCurrentCasterSelfFlipCandidate(blockedDestinationGridIndices);

        if (currentMoveReservationCapacity <= 0)
        {
            if (currentMoveSelectableIndices.Count > 0 && rangePreview != null)
                rangePreview.ShowDirectionCells(currentMoveSelectableIndices);

            if (currentMoveSelectableIndices.Count > 0)
                return;

            ShowBattleWarning("이동에 필요한 Cost가 부족합니다.");
            return;
        }

        List<int> rangeIndices = GetMoveRangeIndices(
            currentCasterGridIndex,
            currentMoveReservationCapacity,
            currentMoveDistancePerCommand,
            gridManager
        );

        HashSet<int> currentBlockedGridIndices = BuildCurrentMoveBlockedGridIndices();
        HashSet<int> projectedBlockedGridIndices = BuildProjectedMoveBlockedGridIndices();

        for (int i = 0; i < rangeIndices.Count; i++)
        {
            int index = rangeIndices[i];

            if (blockedDestinationGridIndices.Contains(index))
                continue;

            List<List<Vector2Int>> pathCandidates = BuildPreferredMovePathCandidates(
                index,
                currentBlockedGridIndices,
                projectedBlockedGridIndices);

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

    private void AddCurrentCasterSelfFlipCandidate(ISet<int> blockedDestinationGridIndices)
    {
        RefreshCurrentCasterStateFromTimelinePreview();

        if (!IsCurrentCasterGridIndexValid())
            return;

        if (blockedDestinationGridIndices != null &&
            blockedDestinationGridIndices.Contains(currentCasterGridIndex))
        {
            return;
        }

        currentMovePathCandidatesByTargetIndex[currentCasterGridIndex] =
            BuildSelfFlipMovePathCandidates();

        if (!currentMoveSelectableIndices.Contains(currentCasterGridIndex))
            currentMoveSelectableIndices.Add(currentCasterGridIndex);
    }

    private bool IsCurrentCasterGridIndexValid()
    {
        return IsValidMoveDestinationGridIndex(currentCasterGridIndex);
    }

    private bool IsCurrentCasterGridIndex(int gridIndex)
    {
        RefreshCurrentCasterStateFromTimelinePreview();
        return gridIndex >= 0 && gridIndex == currentCasterGridIndex;
    }

    private static List<List<Vector2Int>> BuildSelfFlipMovePathCandidates()
    {
        return new List<List<Vector2Int>>
        {
            new List<Vector2Int> { Vector2Int.zero }
        };
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
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData),
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

        bool isCurrentCasterGridIndex = IsCurrentCasterGridIndex(selectedGridIndex);
        HashSet<int> blockedDestinationGridIndices = BuildKnownOtherPlayerDestinationGridIndices();

        if (!isCurrentCasterGridIndex &&
            blockedDestinationGridIndices.Contains(selectedGridIndex))
        {
            ShowBattleWarning("다른 캐릭터가 있는 위치로는 이동할 수 없습니다.");
            return;
        }

        List<List<Vector2Int>> pathCandidates;

        if (isCurrentCasterGridIndex)
        {
            pathCandidates = BuildSelfFlipMovePathCandidates();
        }
        else if (!currentMovePathCandidatesByTargetIndex.TryGetValue(
            selectedGridIndex,
            out pathCandidates))
        {
            pathCandidates = BuildPreferredMovePathCandidates(selectedGridIndex);
        }

        if (isCurrentCasterGridIndex)
        {
            List<PlayerReservedCommand> selfFlipCommands = BuildMoveReservationCommands(
                selectedGridIndex,
                new List<Vector2Int> { Vector2Int.zero }
            );

            if (selfFlipCommands.Count <= 0)
                return;

            ConfirmCommands(selfFlipCommands);

            KeepSkillListOpenForThisClick();
            ClearPreview();
            return;
        }

        List<Vector2Int> moveOffsets = GetFirstReservableMovePath(pathCandidates);

        if (moveOffsets == null || moveOffsets.Count <= 0)
        {
            ShowBattleWarning("이동할 수 없는 위치입니다.");
            return;
        }

        if (!CanReserveMovePathWithEffectiveCost(moveOffsets))
        {
            ShowBattleWarning("선택한 위치까지 이동할 Cost가 부족합니다.");
            return;
        }

        List<PlayerReservedCommand> commands = BuildMoveReservationCommands(
            selectedGridIndex,
            moveOffsets
        );

        if (commands.Count <= 0)
        {
            ShowBattleWarning("이동 예약을 만들 수 없습니다.");
            return;
        }

        ConfirmCommands(commands);

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

            if (!CanReserveMovePathWithEffectiveCost(path))
                continue;

            return path;
        }

        return null;
    }

    private List<List<Vector2Int>> BuildPreferredMovePathCandidates(
        int targetGridIndex,
        ISet<int> currentBlockedGridIndices = null,
        ISet<int> projectedBlockedGridIndices = null)
    {
        List<List<Vector2Int>> result = new();

        currentBlockedGridIndices ??= BuildCurrentMoveBlockedGridIndices();
        projectedBlockedGridIndices ??= BuildProjectedMoveBlockedGridIndices();

        List<Vector2Int> path = ChooseReservableMovePath(
            currentCasterGridIndex,
            targetGridIndex,
            currentMoveDistancePerCommand,
            currentMoveReservationCapacity,
            gridManager,
            currentBlockedGridIndices,
            projectedBlockedGridIndices,
            true);

        if (path != null &&
            path.Count > 0 &&
            CanReserveMovePathWithEffectiveCost(path))
        {
            result.Add(path);
        }

        return result;
    }

    private bool CanReserveMovePathWithEffectiveCost(IReadOnlyList<Vector2Int> moveOffsets)
    {
        if (IsSelfFlipMovePath(moveOffsets))
            return true;

        int effectiveCost = GetEffectiveMoveReservationCost(moveOffsets);

        if (effectiveCost < 0)
            return false;

        return currentUserRuntime != null && currentUserRuntime.CanReserveCost(effectiveCost);
    }

    private int GetEffectiveMoveReservationCost(IReadOnlyList<Vector2Int> moveOffsets)
    {
        if (currentUserRuntime == null ||
            currentSkillData == null ||
            moveOffsets == null ||
            moveOffsets.Count <= 0)
        {
            return -1;
        }

        PlayerReservedCommand previewCommand =
            new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        previewCommand.SetMoveReservationCost(
            GetMoveStepDistance(moveOffsets),
            currentMoveDistancePerCommand);

        EnsureTimelineController();

        if (timelineController != null && currentSlotIndex >= 0)
            timelineController.PreparePreviewCommandForReservation(currentSlotIndex, previewCommand);

        return previewCommand.Cost;
    }

    private static bool IsSelfFlipMovePath(IReadOnlyList<Vector2Int> moveOffsets)
    {
        return moveOffsets != null &&
               moveOffsets.Count == 1 &&
               moveOffsets[0] == Vector2Int.zero;
    }

    private int GetMoveReservationCapacity()
    {
        if (currentUserRuntime == null)
            return 0;

        return Mathf.Max(0, currentUserRuntime.PreviewCost);
    }

    private int GetMoveCommandSlotCapacity()
    {
        EnsureTimelineController();

        if (timelineController == null)
            return ReserveTurnSlotUI.MaxCommandCount;

        return timelineController.GetRemainingPlayerCommandCapacity(currentSlotIndex);
    }

    private int GetMoveDistancePerCommand()
    {
        int distance = Mathf.Max(0, currentSkillData != null ? currentSkillData.GridMove : 0);

        if (distance > 0)
            return distance;

        if (currentSkillData != null)
        {
            if (currentSkillData.SkillId == MoveSkillLevelTwoId)
                return 2;

            if (currentSkillData.SkillId == MoveSkillLevelOneId)
                return 1;
        }

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null || currentSkillData == null)
            return 1;

        string rangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData);

        if (IsAllMoveRangeId(rangeId))
            return 1;

        if (!DataManager.Instance.RangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
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

    private List<PlayerReservedCommand> BuildMoveReservationCommands(
        int selectedGridIndex,
        IReadOnlyList<Vector2Int> moveOffsets)
    {
        List<PlayerReservedCommand> commands = new();

        if (moveOffsets == null || moveOffsets.Count <= 0 || gridManager == null)
            return commands;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return commands;

        Vector2Int totalMoveOffset = GetTotalMoveOffset(moveOffsets);
        Vector2Int targetCoord = currentCoord + totalMoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return new List<PlayerReservedCommand>();

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (targetGridIndex != selectedGridIndex)
            return new List<PlayerReservedCommand>();

        BattleDirection direction = GetDirectionAfterMoveSteps(
            currentCasterDirection,
            moveOffsets);

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionResult(
            direction,
            selectedGridIndex,
            new List<int> { selectedGridIndex },
            totalMoveOffset
        );
        command.SetMoveReservationCost(
            GetMoveStepDistance(moveOffsets),
            currentMoveDistancePerCommand
        );
        command.SetVisualMoveResult(
            selectedGridIndex,
            totalMoveOffset,
            moveOffsets
        );

        commands.Add(command);

        return commands;
    }

    private static Vector2Int GetTotalMoveOffset(IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int total = Vector2Int.zero;

        if (moveSteps == null)
            return total;

        for (int i = 0; i < moveSteps.Count; i++)
            total += moveSteps[i];

        return total;
    }

    private HashSet<int> BuildCurrentMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = BuildCurrentCharacterMoveBlockedGridIndices();

        AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private HashSet<int> BuildProjectedMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = BuildCurrentCharacterMoveBlockedGridIndices();

        AddProjectedMonsterOccupiedGridIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private HashSet<int> BuildCurrentCharacterMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = new();

        if (gridManager == null)
            return blockedGridIndices;

        string selfCharacterId = currentUserRuntime != null
            ? currentUserRuntime.CharacterId
            : null;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                int gridIndex = gridManager.CoordToIndex(new Vector2Int(x, y));

                if (BattleOccupancyService.IsOccupiedByCharacter(gridIndex, selfCharacterId))
                    blockedGridIndices.Add(gridIndex);
            }
        }

        return blockedGridIndices;
    }

    private void AddProjectedMonsterOccupiedGridIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        EnsureTimelineController();

        if (timelineController == null || gridManager == null || currentSlotIndex < 0)
        {
            AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
            return;
        }

        BattleActionSimulationService simulationService = new(gridManager);
        bool includeCurrentSlotMonsterCommands =
            !BattleActionOrderUtility.HasSwift(currentSkillData);

        HashSet<int> projectedGridIndices =
            simulationService.GetProjectedMonsterOccupiedGridIndices(
                timelineController,
                currentSlotIndex,
                includeCurrentSlotMonsterCommands);

        if (projectedGridIndices == null || projectedGridIndices.Count <= 0)
        {
            AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
            return;
        }

        foreach (int gridIndex in projectedGridIndices)
        {
            if (IsValidMoveDestinationGridIndex(gridIndex))
                blockedGridIndices.Add(gridIndex);
        }
    }

    private void AddCurrentMonsterOccupiedGridIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null)
                continue;

            if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                continue;

            IReadOnlyList<int> occupiedGridIndices = monster.OccupiedGridIndices;

            if (occupiedGridIndices == null)
                continue;

            for (int j = 0; j < occupiedGridIndices.Count; j++)
            {
                int gridIndex = occupiedGridIndices[j];

                if (IsValidMoveDestinationGridIndex(gridIndex))
                    blockedGridIndices.Add(gridIndex);
            }
        }
    }

    private HashSet<int> BuildKnownOtherPlayerDestinationGridIndices()
    {
        HashSet<int> blockedGridIndices = new();

        string selfCharacterId = currentUserRuntime != null
            ? currentUserRuntime.CharacterId
            : null;

        EnsureTimelineController();
        AddKnownOtherPlayerDestinationsFromScene(blockedGridIndices, selfCharacterId);
        AddKnownOtherPlayerDestinationsFromPartyStore(blockedGridIndices, selfCharacterId);

        return blockedGridIndices;
    }

    private void AddKnownOtherPlayerDestinationsFromScene(
        HashSet<int> blockedGridIndices,
        string selfCharacterId)
    {
        if (blockedGridIndices == null)
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            AddKnownOtherPlayerDestination(
                blockedGridIndices,
                character.RuntimeData,
                character.CurrentGridIndex,
                selfCharacterId
            );
        }
    }

    private void AddKnownOtherPlayerDestinationsFromPartyStore(
        HashSet<int> blockedGridIndices,
        string selfCharacterId)
    {
        if (blockedGridIndices == null || DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
            return;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!characterStore.TryGet(characterId, out CharacterRuntimeData runtime))
                continue;

            int fallbackGridIndex = partyStore.GetCurrentGridIndex(i);

            if (fallbackGridIndex < 0)
                fallbackGridIndex = partyStore.GetSpawnGridIndex(i);

            AddKnownOtherPlayerDestination(
                blockedGridIndices,
                runtime,
                fallbackGridIndex,
                selfCharacterId
            );
        }
    }

    private void AddKnownOtherPlayerDestination(
        HashSet<int> blockedGridIndices,
        CharacterRuntimeData runtime,
        int fallbackGridIndex,
        string selfCharacterId)
    {
        if (blockedGridIndices == null || runtime == null)
            return;

        if (!string.IsNullOrWhiteSpace(selfCharacterId) &&
            runtime.CharacterId == selfCharacterId)
        {
            return;
        }

        int gridIndex = -1;

        if (timelineController != null && currentSlotIndex >= 0)
            gridIndex = timelineController.GetPreviewGridIndexAtSlotEnd(runtime, currentSlotIndex);

        if (gridIndex < 0)
            gridIndex = fallbackGridIndex;

        if (!IsValidMoveDestinationGridIndex(gridIndex))
            return;

        blockedGridIndices.Add(gridIndex);
    }

    private bool IsValidMoveDestinationGridIndex(int gridIndex)
    {
        if (gridIndex < 0)
            return false;

        if (gridManager == null)
            return true;

        return gridManager.IsValidCoord(gridManager.IndexToCoord(gridIndex));
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
            List<Vector2Int> visualMoveSteps = null;

            if (i + 1 < commands.Count)
            {
                PlayerReservedCommand next = commands[i + 1];

                if (CanMergeToDiagonal(current.MoveOffset, next.MoveOffset))
                {
                    visualOffset = current.MoveOffset + next.MoveOffset;
                    visualMoveSteps = new List<Vector2Int>
                    {
                        current.MoveOffset,
                        next.MoveOffset
                    };

                    next.SetSkipMoveVisual(true);

                    i++;
                }
            }

            Vector2Int visualTargetCoord = visualCurrentCoord + visualOffset;

            if (!gridManager.IsValidCoord(visualTargetCoord))
                continue;

            int visualTargetGridIndex = gridManager.CoordToIndex(visualTargetCoord);

            if (visualMoveSteps != null)
            {
                current.SetVisualMoveResult(
                    visualTargetGridIndex,
                    visualOffset,
                    visualMoveSteps
                );
            }

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
        return Mathf.CeilToInt(GetMoveDistance(moveOffset) / (float)safeDistancePerCommand);
    }

    public static int GetRequiredMoveReservationCount(
        IReadOnlyList<Vector2Int> moveSteps,
        int moveDistancePerCommand)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return 0;

        if (moveSteps.Count == 1 && moveSteps[0] == Vector2Int.zero)
            return 1;

        int totalDistance = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            totalDistance += GetMoveDistance(moveSteps[i]);

        if (totalDistance <= 0)
            return 0;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        return Mathf.CeilToInt(totalDistance / (float)safeDistancePerCommand);
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

    public static List<List<Vector2Int>> GetReservableMovePathCandidates(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices = null,
        bool allowBlockedTargetGridIndex = false)
    {
        List<List<Vector2Int>> result = new();

        if (gridManager == null || reservationCapacity <= 0)
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(casterCoord) ||
            !gridManager.IsValidCoord(targetCoord))
            return result;

        Vector2Int moveOffset = targetCoord - casterCoord;
        List<List<Vector2Int>> directPathCandidates =
            BuildMoveStepPathCandidates(moveOffset);

        for (int i = 0; i < directPathCandidates.Count; i++)
        {
            TryAddReservableMoveStepPath(
                result,
                casterGridIndex,
                directPathCandidates[i],
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                blockedGridIndices,
                targetGridIndex,
                allowBlockedTargetGridIndex
            );
        }

        if (result.Count > 0)
            return result;

        List<Vector2Int> shortestPath = FindShortestReservableMoveStepPath(
            casterGridIndex,
            targetGridIndex,
            moveDistancePerCommand,
            reservationCapacity,
            gridManager,
            blockedGridIndices,
            allowBlockedTargetGridIndex
        );

        TryAddReservableMoveStepPath(
            result,
            casterGridIndex,
            shortestPath,
            moveDistancePerCommand,
            reservationCapacity,
            gridManager,
            blockedGridIndices,
            targetGridIndex,
            allowBlockedTargetGridIndex
        );

        return result;
    }

    public static List<Vector2Int> ChooseReservableMovePath(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> currentBlockedGridIndices,
        ISet<int> projectedBlockedGridIndices,
        bool allowBlockedTargetGridIndex = false)
    {
        List<Vector2Int> currentPath = GetFirstMovePath(
            GetReservableMovePathCandidates(
                casterGridIndex,
                targetGridIndex,
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                currentBlockedGridIndices,
                allowBlockedTargetGridIndex));

        List<Vector2Int> projectedPath = GetFirstMovePath(
            GetReservableMovePathCandidates(
                casterGridIndex,
                targetGridIndex,
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                projectedBlockedGridIndices,
                allowBlockedTargetGridIndex));

        if (currentPath == null)
            return projectedPath != null ? new List<Vector2Int>(projectedPath) : null;

        if (projectedPath == null)
            return new List<Vector2Int>(currentPath);

        int currentDistance = GetMoveStepDistance(currentPath);
        int projectedDistance = GetMoveStepDistance(projectedPath);

        return projectedDistance <= currentDistance
            ? new List<Vector2Int>(projectedPath)
            : new List<Vector2Int>(currentPath);
    }

    private static List<Vector2Int> GetFirstMovePath(List<List<Vector2Int>> pathCandidates)
    {
        if (pathCandidates == null)
            return null;

        for (int i = 0; i < pathCandidates.Count; i++)
        {
            List<Vector2Int> path = pathCandidates[i];

            if (path != null && path.Count > 0)
                return path;
        }

        return null;
    }

    private static void TryAddReservableMoveStepPath(
        List<List<Vector2Int>> result,
        int casterGridIndex,
        List<Vector2Int> path,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex)
    {
        if (result == null || path == null || path.Count <= 0)
            return;

        if (GetRequiredMoveReservationCount(path, moveDistancePerCommand) > reservationCapacity)
            return;

        if (!IsMovePathReservable(
            casterGridIndex,
            path,
            gridManager,
            blockedGridIndices,
            targetGridIndex,
            allowBlockedTargetGridIndex))
        {
            return;
        }

        for (int i = 0; i < result.Count; i++)
        {
            if (IsSamePath(result[i], path))
                return;
        }

        result.Add(new List<Vector2Int>(path));
    }

    private static List<List<Vector2Int>> BuildMoveStepPathCandidates(Vector2Int moveOffset)
    {
        List<List<Vector2Int>> candidates = new();

        if (moveOffset == Vector2Int.zero)
        {
            candidates.Add(new List<Vector2Int> { Vector2Int.zero });
            return candidates;
        }

        List<Vector2Int> xFirst = new();
        AddUnitAxisMoveSteps(xFirst, moveOffset.x, true);
        AddUnitAxisMoveSteps(xFirst, moveOffset.y, false);

        List<Vector2Int> yFirst = new();
        AddUnitAxisMoveSteps(yFirst, moveOffset.y, false);
        AddUnitAxisMoveSteps(yFirst, moveOffset.x, true);

        if (xFirst.Count > 0)
            candidates.Add(xFirst);

        if (yFirst.Count > 0 && !IsSamePath(xFirst, yFirst))
            candidates.Add(yFirst);

        return candidates;
    }

    private static void AddUnitAxisMoveSteps(
        List<Vector2Int> result,
        int amount,
        bool horizontal)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int offset = horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step);

            result.Add(offset);
            remaining -= step;
        }
    }

    private static List<Vector2Int> FindShortestReservableMoveStepPath(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        bool allowBlockedTargetGridIndex)
    {
        if (gridManager == null || reservationCapacity <= 0)
            return null;

        if (casterGridIndex == targetGridIndex)
            return new List<Vector2Int> { Vector2Int.zero };

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(casterCoord) ||
            !gridManager.IsValidCoord(targetCoord))
        {
            return null;
        }

        if (!allowBlockedTargetGridIndex &&
            blockedGridIndices != null &&
            blockedGridIndices.Contains(targetGridIndex))
        {
            return null;
        }

        int maxStepCount = Mathf.Max(1, moveDistancePerCommand) * reservationCapacity;
        Queue<int> open = new();
        Dictionary<int, int> parentByIndex = new();
        Dictionary<int, Vector2Int> stepByIndex = new();
        Dictionary<int, int> distanceByIndex = new();

        open.Enqueue(casterGridIndex);
        parentByIndex[casterGridIndex] = -1;
        distanceByIndex[casterGridIndex] = 0;

        while (open.Count > 0)
        {
            int currentIndex = open.Dequeue();
            int currentDistance = distanceByIndex[currentIndex];

            if (currentDistance >= maxStepCount)
                continue;

            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            List<Vector2Int> directions = GetOrderedMoveDirections(currentCoord, targetCoord);

            for (int i = 0; i < directions.Count; i++)
            {
                Vector2Int step = directions[i];
                Vector2Int nextCoord = currentCoord + step;

                if (!gridManager.IsValidCoord(nextCoord))
                    continue;

                int nextIndex = gridManager.CoordToIndex(nextCoord);

                if (parentByIndex.ContainsKey(nextIndex))
                    continue;

                bool isBlockedTarget =
                    allowBlockedTargetGridIndex &&
                    nextIndex == targetGridIndex;

                if (blockedGridIndices != null &&
                    blockedGridIndices.Contains(nextIndex) &&
                    !isBlockedTarget)
                {
                    continue;
                }

                parentByIndex[nextIndex] = currentIndex;
                stepByIndex[nextIndex] = step;
                distanceByIndex[nextIndex] = currentDistance + 1;

                if (nextIndex == targetGridIndex)
                    return ReconstructMoveStepPath(targetGridIndex, parentByIndex, stepByIndex);

                open.Enqueue(nextIndex);
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructMoveStepPath(
        int targetGridIndex,
        Dictionary<int, int> parentByIndex,
        Dictionary<int, Vector2Int> stepByIndex)
    {
        List<Vector2Int> reversedPath = new();
        int currentIndex = targetGridIndex;

        while (parentByIndex.TryGetValue(currentIndex, out int parentIndex) &&
               parentIndex >= 0)
        {
            reversedPath.Add(stepByIndex[currentIndex]);
            currentIndex = parentIndex;
        }

        reversedPath.Reverse();
        return reversedPath;
    }

    private static List<Vector2Int> GetOrderedMoveDirections(
        Vector2Int currentCoord,
        Vector2Int targetCoord)
    {
        List<Vector2Int> directions = new();
        Vector2Int delta = targetCoord - currentCoord;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            AddPreferredHorizontalDirection(directions, delta.x);
            AddPreferredVerticalDirection(directions, delta.y);
        }
        else
        {
            AddPreferredVerticalDirection(directions, delta.y);
            AddPreferredHorizontalDirection(directions, delta.x);
        }

        AddDirectionIfMissing(directions, Vector2Int.right);
        AddDirectionIfMissing(directions, Vector2Int.left);
        AddDirectionIfMissing(directions, Vector2Int.up);
        AddDirectionIfMissing(directions, Vector2Int.down);

        return directions;
    }

    private static void AddPreferredHorizontalDirection(List<Vector2Int> directions, int deltaX)
    {
        if (deltaX > 0)
            AddDirectionIfMissing(directions, Vector2Int.right);
        else if (deltaX < 0)
            AddDirectionIfMissing(directions, Vector2Int.left);
    }

    private static void AddPreferredVerticalDirection(List<Vector2Int> directions, int deltaY)
    {
        if (deltaY > 0)
            AddDirectionIfMissing(directions, Vector2Int.up);
        else if (deltaY < 0)
            AddDirectionIfMissing(directions, Vector2Int.down);
    }

    private static void AddDirectionIfMissing(List<Vector2Int> directions, Vector2Int direction)
    {
        for (int i = 0; i < directions.Count; i++)
        {
            if (directions[i] == direction)
                return;
        }

        directions.Add(direction);
    }

    private static bool IsMovePathReservable(
        int casterGridIndex,
        List<Vector2Int> path,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex)
    {
        if (gridManager == null || path == null || path.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(casterGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int offset = path[i];

            if (offset == Vector2Int.zero)
                continue;

            if (!TryApplyReservableMoveStep(
                ref currentCoord,
                offset,
                gridManager,
                blockedGridIndices,
                targetGridIndex,
                allowBlockedTargetGridIndex,
                i == path.Count - 1))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryApplyReservableMoveStep(
        ref Vector2Int currentCoord,
        Vector2Int offset,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex,
        bool isLastPathOffset)
    {
        if (gridManager == null)
            return false;

        if (offset.x != 0 && offset.y != 0)
            return false;

        int stepCount = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);

        if (stepCount <= 0)
            return true;

        Vector2Int unitStep = Vector2Int.zero;

        if (offset.x != 0)
            unitStep.x = offset.x > 0 ? 1 : -1;
        else
            unitStep.y = offset.y > 0 ? 1 : -1;

        for (int step = 0; step < stepCount; step++)
        {
            currentCoord += unitStep;

            if (!gridManager.IsValidCoord(currentCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(currentCoord);

            bool isBlockedTarget =
                allowBlockedTargetGridIndex &&
                isLastPathOffset &&
                step == stepCount - 1 &&
                gridIndex == targetGridIndex;

            if (blockedGridIndices != null &&
                blockedGridIndices.Contains(gridIndex) &&
                !isBlockedTarget)
            {
                return false;
            }
        }

        return true;
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

    private static int GetMoveStepDistance(IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null)
            return 0;

        int total = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            total += GetMoveDistance(moveSteps[i]);

        return total;
    }

    private static bool IsAllMoveRangeId(string rangeId)
    {
        return rangeId == "Range_All" || rangeId == "Rnage_All";
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

    private static BattleDirection GetDirectionAfterMoveSteps(
        BattleDirection currentDirection,
        IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return currentDirection;

        BattleDirection direction = currentDirection;

        for (int i = 0; i < moveSteps.Count; i++)
            direction = GetDirectionAfterMove(direction, moveSteps[i]);

        return direction;
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

        bool confirmed = timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        if (confirmed)
            ShowTemporaryMonsterHUDsForCommand(command);

        return confirmed;
    }

    private bool ConfirmCommands(IReadOnlyList<PlayerReservedCommand> commands)
    {
        EnsureTimelineController();

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return false;
        }

        bool confirmed = timelineController.ConfirmPlayerCommands(currentSlotIndex, commands);

        if (confirmed)
            ShowTemporaryMonsterHUDsForCommands(commands);

        return confirmed;
    }

    private void ShowTemporaryMonsterHUDsForCommands(IReadOnlyList<PlayerReservedCommand> commands)
    {
        if (commands == null)
            return;

        for (int i = 0; i < commands.Count; i++)
            ShowTemporaryMonsterHUDsForCommand(commands[i]);
    }

    private void ShowTemporaryMonsterHUDsForCommand(PlayerReservedCommand command)
    {
        if (command == null)
            return;

        if (command.ReservedMoveGridIndex >= 0)
            return;

        if (command.RangeGridIndices == null || command.RangeGridIndices.Count <= 0)
            return;

        MonsterUnit.ShowTemporaryHUDsInRange(command.RangeGridIndices, 1f);
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

    private void SetMoveTargetMonsterVisualActive(bool active)
    {
        if (isMoveTargetMonsterVisualActive == active)
            return;

        isMoveTargetMonsterVisualActive = active;
        MonsterUnit.SetAllReservationVisualState(active);
    }

    private bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        if (skillData.Category == Category.Move)
            return true;

        if (skillData.TimelineNotation == TimelineActionType.Move)
            return true;

        return skillData.SkillId == MoveSkillLevelOneId ||
               skillData.SkillId == MoveSkillLevelTwoId;
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    public void ClearPreview()
    {
        SetMoveTargetMonsterVisualActive(false);

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
