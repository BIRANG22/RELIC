// Applied through the standard status system. Consumed once per attack action.
public class ElasticityEffect : BattleEffectBase
{
    public override string EffectId => "E_Elasticity";

    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
