using System;
using System.Linq;
using Relic.Gameplay.Data;
using UnityEngine;

public static class DebugBattlePartySetup
{
    public const int DefaultDebugPartySize = 1;

    private const string DefaultMoveSkillId = "S_Move_1";
    private const string SkillVfxTestCharacterId = "Char_03";
    private const string SkillVfxTestSkillId = "S_Ability_11";
    private const int SkillVfxTestPreferredSlotIndex = 3;

    public static bool TryCreateDefaultParty(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[DebugBattlePartySetup] DataManager is missing.");
            return false;
        }

        return TryCreateDefaultParty(
            dataManager.CharacterDatabase,
            dataManager.CharacterRuntimeStore,
            dataManager.PartyRuntimeStore,
            dataManager.RelicDatabase);
    }

    public static bool TryCreateDefaultParty(
        CharacterDatabase characterDatabase,
        CharacterRuntimeStore characterStore,
        PartyRuntimeStore partyStore,
        RelicDatabase relicDatabase)
    {
        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[DebugBattlePartySetup] Required runtime stores are missing.");
            return false;
        }

        CharacterMasterData[] defaultCharacters = characterDatabase.GetAll().Values
            .Where(character => character != null &&
                                character.IsDefaultProvided &&
                                !string.IsNullOrWhiteSpace(character.CharacterId))
            .OrderBy(character => character.CharacterId, StringComparer.Ordinal)
            .Take(DefaultDebugPartySize)
            .ToArray();

        characterStore.Clear();
        partyStore.Clear();

        if (defaultCharacters.Length == 0)
        {
            Debug.LogError("[DebugBattlePartySetup] No default-provided characters were found.");
            return false;
        }

        for (int slotIndex = 0; slotIndex < defaultCharacters.Length; slotIndex++)
        {
            CharacterMasterData master = defaultCharacters[slotIndex];
            CharacterRuntimeData runtime = CreateDebugRuntime(master, slotIndex, relicDatabase);

            characterStore.AddOrUpdate(runtime);

            if (!partyStore.SetSlot(slotIndex, master.CharacterId, slotIndex))
            {
                characterStore.Clear();
                partyStore.Clear();
                Debug.LogError($"[DebugBattlePartySetup] Failed to configure party slot {slotIndex}.");
                return false;
            }
        }

        EnsureSkillVfxTestSkill(characterStore);

        Debug.Log($"[DebugBattlePartySetup] Created default debug party with {defaultCharacters.Length} character(s).");
        return true;
    }

    public static bool TryCreateSingleCharacterParty(
        DataManager dataManager,
        string characterId,
        int gridIndex)
    {
        if (dataManager == null)
        {
            Debug.LogError("[DebugBattlePartySetup] DataManager is missing.");
            return false;
        }

        return TryCreateSingleCharacterParty(
            dataManager.CharacterDatabase,
            dataManager.CharacterRuntimeStore,
            dataManager.PartyRuntimeStore,
            dataManager.RelicDatabase,
            characterId,
            gridIndex);
    }

    public static bool TryCreateSingleCharacterParty(
        CharacterDatabase characterDatabase,
        CharacterRuntimeStore characterStore,
        PartyRuntimeStore partyStore,
        RelicDatabase relicDatabase,
        string characterId,
        int gridIndex)
    {
        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[DebugBattlePartySetup] Required runtime stores are missing.");
            return false;
        }

        string normalizedCharacterId = string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCharacterId) ||
            !characterDatabase.TryGet(normalizedCharacterId, out CharacterMasterData master) ||
            master == null)
        {
            Debug.LogWarning($"[DebugBattlePartySetup] Character not found: {normalizedCharacterId}");
            return false;
        }

        characterStore.Clear();
        partyStore.Clear();

        CharacterRuntimeData runtime = CreateDebugRuntime(master, gridIndex, relicDatabase);
        characterStore.AddOrUpdate(runtime);

        if (!partyStore.SetSlot(0, master.CharacterId, Mathf.Clamp(gridIndex, 0, 34)))
        {
            characterStore.Clear();
            partyStore.Clear();
            Debug.LogError("[DebugBattlePartySetup] Failed to configure debug party slot 0.");
            return false;
        }

        EnsureSkillVfxTestSkill(characterStore);
        return true;
    }

    public static bool EnsureSkillVfxTestSkill(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[DebugBattlePartySetup] DataManager is missing.");
            return false;
        }

        return EnsureSkillVfxTestSkill(dataManager.CharacterRuntimeStore);
    }

    public static bool EnsureSkillVfxTestSkill(CharacterRuntimeStore characterStore)
    {
        if (characterStore == null)
        {
            Debug.LogError("[DebugBattlePartySetup] CharacterRuntimeStore is missing.");
            return false;
        }

        if (!characterStore.TryGet(SkillVfxTestCharacterId, out CharacterRuntimeData runtime) ||
            runtime == null)
        {
            return false;
        }

        runtime.EquippedSkillIds = EnsureEquippedSkill(
            runtime.EquippedSkillIds,
            SkillVfxTestSkillId,
            SkillVfxTestPreferredSlotIndex);

        return true;
    }

    public static CharacterRuntimeData CreateDebugRuntime(CharacterMasterData master, int gridIndex)
    {
        return CreateDebugRuntime(master, gridIndex, null);
    }

    public static CharacterRuntimeData CreateDebugRuntime(
        CharacterMasterData master,
        int gridIndex,
        RelicDatabase relicDatabase)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,
            MaxHP = master.MaxHP,
            MaxCost = master.MaxCost,
            CostRecovery = master.CostRecovery,
            CurrentHP = master.MaxHP,
            CurrentCost = master.MaxCost,
            CurrentResource = 0,
            CurrentMoveLevel = 0,
            IsUnlocked = true,
            MoveSkillId = DefaultMoveSkillId,
            PassiveSkillId = master.PassiveSkill1,
            UniqueSkillId = master.UniqueSkill1,
            AbilitySkillId = master.CharacterSkill1,
            EquippedSkillIds = new[]
            {
                master.UniqueSkill1,
                master.CharacterSkill1,
                master.CommonSkill1,
                string.Empty
            },
            EquippedRuneIds = new string[12],
            EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master)
        };

        CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
        return runtime;
    }

    private static string[] EnsureEquippedSkill(
        string[] equippedSkillIds,
        string skillId,
        int preferredSlotIndex)
    {
        int requiredLength = Mathf.Max(preferredSlotIndex + 1, equippedSkillIds != null ? equippedSkillIds.Length : 0);
        string[] result = new string[requiredLength];

        if (equippedSkillIds != null)
            Array.Copy(equippedSkillIds, result, equippedSkillIds.Length);

        for (int i = 0; i < result.Length; i++)
        {
            if (string.Equals(result[i], skillId, StringComparison.Ordinal))
                return result;
        }

        for (int i = 0; i < result.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(result[i]))
            {
                result[i] = skillId;
                return result;
            }
        }

        result[Mathf.Clamp(preferredSlotIndex, 0, result.Length - 1)] = skillId;
        return result;
    }
}
