public sealed class ArmorStrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_ArmorStrike";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerCaster?.RuntimeData == null) return;
        int original = context.Value;
        context.Value = NewSkillEffectRules.ResolveArmorStrikeDamage(
            context.PlayerCaster.RuntimeData.CurrentShield, original);
        new StrikeEffect().Execute(context);
        context.Value = original;
    }
}

