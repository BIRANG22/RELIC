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
}
