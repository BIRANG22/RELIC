using System;
using Relic.Gameplay.Data;

[Serializable]
public sealed class UseConsumableCommand
{
    public string RequestId;
    public string ActorCharacterId;
    public int SlotIndex = -1;
    public string ItemId;
    public int TargetGridIndex = -1;
}

public enum ConsumableRejectReason
{
    None,
    NotReservationPhase,
    InvalidSlot,
    EmptySlot,
    AlreadyUsed,
    ItemMismatch,
    InvalidActor,
    EffectRejected
}

public readonly struct UseConsumableResult
{
    private UseConsumableResult(
        bool succeeded,
        ConsumableRejectReason rejectReason,
        int slotIndex,
        string itemId,
        bool isUsed)
    {
        Succeeded = succeeded;
        RejectReason = rejectReason;
        SlotIndex = slotIndex;
        ItemId = itemId ?? string.Empty;
        IsUsed = isUsed;
    }

    public bool Succeeded { get; }
    public ConsumableRejectReason RejectReason { get; }
    public int SlotIndex { get; }
    public string ItemId { get; }
    public bool IsUsed { get; }

    public static UseConsumableResult Success(int slotIndex, string itemId) =>
        new(true, ConsumableRejectReason.None, slotIndex, itemId, true);

    public static UseConsumableResult Reject(
        ConsumableRejectReason reason,
        int slotIndex = -1,
        string itemId = null,
        bool isUsed = false) =>
        new(false, reason, slotIndex, itemId, isUsed);
}

public static class BattleConsumableService
{
    public static UseConsumableResult TryUse(
        BattleRuntimeData battle,
        UseConsumableCommand command,
        CharacterRuntimeData actor,
        bool isReservationPhase,
        Func<bool> applyEffect)
    {
        if (!isReservationPhase)
            return UseConsumableResult.Reject(ConsumableRejectReason.NotReservationPhase);
        if (battle == null || command == null ||
            command.SlotIndex < 0 || command.SlotIndex >= PartyConsumableRuntime.SlotCount)
            return UseConsumableResult.Reject(ConsumableRejectReason.InvalidSlot);
        if (actor == null || actor.IsDead ||
            string.IsNullOrWhiteSpace(command.ActorCharacterId) ||
            !string.Equals(actor.CharacterId, command.ActorCharacterId, StringComparison.Ordinal))
            return UseConsumableResult.Reject(ConsumableRejectReason.InvalidActor, command.SlotIndex);

        PartyConsumableRuntime.NormalizeBattle(battle);
        BattleConsumableSlotRuntimeData slot = battle.ConsumableSlots[command.SlotIndex];
        string slotItemId = slot.ItemId?.Trim() ?? string.Empty;
        string requestedItemId = command.ItemId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(slotItemId))
            return UseConsumableResult.Reject(ConsumableRejectReason.EmptySlot, command.SlotIndex);
        if (slot.IsUsed)
            return UseConsumableResult.Reject(ConsumableRejectReason.AlreadyUsed, command.SlotIndex, slotItemId, true);
        if (!string.Equals(slotItemId, requestedItemId, StringComparison.Ordinal))
            return UseConsumableResult.Reject(ConsumableRejectReason.ItemMismatch, command.SlotIndex, slotItemId);
        if (applyEffect == null || !applyEffect())
            return UseConsumableResult.Reject(ConsumableRejectReason.EffectRejected, command.SlotIndex, slotItemId);

        slot.IsUsed = true;
        return UseConsumableResult.Success(command.SlotIndex, slotItemId);
    }
}
