using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 새 게임에서 튜토리얼 첫 전투로 바로 진입하기 위한 런타임 데이터를 구성합니다.
/// 전투 자체는 기존 BattleScene / BattleRoom 로직을 그대로 사용합니다.
/// </summary>
public static class TutorialBattleEntrySetup
{
    public const string FirstTutorialMapId = "Map_27";
    public const string SecondTutorialMapId = "Map_28";
    public const string SecondTutorialEventMapId = "Map_31";
    public const string ThirdTutorialMapId = "Map_29";
    public const string HiltCharacterId = "Char_01";
    public const string HazeCharacterId = "Char_03";
    public const string KayaCharacterId = "Char_02";

    private const string DefaultChapterId = "Chapter1";
    private const string DefaultMoveSkillId = "S_Move_1";
    private const int HiltSpawnGridIndex = 11;
    private const int HazeSpawnGridIndex = 7;
    private const int KayaSpawnGridIndex = 13;
    private const int SkillSlotCount = 4;
    private const int RuneSlotCount = 6;
    private const int RelicSlotCount = 7;

    public static bool TryPrepareFirstBattle(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] DataManager is missing.");
            return false;
        }

        dataManager.Initialize();

        if (!TryPrepareHiltParty(dataManager))
            return false;

        if (!TryPrepareTutorialMap(dataManager))
            return false;

        PrepareTutorialBattleRuntime(dataManager);
        PrepareTutorialLobbyRuntime(dataManager);
        return true;
    }

    /// <summary>
    /// 튜토리얼 이벤트 1 종료 후 전투 2에 사용할 헤이즈를 기존 힐트 파티에 추가합니다.
    /// 이벤트에서 획득한 기억/유물 등 기존 런타임 데이터는 초기화하지 않습니다.
    /// </summary>
    public static bool TryPrepareSecondBattleParty(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] DataManager is missing while preparing tutorial battle 2.");
            return false;
        }

        dataManager.Initialize();

        CharacterDatabase characterDatabase = dataManager.CharacterDatabase;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;
        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;

        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] Required character runtime stores are missing for tutorial battle 2.");
            return false;
        }

        if (!characterDatabase.TryGet(HazeCharacterId, out CharacterMasterData master) || master == null)
        {
            Debug.LogError($"[TutorialBattleEntrySetup] Haze character data was not found: {HazeCharacterId}");
            return false;
        }

        if (!characterStore.TryGet(HazeCharacterId, out CharacterRuntimeData hazeRuntime) || hazeRuntime == null)
        {
            hazeRuntime = CreateCharacterRuntime(master, dataManager.RelicDatabase);
            characterStore.AddOrUpdate(hazeRuntime);
        }

        if (!partyStore.SetSlot(1, HazeCharacterId, HazeSpawnGridIndex))
        {
            Debug.LogError("[TutorialBattleEntrySetup] Failed to register Haze in tutorial party slot 1.");
            return false;
        }

        Debug.Log($"[TutorialBattleEntrySetup] Tutorial battle 2 party prepared: Hilt + Haze(Grid {HazeSpawnGridIndex}).");
        return true;
    }

    /// <summary>
    /// 튜토리얼 이벤트 2 종료 후 전투 3에 사용할 카야를 기존 힐트/헤이즈 파티에 추가합니다.
    /// 이네스의 생명력 회복은 Event_T_02의 기존 Modify 결과를 그대로 사용합니다.
    /// </summary>
    public static bool TryPrepareThirdBattleParty(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] DataManager is missing while preparing tutorial battle 3.");
            return false;
        }

        dataManager.Initialize();

        CharacterDatabase characterDatabase = dataManager.CharacterDatabase;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;
        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;

        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] Required character runtime stores are missing for tutorial battle 3.");
            return false;
        }

        if (!characterDatabase.TryGet(KayaCharacterId, out CharacterMasterData master) || master == null)
        {
            Debug.LogError($"[TutorialBattleEntrySetup] Kaya character data was not found: {KayaCharacterId}");
            return false;
        }

        if (!characterStore.TryGet(KayaCharacterId, out CharacterRuntimeData kayaRuntime) || kayaRuntime == null)
        {
            kayaRuntime = CreateCharacterRuntime(master, dataManager.RelicDatabase);
            characterStore.AddOrUpdate(kayaRuntime);
        }

        if (!partyStore.SetSlot(2, KayaCharacterId, KayaSpawnGridIndex))
        {
            Debug.LogError("[TutorialBattleEntrySetup] Failed to register Kaya in tutorial party slot 2.");
            return false;
        }

        Debug.Log($"[TutorialBattleEntrySetup] Tutorial battle 3 party prepared: Hilt + Haze + Kaya(Grid {KayaSpawnGridIndex}).");
        return true;
    }

    /// <summary>
    /// 튜토리얼을 완료하거나 중간에 건너뛴 뒤 로비로 돌아갈 때 사용할 최종 파티 배치를 준비합니다.
    /// 튜토리얼 진행 시점과 관계없이 힐트 / 헤이즈 / 카야를 지정된 Ready 그리드에 등록합니다.
    /// </summary>
    public static bool TryPrepareLobbyPartyAfterTutorial(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] DataManager is missing while preparing the lobby party after tutorial.");
            return false;
        }

        dataManager.Initialize();

        CharacterDatabase characterDatabase = dataManager.CharacterDatabase;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;
        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;

        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] Required character runtime stores are missing while preparing the lobby party after tutorial.");
            return false;
        }

        string[] characterIds =
        {
            HiltCharacterId,
            HazeCharacterId,
            KayaCharacterId
        };

        int[] spawnGridIndices =
        {
            HiltSpawnGridIndex,
            HazeSpawnGridIndex,
            KayaSpawnGridIndex
        };

        for (int i = 0; i < characterIds.Length; i++)
        {
            string characterId = characterIds[i];

            if (!characterDatabase.TryGet(characterId, out CharacterMasterData master) || master == null)
            {
                Debug.LogError($"[TutorialBattleEntrySetup] Character data was not found while preparing lobby party: {characterId}");
                return false;
            }

            if (!characterStore.TryGet(characterId, out CharacterRuntimeData runtime) || runtime == null)
            {
                runtime = CreateCharacterRuntime(master, dataManager.RelicDatabase);
                characterStore.AddOrUpdate(runtime);
            }
        }

        // 튜토리얼 중간 상태에서 1~2명만 등록되어 있을 수 있으므로 최종 파티를 명시적으로 다시 구성합니다.
        partyStore.Clear();

        for (int i = 0; i < characterIds.Length; i++)
        {
            if (!partyStore.SetSlot(i, characterIds[i], spawnGridIndices[i]))
            {
                Debug.LogError(
                    $"[TutorialBattleEntrySetup] Failed to register tutorial lobby party slot {i}: {characterIds[i]} / Grid {spawnGridIndices[i]}");
                return false;
            }
        }

        Debug.Log(
            $"[TutorialBattleEntrySetup] Lobby party prepared after tutorial: " +
            $"{HiltCharacterId}(Grid {HiltSpawnGridIndex}), " +
            $"{HazeCharacterId}(Grid {HazeSpawnGridIndex}), " +
            $"{KayaCharacterId}(Grid {KayaSpawnGridIndex}).");

        return true;
    }

    private static bool TryPrepareHiltParty(DataManager dataManager)
    {
        CharacterDatabase characterDatabase = dataManager.CharacterDatabase;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;
        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;

        if (characterDatabase == null || characterStore == null || partyStore == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] Required character runtime stores are missing.");
            return false;
        }

        if (!characterDatabase.TryGet(HiltCharacterId, out CharacterMasterData master) || master == null)
        {
            Debug.LogError($"[TutorialBattleEntrySetup] Hilt character data was not found: {HiltCharacterId}");
            return false;
        }

        characterStore.Clear();
        partyStore.Clear();

        CharacterRuntimeData hiltRuntime = CreateCharacterRuntime(master, dataManager.RelicDatabase);

        characterStore.AddOrUpdate(hiltRuntime);

        if (!partyStore.SetSlot(0, HiltCharacterId, HiltSpawnGridIndex))
        {
            characterStore.Clear();
            partyStore.Clear();
            Debug.LogError("[TutorialBattleEntrySetup] Failed to register Hilt in tutorial party slot 0.");
            return false;
        }

        return true;
    }

    private static CharacterRuntimeData CreateCharacterRuntime(
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
            EquippedRuneIds = new string[RuneSlotCount],
            EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master),
            ActiveRelicUses = new List<ActiveRelicUseRuntimeData>(),
            StatusEffects = new List<StatusEffectRuntimeData>(),
            AppliedBattleEquipmentEffectIds = new List<string>()
        };

        CharacterStartingRelicUtility.InitializeActiveRelicUses(runtime, relicDatabase);
        return runtime;
    }

    private static bool TryPrepareTutorialMap(DataManager dataManager)
    {
        if (dataManager.MapDatabase == null || dataManager.MapRuntimeStore == null)
        {
            Debug.LogError("[TutorialBattleEntrySetup] Map database or runtime store is missing.");
            return false;
        }

        if (!dataManager.MapDatabase.TryGet(FirstTutorialMapId, out MapData tutorialMap) || tutorialMap == null)
        {
            Debug.LogError($"[TutorialBattleEntrySetup] Tutorial map data was not found: {FirstTutorialMapId}");
            return false;
        }

        string stageId = string.IsNullOrWhiteSpace(tutorialMap.Stage)
            ? "Stage1"
            : tutorialMap.Stage.Trim();

        var tutorialNode = new GeneratedMapNodeData
        {
            NodeIndex = 0,
            LayerIndex = 0,
            MapId = tutorialMap.MapId,
            Type = tutorialMap.Type,
            EventId = tutorialMap.EventId,
            IsMapIdOverride = true,
            Position = Vector2.zero,
            NextNodeIndices = new List<int>()
        };

        var mapRuntime = new MapRuntimeData
        {
            SelectedChapterId = DefaultChapterId,
            CurrentStage = stageId,
            CurrentMapId = tutorialMap.MapId,
            CurrentNodeIndex = tutorialNode.NodeIndex,
            CurrentSceneName = SceneName.Battle,
            ClearedMapIds = new List<string>(),
            VisitedMapIds = new List<string> { tutorialNode.NodeIndex.ToString() },
            IsBossUnlocked = false,
            IsRunInitialized = true,
            IsManualMapTemplate = false,
            ManualMapTemplateKey = string.Empty,
            MapGenerationKey = string.Empty,
            GeneratedNodes = new List<GeneratedMapNodeData> { tutorialNode }
        };

        dataManager.MapRuntimeStore.Set(mapRuntime);
        return true;
    }

    private static void PrepareTutorialBattleRuntime(DataManager dataManager)
    {
        BattleRuntimeData battle = dataManager.BattleRuntimeStore?.GetOrCreate();
        if (battle == null)
            return;

        battle.Remnant = 0;
        battle.OwnedRelicIds = new List<string>();
        battle.BagItemIds = new List<string>();
        battle.SkillInventoryIds = new List<string>();
        battle.StartingSkillInventoryIds = new List<string>();
        battle.AcquiredSkillIds = new List<string>();
        battle.CharacterStatistics = new List<BattleRunCharacterStatisticsData>();
        battle.LobbyLoadoutSnapshots = new List<BattleLobbyLoadoutSnapshotData>();
        battle.CultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>();
        battle.SelectedErosionDifficultyIds = new List<string>();
        battle.AppliedExplorationStartRuneCharacterIds = new List<string>();
        battle.CurrentBattleCount = 0;
        battle.CurrentRewardCount = 0;
        battle.ErosionValue = 0;
        battle.LastErosionRoomKey = string.Empty;
        battle.ErosionPartyStartEffectsApplied = false;
        battle.IsBattleRunInitialized = true;
        battle.IsDemoBattle = false;
        battle.IsTutorialBattle = true;

        dataManager.BattleRuntimeStore.Set(battle);
    }

    private static void PrepareTutorialLobbyRuntime(DataManager dataManager)
    {
        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return;

        lobby.OwnedRelicIds = new List<string>();
        lobby.SkillInventoryIds = new List<string>();
        lobby.BagItemIds = new List<string>();
        lobby.SelectedErosionDifficultyIds = new List<string>();
        lobby.PendingCultureTankBattleStartEffects = new List<CultureTankBattleStartEffectRuntimeData>();
        lobby.CharacterSkillUpgrades = new List<LobbySkillUpgradeRecordData>();
        lobby.CharacterLoadouts = new List<LobbyCharacterLoadoutData>();

        if (dataManager.CharacterRuntimeStore != null &&
            dataManager.CharacterRuntimeStore.TryGet(HiltCharacterId, out CharacterRuntimeData hilt) &&
            hilt != null)
        {
            lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
            {
                CharacterId = hilt.CharacterId,
                EquippedRelicIds = CopyArray(hilt.EquippedRelicIds, RelicSlotCount),
                EquippedSkillIds = CopyArray(hilt.EquippedSkillIds, SkillSlotCount)
            });
        }

        dataManager.LobbyRuntimeStore.Set(lobby);
    }

    private static string[] CopyArray(string[] source, int length)
    {
        string[] result = new string[length];
        if (source == null)
            return result;

        int copyLength = Mathf.Min(source.Length, length);
        for (int i = 0; i < copyLength; i++)
            result[i] = source[i] ?? string.Empty;

        return result;
    }
}
