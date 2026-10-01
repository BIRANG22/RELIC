using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;
using UnityEngine;

/// <summary>선택된 침식 ID를 전투 계산용 값으로 해석하는 단일 진입점입니다.</summary>
public static class BattleErosionEffectService
{
    // 0번 액티브 슬롯 뒤에 1~6번 패시브 유물 슬롯이 이어지며 Artifact06은 인덱스 6입니다.
    public const int LastRelicSlotIndex = ActiveRelicRuntimeUtility.EquippedRelicSlotCount - 1;
    private static readonly string[] PeriodicDebuffIds =
        { "E_Poison", "E_Bleed", "E_Vulnerable", "E_Weaken", "E_Corrosion", "E_Fatigue" };
    public const string MonsterMaxHPPercent = "MonsterMaxHPPercent";
    public const string MonsterDamagePercent = "MonsterDamagePercent";
    public const string PartyMaxHPPercent = "PartyMaxHPPercent";
    public const string PeriodicRandomDebuffTurns = "PeriodicRandomDebuffTurns";
    public const string ShopPricePercent = "ShopPricePercent";
    public const string DiceDifficultyAdd = "DiceDifficultyAdd";
    public const string RemoveRestRoom = "RemoveRestRoom";
    public const string MapVisionLimit = "MapVisionLimit";
    public const string RarityChanceReduction = "RarityChanceReduction";
    public const string CompoundUseLimit = "CompoundUseLimit";
    public const string LockLastRelicSlot = "LockLastRelicSlot";
    public const string MoveFinalManaCostAdd = "MoveFinalManaCostAdd";

    public static int GetValue(string effectType)
    {
        if (string.IsNullOrWhiteSpace(effectType)) return 0;
        DataManager dm = DataManager.Instance;
        BattleRuntimeData battle = dm?.BattleRuntimeStore?.Get();
        return ResolveValue(battle?.SelectedErosionDifficultyIds, dm?.ErosionDatabase, effectType);
    }

    public static int ResolveValue(IEnumerable<string> selectedIds, ErosionDatabase database, string effectType)
    {
        if (selectedIds == null || database == null || string.IsNullOrWhiteSpace(effectType)) return 0;
        var exclusive = new Dictionary<string, ErosionData>(StringComparer.OrdinalIgnoreCase);
        int independentTotal = 0;
        foreach (string id in selectedIds)
        {
            if (!database.TryGet(id, out ErosionData data) || data == null || !data.Selectable ||
                !string.Equals(data.EffectType, effectType, StringComparison.OrdinalIgnoreCase)) continue;
            if (data.IsIndependent) { independentTotal += data.EffectValue; continue; }
            string group = string.IsNullOrWhiteSpace(data.GroupId) ? data.DifficultyId : data.GroupId;
            if (!exclusive.TryGetValue(group, out ErosionData current) || data.Tier > current.Tier)
                exclusive[group] = data;
        }
        foreach (ErosionData data in exclusive.Values) independentTotal += data.EffectValue;
        return independentTotal;
    }

    public static int ApplyPercent(int baseValue, int percent, int minimum = 0)
    {
        return Mathf.Max(minimum, Mathf.RoundToInt(baseValue * (100f + percent) / 100f));
    }

    public static int ModifyMonsterMaxHP(int baseValue) =>
        ApplyPercent(baseValue, GetValue(MonsterMaxHPPercent), 1);
    public static int ModifyMonsterDamage(int baseValue) =>
        ApplyPercent(baseValue, GetValue(MonsterDamagePercent), 1);
    public static int ModifyShopPrice(int baseValue) =>
        ApplyPercent(baseValue, GetValue(ShopPricePercent), 0);
    public static int ModifyDiceDifficulty(int baseValue) =>
        Mathf.Max(0, baseValue + GetValue(DiceDifficultyAdd));
    public static int ModifyMoveFinalCost(int baseValue) =>
        Mathf.Max(0, baseValue + GetValue(MoveFinalManaCostAdd));
    public static bool ShouldRemoveRestRoom => GetValue(RemoveRestRoom) > 0;
    public static bool ShouldLimitMapVision => GetValue(MapVisionLimit) > 0;
    public static bool ShouldReduceRarity => GetValue(RarityChanceReduction) > 0;
    public static bool IsCompoundUseBlocked => GetValue(CompoundUseLimit) > 0;
    public static bool IsLastRelicSlotLocked => GetValue(LockLastRelicSlot) > 0;

    public static bool IsRelicSlotLocked(int slotIndex) =>
        IsRelicSlotLocked(slotIndex, IsLastRelicSlotLocked);

    public static bool IsRelicSlotLocked(int slotIndex, bool lockLastRelicSlot) =>
        lockLastRelicSlot && slotIndex == LastRelicSlotIndex;

    public static void ApplyPartyStartEffects(CharacterRuntimeStore characters, BattleRuntimeData battle)
    {
        if (characters == null || battle == null || battle.ErosionPartyStartEffectsApplied) return;
        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters.GetAll())
        {
            CharacterRuntimeData character = pair.Value;
            if (character == null) continue;
            if (IsRelicSlotLocked(LastRelicSlotIndex) &&
                character.EquippedRelicIds != null &&
                character.EquippedRelicIds.Length > LastRelicSlotIndex)
            {
                character.EquippedRelicIds[LastRelicSlotIndex] = string.Empty;
            }
        }
        battle.ErosionPartyStartEffectsApplied = true;
    }

    public static void ApplyPeriodicRandomDebuff(int turnNumber)
    {
        int interval = GetValue(PeriodicRandomDebuffTurns);
        if (interval <= 0 || turnNumber <= 0 || turnNumber % interval != 0) return;
        DataManager dm = DataManager.Instance;
        if (dm?.CharacterRuntimeStore == null || dm.EffectDatabase == null) return;
        var candidates = new List<string>();
        for (int i = 0; i < PeriodicDebuffIds.Length; i++)
            if (dm.EffectDatabase.Contains(PeriodicDebuffIds[i])) candidates.Add(PeriodicDebuffIds[i]);
        if (candidates.Count == 0) return;
        string effectId = candidates[BattleRandom.Range(0, candidates.Count)];
        foreach (KeyValuePair<string, CharacterRuntimeData> pair in dm.CharacterRuntimeStore.GetAll())
        {
            CharacterRuntimeData character = pair.Value;
            if (character == null || character.IsDead) continue;
            character.StatusEffects ??= new List<StatusEffectRuntimeData>();
            StatusEffectRuntimeData existing = character.StatusEffects.Find(x => x != null && x.EffectId == effectId);
            if (existing != null) { existing.Stack++; existing.TurnCount = Mathf.Max(1, existing.TurnCount); }
            else character.StatusEffects.Add(new StatusEffectRuntimeData(effectId, 1));
        }
    }
}
