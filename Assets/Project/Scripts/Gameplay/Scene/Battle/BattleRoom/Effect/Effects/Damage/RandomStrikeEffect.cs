public sealed class RandomStrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_RandomStrike";
    protected override void Apply(BattleEffectContext context) => new StrikeEffect().Execute(context);
}

