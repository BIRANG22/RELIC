using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BattleEffectDebugPreset
{
    public BattleEffectDebugPreset(
        string key,
        string label,
        string[] relicIds,
        string[] runeIds)
    {
        Key = string.IsNullOrWhiteSpace(key) ? "Preset" : key.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? Key : label.Trim();
        RelicIds = relicIds ?? Array.Empty<string>();
        RuneIds = runeIds ?? Array.Empty<string>();
    }

    public string Key { get; }
    public string Label { get; }
    public string[] RelicIds { get; }
    public string[] RuneIds { get; }
}

public sealed class BattleDebugMonsterEntry
{
    public string RuntimeId;
    public string MonsterId;
    public string Name;
    public int GridIndex;
}

public static class BattleEffectDebugTool
{
    public const int SkillDisplaySlotCount = 5;
    public const int RuneSlotCount = 12;
    public const int PassiveRelicSlotCount = 6;
    public const int BattleGridCellCount = 35;

    private const int SkillRuntimeSlotCount = 4;
    private static readonly BattleEffectDebugPreset[] DefaultPresets = Array.Empty<BattleEffectDebugPreset>();

    public static IReadOnlyList<BattleEffectDebugPreset> GetDefaultPresets()
    {
        return DefaultPresets;
    }

    public static void ApplyPreset(CharacterRuntimeData runtime, BattleEffectDebugPreset preset)
    {
        if (runtime == null || preset == null)
            return;

        EquipOnlyRelics(runtime, preset.RelicIds);
        EquipOnlyRunes(runtime, preset.RuneIds);
        ClearEquipmentEffectState(runtime);
    }

    public static void EquipOnlyRelics(CharacterRuntimeData runtime, IReadOnlyList<string> relicIds)
    {
        if (runtime == null)
            return;

        runtime.EquippedRelicIds = new string[ActiveRelicRuntimeUtility.EquippedRelicSlotCount];

        if (relicIds == null)
            return;

        int passiveSlotIndex = 0;

        for (int i = 0; i < relicIds.Count; i++)
        {
            string relicId = NormalizeId(relicIds[i]);

            if (string.IsNullOrWhiteSpace(relicId) || IsCompoundId(relicId))
                continue;

            if (passiveSlotIndex >= PassiveRelicSlotCount)
                break;

            SetPassiveRelicSlot(runtime, passiveSlotIndex, relicId);
            passiveSlotIndex++;
        }
    }

    public static void EquipOnlyRunes(CharacterRuntimeData runtime, IReadOnlyList<string> runeIds)
    {
        if (runtime == null)
            return;

        runtime.EquippedRuneIds = new string[RuneSlotCount];

        if (runeIds == null)
            return;

        int slotIndex = 0;

        for (int i = 0; i < runeIds.Count && slotIndex < runtime.EquippedRuneIds.Length; i++)
        {
            string runeId = NormalizeId(runeIds[i]);

            if (string.IsNullOrWhiteSpace(runeId))
                continue;

            runtime.EquippedRuneIds[slotIndex] = runeId;
            slotIndex++;
        }
    }

    public static bool SetSkillDisplaySlot(
        CharacterRuntimeData runtime,
        int displaySlotIndex,
        string skillId)
    {
        if (runtime == null ||
            displaySlotIndex < 0 ||
            displaySlotIndex >= SkillDisplaySlotCount)
        {
            return false;
        }

        EnsureSkillSlots(runtime);
        string normalizedSkillId = NormalizeId(skillId);

        switch (displaySlotIndex)
        {
            case 0:
                runtime.AbilitySkillId = normalizedSkillId;
                runtime.EquippedSkillIds[1] = normalizedSkillId;
                break;

            case 1:
                runtime.EquippedSkillIds[2] = normalizedSkillId;
                break;

            case 2:
                runtime.EquippedSkillIds[3] = normalizedSkillId;
                break;

            case 3:
                runtime.UniqueSkillId = normalizedSkillId;
                runtime.EquippedSkillIds[0] = normalizedSkillId;
                break;

            case 4:
                runtime.PassiveSkillId = normalizedSkillId;
                BattlePassiveSkillService.RefreshRuntimePassiveEffects(runtime);
                break;
        }

        return true;
    }

