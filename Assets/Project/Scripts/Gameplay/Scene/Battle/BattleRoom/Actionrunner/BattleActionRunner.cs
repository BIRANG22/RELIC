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

    private const float ReadyDelay = 0.06f;
    private const float ActionDelay = 0.05f;
    private const float BatchEndDelay = 0.05f;

    private const float HitCameraDelay = 0.12f;
    private const float MonsterHUDVisibleDelay = 0.6f;

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner = null,
      BattleRoomLoader roomLoader = null)
    {
        this.gridManager = gridManager;

        unitFinder = new BattleUnitFinder();
        hudService = new BattleHUDService();
        damageService = new BattleDamageService(unitFinder);
        deathService = new BattleDeathService(gridManager, monsterSpawner, roomLoader);
        statusEffectService = new BattleStatusEffectService(damageService, deathService);
        monsterSkillEffectService = new MonsterSkillEffectService(damageService, deathService, hudService, gridManager);
    }

    public IEnumerator RunBatch(BattleActionBatch batch)
    {
        if (batch == null)
            yield break;

        List<IEnumerator> actionRoutines = new();

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (command == null)
                continue;

            if (command.ReservedMoveGridIndex >= 0)
                actionRoutines.Add(ExecutePlayerMove(command));
            else
                actionRoutines.Add(ExecutePlayerSkill(command));
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            actionRoutines.Add(ExecuteMonsterCommand(command));
        }

        bool holdCameraUntilBatchEnd = ShouldHoldCameraUntilBatchEnd(batch);

        if (holdCameraUntilBatchEnd && BattleCameraController.Instance != null)
            BattleCameraController.Instance.SetHoldDefaultReturn(true);

        yield return RunParallel(actionRoutines);

        if (holdCameraUntilBatchEnd && BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(false);
            yield return BattleCameraController.Instance.ReturnDefault();
        }

        IncreaseMonsterTurnCountsOnceInSlot(batch);

        yield return new WaitForSeconds(MonsterHUDVisibleDelay);

        hudService.HideUnselectedMonsterHUDs();

        yield return new WaitForSeconds(BatchEndDelay);

        hudService.PlayAllAliveIdle();
    }

    public void ApplyTurnEndEffects()
    {
        statusEffectService.ApplyTurnEndEffects();
        hudService.RefreshHUDs();
    }

    private bool ShouldHoldCameraUntilBatchEnd(BattleActionBatch batch)
    {
        return CountCrossSideHitActions(batch) > 1;
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

    private IEnumerator RunParallel(List<IEnumerator> routines)
    {
        if (routines == null || routines.Count == 0)
            yield break;

        int runningCount = routines.Count;

        for (int i = 0; i < routines.Count; i++)
        {
            int routineIndex = i;

            CoroutineHost.Instance.StartCoroutine(
                RunAndCountDown(
                    routines[i],
                    () =>
                    {
                        runningCount--;
                        //Debug.Log($"[BattleActionRunner] Routine End:{routineIndex} / Left:{runningCount}");
                    }
                )
            );
        }

        float timeout = 10f;
        float elapsed = 0f;

        while (runningCount > 0)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= timeout)
            {
                Debug.LogError($"[BattleActionRunner] RunParallel Timeout / Left:{runningCount}");
                break;
            }

            yield return null;
        }
    }

    private IEnumerator RunAndCountDown(IEnumerator routine, System.Action onComplete)
    {
        bool done = false;

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

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int moveOffset = command.EffectiveMoveOffset;

        if (moveOffset == Vector2Int.zero)
            yield break;

        Vector2Int targetCoord = currentCoord + moveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            yield break;

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();

        if (facing != null)
            facing.FaceByMoveOffset(moveOffset);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetGridIndex, command.CharacterId))
        {
            Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / To:{targetGridIndex}");
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
            0.25f
        );

        character.SetGridIndex(targetGridIndex);
        UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
        statusEffectService.ApplyBurnDamageToPlayerOnMove(character);

        hudService.RefreshHUDs();

        yield return new WaitForSeconds(ActionDelay);
    }

    private IEnumerator ExecutePlayerSkill(PlayerReservedCommand command)
    {
        BattleCharacter attacker = unitFinder.FindBattleCharacter(command.CharacterId);

        if (attacker == null)
            yield break;

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
            yield return BattleCameraController.Instance.ZoomToHitTarget(hitTargets[0].transform);

        if (attackerAnimator != null)
            attackerAnimator.PlaySkillAction(command.SkillData);

        statusEffectService.ApplyBleedingDamageToPlayerOnAttack(attacker);

        for (int i = 0; i < hitTargets.Count; i++)
        {
            MonsterUnit monster = hitTargets[i];

            BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();

            if (hitFacing != null)
                hitFacing.FaceByWorldTarget(attacker.transform.position);

            ExecutePlayerSkillEffects(attacker, monster, command);

            BattleUnitAnimator hitAnimator = monster.GetComponent<BattleUnitAnimator>();

            if (monster.RuntimeData.IsDead)
            {
                deathService.HandleMonsterDead(monster);

                if (hitAnimator != null)
                    hitAnimator.PlayDead();
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

            effectExecutor.Execute(entry.EffectId, context);
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

            effectExecutor.Execute(entry.EffectId, context);
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
            0.25f
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

        RecalculateMonsterSkillRangeAtExecution(monster, command);

        if (command.SkillData.SkillId == "S_Monster_07")
        {
            yield return ExecuteMonsterDashAttack(command);
            yield break;
        }

        BattleCharacter firstPlayerTarget = FindFirstPlayerTarget(command);

        BattleUnitAnimator monsterAnimator = monster.GetComponent<BattleUnitAnimator>();

        if (monsterAnimator != null)
            monsterAnimator.PlayMonsterSkillReady(command.SkillData);

        yield return new WaitForSeconds(ReadyDelay);

        if (firstPlayerTarget != null)
        {
            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

            if (facing != null)
                facing.FaceByWorldTarget(firstPlayerTarget.transform.position);
        }

        if (firstPlayerTarget != null && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToHitTarget(firstPlayerTarget.transform);

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
            yield return BattleCameraController.Instance.ZoomToHitTarget(hitPlayer.transform);

        if (animator != null)
            animator.PlayCurrentAttackAction();

        if (finalOffset != Vector2Int.zero)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.MainGridIndex);
            Vector2Int movedCoord = currentCoord + finalOffset;
            int movedIndex = gridManager.CoordToIndex(movedCoord);

            Vector3 pos = gridManager.GetWorldPositionByIndex(movedIndex);

            yield return MoveTransformSmooth(
                monster.transform,
                monster.transform.position,
                pos,
                0.25f
            );

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
            targetFacing.FaceByWorldTarget(monster.transform.position);

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