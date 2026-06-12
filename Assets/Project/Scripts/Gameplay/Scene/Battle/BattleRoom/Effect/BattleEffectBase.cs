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
    }

    protected abstract void Apply(BattleEffectContext context);
}