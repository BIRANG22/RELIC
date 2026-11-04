using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleWardReactionServiceTests
{
    [Test]
    public void ResolveReactionDamage_ReturnsWardStack_WhenDirectDamageWasDealt()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData("E_Ward", 4, 3)
        };

        int damage = BattleWardReactionService.ResolveReactionDamage(statuses, 1);

        Assert.That(damage, Is.EqualTo(4));
    }

    [Test]
    public void ResolveReactionDamage_ReturnsZero_WhenNoDamageWasDealt()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData("E_Ward", 4, 3)
        };

        int damage = BattleWardReactionService.ResolveReactionDamage(statuses, 0);

        Assert.That(damage, Is.Zero);
    }

    [Test]
    public void ResolveReactionDamage_SumsOnlyPositiveActiveWardStacks()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData("E_Ward", 2, 2),
            new StatusEffectRuntimeData("E_Ward", 3, 1),
            new StatusEffectRuntimeData("E_Ward", 8, 0),
            new StatusEffectRuntimeData("E_Ward", 0, 2),
            new StatusEffectRuntimeData("E_Bleed", 9, 2)
        };

        int damage = BattleWardReactionService.ResolveReactionDamage(statuses, 5);

        Assert.That(damage, Is.EqualTo(5));
    }

    [Test]
    public void ResolveReactionDamage_ReturnsZero_ForMissingStatuses()
    {
        Assert.That(BattleWardReactionService.ResolveReactionDamage(null, 5), Is.Zero);
    }

    [Test]
    public void ResolveReactionDamage_DoesNotUseExpiredWardForLaterHits()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData("E_Ward", 6, 0)
        };

        Assert.That(BattleWardReactionService.ResolveReactionDamage(statuses, 3), Is.Zero);
    }
}
