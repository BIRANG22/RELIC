using UnityEngine;

public class StrikeEffect : BattleEffectBase
{
    public override string EffectId => "E_Strike";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            context.PlayerTarget.RuntimeData.CurrentHealth =
                Mathf.Max(0, context.PlayerTarget.RuntimeData.CurrentHealth - context.Value);

            BattleUnitAnimator animator =
                context.PlayerTarget.GetComponent<BattleUnitAnimator>();

            if (animator != null)
            {
                if (context.PlayerTarget.RuntimeData.CurrentHealth <= 0)
                    animator.PlayDead();
                else
                    animator.PlayHit();
            }

            Debug.Log($"[Effect] E_Strike Player / Damage:{context.Value}");
        }

        if (context.MonsterTarget != null)
        {
            context.MonsterTarget.RuntimeData.TakeDamage(context.Value);
            context.MonsterTarget.ShowAndRefreshHUD();

            Debug.Log($"[Effect] E_Strike Monster / Damage:{context.Value}");
        }
    }
}