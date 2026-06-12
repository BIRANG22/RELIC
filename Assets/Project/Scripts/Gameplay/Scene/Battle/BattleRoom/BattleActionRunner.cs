using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionRunner
{
    private readonly GridManager gridManager;

    private const float ReadyDelay = 0.12f;
    private const float ActionDelay = 0.12f;
    private const float BatchEndDelay = 0.15f;

    public BattleActionRunner(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public IEnumerator RunBatch(BattleActionBatch batch)
    {
        if (batch == null)
            yield break;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (command == null)
                continue;

            if (command.ReservedMoveGridIndex >= 0)
                ExecutePlayerMove(command);
            else
                yield return ExecutePlayerSkill(command);
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            yield return ExecuteMonsterCommand(command);
        }

        RefreshHUDs();

        yield return new WaitForSeconds(BatchEndDelay);

        PlayAllAliveIdle();
    }

    private void ExecutePlayerMove(PlayerReservedCommand command)
    {
        BattleCharacter character = FindBattleCharacter(command.CharacterId);

        if (character == null)
            return;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            return;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int targetCoord = currentCoord + command.MoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return;

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetGridIndex, command.CharacterId))
        {
            Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / To:{targetGridIndex}");
            return;
        }

        Vector3 pos = gridManager.GetWorldPositionByIndex(targetGridIndex);

        BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();

        if (facing != null)
        {
            if (command.MoveOffset.x > 0)
                facing.FaceRight();
            else if (command.MoveOffset.x < 0)
                facing.FaceLeft();
        }

        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayMove();

        character.transform.position = pos;
        character.SetGridIndex(targetGridIndex);
        UpdatePartyGridIndex(command.CharacterId, targetGridIndex);
        ApplyBurnDamageToPlayerOnMove(character);
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

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (!IsMonsterInRange(monster, command))
                continue;

            monster.RuntimeData.TakeDamage(damage);

            BattleUnitAnimator hitAnimator = monster.GetComponent<BattleUnitAnimator>();

            if (hitAnimator != null)
            {
                if (monster.RuntimeData.IsDead)
                    hitAnimator.PlayDead();
                else
                    hitAnimator.PlayHit();
            }

            Debug.Log(
                $"[BattleActionRunner] Player Hit Monster / " +
                $"{command.CharacterId} -> {monster.RuntimeData.Name} / Damage:{damage} / HP:{monster.RuntimeData.CurrentHp}"
            );
        }

        RefreshHUDs();
        yield return new WaitForSeconds(ActionDelay);
    }

    private void ConsumePlayerSkillCost(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || caster == null || caster.RuntimeData == null)
            return;

        CharacterRuntimeData runtime = caster.RuntimeData;

        if (command.HealthCost > 0)
            Debug.Log($"[BattleCost] {caster.CharacterId} / HP -{command.HealthCost}");

        if (command.StaminaCost > 0)
            Debug.Log($"[BattleCost] {caster.CharacterId} / Stamina -{command.StaminaCost}");

        if (command.ResourceCost > 0)
            Debug.Log($"[BattleCost] {caster.CharacterId} / Resource -{command.ResourceCost}");

        if (command.MoveCost > 0)
            Debug.Log($"[BattleCost] {caster.CharacterId} / Move -{command.MoveCost}");

        if (command.ShieldCost > 0)
            Debug.Log($"[BattleCost] {caster.CharacterId} / Shield -{command.ShieldCost}");

        runtime.ApplyReservedCosts();

        Debug.Log($"[BattleCost] Apply Reserved Costs / {caster.CharacterId}");
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
            ExecuteMonsterMove(command);
        }
        else
        {
            yield return ExecuteMonsterSkill(command);
        }

        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster != null && monster.RuntimeData != null)
            monster.RuntimeData.IncreaseTurnCount();
    }

    private void ExecuteMonsterMove(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            return;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            return;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            return;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / Out of Grid / {monster.RuntimeData.Name}");
                return;
            }

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / {monster.RuntimeData.Name} / To:{targetIndex}");
                return;
            }
        }

        Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int movedMainCoord = mainCoord + moveOffset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        Vector3 pos = gridManager.GetWorldPositionByIndex(movedMainIndex);

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
        {
            if (moveOffset.x > 0)
                facing.FaceRight();
            else if (moveOffset.x < 0)
                facing.FaceLeft();
        }

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayMove();

        monster.transform.position = pos;
        monster.MoveOccupiedCells(moveOffset, gridManager);
        ApplyBurnDamageToMonsterOnMove(monster);
    }

    private IEnumerator ExecuteMonsterSkill(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);


        Debug.Log(
            $"[MonsterAttackCheck] Skill:{command.SkillId} / " +
            $"RangeCount:{command.RangeGridIndices.Count}"
        );

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

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (!command.RangeGridIndices.Contains(character.CurrentGridIndex))
                continue;

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

            Debug.Log(
                $"[BattleActionRunner] Monster Hit Player / " +
                $"{monster.RuntimeData.Name} -> {character.CharacterId} / Damage:{damage} / HP:{character.RuntimeData.CurrentHealth}"
            );

            Debug.Log(
                $"[MonsterAttackCheck] Skill:{command.SkillId} / " +
                $"RangeCount:{command.RangeGridIndices.Count}"
            );
        }

        yield return new WaitForSeconds(ActionDelay);
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

            partyStore.SetGridIndex(i, gridIndex);
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

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (monster.RuntimeData.IsDead)
                animator.PlayDead();
            else
                animator.PlayHit();
        }

        Debug.Log($"[BattleEffect] Burn Damage / Monster:{monster.RuntimeData.Name} / Damage:{burnStack} / HP:{monster.RuntimeData.CurrentHp}");
    }
}