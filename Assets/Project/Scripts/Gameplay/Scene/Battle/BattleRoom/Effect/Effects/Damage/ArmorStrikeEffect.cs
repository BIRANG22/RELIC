public sealed class ArmorStrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_ArmorStrike";
    protected override void Apply(BattleEffectContext context) => new StrikeEffect().Execute(context);
}

