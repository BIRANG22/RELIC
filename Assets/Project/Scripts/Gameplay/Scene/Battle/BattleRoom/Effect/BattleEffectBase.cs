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

        Apply(context);

        if (context.PlayerSkillData != null &&
            context.PlayerSkillData.SkillType == Relic.Gameplay.Data.SkillType.Buff)
        {
            BattleCharacter buffTarget = context.PlayerTarget != null
                ? context.PlayerTarget
                : context.PlayerCaster;

            if (buffTarget != null)
                BattleEffectUtility.OnPlayerBuffApplied?.Invoke(buffTarget);
        }
    }

    protected abstract void Apply(BattleEffectContext context);
}