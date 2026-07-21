public abstract class ResearchStatusEffectBase : BattleEffectBase
{
    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
