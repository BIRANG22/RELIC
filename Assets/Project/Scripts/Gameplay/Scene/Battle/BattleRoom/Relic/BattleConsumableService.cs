using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

[Serializable]
public sealed class UseConsumableCommand
{
    public string RequestId;
    public string TargetCharacterId;
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
    public static List<CharacterRuntimeData> GetPartyMembers(PartyRuntimeStore party, CharacterRuntimeStore characters)
    {
        List<CharacterRuntimeData> members = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        if (party == null || characters == null) return members;
        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            string id = party.GetCharacterId(i);
            if (!string.IsNullOrWhiteSpace(id) && seen.Add(id) && characters.TryGet(id, out var member) && member != null)
                members.Add(member);
        }
        return members;
    }

    public static bool ApplyToLivingParty(IReadOnlyList<CharacterRuntimeData> party, Func<CharacterRuntimeData, bool> apply)
    {
        if (party == null || apply == null) return false;
        HashSet<string> seen = new(StringComparer.Ordinal);
        bool succeeded = false;
        foreach (var member in party)
            if (member != null && !member.IsDead && !string.IsNullOrWhiteSpace(member.CharacterId) && seen.Add(member.CharacterId))
                succeeded |= apply(member);
        return succeeded;
    }

    public static UseConsumableResult TryUseParty(
        BattleRuntimeData battle, UseConsumableCommand command,
        IReadOnlyList<CharacterRuntimeData> party, bool isReservationPhase,
        ActiveRelicService effectService, GridManager gridManager = null,
        BattleGridEffectController gridEffects = null, PartyRuntimeStore partyPositions = null)
    {
        return TryUse(battle, command, isReservationPhase, () =>
        {
            if (effectService == null) return false;
            var availability = effectService.GetConsumableAvailability(command.ItemId);
            if (!availability.CanUse) return false;
            if (availability.TargetMode == ActiveRelicTargetMode.Self)
                return ApplyToLivingParty(party, member => effectService.TryUseConsumableImmediate(member, command.ItemId).Succeeded);
            CharacterRuntimeData target = null;
            bool hasLivingMember = false;
            if (party != null)
                foreach (var member in party)
                    if (member != null && !member.IsDead)
                    {
                        hasLivingMember = true;
                        if (string.Equals(member.CharacterId, command.TargetCharacterId, StringComparison.Ordinal)) target = member;
                    }
            if (!hasLivingMember || command.TargetGridIndex < 0) return false;
            if (availability.EffectId == ActiveRelicEffectIds.MoveToGrid || availability.EffectId == ActiveRelicEffectIds.SwapAlly)
            {
                if (target == null) return false;
                if (availability.EffectId == ActiveRelicEffectIds.SwapAlly)
                {
                    var partyStore = partyPositions;
                    bool validDestination = false;
                    if (partyStore != null)
                        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
                            foreach (var member in party)
                                if (member != null && !member.IsDead && member.CharacterId != target.CharacterId &&
                                    member.CharacterId == partyStore.GetCharacterId(i) && partyStore.GetCurrentGridIndex(i) == command.TargetGridIndex)
                                    validDestination = true;
                    if (!validDestination) return false;
                }
            }
            return effectService.TryUseConsumableTarget(target, command.ItemId, command.TargetGridIndex, gridManager, gridEffects).Succeeded;
        });
    }

    public static UseConsumableResult TryUse(
        BattleRuntimeData battle,
        UseConsumableCommand command,
        bool isReservationPhase,
        Func<bool> applyEffect)
    {
        if (!isReservationPhase)
            return UseConsumableResult.Reject(ConsumableRejectReason.NotReservationPhase);
        if (battle == null || command == null ||
            command.SlotIndex < 0 || command.SlotIndex >= PartyConsumableRuntime.SlotCount)
            return UseConsumableResult.Reject(ConsumableRejectReason.InvalidSlot);
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
