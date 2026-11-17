using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class SmiteFirstAttackPerTurnTests
{
    [Test]
    public void Smite_OnlyFirstAttackEachTurnConsumesOneStack()
    {
        CharacterRuntimeData runtime = RuntimeWithSmite(3);

        Assert.That(BattleEquipmentEffectService.IsSmiteReady(runtime), Is.True);
        Assert.That(BattleEquipmentEffectService.TryConsumeSmiteForAttack(runtime), Is.True);
        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(2));
        Assert.That(BattleEquipmentEffectService.IsSmiteReady(runtime), Is.False);
        Assert.That(BattleEquipmentEffectService.TryConsumeSmiteForAttack(runtime), Is.False);
        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(2));

        BattleEquipmentEffectService.ResetSmiteTurnUsage(runtime);

        Assert.That(BattleEquipmentEffectService.IsSmiteReady(runtime), Is.True);
        Assert.That(BattleEquipmentEffectService.TryConsumeSmiteForAttack(runtime), Is.True);
        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(1));
    }

    [Test]
    public void Smite_AttackOnThirdTurnRemovesLastStack()
    {
        CharacterRuntimeData runtime = RuntimeWithSmite(1);

        Assert.That(BattleEquipmentEffectService.TryConsumeSmiteForAttack(runtime), Is.True);

        Assert.That(runtime.StatusEffects, Is.Empty);
        Assert.That(BattleEquipmentEffectService.IsSmiteReady(runtime), Is.False);
    }

    [Test]
    public void Smite_TurnResetWithoutAttackKeepsStack()
    {
        CharacterRuntimeData runtime = RuntimeWithSmite(3);

        BattleEquipmentEffectService.ResetSmiteTurnUsage(runtime);

        Assert.That(runtime.StatusEffects[0].Stack, Is.EqualTo(3));
        Assert.That(BattleEquipmentEffectService.IsSmiteReady(runtime), Is.True);
    }

    [Test]
    public void Smite_DamageModifierAppliesOnlyWhileReady()
    {
        CharacterRuntimeData runtime = RuntimeWithSmite(3);

        int firstAttack = BattleDamageModifierUtility.ApplyPlayerAttackerModifiersInStatusOrder(
            10, runtime, isAttackSkill: true);
        BattleEquipmentEffectService.TryConsumeSmiteForAttack(runtime);
        int secondAttack = BattleDamageModifierUtility.ApplyPlayerAttackerModifiersInStatusOrder(
            10, runtime, isAttackSkill: true);

        Assert.That(firstAttack, Is.EqualTo(15));
        Assert.That(secondAttack, Is.EqualTo(10));
    }

    private static CharacterRuntimeData RuntimeWithSmite(int stack) => new()
    {
        StatusEffects = new List<StatusEffectRuntimeData>
        {
            new() { EffectId = "E_Smite", Stack = stack }
        },
        AppliedBattleEquipmentEffectIds = new List<string>()
    };
}
