using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

public static class BattleEffectUtility
{
    private const string BarrierEffectId = "E_Barrier";

    public static System.Action<BattleCharacter> OnPlayerDamaged;
    public static System.Action<BattleCharacter> OnPlayerHit;
    public static System.Action<BattleCharacter> OnPlayerBuffApplied;
    public static System.Action<BattleCharacter> OnPlayerDamagedEnemy;

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

    public static bool IsDeadPlayer(BattleCharacter target)
    {
        return target == null ||
               target.RuntimeData == null ||
               target.RuntimeData.IsDead;
    }

    private static void HandlePlayerDeathIfNeeded(BattleCharacter target)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        if (!target.RuntimeData.IsDead)
            return;

        target.RuntimeData.HandleDeath();
    }

    public static bool AddOrStackStatus(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId,
        int value,
        int count = 1)
    {
        if (statusEffects == null)
            return false;

        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        int stack = GetRepeatedValue(value, count);

        if (stack <= 0)
            return false;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];

            if (status == null)
                continue;

            if (status.EffectId != effectId)
                continue;

            // 일반 스킬/효과로 얻는 상태는 패시브 상태와 분리해서 유지합니다.
            // 패시브 갱신 시 IsPassive 상태만 제거되므로 서로 합치면 일반 스택도 함께 사라질 수 있습니다.
            if (status.IsPassive)
                continue;

            status.Stack += stack;
            status.TurnCount = Mathf.Max(status.TurnCount, 1);
            return true;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1
        });

        return true;
    }

    public static int GetRepeatedValue(int value, int count)
    {
        value = Mathf.Max(0, value);
        count = Mathf.Max(0, count);

        if (value <= 0 || count <= 0)
            return 0;

        return value * count;
    }

    public static int GetRepeatedValue(BattleEffectContext context)
    {
        if (context == null)
            return 0;

        return GetRepeatedValue(context.Value, context.Count);
    }

    public static bool AddStatusToDefaultTarget(
        BattleEffectContext context,
        string effectId)
    {
        if (context == null)
            return false;

        if (context.PlayerTarget != null)
            return AddStatusToPlayer(context.PlayerTarget, effectId, context.Value, context.Count);

        if (context.PlayerCaster != null)
            return AddStatusToPlayer(context.PlayerCaster, effectId, context.Value, context.Count);

        if (context.MonsterTarget != null)
            return AddStatusToMonster(context.MonsterTarget, effectId, context.Value, context.Count);

        if (context.MonsterCaster != null)
            return AddStatusToMonster(context.MonsterCaster, effectId, context.Value, context.Count);

        return false;
    }

    public static bool AddStatusToPlayer(
        BattleCharacter target,
        string effectId,
        int stack,
        int count = 1)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return false;

        if (AddOrStackStatus(target.RuntimeData.StatusEffects, effectId, stack, count))
        {
            PlayStatusVfx(ResolveUnitAnimator(target), effectId);
            BattleHitImpactFeedback.PlayStatusHitFeedback(target.transform);
            return true;
        }

        return false;
    }

    public static bool AddStatusToMonster(
        MonsterUnit target,
        string effectId,
        int stack,
        int count = 1)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return false;

        bool applied = AddOrStackStatus(target.RuntimeData.StatusEffects, effectId, stack, count);

        if (applied)
        {
            PlayStatusVfx(ResolveUnitAnimator(target), effectId);
            BattleHitImpactFeedback.PlayStatusHitFeedback(target.transform);
        }

        target.ShowAndRefreshHUD();
        return applied;
    }

    private static BattleUnitAnimator ResolveUnitAnimator(Component target)
    {
        if (target == null)
            return null;

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            return animator;

        animator = target.GetComponentInChildren<BattleUnitAnimator>(true);

        if (animator != null)
            return animator;

        return target.GetComponentInParent<BattleUnitAnimator>();
    }

    private static void PlayStatusVfx(BattleUnitAnimator animator, string effectId)
    {
        if (animator == null)
            return;

        animator.PlayStatusVfx(effectId);
    }

    private static void PlayHealVfx(BattleUnitAnimator animator)
    {
        if (animator == null)
            return;

        animator.PlayHeal();
    }

    public static void DamagePlayer(BattleCharacter target, int damage)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        damage = Mathf.Max(0, damage);

        if (damage > 0 && TryBlockDamageWithBarrier(target))
            return;

        int hpBefore = target.RuntimeData.CurrentHP;

        int shieldDamage = Mathf.Min(target.RuntimeData.CurrentShield, damage);
        target.RuntimeData.CurrentShield -= shieldDamage;
        damage -= shieldDamage;

        if (damage > 0)
        {
            target.RuntimeData.CurrentHP =
                Mathf.Max(0, target.RuntimeData.CurrentHP - damage);
        }

        int hpDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
        int shownDamage = shieldDamage + hpDamage;
        BattleRunStatisticsRecorder.RecordDamageTaken(
            target.RuntimeData.CharacterId,
            shownDamage,
            hpBefore > 0 && target.RuntimeData.CurrentHP <= 0);

        if (shownDamage > 0)
            BattleEquipmentEffectService.MarkPlayerDamagedThisTurn(target.RuntimeData);

        HandlePlayerDeathIfNeeded(target);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        OnPlayerDamaged?.Invoke(target);
        OnPlayerHit?.Invoke(target);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else if (hpDamage > 0)
                animator.PlayHit();
            else if (shieldDamage > 0)
                animator.PlayGuard();
            else
                animator.PlayHit();
        }
    }

    public static int DamageMonster(MonsterUnit target, int damage)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return 0;

        damage = Mathf.Max(0, damage);

        if (damage > 0 && TryBlockDamageWithBarrier(target))
            return 0;

        int hpBefore = target.RuntimeData.CurrentHP;

        int shieldDamage = target.RuntimeData.AbsorbShieldDamage(damage);
        damage -= shieldDamage;

        if (damage > 0)
            target.RuntimeData.TakeDamage(damage);

        int hpDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
        int shownDamage = shieldDamage + hpDamage;
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else if (hpDamage > 0)
                animator.PlayHit();
            else if (shieldDamage > 0)
                animator.PlayGuard();
            else
                animator.PlayHit();
        }

        target.ShowAndRefreshHUD();
        return shownDamage;
    }

    public static void PierceDamagePlayer(BattleCharacter target, int damage)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        damage = Mathf.Max(0, damage);

        if (damage > 0 && TryBlockDamageWithBarrier(target))
            return;

        int hpBefore = target.RuntimeData.CurrentHP;

        target.RuntimeData.CurrentHP =
            Mathf.Max(0, target.RuntimeData.CurrentHP - damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
        BattleRunStatisticsRecorder.RecordDamageTaken(
            target.RuntimeData.CharacterId,
            shownDamage,
            hpBefore > 0 && target.RuntimeData.CurrentHP <= 0);

        if (shownDamage > 0)
            BattleEquipmentEffectService.MarkPlayerDamagedThisTurn(target.RuntimeData);

        HandlePlayerDeathIfNeeded(target);
        BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else
                animator.PlayHit();
        }
    }

    public static int PierceDamageMonster(MonsterUnit target, int damage)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return 0;

        damage = Mathf.Max(0, damage);

        if (damage > 0 && TryBlockDamageWithBarrier(target))
            return 0;

        int hpBefore = target.RuntimeData.CurrentHP;

        target.RuntimeData.TakeDamage(damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
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
        return shownDamage;
    }

    private static bool TryBlockDamageWithBarrier(BattleCharacter target)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        if (!TryConsumeStatusStack(target.RuntimeData.StatusEffects, BarrierEffectId))
            return false;

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayGuard();

        return true;
    }

    private static bool TryBlockDamageWithBarrier(MonsterUnit target)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        if (!TryConsumeStatusStack(target.RuntimeData.StatusEffects, BarrierEffectId))
            return false;

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayGuard();

        target.ShowAndRefreshHUD();
        return true;
    }

    private static bool TryConsumeStatusStack(
        List<StatusEffectRuntimeData> statuses,
        string effectId)
    {
        if (statuses == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null || status.EffectId != effectId)
                continue;

            if (status.Stack > 1)
            {
                status.Stack--;
                status.TurnCount = Mathf.Max(status.TurnCount, 1);
            }
            else
            {
                statuses.RemoveAt(i);
            }

            return true;
        }

        return false;
    }

    public static void PoisonDamagePlayer(BattleCharacter target, int damage)
    {
        StatusDamagePlayerInternal(target, damage, true);
    }

    public static void StatusDamagePlayer(BattleCharacter target, int damage)
    {
        StatusDamagePlayerInternal(target, damage, false);
    }

    private static void StatusDamagePlayerInternal(BattleCharacter target, int damage, bool isPoison)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHP;

        target.RuntimeData.CurrentHP =
            Mathf.Max(0, target.RuntimeData.CurrentHP - damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
        BattleRunStatisticsRecorder.RecordDamageTaken(
            target.RuntimeData.CharacterId,
            shownDamage,
            hpBefore > 0 && target.RuntimeData.CurrentHP <= 0);

        if (shownDamage > 0)
            BattleEquipmentEffectService.MarkPlayerDamagedThisTurn(target.RuntimeData);

        HandlePlayerDeathIfNeeded(target);
        if (isPoison)
            BattleDamageTextPopupUI.ShowPoisonDamage(target.transform, shownDamage);
        else
            BattleDamageTextPopupUI.Show(target.transform, shownDamage);

        OnPlayerDamaged?.Invoke(target);

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            if (target.RuntimeData.IsDead)
                animator.PlayDead();
            else
                animator.PlayHit();
        }
    }

    public static void PoisonDamageMonster(MonsterUnit target, int damage)
    {
        StatusDamageMonsterInternal(target, damage, true);
    }

    public static void StatusDamageMonster(MonsterUnit target, int damage)
    {
        StatusDamageMonsterInternal(target, damage, false);
    }

    private static void StatusDamageMonsterInternal(MonsterUnit target, int damage, bool isPoison)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        damage = Mathf.Max(0, damage);

        int hpBefore = target.RuntimeData.CurrentHP;

        target.RuntimeData.TakeDamage(damage);

        int shownDamage = Mathf.Max(0, hpBefore - target.RuntimeData.CurrentHP);
        if (isPoison)
            BattleDamageTextPopupUI.ShowPoisonDamage(target.transform, shownDamage);
        else
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
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        value = Mathf.Max(0, value);

        if (BattleEquipmentEffectService.ShouldBlockPlayerHealing(target.RuntimeData))
            return;

        int overhealArmor = BattleEquipmentEffectService.GetOverhealArmorAmount(
            target.RuntimeData,
            value);

        if (overhealArmor > 0)
        {
            AddShieldToPlayer(target, overhealArmor);
            return;
        }

        int hpBefore = target.RuntimeData.CurrentHP;

        int maxHP = target.RuntimeData.MaxHP;

        if (maxHP <= 0)
        {
            target.RuntimeData.CurrentHP += value;
            if (target.RuntimeData.CurrentHP > hpBefore)
                PlayHealVfx(ResolveUnitAnimator(target));

            return;
        }

        target.RuntimeData.CurrentHP =
            Mathf.Min(maxHP, target.RuntimeData.CurrentHP + value);

        if (target.RuntimeData.CurrentHP > hpBefore)
            PlayHealVfx(ResolveUnitAnimator(target));
    }

    public static void HealMonster(MonsterUnit target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        value = Mathf.Max(0, value);
        int hpBefore = target.RuntimeData.CurrentHP;

        target.RuntimeData.Heal(value);

        if (target.RuntimeData.CurrentHP > hpBefore)
            PlayHealVfx(ResolveUnitAnimator(target));

        target.ShowAndRefreshHUD();
    }

    public static void AddShieldToPlayer(BattleCharacter target, int value)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        int shieldValue = BattleEquipmentEffectService.ModifyArmorGainForPlayer(
            target.RuntimeData,
            Mathf.Max(0, value));

        if (shieldValue <= 0)
            return;

        target.RuntimeData.CurrentShield += shieldValue;
        BattleDamageTextPopupUI.ShowArmorGain(target.transform, shieldValue);
        PlayStatusVfx(ResolveUnitAnimator(target), "E_Armor");
        BattleHitImpactFeedback.PlayStatusHitFeedback(target.transform);
    }

    public static void AddShieldToMonster(MonsterUnit target, int value)
    {
        if (target == null || target.RuntimeData == null)
            return;

        int shieldValue = Mathf.Max(0, value);

        if (shieldValue <= 0)
            return;

        target.RuntimeData.AddTemporaryShield(shieldValue);
        BattleDamageTextPopupUI.ShowArmorGain(target.transform, shieldValue);
        PlayStatusVfx(ResolveUnitAnimator(target), "E_Armor");
        BattleHitImpactFeedback.PlayStatusHitFeedback(target.transform);
        target.ShowAndRefreshHUD();
    }
}

