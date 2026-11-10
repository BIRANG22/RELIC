using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class EffectValueDecreaseRuleTests
{
    [Test]
    public void ConsumeOnTrigger_DecrementsAndRemovesAtZero()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Bondage", Stack = 1 }
        };

        bool consumed = BattleStatusEffectService.TryConsumeOnTrigger(
            statuses,
            "E_Bondage",
            id => new EffectMasterData { EffectId = id, EndTurn = EndTurn.DecreaseOnTrigger });

        Assert.That(consumed, Is.True);
        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public void ConsumeOnTrigger_IgnoresOtherRules()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Bondage", Stack = 2 }
        };

        bool consumed = BattleStatusEffectService.TryConsumeOnTrigger(
            statuses,
            "E_Bondage",
            id => new EffectMasterData { EffectId = id, EndTurn = EndTurn.Decrease });

        Assert.That(consumed, Is.False);
        Assert.That(statuses[0].Stack, Is.EqualTo(2));
    }

    [Test]
    public void ConsumeVulnerableOnDamageTrigger_DecrementsOneStack()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Vulnerable", Stack = 2 }
        };

        bool consumed = BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
            statuses,
            baseDamage: 3,
            targetWasAlive: true);

        Assert.That(consumed, Is.True);
        Assert.That(statuses[0].Stack, Is.EqualTo(1));
    }

    [Test]
    public void ConsumeVulnerableOnDamageTrigger_RemovesLastStack()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Vulnerable", Stack = 1 }
        };

        bool consumed = BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
            statuses,
            baseDamage: 3,
            targetWasAlive: true);

        Assert.That(consumed, Is.True);
        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public void ConsumeVulnerableOnDamageTrigger_ZeroDamageDoesNotConsume()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Vulnerable", Stack = 2 }
        };

        bool consumed = BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
            statuses,
            baseDamage: 0,
            targetWasAlive: true);

        Assert.That(consumed, Is.False);
        Assert.That(statuses[0].Stack, Is.EqualTo(2));
    }

    [Test]
    public void ConsumeVulnerableOnDamageTrigger_DeadTargetDoesNotConsume()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData { EffectId = "E_Vulnerable", Stack = 2 }
        };

        bool consumed = BattleDamageModifierUtility.TryConsumeVulnerableOnDamageTrigger(
            statuses,
            baseDamage: 3,
            targetWasAlive: false);

        Assert.That(consumed, Is.False);
        Assert.That(statuses[0].Stack, Is.EqualTo(2));
    }
}
