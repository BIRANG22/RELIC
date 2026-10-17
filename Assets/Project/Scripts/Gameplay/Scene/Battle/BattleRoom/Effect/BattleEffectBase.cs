public abstract class BattleEffectBase
{
    public abstract string EffectId { get; }

    public virtual bool CanApply(BattleEffectContext context)
    {
        return context != null;
    }

    public void Execute(BattleEffectContext context)
    {
        if (!CanApply(context))
            return;

        if (BattleEquipmentEffectService.ShouldBlockSelfBuff(context))
            return;

        int originalValue = context.Value;
        context.Value = BattleEquipmentEffectService.ModifyContextualPlayerEffectValue(context);

        var affectedMonster = context.PlayerCaster != null ? context.MonsterTarget?.RuntimeData : null;
        int hpBefore = affectedMonster != null ? affectedMonster.CurrentHP : 0;
        int shieldBefore = affectedMonster != null ? affectedMonster.CurrentShield : 0;
        int statusBefore = CountStatusStacks(affectedMonster);

        Apply(context);

        if (affectedMonster != null && !affectedMonster.IsDead &&
            (affectedMonster.CurrentHP != hpBefore ||
             affectedMonster.CurrentShield != shieldBefore ||
             CountStatusStacks(affectedMonster) != statusBefore))
        {
            affectedMonster.PendingPlayerEffectEscape = true;
        }

        BattleEquipmentEffectService.HandlePlayerEffectApplied(context);

        if (context.PlayerSkillData != null &&
            context.PlayerSkillData.SkillType == Relic.Gameplay.Data.SkillType.Buff)
        {
            BattleCharacter buffTarget = context.PlayerTarget != null
                ? context.PlayerTarget
                : context.PlayerCaster;

            if (buffTarget != null)
            {
                BattleEquipmentEffectService.HandlePlayerBuffApplied(context, buffTarget);
                BattleRunStatisticsRecorder.RecordBuffApplied(
                    context.PlayerCaster?.RuntimeData?.CharacterId,
                    BattleEffectUtility.GetRepeatedValue(context));
            }
        }

        context.Value = originalValue;
    }

    private static int CountStatusStacks(Relic.Gameplay.Data.MonsterRuntimeData monster)
    {
        if (monster?.StatusEffects == null)
            return 0;

        int total = monster.StatusEffects.Count;
        foreach (var status in monster.StatusEffects)
        {
            if (status != null)
                total += status.Stack;
        }
        return total;
    }

    protected abstract void Apply(BattleEffectContext context);
}
