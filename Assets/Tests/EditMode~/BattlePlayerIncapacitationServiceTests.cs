using NUnit.Framework;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public sealed class BattlePlayerIncapacitationServiceTests
{
    [Test]
    public void ResolveState_LivingToZero_ClearsBattleStateAndReturnsTrue()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "C_01",
            MaxHP = 10,
            CurrentHP = 0,
            CurrentShield = 4,
            ReservedHPCost = 2,
            ReservedCost = 3,
            StatusEffects = new System.Collections.Generic.List<StatusEffectRuntimeData>
            {
                new("E_Poison", 2)
            }
        };

        bool incapacitated = BattlePlayerIncapacitationService.ResolveState(runtime, 5);

        Assert.That(incapacitated, Is.True);
        Assert.That(runtime.IsDead, Is.True);
        Assert.That(runtime.CurrentShield, Is.Zero);
        Assert.That(runtime.ReservedHPCost, Is.Zero);
        Assert.That(runtime.ReservedCost, Is.Zero);
        Assert.That(runtime.StatusEffects, Is.Empty);
    }

    [Test]
    public void ResolveState_AlreadyDead_DoesNotHandleAgain()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "C_01",
            MaxHP = 10,
            CurrentHP = 0,
            StatusEffects = new System.Collections.Generic.List<StatusEffectRuntimeData>
            {
                new("E_Poison", 2)
            }
        };

        bool incapacitated = BattlePlayerIncapacitationService.ResolveState(runtime, 0);

        Assert.That(incapacitated, Is.False);
        Assert.That(runtime.StatusEffects, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResolveState_StillAlive_DoesNotChangeBattleState()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "C_01",
            MaxHP = 10,
            CurrentHP = 1,
            CurrentShield = 3,
            StatusEffects = new System.Collections.Generic.List<StatusEffectRuntimeData>
            {
                new("E_Boost", 1)
            }
        };

        bool incapacitated = BattlePlayerIncapacitationService.ResolveState(runtime, 5);

        Assert.That(incapacitated, Is.False);
        Assert.That(runtime.CurrentShield, Is.EqualTo(3));
        Assert.That(runtime.StatusEffects, Has.Count.EqualTo(1));
    }
}
