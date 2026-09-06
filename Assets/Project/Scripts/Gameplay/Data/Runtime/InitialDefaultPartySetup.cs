using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class InitialDefaultPartySetup
    {
        private const string DefaultMoveSkillId = "S_Move_1";
        private const int FirstDefaultSpawnGridIndex = 6;

        private static readonly string[] DefaultCharacterIds =
        {
            "Char_01",
            "Char_02",
            "Char_03"
        };

        public static bool TryInitialize(DataManager dataManager)
        {
            if (dataManager == null)
            {
                Debug.LogError("[InitialDefaultPartySetup] DataManager is missing.");
                return false;
            }

            bool initialized = TryInitialize(
                dataManager.CharacterDatabase,
                dataManager.CharacterRuntimeStore,
                dataManager.PartyRuntimeStore,
                dataManager.RelicDatabase);

            if (!initialized)
                Debug.LogError("[InitialDefaultPartySetup] Failed to initialize the default party.");

            return initialized;
        }

        public static bool TryInitialize(
            CharacterDatabase characterDatabase,
            CharacterRuntimeStore characterStore,
            PartyRuntimeStore partyStore,
            RelicDatabase relicDatabase)
        {
            if (characterDatabase == null || characterStore == null || partyStore == null)
                return false;

            if (partyStore.HasAnyCharacter)
                return true;

            CharacterMasterData[] masters = new CharacterMasterData[DefaultCharacterIds.Length];

            for (int i = 0; i < DefaultCharacterIds.Length; i++)
            {
                string characterId = DefaultCharacterIds[i];
                if (!characterDatabase.TryGet(characterId, out CharacterMasterData master) ||
                    master == null)
                    return false;

                int spawnGridIndex = FirstDefaultSpawnGridIndex + i;
                if (partyStore.IsGridUsed(spawnGridIndex))
                    return false;

                masters[i] = master;
            }

            for (int i = 0; i < masters.Length; i++)
            {
                CharacterMasterData master = masters[i];

                if (!characterStore.TryGet(master.CharacterId, out CharacterRuntimeData runtime) ||
                    runtime == null)
                {
                    runtime = CreateRuntime(master, relicDatabase);
                    characterStore.AddOrUpdate(runtime);
                }

                if (!partyStore.SetSlot(
                        i,
                        master.CharacterId,
                        FirstDefaultSpawnGridIndex + i))
                    return false;
            }

            return true;
        }

        private static CharacterRuntimeData CreateRuntime(
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
                    string.Empty,
                    string.Empty
                },
                EquippedRuneIds = new string[6],
                EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master)
            };

            CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
            return runtime;
        }
    }
}
