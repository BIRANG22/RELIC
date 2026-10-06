public sealed class StrikeCountByDebuffEffect : BattleEffectBase
{
    public override string EffectId => "E_StrikeCountByDebuff";

    protected override void Apply(BattleEffectContext context) =>
        new StrikeEffect().Execute(context);
}
