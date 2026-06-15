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
        if (character == null || character.RuntimeData == null)
            return;

        int burnStack = damageService.GetStatusStack(character.RuntimeData.StatusEffects, "E_Burn");

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
    }

    public void ApplyBurnDamageToMonsterOnMove(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        int burnStack = damageService.GetStatusStack(monster.RuntimeData.StatusEffects, "E_Burn");

        if (burnStack <= 0)
            return;

        monster.RuntimeData.TakeDamage(burnStack);
        monster.ShowAndRefreshHUD();

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (monster.RuntimeData.IsDead)
        {
            deathService.HandleMonsterDead(monster);

            if (animator != null)
                animator.PlayDead();
        }
        else
        {
            if (animator != null)
                animator.PlayHit();
        }
    }
}