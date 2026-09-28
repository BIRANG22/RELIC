using UnityEngine;

public class RemoveArmorEffect : BattleEffectBase
{
    public override string EffectId => "E_RemoveArmor";

    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerTarget != null)
        {
            BattleCharacter target = context.PlayerTarget;
            if (target.RuntimeData == null || target.RuntimeData.IsDead)
                return;

            int armorDamage = Mathf.Max(0, target.RuntimeData.CurrentShield);
            if (armorDamage <= 0)
                return;

            BattleDamageTextPopupUI.PrepareTargetHud(target.transform);
            target.RuntimeData.CurrentShield = 0;
            BattleDamageTextPopupUI.Show(target.transform, armorDamage);
            target.GetComponent<BattleUnitAnimator>()?.PlayGuard();
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(target);
            return;
        }

        if (context?.MonsterTarget != null)
        {
            Relic.Gameplay.Monster.MonsterUnit target = context.MonsterTarget;
            if (target.RuntimeData == null || target.RuntimeData.IsDead)
                return;

            int armorDamage = Mathf.Max(0, target.RuntimeData.CurrentShield);
            if (armorDamage <= 0)
                return;

            BattleDamageTextPopupUI.PrepareTargetHud(target.transform);
            target.RuntimeData.ClearAllShield();
            BattleDamageTextPopupUI.Show(target.transform, armorDamage);
            target.GetComponent<BattleUnitAnimator>()?.PlayGuard();
            target.ShowTemporaryHUDForEffect();
        }
    }
}
