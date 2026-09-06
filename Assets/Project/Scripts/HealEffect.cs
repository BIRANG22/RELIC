public class HealEffect : BattleEffectBase
{
    public override string EffectId => "E_Heal";
    protected override void Apply(BattleEffectContext context)
    {
        int value = BattleEffectUtility.GetRepeatedValue(context);

        if (context?.PlayerTarget != null) BattleEffectUtility.HealPlayer(context.PlayerTarget, value);
        else if (context?.PlayerCaster != null) BattleEffectUtility.HealPlayer(context.PlayerCaster, value);
        else if (context?.MonsterTarget != null) BattleEffectUtility.HealMonster(context.MonsterTarget, value);
        else if (context?.MonsterCaster != null) BattleEffectUtility.HealMonster(context.MonsterCaster, value);
    }
}
