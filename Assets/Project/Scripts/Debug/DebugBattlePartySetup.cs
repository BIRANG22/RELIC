using System;
using System.Linq;
using Relic.Gameplay.Data;
using UnityEngine;

public static class DebugBattlePartySetup
{
    private const int DebugPartySize = 3;
    private const string DefaultMoveSkillId = "S_Move_1";

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
            .Take(DebugPartySize)
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
            CharacterRuntimeData runtime = CreateRuntimeData(master, relicDatabase);

            characterStore.AddOrUpdate(runtime);

            if (!partyStore.SetSlot(slotIndex, master.CharacterId, slotIndex))
            {
                characterStore.Clear();
                partyStore.Clear();
                Debug.LogError($"[DebugBattlePartySetup] Failed to configure party slot {slotIndex}.");
                return false;
            }
        }

        Debug.Log($"[DebugBattlePartySetup] Created default debug party with {defaultCharacters.Length} character(s).");
        return true;
    }

    private static CharacterRuntimeData CreateRuntimeData(
        CharacterMasterData master,
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
}
