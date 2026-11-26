using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleConsumableServiceTests
{
    [Test]
    public void TryUse_RejectsInvalidRequestsWithoutChangingSlot()
    {
        BattleRuntimeData battle = CreateBattle("Compound_01");
        CharacterRuntimeData actor = new() { CharacterId = "Char_01", CurrentHP = 10 };

        UseConsumableResult wrongPhase = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_01"), actor, false, () => true);
        UseConsumableResult wrongItem = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_02"), actor, true, () => true);

        Assert.That(wrongPhase.RejectReason, Is.EqualTo(ConsumableRejectReason.NotReservationPhase));
        Assert.That(wrongItem.RejectReason, Is.EqualTo(ConsumableRejectReason.ItemMismatch));
        Assert.That(battle.ConsumableSlots[0].IsUsed, Is.False);
    }

    [Test]
    public void TryUse_MarksSlotOnlyAfterEffectSucceeds()
    {
        BattleRuntimeData battle = CreateBattle("Compound_01");
        CharacterRuntimeData actor = new() { CharacterId = "Char_01", CurrentHP = 10 };

        UseConsumableResult failed = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_01"), actor, true, () => false);
        UseConsumableResult succeeded = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_01"), actor, true, () => true);
        UseConsumableResult duplicate = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_01"), actor, true, () => true);

        Assert.That(failed.RejectReason, Is.EqualTo(ConsumableRejectReason.EffectRejected));
        Assert.That(succeeded.Succeeded, Is.True);
        Assert.That(succeeded.IsUsed, Is.True);
        Assert.That(duplicate.RejectReason, Is.EqualTo(ConsumableRejectReason.AlreadyUsed));
        Assert.That(battle.ConsumableSlots[0].IsUsed, Is.True);
    }

    [Test]
    public void TryUse_RejectsDeadOrMismatchedActor()
    {
        BattleRuntimeData battle = CreateBattle("Compound_01");
        CharacterRuntimeData dead = new() { CharacterId = "Char_01", CurrentHP = 0 };

        UseConsumableResult deadResult = BattleConsumableService.TryUse(
            battle, Command("Char_01", 0, "Compound_01"), dead, true, () => true);
        UseConsumableResult mismatch = BattleConsumableService.TryUse(
            battle, Command("Char_02", 0, "Compound_01"),
            new CharacterRuntimeData { CharacterId = "Char_01", CurrentHP = 10 },
            true, () => true);

        Assert.That(deadResult.RejectReason, Is.EqualTo(ConsumableRejectReason.InvalidActor));
        Assert.That(mismatch.RejectReason, Is.EqualTo(ConsumableRejectReason.InvalidActor));
        Assert.That(battle.ConsumableSlots[0].IsUsed, Is.False);
    }

    private static BattleRuntimeData CreateBattle(string itemId)
    {
        BattleRuntimeData battle = new();
        PartyConsumableRuntime.NormalizeBattle(battle);
        battle.ConsumableSlots[0].ItemId = itemId;
        return battle;
    }

    private static UseConsumableCommand Command(
        string actorId,
        int slotIndex,
        string itemId) => new()
    {
        RequestId = "request",
        ActorCharacterId = actorId,
        SlotIndex = slotIndex,
        ItemId = itemId,
        TargetGridIndex = -1
    };
}
