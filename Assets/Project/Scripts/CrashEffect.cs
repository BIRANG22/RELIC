using UnityEngine;

/// <summary>
/// 강제 이동이 유닛, 장애물 또는 이동 불가 위치에 막혔을 때 적용되는 고정 피해 효과입니다.
/// 방어도와 피해 증감 효과의 영향을 받지 않습니다.
/// </summary>
public class CrashEffect : BattleEffectBase
{
    private const int DefaultCrashDamage = 2;

    public override string EffectId => "E_Crash";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int damage = context.Value > 0
            ? context.Value
            : DefaultCrashDamage;

        damage = Mathf.Max(0, damage);

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.StatusDamagePlayer(context.PlayerTarget, damage);
            return;
        }

        if (context.MonsterTarget != null)
            BattleEffectUtility.StatusDamageMonster(context.MonsterTarget, damage);
    }
}
