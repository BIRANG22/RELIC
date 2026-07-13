public class HealEffect : BattleEffectBase
{
    public override string EffectId => "E_Heal";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerTarget != null) BattleEffectUtility.HealPlayer(context.PlayerTarget, context.Value);
        else if (context?.PlayerCaster != null) BattleEffectUtility.HealPlayer(context.PlayerCaster, context.Value);
        else if (context?.MonsterTarget != null) BattleEffectUtility.HealMonster(context.MonsterTarget, context.Value);
        else if (context?.MonsterCaster != null) BattleEffectUtility.HealMonster(context.MonsterCaster, context.Value);
    }
}
