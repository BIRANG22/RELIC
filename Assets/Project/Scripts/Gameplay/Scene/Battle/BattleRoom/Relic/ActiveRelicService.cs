using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class ActiveRelicAvailability
{
    public bool CanUse;
    public string Message;
    public string RelicId;
    public RelicData RelicData;
    public string EffectId;
    public ActiveRelicTargetMode TargetMode;
    public int RemainingUses;
    public int MaxUses;

    public bool RequiresTarget => false;
}

public sealed class ActiveRelicUseResult
{
    public bool Succeeded;
    public bool ConsumedUse;
    public string Message;
    public ActiveRelicAvailability Availability;

    public static ActiveRelicUseResult Success(
        ActiveRelicAvailability availability,
        bool consumedUse = true,
        string message = "")
    {
        return new ActiveRelicUseResult
        {
            Succeeded = true,
            ConsumedUse = consumedUse,
            Message = message,
            Availability = availability
        };
    }

    public static ActiveRelicUseResult Fail(
        ActiveRelicAvailability availability,
        string message)
    {
        return new ActiveRelicUseResult
        {
            Succeeded = false,
            ConsumedUse = false,
            Message = message,
            Availability = availability
        };
    }
}

public sealed class ActiveRelicService
{
    private readonly CompoundDatabase compoundDatabase;

    public ActiveRelicService(CompoundDatabase compoundDatabase)
    {
        this.compoundDatabase = compoundDatabase;
    }

    // Character-owned active relics are retired; only party consumable slots can execute.
    public ActiveRelicAvailability GetAvailability(CharacterRuntimeData runtime) => new();

    public ActiveRelicAvailability GetConsumableAvailability(string itemId)
    {
        var availability = new ActiveRelicAvailability { RelicId = itemId?.Trim() ?? string.Empty };
        if (compoundDatabase == null || !compoundDatabase.TryGet(availability.RelicId, out CompoundData compound) || compound == null)
            return availability;
        availability.RelicData = compound;
        availability.TargetMode = ActiveRelicTargetMode.Party;
        var entries = compound.EffectEntries;
        if (entries == null || entries.Count == 0) return availability;
        foreach (var entry in entries)
            if (entry == null || entry.EffectData == null || !consumableRegistry.TryGet(entry.EffectId, out _)) return availability;
        availability.CanUse = true;
        availability.EffectId = entries[0].EffectId;
        return availability;
    }

    private readonly BattleEffectRegistry consumableRegistry = new();

    public bool TryUsePartyEffects(string itemId, IReadOnlyList<CharacterRuntimeData> party)
    {
        var availability = GetConsumableAvailability(itemId);
        if (!availability.CanUse || party == null || party.Count == 0) return false;
        var units = Object.FindObjectsByType<BattleCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var contexts = new List<BattleEffectContext>();
        var effects = new List<BattleEffectBase>();
        foreach (var entry in availability.RelicData.EffectEntries)
        {
            if (!consumableRegistry.TryGet(entry.EffectId, out var effect)) return false;
            if (effect.IsPartyShared)
            {
                var context = new BattleEffectContext { EffectId = entry.EffectId, Value = entry.ValueAmount, Count = entry.CountAmount };
                if (effect.CanApply(context)) { contexts.Add(context); effects.Add(effect); }
                continue;
            }
            var seen = new HashSet<CharacterRuntimeData>();
            foreach (var member in party)
            {
                if (member == null || !seen.Add(member) || (member.IsDead && !effect.AcceptsDeadTarget)) continue;
                BattleCharacter unit = null;
                foreach (var candidate in units)
                    if (candidate != null && candidate.RuntimeData == member) { unit = candidate; break; }
                if (unit == null) continue;
                var context = new BattleEffectContext { PlayerTarget = unit, EffectId = entry.EffectId, Value = entry.ValueAmount, Count = entry.CountAmount };
                if (effect.CanApply(context)) { contexts.Add(context); effects.Add(effect); }
            }
        }
        if (contexts.Count == 0) return false;
        for (int i = 0; i < contexts.Count; i++) effects[i].Execute(contexts[i]);
        return true;
    }

    public ActiveRelicUseResult TryUseImmediate(CharacterRuntimeData runtime) =>
        ActiveRelicUseResult.Fail(GetAvailability(runtime), string.Empty);

    public ActiveRelicUseResult TryUseTarget(CharacterRuntimeData runtime, int gridIndex,
        GridManager gridManager, BattleGridEffectController gridEffectController) =>
        ActiveRelicUseResult.Fail(GetAvailability(runtime), string.Empty);
}
