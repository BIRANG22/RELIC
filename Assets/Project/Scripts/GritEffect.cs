public class GritEffect : BattleEffectBase
{
    public override string EffectId => "E_Grit";

    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
