using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleConsumableServiceTests
{
    private static CharacterRuntimeData Member(string id, int hp = 10) => new() { CharacterId = id, CurrentHP = hp };
    private static BattleRuntimeData Battle()
    {
        var battle = new BattleRuntimeData();
        PartyConsumableRuntime.NormalizeBattle(battle);
        battle.ConsumableSlots[3].ItemId = "Compound_Test";
        return battle;
    }
    private static UseConsumableCommand Command() => new() { SlotIndex = 3, ItemId = "Compound_Test", RequestId = "request" };
    private static ActiveRelicService Effects(string effectId)
    {
        var database = new CompoundDatabase();
        database.Initialize(new[] { new CompoundData { CompoundId = "Compound_Test", EffectIds = effectId } });
        return new ActiveRelicService(database);
    }

    [Test]
    public void PartyBuff_AppliesToEveryLivingDistinctMember_ConsumesFourthSlotOnce()
    {
        var battle = Battle();
        var first = Member("A"); var second = Member("B"); var dead = Member("C", 0);
        var party = new[] { first, dead, second, first };
        var effects = Effects(ActiveRelicEffectIds.DamageBoostThisTurn);
        var result = BattleConsumableService.TryUseParty(battle, Command(), party, true, effects);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(first.StatusEffects.Count, Is.EqualTo(1));
        Assert.That(second.StatusEffects.Count, Is.EqualTo(1));
        Assert.That(dead.StatusEffects, Is.Empty);
        Assert.That(battle.ConsumableSlots[3].IsUsed, Is.True);
        var repeated = BattleConsumableService.TryUseParty(battle, Command(), party, true, effects);
        Assert.That(repeated.RejectReason, Is.EqualTo(ConsumableRejectReason.AlreadyUsed));
        Assert.That(second.StatusEffects.Count, Is.EqualTo(1));
    }

    [Test]
    public void PartyBuff_AlreadyBuffedMemberDoesNotPreventOtherMembersReceivingEffect()
    {
        var first = Member("A"); var second = Member("B");
        var effect = ActiveRelicEffectIds.DamageReductionThisTurn;
        ActiveRelicRuntimeUtility.TryAddTurnScopedStatus(first, effect);
        var result = BattleConsumableService.TryUseParty(Battle(), Command(), new[] { first, second }, true, Effects(effect));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(first.StatusEffects.Count, Is.EqualTo(1));
        Assert.That(second.StatusEffects.Count, Is.EqualTo(1));
    }

    [Test]
    public void PartyBuff_AllAlreadyActive_DoesNotConsume()
    {
        var battle = Battle(); var member = Member("A");
        ActiveRelicRuntimeUtility.TryAddTurnScopedStatus(member, ActiveRelicEffectIds.DamageBoostThisTurn);
        var result = BattleConsumableService.TryUseParty(battle, Command(), new[] { member }, true, Effects(ActiveRelicEffectIds.DamageBoostThisTurn));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(battle.ConsumableSlots[3].IsUsed, Is.False);
    }

    [Test]
    public void ManaRecovery_ChangesAllLivingMembersWithoutChangingReservations()
    {
        var first = Member("A"); first.CurrentCost = 3; first.ReservedCost = 2;
        var second = Member("B"); second.CurrentCost = 9;
        var dead = Member("C", 0);
        var result = BattleConsumableService.TryUseParty(Battle(), Command(), new[] { first, second, dead, first }, true, Effects(ActiveRelicEffectIds.RecoverCostToMax));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(first.CurrentCost, Is.EqualTo(13));
        Assert.That(second.CurrentCost, Is.EqualTo(19));
        Assert.That(first.ReservedCost, Is.EqualTo(2));
        Assert.That(dead.CurrentCost, Is.Zero);
    }

    [Test]
    public void NoLivingParty_DoesNotConsume()
    {
        var battle = Battle();
        var result = BattleConsumableService.TryUseParty(battle, Command(), new[] { Member("A", 0) }, true, Effects(ActiveRelicEffectIds.DamageBoostThisTurn));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(battle.ConsumableSlots[3].IsUsed, Is.False);
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void InvalidSlot_DoesNotInvokeEffect(int slot)
    {
        var command = Command(); command.SlotIndex = slot;
        bool invoked = false;
        var result = BattleConsumableService.TryUse(Battle(), command, true, () => invoked = true);
        Assert.That(result.RejectReason, Is.EqualTo(ConsumableRejectReason.InvalidSlot));
        Assert.That(invoked, Is.False);
    }

    [Test]
    public void WrongPhaseOrItem_DoesNotInvokeEffect()
    {
        var battle = Battle(); var command = Command(); bool invoked = false;
        Assert.That(BattleConsumableService.TryUse(battle, command, false, () => invoked = true).RejectReason,
            Is.EqualTo(ConsumableRejectReason.NotReservationPhase));
        command.ItemId = "Other";
        Assert.That(BattleConsumableService.TryUse(battle, command, true, () => invoked = true).RejectReason,
            Is.EqualTo(ConsumableRejectReason.ItemMismatch));
        Assert.That(invoked, Is.False);
        Assert.That(battle.ConsumableSlots[3].IsUsed, Is.False);
    }

    [TestCase(ActiveRelicEffectIds.MoveToGrid)]
    [TestCase(ActiveRelicEffectIds.SwapAlly)]
    public void Movement_RequiresLivingPartyTarget(string effect)
    {
        var battle = Battle(); var command = Command(); command.TargetGridIndex = 7;
        command.TargetCharacterId = "Outsider";
        Assert.That(BattleConsumableService.TryUseParty(battle, command, new[] { Member("A") }, true, Effects(effect)).Succeeded, Is.False);
        command.TargetCharacterId = "A";
        Assert.That(BattleConsumableService.TryUseParty(battle, command, new[] { Member("A", 0) }, true, Effects(effect)).Succeeded, Is.False);
        Assert.That(battle.ConsumableSlots[3].IsUsed, Is.False);
    }

    [Test]
    public void PartyRoster_ExcludesBenchAndMissingIds_DeduplicatesSlots()
    {
        var party = new PartyRuntimeStore();
        party.SetCharacter(0, "A"); party.SetCharacter(1, "A"); party.SetCharacter(2, "Missing");
        var characters = new CharacterRuntimeStore(); var active = Member("A");
        characters.SetAll(new[] { active, Member("Bench") });
        Assert.That(BattleConsumableService.GetPartyMembers(party, characters), Is.EqualTo(new[] { active }));
    }
}
