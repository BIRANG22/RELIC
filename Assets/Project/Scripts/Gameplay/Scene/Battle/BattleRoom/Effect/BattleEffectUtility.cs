using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

public static class BattleEffectUtility
{
    public static System.Action<BattleCharacter> OnPlayerDamaged;
    public static BattleCharacter GetPlayerTargetOrCaster(BattleEffectContext context)
    {
        if (context == null)
            return null;

        return context.PlayerTarget != null ? context.PlayerTarget : context.PlayerCaster;
    }

    public static MonsterUnit GetMonsterTargetOrCaster(BattleEffectContext context)
    {
        if (context == null)
            return null;

        return context.MonsterTarget != null ? context.MonsterTarget : context.MonsterCaster;
    }

    public static void AddOrStackStatus(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId,
        int stack,
        int turnCount = 1)
    {
        if (statusEffects == null)
            return;

        if (string.IsNullOrWhiteSpace(effectId))
            return;

        stack = Mathf.Max(1, stack);
        turnCount = Mathf.Max(0, turnCount);

        EffectMasterData effectData = null;

        if (DataManager.Instance != null && DataManager.Instance.EffectDatabase != null)
            DataManager.Instance.EffectDatabase.TryGet(effectId, out effectData);

        bool canNest = effectData == null || effectData.Nesting == true;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];

            if (status == null)
                continue;

            if (status.EffectId != effectId)
                continue;

            if (canNest)
                status.Stack += stack;
            else
                status.Stack = Mathf.Max(status.Stack, stack);

            status.TurnCount = Mathf.Max(status.TurnCount, turnCount);
            return;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = turnCount
        });
    }

    public static void AddStatusToPlayer(
        BattleCharacter target,
        string effectId,
        int stack,
        int turnCount = 1)
    {
        if (target == null || target.RuntimeData == null)
            return;

        AddOrStackStatus(target.RuntimeData.StatusEffects, effectId, stack, turnCount);
    }

    public static void AddStatusToMonster(
        MonsterUnit target,
        string effectId,
        int stack,
        int turnCount = 1)
    {
        if (target == null || target.RuntimeData == null)
            return;

        AddOrStackStatus(target.RuntimeData.StatusEffects, effectId, stack, turnCount);
        target.ShowAndRefreshHUD();
    }

    public static void DamagePlayer(BattleCharacter target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHealth;

        int shieldDamage = Mathf.Min(target.RuntimeData.CurrentShield, damage);
        target.RuntimeData.CurrentShield -= shieldDamage;
        damage -= shieldDamage;

        if (damage > 0)
        {
            target.RuntimeData.CurrentHealth =
                Mathf.Max(0, target.RuntimeData.CurrentHealth - damage);
        }

        int healthDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHealth);
        int shownDamage = shieldDamage + healthDamage;
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        OnPlayerDamaged?.Invoke(target);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.CurrentHealth <= 0)
                animator.PlayDead();
            else if (target.RuntimeData.CurrentHealth == hpBefore)
                animator.PlayGuard();
            else
                animator.PlayHit();
        }
    }

    public static void DamageMonster(MonsterUnit target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHp;

        int shieldDamage = Mathf.Min(target.RuntimeData.CurrentShield, damage);
        target.RuntimeData.CurrentShield -= shieldDamage;
        damage -= shieldDamage;

        if (damage > 0)
            target.RuntimeData.TakeDamage(damage);

        int healthDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHp);
        int shownDamage = shieldDamage + healthDamage;
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else if (target.RuntimeData.CurrentHp == hpBefore)
                animator.PlayGuard();
            else
                animator.PlayHit();
        }

        target.ShowAndRefreshHUD();
    }

    public static void PierceDamagePlayer(BattleCharacter target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHealth;

        target.RuntimeData.CurrentHealth =
            Mathf.Max(0, target.RuntimeData.CurrentHealth - damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHealth);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.CurrentHealth <= 0)
                animator.PlayDead();
            else
                animator.PlayHit();
        }
    }

    public static void PierceDamageMonster(MonsterUnit target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHp;

        target.RuntimeData.TakeDamage(damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHp);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else
                animator.PlayHit();
        }

        target.ShowAndRefreshHUD();
    }

    public static void StatusDamagePlayer(BattleCharacter target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHealth;

        target.RuntimeData.CurrentHealth =
            Mathf.Max(0, target.RuntimeData.CurrentHealth - damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHealth);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        OnPlayerDamaged?.Invoke(target);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.CurrentHealth <= 0)
                animator.PlayDead();
            else
                animator.PlayHit();
        }
    }

    public static void StatusDamageMonster(MonsterUnit target, int damage)
    {
        if (target == null || target.RuntimeData == null)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHp;

        target.RuntimeData.TakeDamage(damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHp);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else
                animator.PlayHit();
        }

        target.ShowAndRefreshHUD();
    }

    public static void HealPlayer(BattleCharacter target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        value = Mathf.Max(0, value);

        int maxHealth = target.RuntimeData.MaxHealth;

        if (maxHealth <= 0)
        {
            target.RuntimeData.CurrentHealth += value;
            return;
        }

        target.RuntimeData.CurrentHealth =
            Mathf.Min(maxHealth, target.RuntimeData.CurrentHealth + value);
    }

    public static void HealMonster(MonsterUnit target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        value = Mathf.Max(0, value);

        target.RuntimeData.Heal(value);
        target.ShowAndRefreshHUD();
    }

    public static void AddShieldToPlayer(BattleCharacter target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        target.RuntimeData.CurrentShield += Mathf.Max(0, value);
    }

    public static void AddShieldToMonster(MonsterUnit target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        target.RuntimeData.CurrentShield += Mathf.Max(0, value);
        target.ShowAndRefreshHUD();
    }
}
