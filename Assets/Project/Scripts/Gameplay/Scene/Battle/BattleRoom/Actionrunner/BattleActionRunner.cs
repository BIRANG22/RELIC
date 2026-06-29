using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionRunner
{
    private readonly GridManager gridManager;

    private readonly BattleUnitFinder unitFinder;
    private readonly BattleHUDService hudService;
    private readonly BattleDamageService damageService;
    private readonly BattleDeathService deathService;
    private readonly BattleStatusEffectService statusEffectService;
    private readonly MonsterSkillEffectService monsterSkillEffectService;
    private readonly BattleEffectExecutor effectExecutor = new();
    private readonly bool useSafeSequentialExecution;
    private readonly float actionRoutineTimeout;

    private const float ActionDelay = 0.05f;
    private const float BatchEndDelay = 0.05f;

    private const float HitCameraDelay = 0.12f;
    private const float MonsterHUDVisibleDelay = 0.6f;
    private const float DefaultActionRoutineTimeout = 8f;
    public const float MoveAnimationDuration = 0.15f;

    private class ActionRoutine
    {
        public string Label;
        public IEnumerator Routine;
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner = null,
      BattleRoomLoader roomLoader = null)
        : this(gridManager, monsterSpawner, roomLoader, true, DefaultActionRoutineTimeout)
    {
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner,
      BattleRoomLoader roomLoader,
      bool useSafeSequentialExecution,
      float actionRoutineTimeout)
    {
        this.gridManager = gridManager;
        this.useSafeSequentialExecution = useSafeSequentialExecution;
        this.actionRoutineTimeout = Mathf.Max(0.1f, actionRoutineTimeout);

        unitFinder = new BattleUnitFinder();
        hudService = new BattleHUDService();
        damageService = new BattleDamageService(unitFinder);
        deathService = new BattleDeathService(gridManager, monsterSpawner, roomLoader);
        statusEffectService = new BattleStatusEffectService(damageService, deathService);
        monsterSkillEffectService = new MonsterSkillEffectService(damageService, deathService, hudService, gridManager);
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner,
      BattleRoomLoader roomLoader,
      object fourthArgument,
      object fifthArgument)
        : this(
            gridManager,
            monsterSpawner,
            roomLoader,
            fourthArgument is bool safeSequential ? safeSequential : true,
            fifthArgument is float timeout ? timeout : DefaultActionRoutineTimeout)
    {
    }

    public IEnumerator RunBatch(BattleActionBatch batch, bool keepCameraAfterBatch = false)
    {
        if (batch == null)
            yield break;

        List<ActionRoutine> actionRoutines = BuildActionRoutines(batch);

        if (actionRoutines.Count <= 0)
            yield break;

        bool batchHasCrossSideHit = BatchHasCrossSideHitAction(batch);
        bool holdCameraDuringBatch = batchHasCrossSideHit &&
            (ShouldHoldCameraUntilBatchEnd(batch) || keepCameraAfterBatch);

        if (BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(holdCameraDuringBatch);

            if (!batchHasCrossSideHit && BattleCameraController.Instance.IsCombatZoomActive)
                yield return BattleCameraController.Instance.ReturnDefault();
        }

        if (useSafeSequentialExecution)
            yield return RunSequential(actionRoutines);
        else
            yield return RunParallel(actionRoutines);

        if (holdCameraDuringBatch && !keepCameraAfterBatch && BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(false);
            yield return BattleCameraController.Instance.ReturnDefault();
        }

        IncreaseMonsterTurnCountsOnceInSlot(batch);

        yield return RunPostActionPresentationRoutine();
    }

    private List<ActionRoutine> BuildActionRoutines(BattleActionBatch batch)
    {
        List<ActionRoutine> actionRoutines = new();

        if (batch == null)
            return actionRoutines;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (!BattleActionOrderUtility.HasSwift(command))
                continue;

            AddPlayerActionRoutine(actionRoutines, command);
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            actionRoutines.Add(CreateActionRoutine($"Monster:{command.RuntimeId}:{command.SkillId}", ExecuteMonsterCommand(command)));
        }

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (BattleActionOrderUtility.HasSwift(command))
                continue;

            AddPlayerActionRoutine(actionRoutines, command);
        }

        return actionRoutines;
    }

    private void AddPlayerActionRoutine(List<ActionRoutine> actionRoutines, PlayerReservedCommand command)
    {
        if (actionRoutines == null || command == null)
            return;

        if (command.UserRuntime == null || command.UserRuntime.IsDead)
            return;

        if (command.ReservedMoveGridIndex >= 0)
        {
            if (IsConsumedVisualSkipMove(command))
                return;

            actionRoutines.Add(CreateActionRoutine($"PlayerMove:{command.CharacterId}", ExecutePlayerMove(command)));
            return;
        }

        actionRoutines.Add(CreateActionRoutine($"PlayerSkill:{command.CharacterId}:{command.SkillId}", ExecutePlayerSkill(command)));
    }

    private IEnumerator RunPostActionPresentationRoutine()
    {
        yield return new WaitForSeconds(MonsterHUDVisibleDelay);

        hudService.HideUnselectedMonsterHUDs();

        yield return new WaitForSeconds(BatchEndDelay);

        hudService.PlayAllAliveIdle();
    }

    public bool ApplyTurnEndEffects()
    {
        bool playedPresentation = statusEffectService.ApplyTurnEndEffects();
        hudService.RefreshHUDs();

        return playedPresentation;
    }

    public IEnumerator ApplyTurnEndEffectsRoutine()
    {
        bool playedPresentation = ApplyTurnEndEffects();

        if (!playedPresentation)
            yield break;

        IEnumerator presentationRoutine = RunPostActionPresentationRoutine();

        while (presentationRoutine.MoveNext())
            yield return presentationRoutine.Current;
    }

    public IEnumerator ReturnCameraDefaultIfNeeded()
    {
        if (BattleCameraController.Instance == null)
            yield break;

        BattleCameraController.Instance.SetHoldDefaultReturn(false);
        yield return BattleCameraController.Instance.ReturnDefault();
    }

    private bool ShouldHoldCameraUntilBatchEnd(BattleActionBatch batch)
    {
        return CountCrossSideHitActions(batch) > 1;
    }

    public bool BatchHasCrossSideHitAction(BattleActionBatch batch)
    {
        return CountCrossSideHitActions(batch) > 0;
    }

    private int CountCrossSideHitActions(BattleActionBatch batch)
    {
        if (batch == null)
            return 0;

        int count = 0;

        if (batch.PlayerCommands != null)
        {
            for (int i = 0; i < batch.PlayerCommands.Count; i++)
            {
                PlayerReservedCommand command = batch.PlayerCommands[i];

                if (command == null || command.SkillData == null)
                    continue;

                if (command.ReservedMoveGridIndex >= 0)
                    continue;

                if (command.SkillData.Target == TargetType.Self || command.SkillData.Target == TargetType.PlayerParty)
                    continue;

                if (HasMonsterTarget(command))
                    count++;
            }
        }

        if (batch.MonsterCommands != null)
        {
            for (int i = 0; i < batch.MonsterCommands.Count; i++)
            {
                MonsterReservedCommand command = batch.MonsterCommands[i];

                if (command == null || command.SkillData == null)
                    continue;

                if (command.SkillData.TimelineNotation == TimelineActionType.Move)
                    continue;

                if (HasPlayerTarget(command))
                    count++;
            }
        }

        return count;
    }

    private bool HasMonsterTarget(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (IsMonsterInRange(monster, command))
                return true;
        }

        return false;
    }

    private bool HasPlayerTarget(MonsterReservedCommand command)
    {
        return FindFirstPlayerTarget(command) != null;
    }

    private void IncreaseMonsterTurnCountsOnceInSlot(BattleActionBatch batch)
    {
        if (batch == null || batch.MonsterCommands == null)
            return;

        HashSet<string> increasedRuntimeIds = new();

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null || string.IsNullOrWhiteSpace(command.RuntimeId))
                continue;

            if (increasedRuntimeIds.Contains(command.RuntimeId))
                continue;

            MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

            if (monster == null || monster.RuntimeData == null)
                continue;

            monster.RuntimeData.IncreaseTurnCount();
            increasedRuntimeIds.Add(command.RuntimeId);
        }
    }

    private ActionRoutine CreateActionRoutine(string label, IEnumerator routine)
    {
        return new ActionRoutine
        {
            Label = label,
            Routine = routine
        };
    }

    private IEnumerator RunSequential(List<ActionRoutine> routines)
    {
        if (routines == null || routines.Count == 0)
            yield break;

        for (int i = 0; i < routines.Count; i++)
            yield return RunSingleWithTimeout(routines[i]);
    }

    private IEnumerator RunSingleWithTimeout(ActionRoutine actionRoutine)
    {
        if (actionRoutine == null || actionRoutine.Routine == null)
            yield break;

        if (CoroutineHost.Instance == null)
        {
            Debug.LogError($"[BattleActionRunner] CoroutineHost 없음 / Action:{actionRoutine.Label}");
            yield break;
        }

        bool completed = false;
        Coroutine runningCoroutine = CoroutineHost.Instance.StartCoroutine(
            RunAndCountDown(
                actionRoutine,
                () => completed = true
            )
        );

        float elapsed = 0f;

        while (!completed)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= actionRoutineTimeout)
            {
                Debug.LogError(
                    $"[BattleActionRunner] Action Timeout / " +
                    $"Action:{actionRoutine.Label} / Timeout:{actionRoutineTimeout:0.00}s"
                );

                if (runningCoroutine != null && CoroutineHost.Instance != null)
                    CoroutineHost.Instance.StopCoroutine(runningCoroutine);

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator RunParallel(List<ActionRoutine> routines)
    {
        if (routines == null || routines.Count == 0)
            yield break;

        if (CoroutineHost.Instance == null)
        {
            Debug.LogError("[BattleActionRunner] CoroutineHost 없음");
            yield break;
        }

        int runningCount = routines.Count;
        bool[] completed = new bool[routines.Count];
        Coroutine[] runningCoroutines = new Coroutine[routines.Count];

        for (int i = 0; i < routines.Count; i++)
        {
            int routineIndex = i;

            runningCoroutines[i] = CoroutineHost.Instance.StartCoroutine(
                RunAndCountDown(
                    routines[i],
                    () =>
                    {
                        completed[routineIndex] = true;
                        runningCount--;
                        //Debug.Log($"[BattleActionRunner] Routine End:{routineIndex} / Left:{runningCount}");
                    }
                )
            );
        }

        float elapsed = 0f;

        while (runningCount > 0)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= actionRoutineTimeout)
            {
                Debug.LogError(
                    $"[BattleActionRunner] RunParallel Timeout / " +
                    $"Left:{runningCount} / Timeout:{actionRoutineTimeout:0.00}s / " +
                    $"Actions:{BuildPendingActionLabel(completed, routines)}"
                );

                for (int i = 0; i < runningCoroutines.Length; i++)
                {
                    if (completed[i] || runningCoroutines[i] == null || CoroutineHost.Instance == null)
                        continue;

                    CoroutineHost.Instance.StopCoroutine(runningCoroutines[i]);
                }

                break;
            }

            yield return null;
        }
    }

    private string BuildPendingActionLabel(bool[] completed, List<ActionRoutine> routines)
    {
        if (completed == null || routines == null)
            return "";

        List<string> pendingLabels = new();

        for (int i = 0; i < routines.Count; i++)
        {
            if (i < completed.Length && completed[i])
                continue;

            pendingLabels.Add(routines[i] != null ? routines[i].Label : $"Index:{i}");
        }

        return string.Join(", ", pendingLabels);
    }

    private IEnumerator RunAndCountDown(ActionRoutine actionRoutine, System.Action onComplete)
    {
        bool done = false;
        IEnumerator routine = actionRoutine != null ? actionRoutine.Routine : null;

        while (!done)
        {
            object current = null;

            try
            {
                if (routine == null || !routine.MoveNext())
                {
                    done = true;
                }
                else
                {
                    current = routine.Current;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleActionRunner] Action Exception / Action:{actionRoutine?.Label}");
                Debug.LogException(e);
                done = true;
            }

            if (!done)
                yield return current;
        }

        onComplete?.Invoke();
    }

    private IEnumerator ExecutePlayerMove(PlayerReservedCommand command)
    {
        BattleCharacter character = unitFinder.FindBattleCharacter(command.CharacterId);

        if (character == null)
            yield break;

        if (character.RuntimeData == null || character.RuntimeData.IsDead)
            yield break;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            yield break;

        if (IsConsumedVisualSkipMove(command))
        {
            command.SetExecutedMoveDistance(0);
            hudService.RefreshHUDs();
            yield break;
        }

        ConsumePlayerMoveCost(command, character);

        bool useVisualMove = TryGetPlayerVisualMoveTargetGridIndex(
            command,
            currentGridIndex,
            out int targetGridIndex,
            out Vector2Int moveOffset
        );

        if (!useVisualMove)
            moveOffset = command.ExecutionMoveOffset;

        if (moveOffset == Vector2Int.zero)
        {
            command.SetExecutedMoveDistance(0);
            ApplyBlockedPlayerMoveCostRefund(command);
            ApplyPlayerMoveFacing(character, command.Direction, moveOffset);
            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        if (command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 1)
        {
            yield return ExecutePlayerVisualMoveSteps(command, character, currentGridIndex);
            yield break;
        }

        ApplyPlayerMoveFacing(character, command.Direction, moveOffset);

        if (!useVisualMove &&
            !TryGetPlayerMoveTargetGridIndex(
                currentGridIndex,
                moveOffset,
                command.CharacterId,
                out targetGridIndex))
        {
            command.SetExecutedMoveDistance(0);
            ApplyBlockedPlayerMoveCostRefund(command);
            hudService.RefreshHUDs();
            Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / Offset:{moveOffset}");
            yield break;
        }

        RecordPlayerMoveExecutionDistance(command, currentGridIndex, targetGridIndex);

        if (targetGridIndex == currentGridIndex)
        {
            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        Vector3 pos = gridManager.GetWorldPositionByIndex(targetGridIndex);

        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayMove();

        yield return MoveTransformSmooth(
            character.transform,
            character.transform.position,
            pos,
            MoveAnimationDuration
        );

        character.SetGridIndex(targetGridIndex);
        UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
        statusEffectService.ApplyBurnDamageToPlayerOnMove(character);

        hudService.RefreshHUDs();

        yield return new WaitForSeconds(ActionDelay);
    }

    private IEnumerator ExecutePlayerVisualMoveSteps(
        PlayerReservedCommand command,
        BattleCharacter character,
        int startGridIndex)
    {
        int currentGridIndex = startGridIndex;
        int executedDistance = 0;
        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
        List<List<Vector2Int>> executionStepGroups =
            BuildPlayerMoveExecutionStepGroups(command.VisualMoveSteps);

        for (int i = 0; i < executionStepGroups.Count; i++)
        {
            IReadOnlyList<Vector2Int> stepGroup = executionStepGroups[i];
            Vector2Int stepOffset = GetTotalMoveOffset(stepGroup);

            if (stepOffset == Vector2Int.zero)
            {
                ApplyPlayerMoveFacing(character, command.Direction, stepOffset);
                continue;
            }

            if (!TryGetPlayerMoveTargetGridIndex(
                currentGridIndex,
                stepGroup,
                command.CharacterId,
                out int targetGridIndex))
            {
                break;
            }

            if (targetGridIndex == currentGridIndex)
                break;

            Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int actualOffset = targetCoord - currentCoord;

            ApplyPlayerMoveFacing(character, command.Direction, actualOffset);

            if (animator != null)
                animator.PlayMove();

            Vector3 pos = gridManager.GetWorldPositionByIndex(targetGridIndex);

            yield return MoveTransformSmooth(
                character.transform,
                character.transform.position,
                pos,
                MoveAnimationDuration
            );

            character.SetGridIndex(targetGridIndex);
            UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
            currentGridIndex = targetGridIndex;
            executedDistance += GetMoveDistance(actualOffset);
        }

        command.SetExecutedMoveDistance(executedDistance);
        ApplyBlockedPlayerMoveCostRefund(command);

        if (currentGridIndex != startGridIndex)
            statusEffectService.ApplyBurnDamageToPlayerOnMove(character);

        hudService.RefreshHUDs();
        yield return new WaitForSeconds(ActionDelay);
    }

    private static List<List<Vector2Int>> BuildPlayerMoveExecutionStepGroups(
        IReadOnlyList<Vector2Int> moveSteps)
    {
        List<List<Vector2Int>> groups = new();

        if (moveSteps == null || moveSteps.Count <= 0)
            return groups;

        List<Vector2Int> currentGroup = new();
        int currentXSign = 0;
        int currentYSign = 0;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int step = moveSteps[i];

            if (step == Vector2Int.zero)
            {
                FlushMoveExecutionGroup(groups, currentGroup, ref currentXSign, ref currentYSign);
                groups.Add(new List<Vector2Int> { Vector2Int.zero });
                continue;
            }

            if (WouldReverseMoveExecutionAxis(step, currentXSign, currentYSign))
                FlushMoveExecutionGroup(groups, currentGroup, ref currentXSign, ref currentYSign);

            currentGroup.Add(step);
            UpdateMoveExecutionAxisSigns(step, ref currentXSign, ref currentYSign);
        }

        FlushMoveExecutionGroup(groups, currentGroup, ref currentXSign, ref currentYSign);
        return groups;
    }

    private static void FlushMoveExecutionGroup(
        List<List<Vector2Int>> groups,
        List<Vector2Int> currentGroup,
        ref int currentXSign,
        ref int currentYSign)
    {
        if (currentGroup != null && currentGroup.Count > 0)
        {
            groups.Add(new List<Vector2Int>(currentGroup));
            currentGroup.Clear();
        }

        currentXSign = 0;
        currentYSign = 0;
    }

    private static bool WouldReverseMoveExecutionAxis(
        Vector2Int step,
        int currentXSign,
        int currentYSign)
    {
        int stepXSign = GetSign(step.x);
        int stepYSign = GetSign(step.y);

        return (stepXSign != 0 && currentXSign != 0 && stepXSign != currentXSign) ||
               (stepYSign != 0 && currentYSign != 0 && stepYSign != currentYSign);
    }

    private static void UpdateMoveExecutionAxisSigns(
        Vector2Int step,
        ref int currentXSign,
        ref int currentYSign)
    {
        int stepXSign = GetSign(step.x);
        int stepYSign = GetSign(step.y);

        if (stepXSign != 0)
            currentXSign = stepXSign;

        if (stepYSign != 0)
            currentYSign = stepYSign;
    }

    private static int GetSign(int value)
    {
        if (value > 0)
            return 1;

        if (value < 0)
            return -1;

        return 0;
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

    private static int GetMoveDistance(Vector2Int moveOffset)
    {
        return Mathf.Abs(moveOffset.x) + Mathf.Abs(moveOffset.y);
    }

    private bool IsConsumedVisualSkipMove(PlayerReservedCommand command)
    {
        if (command == null || !command.SkipMoveVisual)
            return false;

        BattleCharacter character = unitFinder.FindBattleCharacter(command.CharacterId);

        return character != null &&
               command.IsVisualSkipConsumedAtGrid(character.CurrentGridIndex);
    }

    private bool TryGetPlayerVisualMoveTargetGridIndex(
        PlayerReservedCommand command,
        int currentGridIndex,
        out int targetGridIndex,
        out Vector2Int visualMoveOffset)
    {
        targetGridIndex = currentGridIndex;
        visualMoveOffset = Vector2Int.zero;

        if (command == null ||
            !command.HasVisualMoveResult ||
            command.VisualMoveSteps == null ||
            command.VisualMoveSteps.Count <= 0)
        {
            return false;
        }

        if (!TryGetPlayerMoveTargetGridIndex(
            currentGridIndex,
            command.VisualMoveSteps,
            command.CharacterId,
            out targetGridIndex))
        {
            return false;
        }

        if (gridManager == null)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(currentCoord) || !gridManager.IsValidCoord(targetCoord))
        {
            return false;
        }

        visualMoveOffset = targetCoord - currentCoord;
        return true;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGridIndex,
        Vector2Int moveOffset,
        string characterId,
        out int targetGridIndex)
    {
        targetGridIndex = currentGridIndex;

        if (gridManager == null)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        if (moveOffset == Vector2Int.zero)
            return true;

        bool reachedTarget = true;

        if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId))
            reachedTarget = false;

        if (reachedTarget &&
            !TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId))
        {
            reachedTarget = false;
        }

        targetGridIndex = gridManager.CoordToIndex(currentCoord);
        return true;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGridIndex,
        IReadOnlyList<Vector2Int> moveSteps,
        string characterId,
        out int targetGridIndex)
    {
        targetGridIndex = currentGridIndex;

        if (gridManager == null || moveSteps == null || moveSteps.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int moveOffset = moveSteps[i];

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId))
                break;

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId))
                break;
        }

        targetGridIndex = gridManager.CoordToIndex(currentCoord);
        return true;
    }

    private bool TryApplyPlayerMoveAxisStep(
        ref Vector2Int currentCoord,
        int amount,
        bool horizontal,
        string characterId)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int nextCoord = currentCoord + (horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step));

            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(nextCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, characterId))
                return false;

            currentCoord = nextCoord;
            remaining -= step;
        }

        return true;
    }

    private void ApplyPlayerMoveFacing(
        BattleCharacter character,
        BattleDirection direction,
        Vector2Int moveOffset)
    {
        if (character == null)
            return;

        BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();

        if (facing != null)
        {
            if (moveOffset.x != 0)
                facing.FaceByMoveOffset(moveOffset);
            else
                facing.FaceRight(direction == BattleDirection.Right);

            if (character.RuntimeData != null)
                character.RuntimeData.Direction = facing.GetBattleDirection();

            return;
        }

        if (character.RuntimeData != null)
            character.RuntimeData.Direction = direction;
    }

    private void ConsumePlayerMoveCost(PlayerReservedCommand command, BattleCharacter character)
    {
        if (command == null || character == null || character.RuntimeData == null)
            return;

        if (command.MoveCostConsumed)
            return;

        int cost = Mathf.Max(0, command.Cost);

        if (cost > 0)
        {
            character.RuntimeData.RemoveReservedCost(cost);
            character.RuntimeData.CurrentCost = Mathf.Max(
                0,
                character.RuntimeData.CurrentCost - cost
            );
        }

        command.MarkMoveCostConsumed();
    }

    private void RecordPlayerMoveExecutionDistance(
        PlayerReservedCommand command,
        int startGridIndex,
        int targetGridIndex)
    {
        if (command == null || gridManager == null)
            return;

        Vector2Int startCoord = gridManager.IndexToCoord(startGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(startCoord) || !gridManager.IsValidCoord(targetCoord))
        {
            command.SetExecutedMoveDistance(0);
            return;
        }

        Vector2Int actualOffset = targetCoord - startCoord;
        command.SetExecutedMoveDistance(
            Mathf.Abs(actualOffset.x) + Mathf.Abs(actualOffset.y)
        );
        ApplyBlockedPlayerMoveCostRefund(command);
    }

    private void ApplyBlockedPlayerMoveCostRefund(PlayerReservedCommand command)
    {
        if (command == null || !command.MoveCostConsumed)
            return;

        int refund = command.ApplyBlockedMoveCostRefund();

        if (refund <= 0)
            return;

        Debug.Log(
            $"[BattleActionRunner] Move Cost refund / " +
            $"Character:{command.CharacterId} / Refund:{refund}"
        );
    }

    private IEnumerator ExecutePlayerSkill(PlayerReservedCommand command)
    {
        BattleCharacter attacker = unitFinder.FindBattleCharacter(command.CharacterId);

        if (attacker == null)
            yield break;

        if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            yield break;

        RecalculatePlayerSkillRangeAtExecution(attacker, command);

        ConsumePlayerSkillCost(command, attacker);

        if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
        {
            hudService.RefreshHUDs();
            yield break;
        }

        BattleUnitAnimator attackerAnimator = attacker.GetComponent<BattleUnitAnimator>();

        if (command.SkillData.Target == TargetType.PlayerParty)
        {
            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

            if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            {
                hudService.RefreshHUDs();
                yield break;
            }

            BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < characters.Length; i++)
            {
                BattleCharacter target = characters[i];

                if (target == null || target.RuntimeData == null)
                    continue;

                if (target.RuntimeData.IsDead)
                    continue;

                if (!command.RangeGridIndices.Contains(target.CurrentGridIndex))
                    continue;

                ExecutePlayerSkillEffectsToPlayer(attacker, target, command);
            }

            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        if (command.SkillData.Target == TargetType.Self)
        {
            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

            if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            {
                hudService.RefreshHUDs();
                yield break;
            }

            ExecutePlayerSkillEffectsToPlayer(attacker, attacker, command);

            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<MonsterUnit> hitTargets = new();

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (!IsMonsterInRange(monster, command))
                continue;

            hitTargets.Add(monster);
        }

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToAttacker(attacker.transform);

        statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

        if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
        {
            hudService.RefreshHUDs();
            yield break;
        }

        if (hitTargets.Count <= 0)
        {
            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        yield return ExecutePlayerSkillEffectsToMonsters(
            attacker,
            hitTargets,
            command,
            attackerAnimator);

        hudService.RefreshHUDs();

        if (BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
    }

    private void RecalculatePlayerSkillRangeAtExecution(
    BattleCharacter attacker,
    PlayerReservedCommand command)
    {
        if (attacker == null || command == null || command.SkillData == null)
            return;

        if (command.ReservedMoveGridIndex >= 0)
            return;

        if (command.SkillData.RangeType == RangeType.None)
            return;

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
            return;

        BattleDirection direction = attacker.RuntimeData != null
            ? attacker.RuntimeData.Direction
            : command.Direction;

        string rangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(attacker.RuntimeData, command.SkillData);

        List<int> rangeGridIndices = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            rangeGridIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                attacker.CurrentGridIndex,
                rangeId,
                direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetDirectionResult(
                direction,
                rangeGridIndices,
                rangeGridIndices
            );

            return;
        }

        if (command.SkillData.RangeType == RangeType.Selection)
        {
            rangeGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                attacker.CurrentGridIndex,
                rangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetDirectionResult(
                direction,
                rangeGridIndices,
                rangeGridIndices
            );
        }
    }

    private IEnumerator ExecutePlayerSkillEffectsToMonsters(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        PlayerReservedCommand command,
        BattleUnitAnimator attackerAnimator)
    {
        if (caster == null || command == null || command.SkillData == null)
            yield break;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
        {
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries 없음 / Skill:{command.SkillData.SkillId}");

            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        bool playedDamageSequence = false;
        bool playedActionForNonDamage = false;

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            int value = GetPlayerEffectValue(command, entry);
            int count = GetPlayerEffectCount(command, entry);

            if (IsDamageHitEffect(entry.EffectId))
            {
                playedDamageSequence = true;

                yield return ExecutePlayerDamageHitSequence(
                    caster,
                    monsterTargets,
                    command,
                    entry.EffectId,
                    value,
                    count,
                    attackerAnimator);

                continue;
            }

            if (!playedDamageSequence && !playedActionForNonDamage)
            {
                if (attackerAnimator != null)
                    attackerAnimator.PlaySkillAction(command.SkillData);

                playedActionForNonDamage = true;
                yield return new WaitForSeconds(ActionDelay);
            }

            ExecutePlayerNonDamageEffectToMonsters(
                caster,
                monsterTargets,
                command,
                entry.EffectId,
                value,
                count);
        }

        if (!playedDamageSequence && !playedActionForNonDamage)
        {
            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            yield return new WaitForSeconds(ActionDelay);
        }
    }

    private IEnumerator ExecutePlayerDamageHitSequence(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count,
        BattleUnitAnimator attackerAnimator)
    {
        int hitCount = Mathf.Max(1, count);

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            if (!HasAliveMonsterTarget(monsterTargets))
                yield break;

            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            yield return new WaitForSeconds(ActionDelay);

            bool appliedAnyHit = false;

            for (int i = 0; i < monsterTargets.Count; i++)
            {
                MonsterUnit monster = monsterTargets[i];

                if (!IsAliveMonsterTarget(monster))
                    continue;

                FaceMonsterToAttacker(monster, caster);

                BattleEffectContext context = CreatePlayerMonsterEffectContext(
                    caster,
                    monster,
                    command,
                    effectId,
                    value,
                    1);

                ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                appliedAnyHit = true;

                if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                    deathService.HandleMonsterDead(monster);
            }

            hudService.RefreshHUDs();

            if (!appliedAnyHit)
                yield break;

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.PlayDamageImpact();

            yield return new WaitForSeconds(HitCameraDelay);
        }
    }

    private void ExecutePlayerNonDamageEffectToMonsters(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count)
    {
        if (monsterTargets == null)
            return;

        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterUnit monster = monsterTargets[i];

            if (!IsAliveMonsterTarget(monster))
                continue;

            FaceMonsterToAttacker(monster, caster);

            BattleEffectContext context = CreatePlayerMonsterEffectContext(
                caster,
                monster,
                command,
                effectId,
                value,
                count);

            ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
        }
    }

    private BattleEffectContext CreatePlayerMonsterEffectContext(
        BattleCharacter caster,
        MonsterUnit monsterTarget,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count)
    {
        return new BattleEffectContext
        {
            PlayerCaster = caster,
            MonsterTarget = monsterTarget,
            PlayerSkillData = command.SkillData,

            Direction = command.Direction,
            GridManager = gridManager,

            EffectId = effectId,
            Value = value,
            Count = count
        };
    }

    private bool IsDamageHitEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce";
    }

    private bool HasAliveMonsterTarget(List<MonsterUnit> monsterTargets)
    {
        if (monsterTargets == null)
            return false;

        for (int i = 0; i < monsterTargets.Count; i++)
        {
            if (IsAliveMonsterTarget(monsterTargets[i]))
                return true;
        }

        return false;
    }

    private bool IsAliveMonsterTarget(MonsterUnit monster)
    {
        return monster != null &&
               monster.RuntimeData != null &&
               !monster.RuntimeData.IsDead;
    }

    private void FaceMonsterToAttacker(MonsterUnit monster, BattleCharacter attacker)
    {
        if (monster == null || attacker == null)
            return;

        BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();

        if (hitFacing == null)
            return;

        hitFacing.FaceByWorldTarget(attacker.transform.position);

        if (monster.RuntimeData != null)
            monster.RuntimeData.Direction = hitFacing.GetBattleDirection();
    }

    private void ExecutePlayerSkillEffects(
        BattleCharacter caster,
        MonsterUnit monsterTarget,
        PlayerReservedCommand command)
    {
        if (caster == null || monsterTarget == null || command == null || command.SkillData == null)
            return;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
        {
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries 없음 / Skill:{command.SkillData.SkillId}");
            return;
        }

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                MonsterTarget = monsterTarget,
                PlayerSkillData = command.SkillData,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = entry.EffectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry)
            };

            if (IsDamageHitEffect(entry.EffectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (monsterTarget.RuntimeData.IsDead)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
            }
        }
    }

    private void ExecutePlayerSkillEffectsToPlayer(
    BattleCharacter caster,
    BattleCharacter playerTarget,
    PlayerReservedCommand command)
    {
        if (caster == null || playerTarget == null || command == null || command.SkillData == null)
            return;

        if (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead)
            return;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
            return;

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            if (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead)
                break;

            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                PlayerTarget = playerTarget,
                PlayerSkillData = command.SkillData,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = entry.EffectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry)
            };

            if (IsDamageHitEffect(entry.EffectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (playerTarget.RuntimeData.CurrentHP <= 0)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
            }
        }
    }

    private void ExecutePlayerEffectSafely(
        string effectId,
        BattleEffectContext context,
        string skillId)
    {
        try
        {
            effectExecutor.Execute(effectId, context);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[PlayerSkillEffect] Effect 실행 중 에러 / " +
                $"Skill:{skillId} / Effect:{effectId}"
            );
            Debug.LogException(e);
        }
    }

    private int GetPlayerEffectValue(PlayerReservedCommand command, SkillEffectEntry entry)
    {
        if (command == null || entry == null)
            return 1;

        if (entry.EffectId == "E_Strike")
            return BattleEquipmentEffectService.ModifyPlayerEffectValue(
                command.UserRuntime,
                command,
                entry,
                damageService.GetPlayerDamage(command));

        if (entry.EffectId == "E_Pierce")
            return BattleEquipmentEffectService.ModifyPlayerEffectValue(
                command.UserRuntime,
                command,
                entry,
                damageService.GetPlayerDamage(command));

        return BattleEquipmentEffectService.ModifyPlayerEffectValue(
            command.UserRuntime,
            command,
            entry,
            entry.ValueAmount);
    }

    private int GetPlayerEffectCount(PlayerReservedCommand command, SkillEffectEntry entry)
    {
        if (command == null || entry == null)
            return 1;

        return BattleEquipmentEffectService.ModifyPlayerEffectCount(
            command.UserRuntime,
            command,
            entry,
            entry.CountAmount);
    }
    private IEnumerator ExecuteMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            yield break;

        if (command.SkillData.TimelineNotation == TimelineActionType.Move)
        {
            yield return ExecuteMonsterMove(command);
        }
        else
        {
            yield return ExecuteMonsterSkill(command);
        }
    }

    private IEnumerator ExecuteMonsterMove(MonsterReservedCommand command)
    {
        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            yield break;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
            facing.FaceByMoveOffset(moveOffset);

        if (!CanApplyMonsterMove(monster, moveOffset))
            yield break;

        Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int movedMainCoord = mainCoord + moveOffset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        Vector3 pos = gridManager.GetWorldPositionByIndex(movedMainIndex);

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayMove();

        yield return MoveTransformSmooth(
            monster.transform,
            monster.transform.position,
            pos,
            MoveAnimationDuration
        );

        monster.MoveOccupiedCells(moveOffset, gridManager);
        statusEffectService.ApplyBurnDamageToMonsterOnMove(monster);

        hudService.RefreshHUDs();

        yield return new WaitForSeconds(ActionDelay);
    }

    private bool CanApplyMonsterMove(MonsterUnit monster, Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            return false;

        if (moveOffset == Vector2Int.zero)
            return false;

        if (moveOffset.x != 0 && moveOffset.y != 0)
        {
            return CanApplyMonsterMoveAxisOrder(monster, moveOffset, true) ||
                   CanApplyMonsterMoveAxisOrder(monster, moveOffset, false);
        }

        return CanApplyMonsterMoveAxisOrder(monster, moveOffset, moveOffset.x != 0);
    }

    private bool CanApplyMonsterMoveAxisOrder(
        MonsterUnit monster,
        Vector2Int moveOffset,
        bool horizontalFirst)
    {
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        if (horizontalFirst)
        {
            return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster) &&
                   TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster);
        }

        return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster) &&
               TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster);
    }

    private bool TryApplyMonsterMoveAxisSteps(
        List<Vector2Int> currentCoords,
        int amount,
        bool horizontal,
        MonsterUnit monster)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            List<Vector2Int> nextCoords = new();

            for (int i = 0; i < currentCoords.Count; i++)
            {
                Vector2Int nextCoord = currentCoords[i] + (horizontal
                    ? new Vector2Int(step, 0)
                    : new Vector2Int(0, step));

                if (!gridManager.IsValidCoord(nextCoord))
                    return false;

                int targetIndex = gridManager.CoordToIndex(nextCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                    return false;

                nextCoords.Add(nextCoord);
            }

            currentCoords.Clear();
            currentCoords.AddRange(nextCoords);
            remaining -= step;
        }

        return true;
    }

    private IEnumerator ExecuteMonsterSkill(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            yield break;

        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        if (command.SkillData.SkillId == "S_Monster_07")
        {
            yield return ExecuteMonsterDashAttack(command);
            yield break;
        }

        // 현재 방향 기준으로 먼저 범위 재계산
        RecalculateMonsterSkillRangeAtExecution(monster, command);

        BattleCharacter firstPlayerTarget = FindFirstPlayerTarget(command);

        BattleUnitAnimator monsterAnimator = monster.GetComponent<BattleUnitAnimator>();

        // 타겟이 있으면 바라보고, 바라본 방향으로 다시 범위 재계산
        if (firstPlayerTarget != null)
        {
            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

            if (facing != null)
            {
                facing.FaceByWorldTarget(firstPlayerTarget.transform.position);

                if (monster.RuntimeData != null)
                    monster.RuntimeData.Direction = facing.GetBattleDirection();

                RecalculateMonsterSkillRangeAtExecution(monster, command);
                firstPlayerTarget = FindFirstPlayerTarget(command);
            }
        }

        if (firstPlayerTarget != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToAttacker(monster.transform);

        statusEffectService.ApplyBleedingDamageToMonsterOnAttack(monster);

        bool hasDamageHitEffect = monsterSkillEffectService.HasDamageHitEffect(command);

        if (hasDamageHitEffect)
        {
            yield return ExecuteMonsterDamageHitSequence(monster, command, monsterAnimator);
            monsterSkillEffectService.ApplyMonsterSkillNonDamageEffects(monster, command);
        }
        else
        {
            if (monsterAnimator != null)
                monsterAnimator.PlayMonsterSkillAction(command);

            monsterSkillEffectService.ApplyMonsterSkill(monster, command);

            if (firstPlayerTarget != null && BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.PlayDamageImpact();

            if (firstPlayerTarget != null)
                yield return new WaitForSeconds(HitCameraDelay);
            else
                yield return new WaitForSeconds(ActionDelay);
        }

        if (BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
    }

    private IEnumerator ExecuteMonsterDamageHitSequence(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator monsterAnimator)
    {
        if (monster == null || command == null || command.SkillData == null)
            yield break;

        int hitCount = monsterSkillEffectService.GetDamageHitCount(command);

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            if (!HasAliveMonsterSkillTarget(monster, command))
                yield break;

            if (monsterAnimator != null)
                monsterAnimator.PlayMonsterSkillAction(command);

            yield return new WaitForSeconds(ActionDelay);

            bool hadCameraTarget = HasMonsterSkillCameraTarget(command);

            monsterSkillEffectService.ApplyMonsterSkillDamageHit(monster, command, hitIndex);

            if (BattleCameraController.Instance != null && hadCameraTarget)
                yield return BattleCameraController.Instance.PlayDamageImpact();

            yield return new WaitForSeconds(HitCameraDelay);
        }
    }

    private void RecalculateMonsterSkillRangeAtExecution(
    MonsterUnit monster,
    MonsterReservedCommand command)
    {
        if (monster == null || command == null || command.SkillData == null)
            return;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        bool facingRight = facing == null || facing.IsFacingRight;

        List<int> rangeGridIndices =
            MonsterSkillRangeService.BuildRangeGridIndices(
                monster,
                command.SkillData,
                gridManager,
                facingRight
            );

        List<int> targetGridIndices =
            MonsterSkillRangeService.FilterTargetGridIndices(
                command.SkillData,
                rangeGridIndices
            );

        command.SetRangeResult(rangeGridIndices, targetGridIndices);
    }

    private BattleCharacter FindFirstPlayerTarget(MonsterReservedCommand command)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                return character;
        }

        return null;
    }

    private bool HasAliveMonsterSkillTarget(
        MonsterUnit caster,
        MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        switch (command.SkillData.Target)
        {
            case TargetType.PlayerParty:
                return FindFirstAlivePlayerTarget(command) != null;

            case TargetType.EnemyParty:
                return FindFirstAliveMonsterTarget(command, caster) != null;

            case TargetType.Self:
                return caster != null &&
                       caster.RuntimeData != null &&
                       !caster.RuntimeData.IsDead;

            default:
                return true;
        }
    }

    private bool HasMonsterSkillCameraTarget(MonsterReservedCommand command)
    {
        return FindFirstAlivePlayerTarget(command) != null;
    }

    private BattleCharacter FindFirstAlivePlayerTarget(MonsterReservedCommand command)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CurrentHP <= 0)
                continue;

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                return character;
        }

        return null;
    }

    private MonsterUnit FindFirstAliveMonsterTarget(
        MonsterReservedCommand command,
        MonsterUnit caster)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster == caster || monster.RuntimeData == null)
                continue;

            if (monster.RuntimeData.IsDead)
                continue;

            for (int j = 0; j < monster.OccupiedGridIndices.Count; j++)
            {
                if (command.TargetGridIndices.Contains(monster.OccupiedGridIndices[j]))
                    return monster;
            }
        }

        return null;
    }

    private IEnumerator MoveTransformSmooth(
        Transform target,
        Vector3 start,
        Vector3 end,
        float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.position = end;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);

            yield return null;
        }

        target.position = end;
    }

    private Vector2Int GetMonsterMoveOffset(MonsterReservedCommand command)
    {
        if (command == null)
            return Vector2Int.zero;

        return command.EffectiveMoveOffset;
    }

    private bool IsMonsterInRange(MonsterUnit monster, PlayerReservedCommand command)
    {
        if (monster == null || command == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (command.RangeGridIndices.Contains(monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private void ConsumePlayerSkillCost(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || caster == null || caster.RuntimeData == null)
            return;

        caster.RuntimeData.ApplyReservedCosts();
    }

    private void UpdatePartyGridIndex(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null)
            return;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.SetCurrentGridIndex(i, gridIndex);
            return;
        }
    }

    private IEnumerator ExecuteMonsterDashAttack(MonsterReservedCommand command)
    {
        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null || command == null || command.SkillData == null)
            yield break;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null && monster.RuntimeData != null)
            monster.RuntimeData.Direction = facing.GetBattleDirection();

        bool facingRight = facing == null || facing.IsFacingRight;

        int dirX = facingRight ? 1 : -1;
        int maxMove = gridManager.Width;

        Vector2Int finalOffset = Vector2Int.zero;
        BattleCharacter hitPlayer = null;

        for (int step = 1; step <= maxMove; step++)
        {
            Vector2Int testOffset = new Vector2Int(dirX * step, 0);

            if (!CanMonsterDashToOffset(monster, testOffset, out BattleCharacter blockingPlayer))
                break;

            if (blockingPlayer != null)
            {
                hitPlayer = blockingPlayer;
                break;
            }

            finalOffset = testOffset;
        }

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (hitPlayer != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToAttacker(monster.transform);

        if (animator != null)
            animator.PlayMonsterSkillAction(command);

        if (finalOffset != Vector2Int.zero)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.MainGridIndex);
            Vector2Int movedCoord = currentCoord + finalOffset;
            int movedIndex = gridManager.CoordToIndex(movedCoord);

            Vector3 pos = gridManager.GetWorldPositionByIndex(movedIndex);

            if (hitPlayer != null && BattleCameraController.Instance != null)
                BattleCameraController.Instance.BeginZoomFollowTarget(monster.transform);

            yield return MoveTransformSmooth(
                monster.transform,
                monster.transform.position,
                pos,
                MoveAnimationDuration
            );

            if (hitPlayer != null && BattleCameraController.Instance != null)
                BattleCameraController.Instance.EndZoomFollowTarget();

            monster.MoveOccupiedCells(finalOffset, gridManager);
        }

        if (hitPlayer != null)
        {
            ApplyMonsterDashDamage(command, monster, hitPlayer);

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.PlayDamageImpact();

            yield return new WaitForSeconds(HitCameraDelay);

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
        }
        else
        {
            yield return new WaitForSeconds(ActionDelay);
        }

        hudService.RefreshHUDs();
    }

    private bool CanMonsterDashToOffset(
    MonsterUnit monster,
    Vector2Int moveOffset,
    out BattleCharacter blockingPlayer)
    {
        blockingPlayer = null;

        if (monster == null || gridManager == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];

            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            BattleCharacter player = FindPlayerAtGrid(targetIndex);

            if (player != null)
            {
                blockingPlayer = player;
                return true;
            }

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                return false;
        }

        return true;
    }

    private BattleCharacter FindPlayerAtGrid(int gridIndex)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (character.CurrentGridIndex == gridIndex)
                return character;
        }

        return null;
    }

    private void ApplyMonsterDashDamage(
    MonsterReservedCommand command,
    MonsterUnit monster,
    BattleCharacter target)
    {
        if (command == null || monster == null || target == null || target.RuntimeData == null)
            return;

        if (target.RuntimeData.IsDead)
            return;

        int damage = damageService.GetMonsterDamage(command);

        BattleEffectUtility.DamagePlayer(target, damage);

        BattleUnitFacing targetFacing = target.GetComponent<BattleUnitFacing>();

        if (targetFacing != null)
        {
            targetFacing.FaceByWorldTarget(monster.transform.position);

            if (target.RuntimeData != null)
                target.RuntimeData.Direction = targetFacing.GetBattleDirection();
        }

        BattleUnitAnimator hitAnimator = target.GetComponent<BattleUnitAnimator>();

        if (hitAnimator != null)
        {
            if (target.RuntimeData.CurrentHP <= 0)
                hitAnimator.PlayDead();
            else
                hitAnimator.PlayHit();
        }

        Debug.Log(
            $"[MonsterDashAttack] {monster?.RuntimeData?.Name} / " +
            $"Skill:{command?.SkillData?.SkillId} / " +
            $"Target:{target?.CharacterId} / Damage:{damage}"
        );
    }
}
