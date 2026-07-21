public class ArmorEffect : BattleEffectBase
{
    public override string EffectId => "E_Armor";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int value = BattleEffectUtility.GetRepeatedValue(context);

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.AddShieldToPlayer(context.PlayerTarget, value);
            return;
        }

        if (context.PlayerCaster != null)
        {
            BattleEffectUtility.AddShieldToPlayer(context.PlayerCaster, value);
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.AddShieldToMonster(context.MonsterTarget, value);
            return;
        }

        if (context.MonsterCaster != null)
        {
            BattleEffectUtility.AddShieldToMonster(context.MonsterCaster, value);
        }
    }
}
