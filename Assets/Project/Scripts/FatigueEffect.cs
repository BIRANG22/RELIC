public class FatigueEffect : BattleEffectBase
{
    public override string EffectId => "E_Fatigue";

    protected override void Apply(BattleEffectContext context)
    {
        BattleEffectUtility.AddStatusToDefaultTarget(context, EffectId);
    }
}
