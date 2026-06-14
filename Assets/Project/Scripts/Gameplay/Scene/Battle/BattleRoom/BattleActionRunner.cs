using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionRunner
{
    private readonly GridManager gridManager;

    private const float ReadyDelay = 0.06f;
    private const float ActionDelay = 0.05f;
    private const float BatchEndDelay = 0.05f;

    private const float HitCameraDelay = 0.12f;
    private const float MonsterHUDVisibleDelay = 0.6f;

    public BattleActionRunner(GridManager gridManager)
    {
        this.gridManager = gridManager;
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

        yield return RunParallel(actionRoutines);

        RefreshHUDs();

        yield return new WaitForSeconds(MonsterHUDVisibleDelay);

        HideUnselectedMonsterHUDs();

        yield return new WaitForSeconds(BatchEndDelay);

        PlayAllAliveIdle();
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
                        Debug.Log($"[BattleActionRunner] Routine End:{routineIndex} / Left:{runningCount}");
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

    private void HideUnselectedMonsterHUDs()
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null)
                monsters[i].HideHUDIfNotSelected();
        }
    }

    private IEnumerator RunAndCountDown(IEnumerator routine, System.Action onComplete)
    {
        try
        {
            if (routine != null)
                yield return CoroutineHost.Instance.StartCoroutine(routine);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator ExecutePlayerMove(PlayerReservedCommand command)
    {
        BattleCharacter character = FindBattleCharacter(command.CharacterId);

        if (character == null)
            yield break;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int targetCoord = currentCoord + command.MoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            yield break;

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();

        if (facing != null)
            facing.FaceByMoveOffset(command.MoveOffset);

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
        ApplyBurnDamageToPlayerOnMove(character);

        RefreshHUDs();

        yield return new WaitForSeconds(ActionDelay);
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

    private IEnumerator ExecutePlayerSkill(PlayerReservedCommand command)
    {
        BattleCharacter attacker = FindBattleCharacter(command.CharacterId);

        if (attacker == null)
            yield break;

        ConsumePlayerSkillCost(command, attacker);

        BattleUnitAnimator attackerAnimator = attacker.GetComponent<BattleUnitAnimator>();

        if (attackerAnimator != null)
            attackerAnimator.PlaySkillReady(command.SkillData);

        yield return new WaitForSeconds(ReadyDelay);

        if (attackerAnimator != null)
            attackerAnimator.PlaySkillAction(command.SkillData);

        if (TryApplyPlayerSelfEffect(command, attacker))
        {
            RefreshHUDs();
            yield return new WaitForSeconds(ActionDelay);
            yield break;
        }

        int damage = GetPlayerDamage(command);

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
            yield return BattleCameraController.Instance.ZoomTo(hitTargets[0].transform);

        for (int i = 0; i < hitTargets.Count; i++)
        {
            MonsterUnit monster = hitTargets[i];

            BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();

            if (hitFacing != null)
                hitFacing.FaceByWorldTarget(attacker.transform.position);

            monster.RuntimeData.TakeDamage(damage);
            monster.ShowAndRefreshHUD();

            BattleUnitAnimator hitAnimator = monster.GetComponent<BattleUnitAnimator>();

            if (monster.RuntimeData.IsDead)
            {
                CollectMonsterReward(monster);
                hitAnimator.PlayDead();
            }
            else
            {
                hitAnimator.PlayHit();
            }
        }

        RefreshHUDs();

        if (hitTargets.Count > 0)
            yield return new WaitForSeconds(HitCameraDelay);
        else
            yield return new WaitForSeconds(ActionDelay);

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ReturnDefault();
    }

    private void ConsumePlayerSkillCost(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || caster == null || caster.RuntimeData == null)
            return;

        CharacterRuntimeData runtime = caster.RuntimeData;

        //if (command.HealthCost > 0)
        //    Debug.Log($"[BattleCost] {caster.CharacterId} / HP -{command.HealthCost}");

        //if (command.StaminaCost > 0)
        //    Debug.Log($"[BattleCost] {caster.CharacterId} / Stamina -{command.StaminaCost}");

        //if (command.ResourceCost > 0)
        //    Debug.Log($"[BattleCost] {caster.CharacterId} / Resource -{command.ResourceCost}");

        //if (command.MoveCost > 0)
        //    Debug.Log($"[BattleCost] {caster.CharacterId} / Move -{command.MoveCost}");

        //if (command.ShieldCost > 0)
        //    Debug.Log($"[BattleCost] {caster.CharacterId} / Shield -{command.ShieldCost}");

        runtime.ApplyReservedCosts();

        //Debug.Log($"[BattleCost] Apply Reserved Costs / {caster.CharacterId}");
    }

    private bool TryApplyPlayerSelfEffect(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || command.SkillData == null || caster == null || caster.RuntimeData == null)
            return false;

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return false;

        string[] effectIds = command.SkillData.EffectIds.Split(';');
        string[] values = !string.IsNullOrWhiteSpace(command.SkillData.ValueRate)
            ? command.SkillData.ValueRate.Split(';')
            : null;

        string[] counts = !string.IsNullOrWhiteSpace(command.SkillData.CountRate)
            ? command.SkillData.CountRate.Split(';')
            : null;

        bool applied = false;

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            int value = values != null && i < values.Length
                ? ParseFirstInt(values[i])
                : ParseFirstInt(command.SkillData.ValueRate);

            int count = counts != null && i < counts.Length
                ? ParseFirstInt(counts[i])
                : ParseFirstInt(command.SkillData.CountRate);

            if (effectId == "E_Armor")
            {
                caster.RuntimeData.CurrentShield += Mathf.Max(0, value);
                applied = true;

                Debug.Log($"[BattleEffect] E_Armor / {caster.CharacterId} / Shield +{value} / Current:{caster.RuntimeData.CurrentShield}");
            }
            else if (effectId == "E_Power")
            {
                AddOrStackStatusEffect(
                    caster.RuntimeData.StatusEffects,
                    "E_Power",
                    Mathf.Max(1, value),
                    Mathf.Max(1, count)
                );

                applied = true;

                Debug.Log($"[BattleEffect] E_Power / {caster.CharacterId} / Stack +{value} / Turn:{count}");
            }
        }

        return applied;
    }

    private void AddOrStackStatusEffect(
    List<StatusEffectRuntimeData> statusEffects,
    string effectId,
    int stack,
    int turnCount)
    {
        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            if (statusEffects[i].EffectId != effectId)
                continue;

            statusEffects[i].Stack += stack;
            statusEffects[i].TurnCount = Mathf.Max(statusEffects[i].TurnCount, turnCount);
            return;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = turnCount
        });
    }

    private IEnumerator ExecuteMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            yield break;

        if (command.MoveOffset != Vector2Int.zero ||
            command.SkillData.TimelineNotation == TimelineActionType.Move)
        {
            yield return ExecuteMonsterMove(command);
        }
        else
        {
            yield return ExecuteMonsterSkill(command);
        }

        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster != null && monster.RuntimeData != null)
            monster.RuntimeData.IncreaseTurnCount();
    }

    private IEnumerator ExecuteMonsterMove(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            yield break;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / Out of Grid / {monster.RuntimeData.Name}");
                yield break;
            }

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / {monster.RuntimeData.Name} / To:{targetIndex}");
                yield break;
            }
        }

        Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int movedMainCoord = mainCoord + moveOffset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        Vector3 pos = gridManager.GetWorldPositionByIndex(movedMainIndex);

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
            facing.FaceByMoveOffset(command.MoveOffset);

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
        ApplyBurnDamageToMonsterOnMove(monster);

        RefreshHUDs();

        yield return new WaitForSeconds(ActionDelay);
    }

    private IEnumerator ExecuteMonsterSkill(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        BattleUnitAnimator monsterAnimator = monster.GetComponent<BattleUnitAnimator>();

        if (monsterAnimator != null)
            monsterAnimator.PlayRandomAttackReady();

        yield return new WaitForSeconds(ReadyDelay);

        if (monsterAnimator != null)
            monsterAnimator.PlayCurrentAttackAction();

        int damage = GetMonsterDamage(command);

        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<BattleCharacter> hitTargets = new();

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            bool inRange = command.RangeGridIndices.Contains(character.CurrentGridIndex);

            if (!inRange)
                continue;

            hitTargets.Add(character);
        }

        if (hitTargets.Count > 0)
        {
            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

            if (facing != null)
                facing.FaceByWorldTarget(hitTargets[0].transform.position);
        }

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomTo(hitTargets[0].transform);

        for (int i = 0; i < hitTargets.Count; i++)
        {
            BattleCharacter character = hitTargets[i];

            BattleUnitFacing hitFacing = character.GetComponent<BattleUnitFacing>();

            if (hitFacing != null)
                hitFacing.FaceByWorldTarget(monster.transform.position);

            character.RuntimeData.CurrentHealth =
                Mathf.Max(0, character.RuntimeData.CurrentHealth - damage);

            BattleUnitAnimator hitAnimator = character.GetComponent<BattleUnitAnimator>();

            if (hitAnimator != null)
            {
                if (character.RuntimeData.CurrentHealth <= 0)
                    hitAnimator.PlayDead();
                else
                    hitAnimator.PlayHit();
            }
        }

        RefreshHUDs();

        if (hitTargets.Count > 0)
            yield return new WaitForSeconds(HitCameraDelay);
        else
            yield return new WaitForSeconds(ActionDelay);

        if (hitTargets.Count > 0 && BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ReturnDefault();
    }

    private Vector2Int GetMonsterMoveOffset(MonsterReservedCommand command)
    {
        if (command == null)
            return Vector2Int.zero;

        if (command.MoveOffset != Vector2Int.zero)
            return command.MoveOffset;

        int move = command.SkillData != null ? command.SkillData.GridMove : 0;

        if (move == 0)
            return Vector2Int.zero;

        return new Vector2Int(-1 * Mathf.Abs(move), 0);
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

    private int GetPlayerDamage(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        int value = ParseFirstInt(command.SkillData.ValueRate);

        BattleCharacter attacker = FindBattleCharacter(command.CharacterId);

        if (attacker != null && attacker.RuntimeData != null)
            value += GetStatusStack(attacker.RuntimeData.StatusEffects, "E_Power");

        return Mathf.Max(1, value);
    }

    private int GetStatusStack(
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statusEffects,
        string effectId)
    {
        if (statusEffects == null)
            return 0;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            if (statusEffects[i].EffectId == effectId)
                return statusEffects[i].Stack;
        }

        return 0;
    }

    private int GetMonsterDamage(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        int value = ParseFirstInt(command.SkillData.ValueRate);

        return Mathf.Max(1, value);
    }

    private int ParseFirstInt(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 1;

        string number = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]) || text[i] == '-')
                number += text[i];
            else if (!string.IsNullOrEmpty(number))
                break;
        }

        if (int.TryParse(number, out int result))
            return result;

        return 1;
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

    private BattleCharacter FindBattleCharacter(string characterId)
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].CharacterId == characterId)
                return characters[i];
        }

        return null;
    }

    private MonsterUnit FindMonsterUnit(string runtimeId)
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == runtimeId)
                return monsters[i];
        }

        return null;
    }

    private void RefreshHUDs()
    {
        PlayerHUDSlot[] playerHuds =
            Object.FindObjectsByType<PlayerHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < playerHuds.Length; i++)
        {
            if (playerHuds[i] != null)
                playerHuds[i].Refresh();
        }

        MonsterHUDSlot[] monsterHuds =
            Object.FindObjectsByType<MonsterHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsterHuds.Length; i++)
        {
            if (monsterHuds[i] != null)
                monsterHuds[i].Refresh();
        }
    }

    private void PlayAllAliveIdle()
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            if (characters[i].RuntimeData.CurrentHealth <= 0)
                continue;

            BattleUnitAnimator animator = characters[i].GetComponent<BattleUnitAnimator>();

            if (animator != null)
                animator.PlayIdle();
        }

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.IsDead)
                continue;

            BattleUnitAnimator animator = monsters[i].GetComponent<BattleUnitAnimator>();

            if (animator != null)
                animator.PlayIdle();
        }
    }

    private void ApplyBurnDamageToPlayerOnMove(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return;

        int burnStack = GetStatusStack(character.RuntimeData.StatusEffects, "E_Burn");

        if (burnStack <= 0)
            return;

        character.RuntimeData.CurrentHealth =
            Mathf.Max(0, character.RuntimeData.CurrentHealth - burnStack);

        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (character.RuntimeData.CurrentHealth <= 0)
                animator.PlayDead();
            else
                animator.PlayHit();
        }

        Debug.Log($"[BattleEffect] Burn Damage / Player:{character.CharacterId} / Damage:{burnStack} / HP:{character.RuntimeData.CurrentHealth}");
    }

    private void ApplyBurnDamageToMonsterOnMove(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        int burnStack = GetStatusStack(monster.RuntimeData.StatusEffects, "E_Burn");

        if (burnStack <= 0)
            return;

        monster.RuntimeData.TakeDamage(burnStack);
        monster.ShowAndRefreshHUD();

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (monster.RuntimeData.IsDead)
        {
            CollectMonsterReward(monster);
            animator.PlayDead();
        }
        else
        {
            animator.PlayHit();
        }

        Debug.Log($"[BattleEffect] Burn Damage / Monster:{monster.RuntimeData.Name} / Damage:{burnStack} / HP:{monster.RuntimeData.CurrentHp}");
    }

    private void CollectMonsterReward(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (BattleRewardCollector.Instance == null)
            return;

        BattleRewardCollector.Instance.CollectMonsterDrop(
            monster.RuntimeData.RuntimeId,
            monster.RuntimeData.DropTableId
        );
    }
}