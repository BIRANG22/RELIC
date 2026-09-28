public class HealEffect : BattleEffectBase
{
    public override string EffectId => "E_Heal";

    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
