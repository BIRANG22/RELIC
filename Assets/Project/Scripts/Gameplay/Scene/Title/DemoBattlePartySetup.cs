using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class DemoBattlePartySetup
{
    private const string DefaultMoveSkillId = "S_Move_1";
    private const int RuneSlotCount = 6;
    private const int RelicSlotCount = 7;

    private static readonly string[] DemoCharacterIds =
    {
        "Char_01",
        "Char_02",
        "Char_04"
    };

    public static bool TryPrepare(DataManager dataManager)
    {
        if (dataManager == null)
            return false;

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        BattleRuntimeData battle = dataManager.BattleRuntimeStore?.GetOrCreate();

        return TryPrepare(
            dataManager.CharacterDatabase,
            dataManager.CharacterRuntimeStore,
            dataManager.PartyRuntimeStore,
            lobby,
            battle);
    }

    public static bool TryPrepare(
        CharacterDatabase characterDatabase,
        CharacterRuntimeStore characterStore,
        PartyRuntimeStore partyStore,
        LobbyRuntimeData lobby,
        BattleRuntimeData battle)
    {
        if (characterDatabase == null ||
            characterStore == null ||
            partyStore == null ||
            lobby == null ||
            battle == null)
        {
            return false;
        }

        CharacterMasterData[] masters = new CharacterMasterData[DemoCharacterIds.Length];
        for (int i = 0; i < DemoCharacterIds.Length; i++)
        {
            if (!characterDatabase.TryGet(DemoCharacterIds[i], out CharacterMasterData master) ||
                master == null)
            {
                return false;
            }

            masters[i] = master;
        }

        characterStore.Clear();
        partyStore.Clear();

        for (int slotIndex = 0; slotIndex < masters.Length; slotIndex++)
        {
            CharacterRuntimeData runtime = CreateCharacterRuntime(masters[slotIndex]);
            characterStore.AddOrUpdate(runtime);

            if (!partyStore.SetSlot(slotIndex, runtime.CharacterId, slotIndex))
            {
                characterStore.Clear();
                partyStore.Clear();
                return false;
            }
        }

        ResetLobbyRuntime(lobby, masters);
        ResetBattleRuntime(battle);
        return true;
    }

    private static CharacterRuntimeData CreateCharacterRuntime(CharacterMasterData master)
    {
        return new CharacterRuntimeData
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
            EquippedRuneIds = new string[RuneSlotCount],
            EquippedRelicIds = new string[RelicSlotCount],
            ActiveRelicUses = new List<ActiveRelicUseRuntimeData>(),
            StatusEffects = new List<StatusEffectRuntimeData>(),
            AppliedBattleEquipmentEffectIds = new List<string>()
        };
    }

    private static void ResetLobbyRuntime(
        LobbyRuntimeData lobby,
        IReadOnlyList<CharacterMasterData> masters)
    {
        lobby.OwnedRelicIds = new List<string>();
        lobby.SkillInventoryIds = new List<string>();
        lobby.BagItemIds = new List<string>();
        lobby.SelectedErosionDifficultyIds = new List<string>();
        lobby.CharacterSkillUpgrades = new List<LobbySkillUpgradeRecordData>();
        lobby.PendingCultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>();
        lobby.CharacterLoadouts = new List<LobbyCharacterLoadoutData>();

        for (int i = 0; i < masters.Count; i++)
        {
            CharacterMasterData master = masters[i];
            lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
            {
                CharacterId = master.CharacterId,
                EquippedRelicIds = new string[RelicSlotCount],
                EquippedSkillIds = new[]
                {
                    master.UniqueSkill1,
                    master.CharacterSkill1,
                    string.Empty,
                    string.Empty
                }
            });
        }
    }

    private static void ResetBattleRuntime(BattleRuntimeData battle)
    {
        battle.Remnant = 0;
        battle.OwnedRelicIds = new List<string>();
        battle.BagItemIds = new List<string>();
        battle.SkillInventoryIds = new List<string>();
        battle.StartingSkillInventoryIds = new List<string>();
        battle.AcquiredSkillIds = new List<string>();
        battle.CharacterStatistics = new List<BattleRunCharacterStatisticsData>();
        battle.LobbyLoadoutSnapshots = new List<BattleLobbyLoadoutSnapshotData>();
        battle.CultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>();
        battle.AppliedExplorationStartRuneCharacterIds = new List<string>();
        battle.CurrentBattleCount = 0;
        battle.CurrentRewardCount = 0;
        battle.ErosionValue = 0;
        battle.LastErosionRoomKey = string.Empty;
        battle.IsBattleRunInitialized = true;
        battle.IsDemoBattle = true;
    }
}
