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

    private const float ReadyDelay = 0.06f;
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

        List<ActionRoutine> actionRoutines = new();

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (command == null)
                continue;

            if (command.ReservedMoveGridIndex >= 0)
            {
                if (IsConsumedVisualSkipMove(command))
                    continue;

                actionRoutines.Add(CreateActionRoutine($"PlayerMove:{command.CharacterId}", ExecutePlayerMove(command)));
            }
            else
            {
                actionRoutines.Add(CreateActionRoutine($"PlayerSkill:{command.CharacterId}:{command.SkillId}", ExecutePlayerSkill(command)));
            }
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            actionRoutines.Add(CreateActionRoutine($"Monster:{command.RuntimeId}:{command.SkillId}", ExecuteMonsterCommand(command)));
        }

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

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            yield break;

        if (IsConsumedVisualSkipMove(command))
        {
            hudService.RefreshHUDs();
            yield break;
        }

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
            ApplyPlayerMoveFacing(character, command.Direction, moveOffset);
            hudService.RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
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
            Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / Offset:{moveOffset}");
            yield break;
        }

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
            command.IsSimulatedMoveBlocked ||
            command.VisualMoveSteps == null ||
            command.VisualMoveSteps.Count <= 1)
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

        if (command.EffectiveVisualMoveGridIndex >= 0 &&
            targetGridIndex != command.EffectiveVisualMoveGridIndex)
        {
            return false;
        }

        visualMoveOffset = command.EffectiveVisualMoveOffset;
        return visualMoveOffset != Vector2Int.zero;
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

    private IEnumerator ExecutePlayerSkill(PlayerReservedCommand command)
    {
        BattleCharacter attacker = unitFinder.FindBattleCharacter(command.CharacterId);

        if (attacker == null)
            yield break;

        RecalculatePlayerSkillRangeAtExecution(attacker, command);

        ConsumePlayerSkillCost(command, attacker);

        BattleUnitAnimator attackerAnimator = attacker.GetComponent<BattleUnitAnimator>();

        if (attackerAnimator != null)
            attackerAnimator.PlaySkillReady(command.SkillData);

        yield return new WaitForSeconds(ReadyDelay);

        if (command.SkillData.Target == TargetType.PlayerParty)
        {
            if (attackerAnimator != null)
                attackerAnimator.PlaySkillAction(command.SkillData);

            statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

            BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < characters.Length; i++)
            {
                BattleCharacter target = characters[i];

                if (target == null || target.RuntimeData == null)
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

        if (attackerAnimator != null)
            attackerAnimator.PlaySkillAction(command.SkillData);

        statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

        for (int i = 0; i < hitTargets.Count; i++)
        {
            MonsterUnit monster = hitTargets[i];

            BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();

            if (hitFacing != null)
            {
                hitFacing.FaceByWorldTarget(attacker.transform.position);

                if (monster.RuntimeData != null)
                    monster.RuntimeData.Direction = hitFacing.GetBattleDirection();
            }

            ExecutePlayerSkillEffects(attacker, monster, command);

            BattleUnitAnimator hitAnimator = monster.GetComponent<BattleUnitAnimator>();

            if (monster.RuntimeData.IsDead)
            {
                if (hitAnimator != null)
                    hitAnimator.PlayDead();

                deathService.HandleMonsterDead(monster);
            }
            else
            {
                if (hitAnimator != null)
                    hitAnimator.PlayHit();
            }
        }

        hudService.RefreshHUDs();

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.PlayDamageImpact();

        if (hitTargets.Count > 0)
            yield return new WaitForSeconds(HitCameraDelay);
        else
            yield return new WaitForSeconds(ActionDelay);

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
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

        List<int> rangeGridIndices = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            rangeGridIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                attacker.CurrentGridIndex,
                command.SkillData.RangeId,
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
                command.SkillData.RangeId,
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
                Count = entry.CountAmount
            };

            ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
        }
    }

    private void ExecutePlayerSkillEffectsToPlayer(
    BattleCharacter caster,
    BattleCharacter playerTarget,
    PlayerReservedCommand command)
    {
        if (caster == null || playerTarget == null || command == null || command.SkillData == null)
            return;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
            return;

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
                PlayerTarget = playerTarget,
                PlayerSkillData = command.SkillData,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = entry.EffectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = entry.CountAmount
            };

            ExecutePlayerEffectSafely(entry.EffectId, context, command.SkillData.SkillId);
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
            return damageService.GetPlayerDamage(command);

        if (entry.EffectId == "E_Pierce")
            return damageService.GetPlayerDamage(command);

        return entry.ValueAmount;
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

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                yield break;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                yield break;
        }

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

        if (monsterAnimator != null)
            monsterAnimator.PlayMonsterSkillReady(command.SkillData);

        yield return new WaitForSeconds(ReadyDelay);

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

        if (monsterAnimator != null)
            monsterAnimator.PlayMonsterSkillAction(command.SkillData);

        statusEffectService.ApplyBleedingDamageToMonsterOnAttack(monster);

        monsterSkillEffectService.ApplyMonsterSkill(monster, command);

        if (firstPlayerTarget != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.PlayDamageImpact();

        if (firstPlayerTarget != null)
            yield return new WaitForSeconds(HitCameraDelay);
        else
            yield return new WaitForSeconds(ActionDelay);

        if (firstPlayerTarget != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
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

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                return character;
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
        int maxMove = Mathf.Max(1, command.SkillData.GridMove);

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

        if (animator != null)
            animator.PlayRandomAttackReady();

        yield return new WaitForSeconds(ReadyDelay);

        if (hitPlayer != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToAttacker(monster.transform);

        if (animator != null)
            animator.PlayCurrentAttackAction();

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
            if (target.RuntimeData.CurrentHealth <= 0)
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
