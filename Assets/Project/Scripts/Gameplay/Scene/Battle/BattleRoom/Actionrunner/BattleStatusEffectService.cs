using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

public class BattleStatusEffectService
{
    private readonly BattleDamageService damageService;
    private readonly BattleDeathService deathService;

    public BattleStatusEffectService(
        BattleDamageService damageService,
        BattleDeathService deathService)
    {
        this.damageService = damageService;
        this.deathService = deathService;
    }

    public bool TryApplyPlayerSelfEffect(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || command.SkillData == null || caster == null || caster.RuntimeData == null)
            return false;

        if (caster.RuntimeData.IsDead)
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
                ? damageService.ParseFirstInt(values[i])
                : damageService.ParseFirstInt(command.SkillData.ValueRate);

            int count = counts != null && i < counts.Length
                ? damageService.ParseFirstInt(counts[i])
                : damageService.ParseFirstInt(command.SkillData.CountRate);

            if (effectId == "E_Armor")
            {
                caster.RuntimeData.CurrentShield += Mathf.Max(0, value);
                applied = true;
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
            }
        }

        return applied;
    }

    public void AddOrStackStatusEffect(
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

    public void ApplyBurnDamageToPlayerOnMove(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return;

        int burnStack = damageService.GetStatusStack(character.RuntimeData.StatusEffects, "E_Burn");

        if (burnStack <= 0)
            return;

        BattleEffectUtility.StatusDamagePlayer(character, burnStack);

        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (character.RuntimeData.CurrentHP <= 0)
                animator.PlayDead();
            else
                animator.PlayHit();
        }
    }

    public void ApplyBurnDamageToMonsterOnMove(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        int burnStack = damageService.GetStatusStack(monster.RuntimeData.StatusEffects, "E_Burn");

        if (burnStack <= 0)
            return;

        BattleEffectUtility.StatusDamageMonster(monster, burnStack);

        if (monster.RuntimeData.IsDead)
            deathService.HandleMonsterDead(monster);
    }

    public bool ApplyTurnEndEffects()
    {
        bool playedPresentation = false;

        playedPresentation |= ApplyTurnEndEffectsToPlayers();
        playedPresentation |= ApplyTurnEndEffectsToMonsters();

        return playedPresentation;
    }

    private bool ApplyTurnEndEffectsToPlayers()
    {
        bool playedPresentation = false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            playedPresentation |= ApplyPlayerTurnEndStatusEffects(character);
        }

        return playedPresentation;
    }

    private bool ApplyTurnEndEffectsToMonsters()
    {
        bool playedPresentation = false;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            playedPresentation |= ApplyMonsterTurnEndStatusEffects(monster);
        }

        return playedPresentation;
    }

    private bool ApplyPlayerTurnEndStatusEffects(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return false;

        List<StatusEffectRuntimeData> statuses = character.RuntimeData.StatusEffects;

        if (statuses == null)
            return false;

        bool playedPresentation = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null)
                continue;

            if (status.EffectId == "E_Addicted")
            {
                BattleEffectUtility.StatusDamagePlayer(character, status.Stack);
                playedPresentation = true;

                if (character.RuntimeData.IsDead)
                    return playedPresentation;
            }

            if (status.EffectId == "E_Recover")
                character.RuntimeData.CurrentResource += 1;

            if (status.EffectId == "E_Recharge")
            {
                character.RuntimeData.CurrentCost =
                    Mathf.Min(
                        character.RuntimeData.MaxCost,
                        character.RuntimeData.CurrentCost + 1
                    );
            }

            ApplyEndTurnRule(statuses, i, status);
        }

        return playedPresentation;
    }

    private bool ApplyMonsterTurnEndStatusEffects(MonsterUnit monster)
    {
        List<StatusEffectRuntimeData> statuses = monster.RuntimeData.StatusEffects;

        if (statuses == null)
            return false;

        bool playedPresentation = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null)
                continue;

            if (status.EffectId == "E_Addicted")
            {
                BattleEffectUtility.StatusDamageMonster(monster, status.Stack);
                playedPresentation = true;

                if (monster.RuntimeData.IsDead)
                    deathService.HandleMonsterDead(monster);
            }

            ApplyEndTurnRule(statuses, i, status);
        }

        return playedPresentation;
    }

    private void ApplyEndTurnRule(
        List<StatusEffectRuntimeData> statuses,
        int index,
        StatusEffectRuntimeData status)
    {
        if (statuses == null || status == null)
            return;

        EffectMasterData effectData = null;

        if (DataManager.Instance != null &&
            DataManager.Instance.EffectDatabase != null)
        {
            DataManager.Instance.EffectDatabase.TryGet(
                status.EffectId,
                out effectData
            );
        }

        if (effectData == null)
            return;

        switch (effectData.EndTurn)
        {
            case EndTurn.None:
                break;

            case EndTurn.ReMove:
                statuses.RemoveAt(index);
                break;

            case EndTurn.Decrease:
                status.Stack--;

                if (status.Stack <= 0)
                    statuses.RemoveAt(index);

                break;

            case EndTurn.Maintain:
                break;
        }
    }

    public void ApplyBleedingDamageToPlayerOnAttack(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return;

        int bleedingStack = damageService.GetStatusStack(
            character.RuntimeData.StatusEffects,
            "E_Bleeding"
        );

        if (bleedingStack <= 0)
            return;

        BattleEffectUtility.StatusDamagePlayer(character, bleedingStack);
    }

    public void ApplyBleedingDamageToMonsterOnAttack(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        int bleedingStack = damageService.GetStatusStack(
            monster.RuntimeData.StatusEffects,
            "E_Bleeding"
        );

        if (bleedingStack <= 0)
            return;

        BattleEffectUtility.StatusDamageMonster(monster, bleedingStack);

        if (monster.RuntimeData.IsDead)
            deathService.HandleMonsterDead(monster);
    }
}
