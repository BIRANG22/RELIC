using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbySharedStateCharacterRuntimeUtility
{
    private const int EquippedSkillSlotCount = 4;
    private const int EquippedRelicSlotCount = 7;

    public static void ApplyLobbyLoadouts(
        LobbyRuntimeData lobby,
        PartyRuntimeStore partyStore,
        CharacterRuntimeStore characterStore,
        CharacterDatabase characterDatabase,
        RelicDatabase relicDatabase)
    {
        if (characterStore == null)
            return;

        EnsurePartyCharacterRuntimes(
            partyStore,
            characterStore,
            characterDatabase,
            relicDatabase);

        if (lobby?.CharacterLoadouts == null)
            return;

        for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
        {
            LobbyCharacterLoadoutData loadout = lobby.CharacterLoadouts[i];
            if (loadout == null || string.IsNullOrWhiteSpace(loadout.CharacterId))
                continue;

            CharacterRuntimeData runtime = EnsureCharacterRuntime(
                loadout.CharacterId,
                characterStore,
                characterDatabase,
                relicDatabase);

            if (runtime == null)
                continue;

            CharacterMasterData master = TryGetMaster(characterDatabase, loadout.CharacterId);
            runtime.EquippedRelicIds = CopyArray(loadout.EquippedRelicIds, EquippedRelicSlotCount);
            runtime.EquippedSkillIds = CopySkillLoadoutWithDefaults(loadout.EquippedSkillIds, master);

            CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
            characterStore.AddOrUpdate(runtime);
        }
    }

    public static void EnsurePartyCharacterRuntimes(
        PartyRuntimeStore partyStore,
        CharacterRuntimeStore characterStore,
        CharacterDatabase characterDatabase,
        RelicDatabase relicDatabase)
    {
        if (partyStore == null || characterStore == null)
            return;

        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            string trimmed = characterId.Trim();
            if (!seen.Add(trimmed))
                continue;

            EnsureCharacterRuntime(
                trimmed,
                characterStore,
                characterDatabase,
                relicDatabase);
        }
    }

    private static CharacterRuntimeData EnsureCharacterRuntime(
        string characterId,
        CharacterRuntimeStore characterStore,
        CharacterDatabase characterDatabase,
        RelicDatabase relicDatabase)
    {
        if (characterStore == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        string trimmedId = characterId.Trim();
        CharacterMasterData master = TryGetMaster(characterDatabase, trimmedId);

        if (characterStore.TryGet(trimmedId, out CharacterRuntimeData runtime) &&
            runtime != null)
        {
            EnsureDefaultRuntimeFields(runtime, master, relicDatabase);
            return runtime;
        }

        if (master == null)
            return null;

        runtime = CreateRuntime(master);
        CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
        characterStore.AddOrUpdate(runtime);
        return runtime;
    }

    private static CharacterRuntimeData CreateRuntime(CharacterMasterData master)
    {
        return new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,
            CurrentHP = master.MaxHP,
            CurrentCost = master.MaxCost,
            CurrentResource = 0,
            CurrentMoveLevel = 0,
            IsUnlocked = master.IsDefaultProvided,
            MoveSkillId = "S_Move_1",
            PassiveSkillId = master.PassiveSkill1,
            UniqueSkillId = master.UniqueSkill1,
            AbilitySkillId = master.CharacterSkill1,
            EquippedSkillIds = CreateDefaultSkillSlots(master),
            EquippedRuneIds = new string[6],
            EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master)
        };
    }

    private static void EnsureDefaultRuntimeFields(
        CharacterRuntimeData runtime,
        CharacterMasterData master,
        RelicDatabase relicDatabase)
    {
        if (runtime == null || master == null)
            return;

        if (string.IsNullOrWhiteSpace(runtime.CharacterId))
            runtime.CharacterId = master.CharacterId;

        if (string.IsNullOrWhiteSpace(runtime.MoveSkillId))
            runtime.MoveSkillId = "S_Move_1";

        if (string.IsNullOrWhiteSpace(runtime.PassiveSkillId))
            runtime.PassiveSkillId = master.PassiveSkill1;

        if (string.IsNullOrWhiteSpace(runtime.UniqueSkillId))
            runtime.UniqueSkillId = master.UniqueSkill1;

        if (string.IsNullOrWhiteSpace(runtime.AbilitySkillId))
            runtime.AbilitySkillId = master.CharacterSkill1;

        runtime.EquippedSkillIds = CopySkillLoadoutWithDefaults(
            runtime.EquippedSkillIds,
            master);

        if (runtime.EquippedRelicIds == null ||
            runtime.EquippedRelicIds.Length != EquippedRelicSlotCount)
        {
            runtime.EquippedRelicIds = CopyArray(
                runtime.EquippedRelicIds,
                EquippedRelicSlotCount);
        }

        runtime.EquippedRuneIds ??= new string[6];
        runtime.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();
        CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
    }

    private static string[] CreateDefaultSkillSlots(CharacterMasterData master)
    {
        string[] result = new string[EquippedSkillSlotCount];

        if (master == null)
            return result;

        result[0] = master.UniqueSkill1;
        result[1] = master.CharacterSkill1;
        result[2] = string.Empty;
        result[3] = string.Empty;
        return result;
    }

    private static string[] CopySkillLoadoutWithDefaults(
        string[] source,
        CharacterMasterData master)
    {
        string[] result = CreateDefaultSkillSlots(master);

        if (source == null)
            return result;

        int count = Math.Min(source.Length, 2);
        for (int i = 0; i < count; i++)
        {
            if (i <= 1 && string.IsNullOrWhiteSpace(source[i]))
                continue;

            result[i] = source[i] ?? string.Empty;
        }

        return result;
    }

    private static string[] CopyArray(string[] source, int length)
    {
        string[] result = new string[length];

        if (source == null)
            return result;

        Array.Copy(source, result, Math.Min(source.Length, length));
        return result;
    }

    private static CharacterMasterData TryGetMaster(
        CharacterDatabase characterDatabase,
        string characterId)
    {
        if (characterDatabase == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        return characterDatabase.TryGet(characterId.Trim(), out CharacterMasterData master)
            ? master
            : null;
    }
}
