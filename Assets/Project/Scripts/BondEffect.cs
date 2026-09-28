public class BondEffect : BattleEffectBase
{
    public override string EffectId => "E_Bond";

    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