    public static string GetSkillDisplaySlotId(CharacterRuntimeData runtime, int displaySlotIndex)
    {
        if (runtime == null)
            return string.Empty;

        return displaySlotIndex switch
        {
            0 => runtime.AbilitySkillId ?? string.Empty,
            1 => GetRuntimeSkillSlotId(runtime, 2),
            2 => GetRuntimeSkillSlotId(runtime, 3),
            3 => runtime.UniqueSkillId ?? string.Empty,
            4 => runtime.PassiveSkillId ?? string.Empty,
            _ => string.Empty
        };
    }

    public static bool SetPassiveRelicSlot(
        CharacterRuntimeData runtime,
        int passiveSlotIndex,
        string relicId)
    {
        if (runtime == null ||
            passiveSlotIndex < 0 ||
            passiveSlotIndex >= PassiveRelicSlotCount)
        {
            return false;
        }

        string normalizedRelicId = NormalizeId(relicId);

        if (!string.IsNullOrWhiteSpace(normalizedRelicId) && IsCompoundId(normalizedRelicId))
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        runtime.EquippedRelicIds[passiveSlotIndex + 1] = normalizedRelicId;
        ClearEquipmentEffectState(runtime);
        return true;
    }

    public static string GetPassiveRelicSlotId(CharacterRuntimeData runtime, int passiveSlotIndex)
    {
        if (runtime == null ||
            passiveSlotIndex < 0 ||
            passiveSlotIndex >= PassiveRelicSlotCount)
        {
            return string.Empty;
        }

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        return runtime.EquippedRelicIds[passiveSlotIndex + 1] ?? string.Empty;
    }

    public static bool SetCompoundSlot(CharacterRuntimeData runtime, string compoundId)
    {
        if (runtime == null)
            return false;

        string normalizedCompoundId = NormalizeId(compoundId);
        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        runtime.EquippedRelicIds[ActiveRelicRuntimeUtility.ActiveRelicSlotIndex] = normalizedCompoundId;
        ClearEquipmentEffectState(runtime);
        return true;
    }

    public static string GetCompoundSlotId(CharacterRuntimeData runtime)
    {
        return ActiveRelicRuntimeUtility.GetActiveRelicId(runtime) ?? string.Empty;
    }

    public static bool SetRuneSlot(CharacterRuntimeData runtime, int slotIndex, string runeId)
    {
        if (runtime == null || slotIndex < 0 || slotIndex >= RuneSlotCount)
            return false;

        EnsureRuneSlots(runtime);
        runtime.EquippedRuneIds[slotIndex] = NormalizeId(runeId);
        ClearEquipmentEffectState(runtime);
        return true;
    }

    public static string GetRuneSlotId(CharacterRuntimeData runtime, int slotIndex)
    {
        if (runtime == null || slotIndex < 0 || slotIndex >= RuneSlotCount)
            return string.Empty;

        EnsureRuneSlots(runtime);
        return runtime.EquippedRuneIds[slotIndex] ?? string.Empty;
    }

    public static void AdjustCurrentHP(CharacterRuntimeData runtime, int delta)
    {
        if (runtime == null)
            return;

        int maxHp = Mathf.Max(1, runtime.MaxHP);
        runtime.CurrentHP = Mathf.Clamp(runtime.CurrentHP + delta, 1, maxHp);
    }

    public static void AdjustCurrentCost(CharacterRuntimeData runtime, int delta)
    {
        if (runtime == null)
            return;

        SetCurrentCost(runtime, runtime.CurrentCost + delta);
    }

    public static void AdjustCurrentShield(CharacterRuntimeData runtime, int delta)
    {
        if (runtime == null)
            return;

        runtime.CurrentShield = Mathf.Max(0, runtime.CurrentShield + delta);
    }

    public static void AdjustCostRecovery(CharacterRuntimeData runtime, int delta)
    {
        if (runtime == null)
            return;

        runtime.CostRecovery = Mathf.Max(0, runtime.CostRecovery + delta);
    }

    public static void SetHpPercent(CharacterRuntimeData runtime, float percent)
    {
        if (runtime == null)
            return;

        int maxHp = Mathf.Max(1, runtime.MaxHP);
        int hp = Mathf.RoundToInt(maxHp * Mathf.Clamp01(percent));
        runtime.CurrentHP = Mathf.Clamp(hp, 1, maxHp);
    }

