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

        Apply(context);

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

    protected abstract void Apply(BattleEffectContext context);
}
