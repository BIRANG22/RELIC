public sealed class BondageEffect : BattleEffectBase
{
    public override string EffectId => "E_Bondage";
    protected override void Apply(BattleEffectContext context) =>
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
}