    public static void SetCurrentCost(CharacterRuntimeData runtime, int cost)
    {
        if (runtime == null)
            return;

        runtime.CurrentCost = Mathf.Clamp(cost, 0, Mathf.Max(0, runtime.MaxCost));
    }

    public static void SetCurrentResource(CharacterRuntimeData runtime, int resource, int maxResource)
    {
        if (runtime == null)
            return;

        runtime.CurrentResource = Mathf.Clamp(resource, 0, Mathf.Max(0, maxResource));
    }

    public static void SetFullResources(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        SetCurrentCost(runtime, runtime.MaxCost);
        int maxResource = GetMaxResource(runtime);
        SetCurrentResource(runtime, maxResource, maxResource);
    }

    public static int GetMaxResource(CharacterRuntimeData runtime)
    {
        if (runtime == null ||
            DataManager.Instance == null ||
            DataManager.Instance.CharacterDatabase == null ||
            !DataManager.Instance.CharacterDatabase.TryGet(
                runtime.CharacterId,
                out CharacterMasterData masterData) ||
            masterData == null)
        {
            return Mathf.Max(0, runtime != null ? runtime.CurrentResource : 0);
        }

        return Mathf.Max(0, masterData.MaxResource);
    }

    public static bool AddOrStackStatus(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId,
        int stack,
        int turnCount)
    {
        if (statusEffects == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        string normalizedEffectId = effectId.Trim();
        int safeStack = Mathf.Max(1, stack);
        int safeTurnCount = Mathf.Max(1, turnCount);

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];

            if (status == null || status.EffectId != normalizedEffectId)
                continue;

            status.Stack += safeStack;
            status.TurnCount = Mathf.Max(status.TurnCount, safeTurnCount);
            return true;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = normalizedEffectId,
            Stack = safeStack,
            TurnCount = safeTurnCount
        });
        return true;
    }

    public static void AddStatusToAllMonsters(string effectId, int stack, int turnCount)
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            BattleEffectUtility.AddStatusToMonster(
                monster,
                effectId,
                Mathf.Max(1, stack),
                Mathf.Max(1, turnCount));
        }
    }

    public static void AddStatusToPlayer(
        CharacterRuntimeData runtime,
        string effectId,
        int stack,
        int turnCount)
    {
        if (runtime == null)
            return;

        runtime.StatusEffects ??= new List<StatusEffectRuntimeData>();
        AddOrStackStatus(runtime.StatusEffects, effectId, stack, turnCount);
    }

    public static bool TryPlaceGridEffect(int gridIndex, string gridEffectId)
    {
        BattleGridEffectController controller = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include);

        return controller != null && controller.TryPlaceEffect(gridIndex, gridEffectId);
    }

    public static bool TryRemoveGridEffect(int gridIndex)
    {
        BattleGridEffectController controller = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include);

        return controller != null && controller.TryRemoveEffect(gridIndex);
    }

    public static void RefreshBattle()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader != null)
            loader.RefreshBattleHUDs();

        new BattleHUDService().RefreshHUDs();
    }

    public static void ReloadBattleRoom()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader == null)
            return;

        loader.ResetLoadedStateForNextBattle(true);
        loader.RequestLoadBattle();
    }

    public static bool TryApplyPartyCharacter(int partyIndex, string characterId, int gridIndex)
    {
        if (DataManager.Instance == null)
            return false;

        bool configured = DebugBattlePartySetup.TrySetPartyCharacter(
            DataManager.Instance,
            partyIndex,
            characterId,
            gridIndex);

        if (!configured)
            return false;

        ReloadBattleRoom();
        RefreshBattle();
        return true;
    }

    public static List<MonsterMasterData> GetMonsterMasters()
    {
        List<MonsterMasterData> result = new();
        IReadOnlyDictionary<string, MonsterMasterData> all = DataManager.Instance?.MonsterDatabase?.GetAll();

        if (all == null)
            return result;

        foreach (KeyValuePair<string, MonsterMasterData> pair in all)
        {
            MonsterMasterData data = pair.Value;
            if (data == null || string.IsNullOrWhiteSpace(data.MonsterId) || data.BattlePrefab == null)
                continue;

            result.Add(data);
        }

        result.Sort((a, b) => string.Compare(a.MonsterId, b.MonsterId, StringComparison.Ordinal));
        return result;
    }

    public static bool TrySpawnMonster(string monsterId, int gridIndex, out string monsterRuntimeId)
    {
        monsterRuntimeId = string.Empty;
        string normalizedMonsterId = NormalizeId(monsterId);

        if (string.IsNullOrWhiteSpace(normalizedMonsterId) ||
            gridIndex < 0 ||
            gridIndex >= BattleGridCellCount ||
            DataManager.Instance?.MonsterDatabase == null)
        {
            return false;
        }

        MonsterMasterData monsterData = DataManager.Instance.MonsterDatabase.Get(normalizedMonsterId);
        if (monsterData == null || monsterData.BattlePrefab == null)
            return false;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
        {
            Debug.LogWarning($"[BattleEffectDebug] Grid {gridIndex} is already occupied.");
            return false;
        }

        BattleMonsterSpawner spawner = Object.FindFirstObjectByType<BattleMonsterSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[BattleEffectDebug] BattleMonsterSpawner not found.");
            return false;
        }

        SpawnedMonsterResult spawned = spawner.SpawnRuntimeMonster(
            normalizedMonsterId,
            new List<int> { gridIndex });

        if (spawned?.RuntimeData == null)
            return false;

        monsterRuntimeId = spawned.RuntimeData.RuntimeId ?? string.Empty;
        RefreshBattle();
        return !string.IsNullOrWhiteSpace(monsterRuntimeId);
    }

    public static List<BattleDebugMonsterEntry> GetLiveMonsters()
    {
        List<BattleDebugMonsterEntry> result = new();
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            result.Add(new BattleDebugMonsterEntry
            {
                RuntimeId = monster.RuntimeData.RuntimeId ?? string.Empty,
                MonsterId = monster.RuntimeData.MonsterId ?? string.Empty,
                Name = monster.RuntimeData.GetDisplayName(),
                GridIndex = monster.MainGridIndex
            });
        }

        result.Sort((a, b) => string.Compare(a.RuntimeId, b.RuntimeId, StringComparison.Ordinal));
        return result;
    }

    public static List<MonsterSkillData> GetMonsterSkills(string monsterRuntimeId)
    {
        List<MonsterSkillData> result = new();
        MonsterUnit monster = FindMonsterUnit(monsterRuntimeId);
        if (monster == null || monster.RuntimeData == null || DataManager.Instance == null)
            return result;

        MonsterMasterData master = DataManager.Instance.MonsterDatabase?.Get(monster.RuntimeData.MonsterId);
        if (master == null)
            return result;

        string[] skillIds = master.GetPossibleSkillIds();
        for (int i = 0; i < skillIds.Length; i++)
        {
            MonsterSkillData skill = DataManager.Instance.MonsterSkillDatabase?.Get(skillIds[i]);
            if (skill != null)
                result.Add(skill);
        }

        return result;
    }

    public static bool TryQueueMonsterSkill(
        string monsterRuntimeId,
        string skillId,
        int slotIndex)
    {
        MonsterUnit monster = FindMonsterUnit(monsterRuntimeId);
        BattleTimelineController timeline = Object.FindFirstObjectByType<BattleTimelineController>(
            FindObjectsInactive.Include);
        GridManager gridManager = Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (monster == null || monster.RuntimeData == null || timeline == null || gridManager == null)
            return false;

        MonsterSkillData skillData = DataManager.Instance?.MonsterSkillDatabase?.Get(NormalizeId(skillId));
        if (skillData == null)
            return false;

        MonsterReservedCommand command = new(monster.RuntimeData, skillData);
        command.SetRangeOriginGridIndex(monster.MainGridIndex);
        command.SetForcedDirection(monster.RuntimeData.Direction);

        if (skillData.TimelineNotation == TimelineActionType.Move)
        {
            int forwardX = monster.RuntimeData.Direction == BattleDirection.Right ? 1 : -1;
            command.SetMoveOffset(new Vector2Int(forwardX, 0));
            command.SetUseRequestedMoveOffsetForExecution(true);
        }

        bool facingRight = monster.RuntimeData.Direction == BattleDirection.Right;
        List<int> range = MonsterSkillRangeService.BuildRangeGridIndices(
            monster,
            skillData,
            gridManager,
            facingRight,
            monster.MainGridIndex,
            DataManager.Instance?.RangeDatabase);
        List<int> targets = MonsterSkillRangeService.FilterTargetGridIndices(skillData, range);
        command.SetRangeResult(range, targets);

        timeline.AddMonsterCommand(Mathf.Clamp(slotIndex, 0, Mathf.Max(0, timeline.SlotCount - 1)), command);
        return true;
    }

    private static MonsterUnit FindMonsterUnit(string monsterRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(monsterRuntimeId))
            return null;

        string normalizedRuntimeId = monsterRuntimeId.Trim();
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster?.RuntimeData == null)
                continue;

            if (string.Equals(monster.RuntimeData.RuntimeId, normalizedRuntimeId, StringComparison.Ordinal))
                return monster;
        }

        return null;
    }

    public static bool TryApplySingleCharacterParty(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null)
            return false;

        bool configured = DebugBattlePartySetup.TryCreateSingleCharacterParty(
            DataManager.Instance,
            characterId,
            gridIndex);

        if (!configured)
            return false;

        ReloadBattleRoom();
        RefreshBattle();
        return true;
    }

    public static CharacterRuntimeData GetPartyRuntime(int partyIndex)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return null;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        return DataManager.Instance.CharacterRuntimeStore.TryGet(
            characterId,
            out CharacterRuntimeData runtime)
            ? runtime
            : null;
    }

    public static List<CharacterRuntimeData> GetPartyRuntimes()
    {
        List<CharacterRuntimeData> result = new();

        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return result;

        for (int i = 0; i < DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue; i++)
        {
            CharacterRuntimeData runtime = GetPartyRuntime(i);

            if (runtime != null)
                result.Add(runtime);
        }

        return result;
    }

    public static bool IsCompoundId(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        string normalizedRelicId = relicId.Trim();

        if (DataManager.Instance != null &&
            DataManager.Instance.CompoundDatabase != null &&
            DataManager.Instance.CompoundDatabase.TryGet(normalizedRelicId, out CompoundData compound) &&
            compound != null)
        {
            return true;
        }

        return normalizedRelicId.StartsWith("Compound_", StringComparison.OrdinalIgnoreCase) ||
               normalizedRelicId.StartsWith("Relic_A_", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRuntimeSkillSlotId(CharacterRuntimeData runtime, int equippedIndex)
    {
        EnsureSkillSlots(runtime);
        return runtime.EquippedSkillIds[equippedIndex] ?? string.Empty;
    }

    private static void EnsureSkillSlots(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        if (runtime.EquippedSkillIds != null &&
            runtime.EquippedSkillIds.Length == SkillRuntimeSlotCount)
        {
            return;
        }

        string[] normalized = new string[SkillRuntimeSlotCount];

        if (runtime.EquippedSkillIds != null)
        {
            int count = Mathf.Min(runtime.EquippedSkillIds.Length, normalized.Length);

            for (int i = 0; i < count; i++)
                normalized[i] = runtime.EquippedSkillIds[i];
        }

        runtime.EquippedSkillIds = normalized;
    }

    private static void EnsureRuneSlots(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        if (runtime.EquippedRuneIds != null &&
            runtime.EquippedRuneIds.Length == RuneSlotCount)
        {
            return;
        }

        string[] normalized = new string[RuneSlotCount];

        if (runtime.EquippedRuneIds != null)
        {
            int count = Mathf.Min(runtime.EquippedRuneIds.Length, normalized.Length);

            for (int i = 0; i < count; i++)
                normalized[i] = runtime.EquippedRuneIds[i];
        }

        runtime.EquippedRuneIds = normalized;
    }

    private static void ClearEquipmentEffectState(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();
        runtime.ActiveRelicUses.Clear();

        runtime.AppliedBattleEquipmentEffectIds ??= new List<string>();
        runtime.AppliedBattleEquipmentEffectIds.Clear();
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
