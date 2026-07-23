using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
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
    private readonly Action<PlayerReservedCommand, int> onPlayerCommandExecuted;
    private BattleGridEffectController gridEffectController;
    private readonly HashSet<string> nocturnPortalFailedRuntimeIds = new();

    private const float ActionDelay = 0.03f;
    private const float MultiHitAnimationSpeed = 1.35f;
    private const float BatchEndDelay = 0.03f;
    private const float NoInteractionPostDelay = 0.12f;

    private const float HitCameraDelay = 0.08f;
    private const float MonsterHUDVisibleDelay = 0.45f;
    private const float DefaultActionRoutineTimeout = 8f;
    private const string MuckProjectileSkillId = "S_Monster_04";
    private const string BlobMonsterId = "Mon_02";
    private const string ResidueGridEffectId = "GR_Residue";
    private static readonly Color ExecutionRangeColor = Color.red;
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
      float actionRoutineTimeout,
      Action<PlayerReservedCommand, int> onPlayerCommandExecuted = null)
    {
        this.gridManager = gridManager;
        this.useSafeSequentialExecution = useSafeSequentialExecution;
        this.actionRoutineTimeout = Mathf.Max(0.1f, actionRoutineTimeout);
        this.onPlayerCommandExecuted = onPlayerCommandExecuted;

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

        bool batchHasInteraction = BatchHasInteractionAction(batch);
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

        yield return RunPostActionPresentationRoutine(batchHasInteraction);
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

            AddPlayerActionRoutine(actionRoutines, command, batch.TimelineSlotIndex);
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

            AddPlayerActionRoutine(actionRoutines, command, batch.TimelineSlotIndex);
        }

        return actionRoutines;
    }

    private void AddPlayerActionRoutine(
        List<ActionRoutine> actionRoutines,
        PlayerReservedCommand command,
        int timelineSlotIndex)
    {
        if (actionRoutines == null || command == null)
            return;

        if (command.UserRuntime == null || command.UserRuntime.IsDead)
            return;

        if (command.ReservedMoveGridIndex >= 0)
        {
            if (IsConsumedVisualSkipMove(command))
                return;

            actionRoutines.Add(CreateActionRoutine(
                $"PlayerMove:{command.CharacterId}",
                ExecutePlayerCommandAndNotify(command, timelineSlotIndex, ExecutePlayerMove(command))));
            return;
        }

        actionRoutines.Add(CreateActionRoutine(
            $"PlayerSkill:{command.CharacterId}:{command.SkillId}",
            ExecutePlayerCommandAndNotify(command, timelineSlotIndex, ExecutePlayerSkill(command))));
    }

    private IEnumerator ExecutePlayerCommandAndNotify(
        PlayerReservedCommand command,
        int timelineSlotIndex,
        IEnumerator actionRoutine)
    {
        if (actionRoutine != null)
        {
            while (actionRoutine.MoveNext())
                yield return actionRoutine.Current;
        }

        if (command != null && command.UserRuntime != null && !command.UserRuntime.IsDead)
            onPlayerCommandExecuted?.Invoke(command, timelineSlotIndex);
    }

    private IEnumerator RunPostActionPresentationRoutine(bool hasInteraction = true)
    {
        if (hasInteraction)
        {
            yield return new WaitForSeconds(MonsterHUDVisibleDelay);

            hudService.HideUnselectedMonsterHUDs();

            yield return new WaitForSeconds(BatchEndDelay);

            hudService.PlayAllAliveIdle();
            yield break;
        }

        hudService.HideUnselectedMonsterHUDs();

        yield return new WaitForSeconds(NoInteractionPostDelay);

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

    public bool BatchHasInteractionAction(BattleActionBatch batch)
    {
        if (batch == null)
            return false;

        if (batch.PlayerCommands != null)
        {
            for (int i = 0; i < batch.PlayerCommands.Count; i++)
            {
                if (PlayerCommandHasInteraction(batch.PlayerCommands[i]))
                    return true;
            }
        }

        if (batch.MonsterCommands != null)
        {
            for (int i = 0; i < batch.MonsterCommands.Count; i++)
            {
                if (MonsterCommandHasInteraction(batch.MonsterCommands[i]))
                    return true;
            }
        }

        return false;
    }

    private bool PlayerCommandHasInteraction(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (command.ReservedMoveGridIndex >= 0)
            return false;

        if (command.SkillData.Target == TargetType.Self)
            return command.UserRuntime != null && !command.UserRuntime.IsDead;

        if (command.SkillData.Target == TargetType.PlayerParty)
            return HasPlayerPartyTarget(command);

        return HasMonsterTarget(command);
    }

    private bool MonsterCommandHasInteraction(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (command.SkillData.TimelineNotation == TimelineActionType.Move)
            return false;

        if (command.SkillData.Target == TargetType.Self)
        {
            MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);
            return monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead;
        }

        if (command.SkillData.Target == TargetType.EnemyParty)
            return FindFirstAliveMonsterTarget(command, unitFinder.FindMonsterUnit(command.RuntimeId)) != null;

        return HasPlayerTarget(command);
    }

    private bool HasPlayerPartyTarget(PlayerReservedCommand command)
    {
        if (command == null || command.RangeGridIndices == null)
            return false;

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

            if (command.RangeGridIndices.Contains(character.CurrentGridIndex))
                return true;
        }

        return false;
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
            Debug.LogError($"[BattleActionRunner] CoroutineHost ?놁쓬 / Action:{actionRoutine.Label}");
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
            Debug.LogError("[BattleActionRunner] CoroutineHost ?놁쓬");
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

    private void ShowExecutionRange(IReadOnlyCollection<int> rangeGridIndices)
    {
        if (gridManager == null)
            return;

        gridManager.ShowExecutionRange(rangeGridIndices, ExecutionRangeColor);
    }

    private void ClearExecutionRange()
    {
        if (gridManager == null)
            return;

        gridManager.ClearExecutionRange();
    }

    private List<int> BuildPlayerExecutionRange(PlayerReservedCommand command)
    {
        List<int> range = new();

        if (command == null)
            return range;

        AddUniqueRange(range, command.RangeGridIndices);

        if (range.Count <= 0 && command.ReservedMoveGridIndex >= 0)
            AddUnique(range, command.EffectiveVisualMoveGridIndex);

        if (range.Count <= 0 && command.SelectedGridIndex >= 0)
            AddUnique(range, command.SelectedGridIndex);

        return range;
    }

    private List<int> BuildPlayerExecutionRange(PlayerReservedCommand command, int excludeGridIndex)
    {
        List<int> range = BuildPlayerExecutionRange(command);
        RemoveGridIndex(range, excludeGridIndex);
        return range;
    }

    private List<int> BuildMonsterSkillExecutionRange(MonsterReservedCommand command)
    {
        List<int> range = new();

        if (command == null)
            return range;

        AddUniqueRange(range, command.RangeGridIndices);
        return range;
    }

    private List<int> BuildMonsterSkillExecutionRange(MonsterReservedCommand command, int excludeGridIndex)
    {
        List<int> range = BuildMonsterSkillExecutionRange(command);
        RemoveGridIndex(range, excludeGridIndex);
        return range;
    }

    private static void RemoveGridIndex(List<int> range, int gridIndex)
    {
        if (range == null || gridIndex < 0)
            return;

        range.RemoveAll(index => index == gridIndex);
    }

    private List<int> BuildMonsterMoveExecutionRange(
        MonsterUnit monster,
        MonsterReservedCommand command)
    {
        List<int> range = new();

        if (monster == null || command == null || gridManager == null)
            return range;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            return range;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int movedCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(movedCoord))
                continue;

            AddUnique(range, gridManager.CoordToIndex(movedCoord));
        }

        return range;
    }

    private static void AddUniqueRange(List<int> target, IReadOnlyCollection<int> source)
    {
        if (target == null || source == null)
            return;

        foreach (int index in source)
            AddUnique(target, index);
    }

    private static void AddUnique(List<int> target, int index)
    {
        if (target == null || index < 0)
            return;

        if (!target.Contains(index))
            target.Add(index);
    }

    private void TryPlaceMuckProjectileResidue(MonsterReservedCommand command)
    {
        if (command == null)
            return;

        int targetGridIndex = command.RangeOriginGridIndex;

        if (targetGridIndex < 0 && command.TargetGridIndices != null && command.TargetGridIndices.Count > 0)
            targetGridIndex = command.TargetGridIndices[0];

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || !controller.TryPlaceEffect(targetGridIndex, ResidueGridEffectId))
            return;

        // 癒명겕???ъ궗泥??쇳빐? ?붿뿬臾??쇳빐??蹂꾨룄???寃⑹쑝濡?泥섎━?⑸땲??
        // 紐⑺몴 洹몃━?쒖뿉 罹먮┃?곌? ?덈떎硫??붿뿬臾??앹꽦 吏곹썑 GR_Residue ?쇳빐瑜??곕줈 ?곸슜?⑸땲??
        BattleCharacter character = FindPlayerAtGrid(targetGridIndex);

        if (character != null)
            controller.ApplyToPlayer(targetGridIndex, character);
    }

    private void TryPlaceResidue(int gridIndex)
    {
        if (gridIndex < 0)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        controller.TryPlaceEffect(gridIndex, ResidueGridEffectId);
    }

    private bool IsGridEffectBlocked(int gridIndex)
    {
        BattleGridEffectController controller = ResolveGridEffectController();
        return controller != null && controller.IsBlocked(gridIndex);
    }

    private void ApplyGridEffectsToPlayer(
        IReadOnlyList<int> gridIndices,
        BattleCharacter character)
    {
        if (gridIndices == null || character == null || character.RuntimeData == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        for (int i = 0; i < gridIndices.Count; i++)
        {
            controller.ApplyToPlayer(gridIndices[i], character);

            if (character.RuntimeData.IsDead)
                break;
        }
    }

    private void ApplyGridEffectsToMonster(
        IReadOnlyList<int> gridIndices,
        MonsterUnit monster)
    {
        if (gridIndices == null || monster == null || monster.RuntimeData == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        for (int i = 0; i < gridIndices.Count; i++)
        {
            controller.ApplyToMonster(gridIndices[i], monster);

            if (monster.RuntimeData.IsDead)
                break;
        }
    }

    private BattleGridEffectController ResolveGridEffectController()
    {
        if (gridEffectController != null)
            return gridEffectController;

        gridEffectController = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include
        );

        return gridEffectController;
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

        ShowExecutionRange(BuildPlayerExecutionRange(command));

        try
        {
            ConsumePlayerMoveCost(command, character);

            bool useVisualMove = TryGetPlayerVisualMoveTargetGridIndex(
                command,
                currentGridIndex,
                out int targetGridIndex,
                out Vector2Int moveOffset
            );

            if (!useVisualMove)
                moveOffset = command.ExecutionMoveOffset;

            List<int> enteredGridIndices = new();

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

            bool foundMoveTarget = useVisualMove && command.VisualMoveSteps != null
                ? TryGetPlayerMoveTargetGridIndex(
                    currentGridIndex,
                    command.VisualMoveSteps,
                    command.CharacterId,
                    out targetGridIndex,
                    enteredGridIndices)
                : TryGetPlayerMoveTargetGridIndex(
                    currentGridIndex,
                    moveOffset,
                    command.CharacterId,
                    out targetGridIndex,
                    enteredGridIndices);

            if (!foundMoveTarget)
            {
                command.SetExecutedMoveDistance(0);
                ApplyBlockedPlayerMoveCostRefund(command);
                hudService.RefreshHUDs();
                Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / Offset:{moveOffset}");
                yield break;
            }

            RecordPlayerMoveExecutionDistance(command, currentGridIndex, targetGridIndex);

            Vector2Int startCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int resolvedCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int actualMoveOffset = resolvedCoord - startCoord;
            bool wasBlockedDuringMove = actualMoveOffset != moveOffset;

            if (targetGridIndex == currentGridIndex)
            {
                if (wasBlockedDuringMove)
                    ApplyCrashToPlayer(character);

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
            character.RuntimeData.SetLastMoveOffset(actualMoveOffset);
            BattleEquipmentEffectService.MarkMovedBeforeNextAttack(character.RuntimeData);
            UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
            ApplyGridEffectsToPlayer(enteredGridIndices, character);
            statusEffectService.ApplyBleedDamageToPlayerOnMove(character);

            if (wasBlockedDuringMove && character.RuntimeData != null && !character.RuntimeData.IsDead)
                ApplyCrashToPlayer(character);

            hudService.RefreshHUDs();

            yield return new WaitForSeconds(ActionDelay);
        }
        finally
        {
            ClearExecutionRange();
        }
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

            List<int> enteredGridIndices = new();

            if (!TryGetPlayerMoveTargetGridIndex(
                currentGridIndex,
                stepGroup,
                command.CharacterId,
                out int targetGridIndex,
                enteredGridIndices))
            {
                break;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int actualOffset = targetCoord - currentCoord;
            bool wasBlockedDuringStep = actualOffset != stepOffset;

            if (targetGridIndex == currentGridIndex)
            {
                if (wasBlockedDuringStep)
                    ApplyCrashToPlayer(character);

                break;
            }

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
            character.RuntimeData.SetLastMoveOffset(actualOffset);
            BattleEquipmentEffectService.MarkMovedBeforeNextAttack(character.RuntimeData);
            UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
            ApplyGridEffectsToPlayer(enteredGridIndices, character);
            currentGridIndex = targetGridIndex;
            executedDistance += GetMoveDistance(actualOffset);

            if (wasBlockedDuringStep && character.RuntimeData != null && !character.RuntimeData.IsDead)
                ApplyCrashToPlayer(character);

            if (character.RuntimeData == null || character.RuntimeData.IsDead || wasBlockedDuringStep)
                break;
        }

        command.SetExecutedMoveDistance(executedDistance);
        ApplyBlockedPlayerMoveCostRefund(command);

        if (currentGridIndex != startGridIndex)
            statusEffectService.ApplyBleedDamageToPlayerOnMove(character);

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
        out int targetGridIndex,
        List<int> enteredGridIndices = null)
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

        if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId, enteredGridIndices))
            reachedTarget = false;

        if (reachedTarget &&
            !TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId, enteredGridIndices))
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
        out int targetGridIndex,
        List<int> enteredGridIndices = null)
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

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId, enteredGridIndices))
                break;

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId, enteredGridIndices))
                break;
        }

        targetGridIndex = gridManager.CoordToIndex(currentCoord);
        return true;
    }

    private bool TryApplyPlayerMoveAxisStep(
        ref Vector2Int currentCoord,
        int amount,
        bool horizontal,
        string characterId,
        List<int> enteredGridIndices = null)
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
            {
                ApplyCrashToBlockingUnitAtGrid(gridIndex, characterId, null);
                return false;
            }

            if (IsGridEffectBlocked(gridIndex))
                return false;

            currentCoord = nextCoord;
            AddUnique(enteredGridIndices, gridIndex);
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

        ShowExecutionRange(BuildPlayerExecutionRange(command, attacker.CurrentGridIndex));

        try
        {
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

            List<int> gridEffectTargets = BuildDamageableGridEffectTargets(command);

            if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ZoomToAttacker(attacker.transform);


            if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            {
                hudService.RefreshHUDs();
                yield break;
            }

            if (hitTargets.Count <= 0 && gridEffectTargets.Count <= 0)
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
                gridEffectTargets,
                command,
                attackerAnimator);

            hudService.RefreshHUDs();

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
        }
        finally
        {
            BattleEquipmentEffectService.ClearMoveFirstAttackPowerIfAttack(
                attacker != null ? attacker.RuntimeData : command.UserRuntime,
                command.SkillData);
            ClearExecutionRange();
        }
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
            if (IsMoveSkill(command.SkillData) || command.SelectedGridIndex < 0)
                return;

            rangeGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                command.SelectedGridIndex,
                rangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetSelectionAreaResult(
                direction,
                command.SelectedGridIndex,
                rangeGridIndices
            );
        }
    }

    private static bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        return skillData.Category == Category.Move ||
               skillData.TimelineNotation == TimelineActionType.Move ||
               skillData.SkillId == "S_Move_1" ||
               skillData.SkillId == "S_Move_2";
    }

    private IEnumerator ExecutePlayerSkillEffectsToMonsters(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        List<int> gridEffectTargets,
        PlayerReservedCommand command,
        BattleUnitAnimator attackerAnimator)
    {
        if (caster == null || command == null || command.SkillData == null)
            yield break;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
        {
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries ?놁쓬 / Skill:{command.SkillData.SkillId}");

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

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);
            int value = GetPlayerEffectValue(command, entry);
            int count = GetPlayerEffectCount(command, entry);

            if (IsDamageHitEffect(effectId))
            {
                playedDamageSequence = true;

                yield return ExecutePlayerDamageHitSequence(
                    caster,
                    monsterTargets,
                    gridEffectTargets,
                    command,
                    effectId,
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
                effectId,
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
        List<int> gridEffectTargets,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count,
        BattleUnitAnimator attackerAnimator)
    {
        int hitCount = Mathf.Max(1, count);
        bool isMultiHit = hitCount > 1;

        // ?ㅻ떒 怨듦꺽? ?寃??잛닔留뚰겮 ?쒕줈 ?ㅻⅨ 怨듦꺽 紐⑥뀡??鍮좊Ⅴ寃??댁뼱???ъ깮?쒕떎.
        // 1??怨듦꺽蹂대떎 媛?紐⑥뀡???ъ깮 ?띾룄瑜??믪뿬 ?꾩껜 ?됰룞 ?쒓컙??怨쇰룄?섍쾶 湲몄뼱吏吏 ?딅룄濡??쒕떎.
        if (isMultiHit && attackerAnimator != null)
            attackerAnimator.SetPlaybackSpeed(MultiHitAnimationSpeed);

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            if (!HasAliveMonsterTarget(monsterTargets) &&
                !HasDamageableGridEffectTarget(gridEffectTargets))
            {
                BattleEquipmentEffectService.TryApplyAttackMissCharge(command.UserRuntime);

                if (isMultiHit && attackerAnimator != null)
                    attackerAnimator.RestorePlaybackSpeed();

                yield break;
            }

            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData, hitIndex);

            float hitActionDelay = isMultiHit
                ? ActionDelay / MultiHitAnimationSpeed
                : ActionDelay;
            yield return new WaitForSeconds(hitActionDelay);

            List<Transform> feedbackTargets = new();
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

                int hpBeforeHit = monster.RuntimeData != null
                    ? monster.RuntimeData.CurrentHP
                    : 0;
                int shieldBeforeHit = monster.RuntimeData != null
                    ? monster.RuntimeData.CurrentShield
                    : 0;

                ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                appliedAnyHit = true;
                AddFeedbackTarget(feedbackTargets, monster.transform);

                bool receivedDamage = monster.RuntimeData != null &&
                    (monster.RuntimeData.CurrentHP < hpBeforeHit ||
                     monster.RuntimeData.CurrentShield < shieldBeforeHit);
                bool shouldSplit = receivedDamage &&
                    statusEffectService.ApplySplitHitAndCheckTrigger(monster);

                if (shouldSplit)
                {
                    deathService.HandleMuckSplit(monster);
                    continue;
                }

                if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                    deathService.HandleMonsterDead(monster);
            }

            appliedAnyHit |= ApplyPlayerDamageHitToGridEffects(
                gridEffectTargets,
                Mathf.Max(0, value));

            hudService.RefreshHUDs();

            if (!appliedAnyHit)
            {
                BattleEquipmentEffectService.TryApplyAttackMissCharge(command.UserRuntime);

                if (isMultiHit && attackerAnimator != null)
                    attackerAnimator.RestorePlaybackSpeed();

                yield break;
            }

            yield return PlayDamageHitFeedback(
                caster != null ? caster.transform : null,
                feedbackTargets,
                command.Direction);

            if (hitIndex >= hitCount - 1)
                yield return new WaitForSeconds(HitCameraDelay);
        }

        if (isMultiHit && attackerAnimator != null)
            attackerAnimator.RestorePlaybackSpeed();
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
            PlayerCommand = command,

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

    private List<int> BuildDamageableGridEffectTargets(PlayerReservedCommand command)
    {
        List<int> targets = new();

        if (command == null || !HasPlayerDamageHitEffect(command))
            return targets;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || command.RangeGridIndices == null)
            return targets;

        for (int i = 0; i < command.RangeGridIndices.Count; i++)
        {
            int gridIndex = command.RangeGridIndices[i];

            if (!controller.HasDamageableEffect(gridIndex))
                continue;

            AddUnique(targets, gridIndex);
        }

        return targets;
    }

    private bool HasPlayerDamageHitEffect(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (command.SkillData.EffectEntries != null && command.SkillData.EffectEntries.Count > 0)
        {
            for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
            {
                SkillEffectEntry entry = command.SkillData.EffectEntries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                    continue;

                string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                    command.UserRuntime,
                    command,
                    entry.EffectId);

                if (IsDamageHitEffect(effectId))
                    return true;
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return false;

        string[] effectIds = command.SkillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (IsDamageHitEffect(effectIds[i].Trim()))
                return true;
        }

        return false;
    }

    private bool HasDamageableGridEffectTarget(List<int> gridEffectTargets)
    {
        if (gridEffectTargets == null || gridEffectTargets.Count <= 0)
            return false;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return false;

        for (int i = gridEffectTargets.Count - 1; i >= 0; i--)
        {
            int gridIndex = gridEffectTargets[i];

            if (controller.HasDamageableEffect(gridIndex))
                return true;

            gridEffectTargets.RemoveAt(i);
        }

        return false;
    }

    private bool ApplyPlayerDamageHitToGridEffects(
        List<int> gridEffectTargets,
        int damage)
    {
        if (gridEffectTargets == null || gridEffectTargets.Count <= 0 || damage <= 0)
            return false;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return false;

        bool applied = false;

        for (int i = gridEffectTargets.Count - 1; i >= 0; i--)
        {
            int gridIndex = gridEffectTargets[i];

            if (!controller.TryDamageEffect(gridIndex, damage, out bool destroyed))
            {
                gridEffectTargets.RemoveAt(i);
                continue;
            }

            applied = true;

            if (destroyed)
                gridEffectTargets.RemoveAt(i);
        }

        return applied;
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
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries ?놁쓬 / Skill:{command.SkillData.SkillId}");
            return;
        }

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);
            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                MonsterTarget = monsterTarget,
                PlayerSkillData = command.SkillData,
                PlayerCommand = command,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = effectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry)
            };

            if (IsDamageHitEffect(effectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (monsterTarget.RuntimeData.IsDead)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
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

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);
            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                PlayerTarget = playerTarget,
                PlayerSkillData = command.SkillData,
                PlayerCommand = command,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = effectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry)
            };

            if (IsDamageHitEffect(effectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (playerTarget.RuntimeData.CurrentHP <= 0)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
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
                $"[PlayerSkillEffect] Effect ?ㅽ뻾 以??먮윭 / " +
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

        // ?ы깉 ?대룞? ?ㅽ궗 ?곗씠?곗쓽 ??꾨씪???쒓린? 愿怨꾩뾾???대룞 泥섎━濡?蹂대깄?덈떎.
        if (IsNocturnPortalMove(command))
        {
            yield return ExecuteMonsterMove(command);
        }
        else if (command.SkillData.TimelineNotation == TimelineActionType.Move)
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

        // ?ы깉 ?대룞? ?덉빟???덈? 紐⑹쟻吏瑜??ъ슜?????덉쑝誘濡??쇰컲 ?대룞??0 ?ㅽ봽??寃?щ낫??癒쇱? 泥섎━?⑸땲??
        if (IsNocturnPortalMove(command))
        {
            ShowExecutionRange(BuildMonsterMoveExecutionRange(monster, command));

            try
            {
                yield return ExecuteNocturnPortalMove(monster, command, moveOffset);
            }
            finally
            {
                ClearExecutionRange();
            }

            yield break;
        }

        if (moveOffset == Vector2Int.zero)
            yield break;

        ShowExecutionRange(BuildMonsterMoveExecutionRange(monster, command));

        try
        {

            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

            if (facing != null)
                facing.FaceByMoveOffset(moveOffset);

            // 泥??대룞 移몄씠 ?대? ?ㅻⅨ ?좊떅?먭쾶 ?먯쑀?섏뼱 ?덈떎硫???移몃룄 ?대룞?섏? ?딄퀬 利됱떆 異⑸룎?⑸땲??
            // ?대룞?먯? ?대떦 移몄쓣 ?먯쑀???좊떅 紐⑤몢 異⑸룎 怨좎젙 ?쇳빐瑜?諛쏆뒿?덈떎.
            if (TryHandleImmediateMonsterUnitCollision(monster, moveOffset))
            {
                hudService.RefreshHUDs();
                yield return new WaitForSeconds(ActionDelay);
                yield break;
            }

            MonsterMoveResolution moveResolution = ResolveMonsterMove(monster, moveOffset);

            if (moveResolution.ActualOffset == Vector2Int.zero)
            {
                if (moveResolution.WasBlocked)
                    ApplyCrashToMonster(monster);

                hudService.RefreshHUDs();
                yield return new WaitForSeconds(ActionDelay);
                yield break;
            }

            List<int> enteredGridIndices = moveResolution.EnteredGridIndices;

            Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int movedMainCoord = mainCoord + moveResolution.ActualOffset;
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

            monster.MoveOccupiedCells(moveResolution.ActualOffset, gridManager);

            // 釉붾∼? ?대룞???꾨즺?????대룞 ??洹몃━?쒖뿉 ?붿뿬臾쇱쓣 ?④퉩?덈떎.
            // ?붿뿬臾쇱쓽 ?쇳빐???대룞 怨듦꺽怨?蹂꾧컻濡?GridEffect ?곗씠?곗뿉 ?곕씪 ?곸슜?⑸땲??
            if (monster.RuntimeData != null &&
                string.Equals(monster.RuntimeData.MonsterId, BlobMonsterId, System.StringComparison.Ordinal))
            {
                TryPlaceResidue(currentGridIndex);
            }

            ApplyGridEffectsToMonster(enteredGridIndices, monster);
            statusEffectService.ApplyBleedDamageToMonsterOnMove(monster);

            if (moveResolution.WasBlocked && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                ApplyCrashToMonster(monster);

            hudService.RefreshHUDs();

            yield return new WaitForSeconds(ActionDelay);
        }
        finally
        {
            ClearExecutionRange();
        }
    }



    private static void HideNocturnPortalDestinationIndicator(MonsterReservedCommand command)
    {
        if (command == null || command.RangeOriginGridIndex < 0)
            return;

        PlayerSkillReservationController reservationController =
            Object.FindFirstObjectByType<PlayerSkillReservationController>(
                FindObjectsInactive.Include);

        if (reservationController != null)
        {
            reservationController.HideNocturnPortalDestinationIndicator(
                command.RuntimeId,
                command.RangeOriginGridIndex);
        }
    }

    private void MarkNocturnPortalFailed(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return;

        nocturnPortalFailedRuntimeIds.Add(command.RuntimeId);
    }

    private void ClearNocturnPortalFailed(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return;

        nocturnPortalFailedRuntimeIds.Remove(command.RuntimeId);
    }

    private bool ConsumeNocturnPortalFailure(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return false;

        return nocturnPortalFailedRuntimeIds.Remove(command.RuntimeId);
    }

    private static bool IsNocturnPortalMove(MonsterReservedCommand command)
    {
        return command != null && command.IsPortalMove;
    }

    private IEnumerator ExecuteNocturnPortalMove(
        MonsterUnit monster,
        MonsterReservedCommand command,
        Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            yield break;

        // ?ы깉? ?덉빟???덈? 紐⑹쟻吏瑜??곗꽑 ?ъ슜?⑸땲??
        // ?욎꽑 ?됰룞?쇰줈 ?꾩옱 ?꾩튂媛 ?щ씪吏硫??덉빟 ?뱀떆???곷? ?ㅽ봽?뗭씠 0?????섎룄 ?덉쑝誘濡?
        // ?덈? 紐⑹쟻吏媛 ?덈뒗 紐낅졊? MoveOffset??0?댁뼱??痍⑥냼?섏? ?딆뒿?덈떎.
        bool hasReservedDestination = command != null && command.RangeOriginGridIndex >= 0;

        if (!hasReservedDestination && moveOffset == Vector2Int.zero)
            yield break;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        // ?덉빟 ?뱀떆???덈? 紐⑹쟻吏媛 ?덉쑝硫?洹멸쾬???곗꽑 ?ъ슜?⑸땲??
        // ?욎꽑 ?대룞?대굹 洹몃옪 ?뚮Ц???뱁꽩???꾩옱 ?꾩튂媛 ?щ씪?몃룄 ?ы깉 紐⑹쟻吏媛 ??댁?吏 ?딆뒿?덈떎.
        int destinationGridIndex = hasReservedDestination
            ? command.RangeOriginGridIndex
            : -1;

        Vector2Int destinationCoord;

        if (destinationGridIndex >= 0)
        {
            destinationCoord = gridManager.IndexToCoord(destinationGridIndex);
            moveOffset = destinationCoord - currentCoord;
        }
        else
        {
            destinationCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(destinationCoord))
                yield break;

            destinationGridIndex = gridManager.CoordToIndex(destinationCoord);
        }

        // ?ㅽ뻾 吏곸쟾源뚯? 紐⑹쟻吏 移몄씠 鍮꾩뼱 ?덉뼱???⑸땲??
        // 罹먮┃?곕굹 ?ㅻⅨ 紐ъ뒪?곌? ?대떦 移몄쓣 李⑥??덈떎硫??ы깉 ?대룞??痍⑥냼?⑸땲??
        if (BattleOccupancyService.IsOccupiedByAnyUnit(destinationGridIndex, null, monster))
        {
            MarkNocturnPortalFailed(command);
            HideNocturnPortalDestinationIndicator(command);
            yield break;
        }

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null && gridEffectController.IsBlocked(destinationGridIndex))
        {
            MarkNocturnPortalFailed(command);
            HideNocturnPortalDestinationIndicator(command);
            yield break;
        }

        // 紐⑹쟻吏 寃?щ? ?듦낵?덉쑝誘濡??대쾲 ?ы깉? ?깃났 ?곹깭濡?湲곕줉?⑸땲??
        ClearNocturnPortalFailed(command);

        // ?대룞 ?곗텧???쒖옉?섎뒗 ?쒓컙 紐⑹쟻吏 ?덇퀬 ?대?吏瑜??쒓굅?⑸땲??
        HideNocturnPortalDestinationIndicator(command);

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
            facing.FaceByMoveOffset(moveOffset);

        Vector3 destinationPosition = gridManager.GetWorldPositionByIndex(destinationGridIndex);

        // ?ы깉 ?대룞???대룞 ?곹깭?꾩쓣 蹂댁뿬二쇨린 ?꾪빐 Move ?좊땲硫붿씠?섏쓣 ?ъ깮?⑸땲??
        // ?꾩튂 蹂닿컙? ?ъ슜?섏? ?딄퀬 紐⑹쟻吏?먮뒗 利됱떆 ?섑??⑸땲??
        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            animator.PlayMove();
            yield return new WaitForSeconds(MoveAnimationDuration);
        }

        // ?쇰━ 洹몃━?쒖? ?붾㈃ ?꾩튂瑜??④퍡 媛깆떊???ㅼ젣 ?먯쑀 ?꾩튂???꾨갑 移몄쑝濡??대룞?쒗궢?덈떎.
        monster.MoveOccupiedCells(moveOffset, gridManager);
        monster.transform.position = destinationPosition;

        if (animator != null)
        {
            animator.PlayMoveReverse();
            yield return new WaitForSeconds(MoveAnimationDuration);
            animator.RestorePlaybackSpeed();
        }

        ApplyGridEffectsToMonster(new List<int> { destinationGridIndex }, monster);
        statusEffectService.ApplyBleedDamageToMonsterOnMove(monster);
        hudService.RefreshHUDs();
        yield return new WaitForSeconds(ActionDelay);
    }

    private sealed class MonsterMoveResolution
    {
        public Vector2Int ActualOffset;
        public bool WasBlocked;
        public List<int> EnteredGridIndices = new();
    }

    private MonsterMoveResolution ResolveMonsterMove(MonsterUnit monster, Vector2Int requestedOffset)
    {
        if (monster == null || gridManager == null || requestedOffset == Vector2Int.zero)
            return new MonsterMoveResolution();

        if (requestedOffset.x != 0 && requestedOffset.y != 0)
        {
            MonsterMoveResolution horizontalFirst =
                ResolveMonsterMoveAxisOrder(monster, requestedOffset, true, false);
            MonsterMoveResolution verticalFirst =
                ResolveMonsterMoveAxisOrder(monster, requestedOffset, false, false);

            bool useVerticalFirst =
                GetMoveDistance(verticalFirst.ActualOffset) >
                GetMoveDistance(horizontalFirst.ActualOffset);

            return ResolveMonsterMoveAxisOrder(
                monster,
                requestedOffset,
                !useVerticalFirst,
                true);
        }

        return ResolveMonsterMoveAxisOrder(
            monster,
            requestedOffset,
            requestedOffset.x != 0,
            true);
    }

    private MonsterMoveResolution ResolveMonsterMoveAxisOrder(
        MonsterUnit monster,
        Vector2Int requestedOffset,
        bool horizontalFirst,
        bool applyCrashToBlockingUnit)
    {
        MonsterMoveResolution result = new();
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        Vector2Int startMainCoord = gridManager.IndexToCoord(monster.MainGridIndex);
        bool completed;

        if (horizontalFirst)
        {
            completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.x, true, monster, result.EnteredGridIndices, applyCrashToBlockingUnit);
            if (completed)
                completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.y, false, monster, result.EnteredGridIndices, applyCrashToBlockingUnit);
        }
        else
        {
            completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.y, false, monster, result.EnteredGridIndices, applyCrashToBlockingUnit);
            if (completed)
                completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.x, true, monster, result.EnteredGridIndices, applyCrashToBlockingUnit);
        }

        if (currentCoords.Count > 0)
            result.ActualOffset = currentCoords[0] - startMainCoord;

        result.WasBlocked = !completed || result.ActualOffset != requestedOffset;
        return result;
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
        MonsterUnit monster,
        List<int> enteredGridIndices = null,
        bool applyCrashToBlockingUnit = false)
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
                {
                    if (applyCrashToBlockingUnit)
                        ApplyCrashToBlockingUnitAtGrid(targetIndex, null, monster);

                    return false;
                }

                if (IsGridEffectBlocked(targetIndex))
                    return false;

                nextCoords.Add(nextCoord);
                AddUnique(enteredGridIndices, targetIndex);
            }

            currentCoords.Clear();
            currentCoords.AddRange(nextCoords);
            remaining -= step;
        }

        return true;
    }

    private List<int> BuildMonsterEnteredGridIndices(MonsterUnit monster, Vector2Int moveOffset)
    {
        List<int> enteredGridIndices = new();

        if (monster == null || gridManager == null || moveOffset == Vector2Int.zero)
            return enteredGridIndices;

        if (moveOffset.x != 0 && moveOffset.y != 0)
        {
            if (TryBuildMonsterMovePath(monster, moveOffset, true, enteredGridIndices))
                return enteredGridIndices;

            enteredGridIndices.Clear();

            if (TryBuildMonsterMovePath(monster, moveOffset, false, enteredGridIndices))
                return enteredGridIndices;

            enteredGridIndices.Clear();
            return enteredGridIndices;
        }

        TryBuildMonsterMovePath(monster, moveOffset, moveOffset.x != 0, enteredGridIndices);
        return enteredGridIndices;
    }

    private bool TryBuildMonsterMovePath(
        MonsterUnit monster,
        Vector2Int moveOffset,
        bool horizontalFirst,
        List<int> enteredGridIndices)
    {
        if (monster == null || gridManager == null)
            return false;

        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        if (horizontalFirst)
        {
            return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster, enteredGridIndices) &&
                   TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster, enteredGridIndices);
        }

        return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster, enteredGridIndices) &&
               TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster, enteredGridIndices);
    }

    private bool TryHandleImmediateMonsterUnitCollision(
        MonsterUnit monster,
        Vector2Int requestedOffset)
    {
        if (monster == null || gridManager == null || requestedOffset == Vector2Int.zero)
            return false;

        Vector2Int firstStep;

        if (requestedOffset.x != 0)
            firstStep = new Vector2Int(requestedOffset.x > 0 ? 1 : -1, 0);
        else
            firstStep = new Vector2Int(0, requestedOffset.y > 0 ? 1 : -1);

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int occupiedCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int nextCoord = occupiedCoord + firstStep;

            // 留??멸낸? 異⑸룎 ?쇳빐 ??곸씠 ?꾨떃?덈떎.
            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int nextGridIndex = gridManager.CoordToIndex(nextCoord);

            if (!BattleOccupancyService.IsOccupiedByAnyUnit(nextGridIndex, null, monster))
                continue;

            ApplyCrashToMonster(monster);
            ApplyCrashToBlockingUnitAtGrid(nextGridIndex, null, monster);
            return true;
        }

        return false;
    }

    private void ApplyCrashToBlockingUnitAtGrid(
        int gridIndex,
        string movingCharacterId,
        MonsterUnit movingMonster)
    {
        BattleCharacter movingCharacter = !string.IsNullOrWhiteSpace(movingCharacterId)
            ? unitFinder.FindBattleCharacter(movingCharacterId)
            : null;

        if (BattleOccupancyService.TryGetCharacterAtGrid(
                gridIndex,
                out BattleCharacter blockingCharacter,
                movingCharacterId))
        {
            ApplyCrashToPlayer(blockingCharacter);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingCharacter,
                blockingCharacter,
                null);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingCharacter,
                movingCharacter,
                movingMonster);
            return;
        }

        if (BattleOccupancyService.TryGetMonsterAtGrid(
                gridIndex,
                out MonsterUnit blockingMonster,
                movingMonster))
        {
            ApplyCrashToMonster(blockingMonster);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingCharacter,
                null,
                blockingMonster);
        }
    }

    private void ApplyCrashToPlayer(BattleCharacter target)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = 2,
            Count = 1
        });
    }

    private void ApplyCrashToMonster(MonsterUnit target)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            MonsterTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = 2,
            Count = 1
        });
    }

    private IEnumerator ExecuteMonsterSkill(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            yield break;

        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        // 吏곸쟾 ?ы깉???먯쑀 ?먮뒗 ?대룞 遺덇?濡?痍⑥냼?먮떎硫??덉빟???ы깉 ?꾩튂瑜?怨듦꺽 ?먯젏?쇰줈 ?ъ슜?섏? ?딆뒿?덈떎.
        // ?뱁꽩???ㅼ젣 ?꾩옱 ?꾩튂瑜??먯젏?쇰줈 諛붽씀怨? ?꾩옱 ?꾩튂?먯꽌 ?ㅼ젣 ??곸쓣 ?ν빐 諛⑺뼢???ㅼ떆 ?뺥빀?덈떎.
        if (ConsumeNocturnPortalFailure(command))
        {
            command.SetRangeOriginGridIndex(monster.MainGridIndex);
            command.ClearForcedDirection();
        }

        if (command.SkillData.SkillId == "S_Monster_07")
        {
            ShowExecutionRange(BuildMonsterSkillExecutionRange(command, monster.MainGridIndex));

            try
            {
                yield return ExecuteMonsterDashAttack(command);
            }
            finally
            {
                ClearExecutionRange();
            }

            yield break;
        }

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        // AI媛 怨듦꺽 諛⑺뼢??吏?뺥븳 寃쎌슦 ?덉빟??諛⑺뼢???ㅽ뻾 ?쒖젏?먮룄 洹몃?濡??ъ슜?⑸땲??
        // 踰붿쐞???ㅻⅨ履쎌씤???좊땲硫붿씠?섎쭔 ?쇱そ???ν븯???꾩긽??諛⑹??⑸땲??
        if (command.HasForcedDirection)
        {
            if (facing != null)
                facing.FaceRight(command.ForcedDirection == BattleDirection.Right);

            if (monster.RuntimeData != null)
                monster.RuntimeData.Direction = command.ForcedDirection;
        }

        // ?뺤젙??諛⑺뼢??湲곗??쇰줈 怨듦꺽 踰붿쐞瑜?怨꾩궛?⑸땲??
        RecalculateMonsterSkillRangeAtExecution(monster, command);

        BattleCharacter firstPlayerTarget = FindFirstPlayerTarget(command);

        BattleUnitAnimator monsterAnimator = monster.GetComponent<BattleUnitAnimator>();

        // 媛뺤젣 諛⑺뼢???녿뒗 ?쇰컲 AI ?됰룞留??ㅼ젣 紐낆쨷 ???履쎌쑝濡??뚯쟾?⑸땲??
        // ?ы깉 ?꾩냽 怨듦꺽泥섎읆 諛⑺뼢???덉빟???됰룞? ?꾩뿉??吏?뺥븳 諛⑺뼢???좎??⑸땲??
        if (!command.HasForcedDirection && firstPlayerTarget != null && facing != null)
        {
            facing.FaceByWorldTarget(firstPlayerTarget.transform.position);

            if (monster.RuntimeData != null)
                monster.RuntimeData.Direction = facing.GetBattleDirection();

            RecalculateMonsterSkillRangeAtExecution(monster, command);
            firstPlayerTarget = FindFirstPlayerTarget(command);
        }

        ShowExecutionRange(BuildMonsterSkillExecutionRange(command, monster.MainGridIndex));

        try
        {
            if (firstPlayerTarget != null && BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ZoomToAttacker(monster.transform);


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

                yield return PlayMonsterProjectileVfxIfNeeded(monster, command, monsterAnimator);

                monsterSkillEffectService.ApplyMonsterSkill(monster, command);

                if (firstPlayerTarget != null)
                    yield return new WaitForSeconds(HitCameraDelay);
                else
                    yield return new WaitForSeconds(ActionDelay);
            }

            // 癒명겕???ъ궗泥대뒗 紐낆쨷 ?щ?? 愿怨꾩뾾???덉빟??紐⑺몴 洹몃━?쒖뿉 ?붿뿬臾쇱쓣 ?앹꽦?⑸땲??
            // ?ъ궗泥??쇳빐? ?붿뿬臾??쇳빐??遺꾨━?섎ŉ, ?앹꽦 ?쒓컙?먮뒗 ?붿뿬臾??쇳빐瑜?利됱떆 ?곸슜?섏? ?딆뒿?덈떎.
            if (string.Equals(command.SkillData.SkillId, MuckProjectileSkillId, System.StringComparison.Ordinal))
                TryPlaceMuckProjectileResidue(command);

            // ?좊뜑???먰룺? 怨듦꺽 ?④낵 ?곸슜???앸궃 ???먯떊???쒓굅?⑸땲??
            // ?쇰컲 泥섏튂媛 ?꾨땲誘濡??섎꼳?? 怨좎쑀 ?꾩씠?? ?좊Ъ ?깆쓽 ?щ쭩 蹂댁긽? ?섏쭛?섏? ?딆뒿?덈떎.
            if (monster.RuntimeData != null &&
                monster.RuntimeData.MonsterId == "Mon_06" &&
                command.SkillData.SkillId == "S_Monster_14")
            {
                monster.RuntimeData.IsExplodeReady = false;
                monster.RuntimeData.CurrentHP = 0;
                deathService.HandleMonsterDeadWithoutReward(monster);
            }

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
        }
        finally
        {
            ClearExecutionRange();
        }
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
            bool hasAliveTarget = HasAliveMonsterSkillTarget(monster, command);
            bool canPlayProjectileVfx = ShouldPlayMonsterProjectileVfx(monster, command, monsterAnimator);

            // 紐낆쨷 ??곸씠 ?놁뼱??紐ъ뒪?곗쓽 怨듦꺽 紐⑥뀡? 諛섎뱶???ъ깮?⑸땲??
            // ???遺?щ뒗 ?쇳빐 ?곸슜留?嫄대꼫?곕ŉ, 怨듦꺽 ?쒕룄 ?먯껜瑜?痍⑥냼?섏? ?딆뒿?덈떎.
            if (monsterAnimator != null)
                monsterAnimator.PlayMonsterSkillAction(command);

            if (canPlayProjectileVfx)
                yield return PlayMonsterProjectileVfxIfNeeded(monster, command, monsterAnimator);
            else
                yield return new WaitForSeconds(ActionDelay);

            if (!hasAliveTarget)
            {
                yield return new WaitForSeconds(ActionDelay);
                yield break;
            }

            bool hadCameraTarget = HasMonsterSkillCameraTarget(command);
            List<Transform> feedbackTargets = BuildMonsterSkillFeedbackTargets(monster, command);

            monsterSkillEffectService.ApplyMonsterSkillDamageHit(monster, command, hitIndex);

            if (hadCameraTarget)
            {
                yield return PlayDamageHitFeedback(
                    monster != null ? monster.transform : null,
                    feedbackTargets,
                    GetMonsterImpactFallbackDirection(monster));
            }

            yield return new WaitForSeconds(HitCameraDelay);
        }
    }

    private void RecalculateMonsterSkillRangeAtExecution(
    MonsterUnit monster,
    MonsterReservedCommand command)
    {
        if (monster == null || command == null || command.SkillData == null)
            return;

        if (command.HasExplicitRangeResult)
            return;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        bool facingRight = command.HasForcedDirection
            ? command.ForcedDirection == BattleDirection.Right
            : command.RangeOriginGridIndex >= 0
                ? IsNearestPlayerToRight(command.RangeOriginGridIndex)
                : facing == null || facing.IsFacingRight;

        List<int> rangeGridIndices =
            MonsterSkillRangeService.BuildRangeGridIndices(
                monster,
                command.SkillData,
                gridManager,
                facingRight,
                command.RangeOriginGridIndex
            );

        List<int> targetGridIndices =
            MonsterSkillRangeService.FilterTargetGridIndices(
                command.SkillData,
                rangeGridIndices
            );

        command.SetRangeResult(rangeGridIndices, targetGridIndices);
    }

    private bool IsNearestPlayerToRight(int originGridIndex)
    {
        if (gridManager == null || originGridIndex < 0)
            return false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
        BattleCharacter nearest = null;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (character.CurrentGridIndex < 0)
                continue;

            Vector2Int targetCoord = gridManager.IndexToCoord(character.CurrentGridIndex);
            int distance =
                Mathf.Abs(targetCoord.x - originCoord.x) +
                Mathf.Abs(targetCoord.y - originCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = character;
            }
        }

        if (nearest == null)
            return false;

        return gridManager.IndexToCoord(nearest.CurrentGridIndex).x >= originCoord.x;
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

    private IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        BattleDirection fallbackDirection)
    {
        yield return PlayDamageHitFeedback(
            attacker,
            targets,
            BattleDirectionToHorizontal(fallbackDirection));
    }

    private IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection)
    {
        yield return BattleHitImpactFeedback.PlayDamageHitFeedback(
            attacker,
            targets,
            fallbackHorizontalDirection);
    }

    private static int BattleDirectionToHorizontal(BattleDirection direction)
    {
        return direction == BattleDirection.Left ? -1 : 1;
    }

    private static void AddFeedbackTarget(List<Transform> targets, Transform target)
    {
        if (targets == null || target == null)
            return;

        if (!targets.Contains(target))
            targets.Add(target);
    }

    private int GetMonsterImpactFallbackDirection(MonsterUnit monster)
    {
        if (monster == null)
            return 1;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
            return facing.IsFacingRight ? 1 : -1;

        if (monster.RuntimeData != null)
            return BattleDirectionToHorizontal(monster.RuntimeData.Direction);

        return 1;
    }

    private List<Transform> BuildMonsterSkillFeedbackTargets(
        MonsterUnit caster,
        MonsterReservedCommand command)
    {
        List<Transform> targets = new();

        if (command == null || command.SkillData == null)
            return targets;

        switch (command.SkillData.Target)
        {
            case TargetType.PlayerParty:
                AddPlayerFeedbackTargets(command, targets);
                break;

            case TargetType.EnemyParty:
                AddMonsterFeedbackTargets(caster, command, targets);
                break;

            case TargetType.Self:
                AddFeedbackTarget(targets, caster != null ? caster.transform : null);
                break;
        }

        return targets;
    }

    private void AddPlayerFeedbackTargets(MonsterReservedCommand command, List<Transform> targets)
    {
        if (command == null || command.TargetGridIndices == null)
            return;

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
                AddFeedbackTarget(targets, character.transform);
        }
    }

    private void AddMonsterFeedbackTargets(
        MonsterUnit caster,
        MonsterReservedCommand command,
        List<Transform> targets)
    {
        if (command == null || command.TargetGridIndices == null)
            return;

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

            if (IsMonsterInTargetGridIndices(monster, command.TargetGridIndices))
                AddFeedbackTarget(targets, monster.transform);
        }
    }

    private static bool IsMonsterInTargetGridIndices(
        MonsterUnit monster,
        IReadOnlyCollection<int> targetGridIndices)
    {
        if (monster == null || targetGridIndices == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (ContainsGridIndex(targetGridIndices, monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private static bool ContainsGridIndex(IReadOnlyCollection<int> gridIndices, int gridIndex)
    {
        if (gridIndices == null)
            return false;

        foreach (int candidate in gridIndices)
        {
            if (candidate == gridIndex)
                return true;
        }

        return false;
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

    private bool ShouldPlayMonsterProjectileVfx(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator animator)
    {
        return monster != null &&
               command != null &&
               animator != null &&
               animator.HasMonsterProjectileVfx(command) &&
               TryGetMonsterProjectileTargetPosition(command, out _);
    }

    private IEnumerator PlayMonsterProjectileVfxIfNeeded(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator animator)
    {
        if (!ShouldPlayMonsterProjectileVfx(monster, command, animator))
            yield break;

        if (!TryGetMonsterProjectileTargetPosition(command, out Vector3 targetPosition))
            yield break;

        yield return animator.PlayMonsterProjectileVfx(command, targetPosition);
    }

    private bool TryGetMonsterProjectileTargetPosition(
        MonsterReservedCommand command,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (gridManager == null || command == null)
            return false;

        if (TryGetGroundTargetedProjectilePosition(command, out targetPosition))
            return true;

        BattleCharacter target = FindFirstAlivePlayerTarget(command);

        if (target == null)
            target = FindFirstPlayerTarget(command);

        if (target != null && target.CurrentGridIndex >= 0)
        {
            targetPosition = gridManager.GetWorldPositionByIndex(target.CurrentGridIndex);
            return true;
        }

        if (command.TargetGridIndices == null || command.TargetGridIndices.Count <= 0)
            return false;

        targetPosition = gridManager.GetWorldPositionByIndex(command.TargetGridIndices[0]);
        return true;
    }

    private bool TryGetGroundTargetedProjectilePosition(
        MonsterReservedCommand command,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (!ShouldUseRangeOriginForProjectileTarget(command))
            return false;

        Vector2Int originCoord = gridManager.IndexToCoord(command.RangeOriginGridIndex);

        if (!gridManager.IsValidCoord(originCoord))
            return false;

        targetPosition = gridManager.GetWorldPositionByIndex(command.RangeOriginGridIndex);
        return true;
    }

    private static bool ShouldUseRangeOriginForProjectileTarget(MonsterReservedCommand command)
    {
        return command != null &&
               command.RangeOriginGridIndex >= 0 &&
               string.Equals(
                   command.SkillId,
                   MuckProjectileSkillId,
                   System.StringComparison.Ordinal);
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

        if (facing != null && command.HasForcedDirection)
            facing.FaceRight(command.ForcedDirection == BattleDirection.Right);

        if (facing != null && monster.RuntimeData != null)
            monster.RuntimeData.Direction = facing.GetBattleDirection();

        bool facingRight = command.HasForcedDirection
            ? command.ForcedDirection == BattleDirection.Right
            : facing == null || facing.IsFacingRight;

        int dirX = facingRight ? 1 : -1;
        int maxMove = gridManager.Width;

        Vector2Int finalOffset = Vector2Int.zero;
        BattleCharacter hitPlayer = null;
        bool wasBlockedByCollision = false;

        for (int step = 1; step <= maxMove; step++)
        {
            Vector2Int testOffset = new Vector2Int(dirX * step, 0);

            if (IsMonsterDashOutsideGrid(monster, testOffset))
            {
                // 洹몃━???멸낸? 異⑸룎 ??곸씠 ?꾨땲誘濡? 留?諛뽰쑝濡??섍?吏 ?딄퀬 硫덉텛湲곕쭔 ?⑸땲??
                break;
            }

            if (!CanMonsterDashToOffset(monster, testOffset, out BattleCharacter blockingPlayer))
            {
                // ?μ븷臾쇱씠???ㅻⅨ ?좊떅??留됲엺 寃쎌슦?먮쭔 異⑸룎濡?泥섎━?⑸땲??
                wasBlockedByCollision = true;
                break;
            }

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
            List<int> enteredGridIndices = BuildMonsterEnteredGridIndices(monster, finalOffset);
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
            ApplyGridEffectsToMonster(enteredGridIndices, monster);
        }

        if (hitPlayer != null)
        {
            ApplyMonsterDashDamage(command, monster, hitPlayer);
            ApplyMonsterDashKnockback(command, monster, hitPlayer);

            yield return PlayDamageHitFeedback(
                monster != null ? monster.transform : null,
                new List<Transform> { hitPlayer.transform },
                GetMonsterImpactFallbackDirection(monster));

            yield return new WaitForSeconds(HitCameraDelay);

            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
        }
        else
        {
            if (wasBlockedByCollision && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                ApplyCrashToMonster(monster);

            yield return new WaitForSeconds(ActionDelay);
        }

        hudService.RefreshHUDs();
    }


    private bool IsMonsterDashOutsideGrid(MonsterUnit monster, Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            return true;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                return true;
        }

        return false;
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

            if (IsGridEffectBlocked(targetIndex))
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


    private void ApplyMonsterDashKnockback(
        MonsterReservedCommand command,
        MonsterUnit monster,
        BattleCharacter target)
    {
        if (command == null || command.SkillData == null || monster == null || target == null)
            return;

        if (target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        if (!TryGetMonsterSkillEffectValue(command.SkillData, "E_Knockback", out int knockbackValue))
            return;

        Vector2Int monsterCoord = gridManager.IndexToCoord(monster.MainGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);
        BattleDirection knockbackDirection = targetCoord.x < monsterCoord.x
            ? BattleDirection.Left
            : BattleDirection.Right;

        new KnockbackEffect().Execute(new BattleEffectContext
        {
            MonsterCaster = monster,
            PlayerTarget = target,
            Direction = knockbackDirection,
            GridManager = gridManager,
            EffectId = "E_Knockback",
            Value = Mathf.Max(1, knockbackValue),
            Count = 1
        });
    }

    private static bool TryGetMonsterSkillEffectValue(
        Relic.Gameplay.Data.MonsterSkillData skillData,
        string targetEffectId,
        out int value)
    {
        value = 0;

        if (skillData == null || string.IsNullOrWhiteSpace(skillData.EffectIds))
            return false;

        string[] effectIds = skillData.EffectIds.Split(';');
        string[] valueRates = string.IsNullOrWhiteSpace(skillData.ValueRate)
            ? System.Array.Empty<string>()
            : skillData.ValueRate.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (!string.Equals(
                    effectIds[i].Trim(),
                    targetEffectId,
                    System.StringComparison.Ordinal))
            {
                continue;
            }

            if (i < valueRates.Length && int.TryParse(valueRates[i].Trim(), out int parsed))
                value = parsed;
            else
                value = 1;

            return true;
        }

        return false;
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

        int damage = BattleDamageService.CalculateFinalMonsterDamageToPlayer(
            command,
            monster,
            target,
            damageService.GetMonsterDamage(command));

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
