using System;
using System.Collections.Generic;
using System.IO;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : Singleton<SaveSystem>
{
    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "relic-save.json";
    private const int EquippedSkillSlotCount = 4;
    private const int EquippedRuneSlotCount = 6;
    private const int EquippedRelicSlotCount = 7;

    private GameSaveData battleRoomEntryCheckpoint;
    private int battleRoomEntryCheckpointNodeIndex = -1;
    private string battleRoomEntryCheckpointMapId = string.Empty;
    private bool hasPendingResolvedBattleRoomEntryState;
    private bool suppressCheckpointAutosave;
    private List<BattleRoomGridEffectSaveData> pendingBattleRoomGridEffects = new();
    private List<BattleRoomMonsterCommandSaveData> pendingBattleRoomMonsterCommands = new();
    // 탐사진행으로 읽은 저장본을 BattleScene 초기화가 끝날 때까지 유지합니다.
    // 런타임 데이터 Apply 후에도 전투방 1턴의 확정 결과(장애물/몬스터 예약)는 이 스냅샷에서 복구합니다.
    private GameSaveData pendingBattleContinueSaveData;
    private ResumeData pendingResumeData;

    public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public void Initialize()
    {
        EnsureSaveDirectory();
    }

    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// 저장 파일을 삭제합니다. 언어와 음량 같은 PlayerPrefs 설정값은 건드리지 않습니다.
    /// </summary>
    public bool DeleteSaveFile()
    {
        ClearBattleRoomEntryCheckpoint();
        ClearPendingResolvedBattleRoomEntryState();
        pendingBattleContinueSaveData = null;

        try
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            Debug.Log($"[SaveSystem] Save file deleted: {SaveFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to delete save file. {ex}");
            return false;
        }
    }

    public bool HasBattleContinueSave()
    {
        if (!HasSaveFile())
            return false;

        return CanContinueBattle(ReadSaveData());
    }

    public bool SaveCurrentProgress()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] DataManager is not ready. Progress was not saved.");
            return false;
        }

        if (TryCreateBattleRoomEntryCheckpointSave(out GameSaveData checkpointSave))
        {
            if (CanContinueBattle(checkpointSave))
                return WriteSaveData(checkpointSave);

            Debug.LogWarning("[SaveSystem] Battle room entry checkpoint is not a valid continue save. Falling back to the current runtime snapshot.");
        }

        CommitRuntimeStateContributorsForSave();
        RecordDiscoveryService.BackfillFromCurrentState(DataManager.Instance);

        GameSaveData saveData = CreateSaveData();
        return WriteSaveData(saveData);
    }

    public bool SaveCheckpoint()
    {
        return SaveCheckpoint(null);
    }

    public bool SaveCheckpoint(ResumeData resumeData)
    {
        if (suppressCheckpointAutosave)
        {
            Debug.Log("[SaveSystem] Checkpoint autosave suppressed while Continue is restoring.");
            return false;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] DataManager is not ready. Checkpoint was not saved.");
            return false;
        }

        CommitRuntimeStateContributorsForSave();
        RecordDiscoveryService.BackfillFromCurrentState(DataManager.Instance);
        GameSaveData saveData = CreateSaveData();
        saveData.Resume = CloneSerializable(resumeData);
        return WriteSaveData(saveData);
    }

    public void CaptureBattleRoomEntryCheckpoint()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData map = DataManager.Instance.MapRuntimeStore?.Get();
        if (!MapRuntimeProgressUtility.HasUnclearedCurrentNode(map))
        {
            ClearBattleRoomEntryCheckpoint();
            return;
        }

        RecordDiscoveryService.BackfillFromCurrentState(DataManager.Instance);

        battleRoomEntryCheckpoint = CreateSaveData();
        battleRoomEntryCheckpointNodeIndex = map.CurrentNodeIndex;
        battleRoomEntryCheckpointMapId = map.CurrentMapId ?? string.Empty;

        // 이어하기로 같은 전투방에 재진입하면 BattleSceneController가 체크포인트를 다시 잡습니다.
        // 이때 저장 파일에 있던 확정 1턴 상태를 잃지 않도록 새 체크포인트에 즉시 병합합니다.
        MergeResolvedBattleRoomEntryStateIntoCheckpoint(map);
    }

    public void ClearBattleRoomEntryCheckpoint()
    {
        battleRoomEntryCheckpoint = null;
        battleRoomEntryCheckpointNodeIndex = -1;
        battleRoomEntryCheckpointMapId = string.Empty;
    }

    /// <summary>
    /// 이어하기 전용 전투방 1턴 스냅샷과 체크포인트 캐시를 모두 비웁니다.
    /// 전투 포기 또는 새 탐사를 시작할 때 호출해 이전 탐사의 장애물/몬스터 예약이 재사용되지 않게 합니다.
    /// </summary>
    public void ClearBattleRoomResumeState()
    {
        ClearBattleRoomEntryCheckpoint();
        ClearPendingResolvedBattleRoomEntryState();
        pendingBattleContinueSaveData = null;
    }

    private bool TryCreateBattleRoomEntryCheckpointSave(out GameSaveData saveData)
    {
        saveData = null;

        if (battleRoomEntryCheckpoint == null || DataManager.Instance == null)
            return false;

        if (!string.Equals(SceneManager.GetActiveScene().name, SceneName.Battle, StringComparison.OrdinalIgnoreCase))
            return false;

        MapRuntimeData currentMap = DataManager.Instance.MapRuntimeStore?.Get();
        if (!MapRuntimeProgressUtility.HasUnclearedCurrentNode(currentMap))
            return false;

        if (currentMap.CurrentNodeIndex != battleRoomEntryCheckpointNodeIndex)
            return false;

        if (!string.Equals(currentMap.CurrentMapId ?? string.Empty, battleRoomEntryCheckpointMapId, StringComparison.Ordinal))
            return false;

        saveData = CloneSerializable(battleRoomEntryCheckpoint);
        if (saveData == null)
            return false;

        saveData.SavedAtUtc = DateTime.UtcNow.ToString("O");
        saveData.ActiveSceneName = SceneName.Battle;
        return true;
    }

    public bool TryLoadProgress()
    {
        ClearPendingResolvedBattleRoomEntryState();
        pendingBattleContinueSaveData = null;
        if (!HasSaveFile())
            return false;

        GameSaveData saveData = ReadSaveData();
        if (saveData == null)
            return false;

        ApplySaveData(saveData);
        return true;
    }

    public bool TryLoadBattleContinueProgress()
    {
        if (!HasSaveFile())
            return false;

        GameSaveData saveData = ReadSaveData();
        if (!CanContinueBattle(saveData))
            return false;

        // ApplySaveData는 커스텀 전투방 체크포인트 필드를 런타임 Store로 옮기지 않으므로
        // 원본 저장 스냅샷을 별도로 보관합니다.
        pendingBattleContinueSaveData = CloneSerializable(saveData);
        pendingResumeData = CloneSerializable(saveData.Resume);
        PreparePendingResolvedBattleRoomEntryState(saveData);
        suppressCheckpointAutosave = true;
        ApplySaveData(saveData);
        return true;
    }

    public void UpdateBattleRoomEntryCheckpointResolvedState(
        List<BattleRoomGridEffectSaveData> gridEffects,
        List<BattleRoomMonsterCommandSaveData> monsterCommands)
    {
        if (battleRoomEntryCheckpoint == null)
            return;

        battleRoomEntryCheckpoint.HasResolvedBattleRoomEntryState = true;
        battleRoomEntryCheckpoint.BattleRoomGridEffects = CloneGridEffectSaves(gridEffects);
        battleRoomEntryCheckpoint.BattleRoomMonsterCommands = CloneMonsterCommandSaves(monsterCommands);

        MapRuntimeData map = DataManager.Instance?.MapRuntimeStore?.Get();
        SaveCheckpoint(new ResumeData
        {
            Phase = ResumePhase.BattleEntry,
            NodeIndex = map != null ? map.CurrentNodeIndex : -1,
            MapId = map?.CurrentMapId,
            InitialGridEffects = CloneGridEffectSaves(gridEffects),
            InitialMonsterCommands = CloneMonsterCommandSaves(monsterCommands)
        });
    }

    public bool TryGetPendingResolvedBattleRoomEntryState(
        out IReadOnlyList<BattleRoomGridEffectSaveData> gridEffects,
        out IReadOnlyList<BattleRoomMonsterCommandSaveData> monsterCommands)
    {
        return TryGetResolvedBattleRoomEntryState(out gridEffects, out monsterCommands);
    }

    /// <summary>
    /// 탐사진행으로 읽은 저장본에서 현재 전투방의 확정된 1턴 시작 상태를 반환합니다.
    /// pending 리스트가 이미 소비됐더라도 저장 스냅샷을 통해 다시 얻을 수 있습니다.
    /// </summary>
    public bool TryGetResolvedBattleRoomEntryState(
        out IReadOnlyList<BattleRoomGridEffectSaveData> gridEffects,
        out IReadOnlyList<BattleRoomMonsterCommandSaveData> monsterCommands)
    {
        if (hasPendingResolvedBattleRoomEntryState)
        {
            gridEffects = pendingBattleRoomGridEffects;
            monsterCommands = pendingBattleRoomMonsterCommands;
            return true;
        }

        MapRuntimeData currentMap = DataManager.Instance?.MapRuntimeStore?.Get();
        if (HasMatchingResolvedBattleRoomEntryState(pendingBattleContinueSaveData, currentMap))
        {
            gridEffects = CloneGridEffectSaves(pendingBattleContinueSaveData.BattleRoomGridEffects);
            monsterCommands = CloneMonsterCommandSaves(pendingBattleContinueSaveData.BattleRoomMonsterCommands);
            return true;
        }

        gridEffects = Array.Empty<BattleRoomGridEffectSaveData>();
        monsterCommands = Array.Empty<BattleRoomMonsterCommandSaveData>();
        return false;
    }

    private void MergeResolvedBattleRoomEntryStateIntoCheckpoint(MapRuntimeData currentMap)
    {
        if (battleRoomEntryCheckpoint == null || currentMap == null)
            return;

        GameSaveData source = null;
        if (HasMatchingResolvedBattleRoomEntryState(pendingBattleContinueSaveData, currentMap))
            source = pendingBattleContinueSaveData;

        if (source == null)
            return;

        battleRoomEntryCheckpoint.HasResolvedBattleRoomEntryState = true;
        battleRoomEntryCheckpoint.BattleRoomGridEffects = CloneGridEffectSaves(source.BattleRoomGridEffects);
        battleRoomEntryCheckpoint.BattleRoomMonsterCommands = CloneMonsterCommandSaves(source.BattleRoomMonsterCommands);
    }

    private static bool HasMatchingResolvedBattleRoomEntryState(GameSaveData saveData, MapRuntimeData currentMap)
    {
        if (saveData == null || !saveData.HasResolvedBattleRoomEntryState || saveData.Map == null || currentMap == null)
            return false;

        return saveData.Map.CurrentNodeIndex == currentMap.CurrentNodeIndex &&
               string.Equals(saveData.Map.CurrentMapId ?? string.Empty,
                   currentMap.CurrentMapId ?? string.Empty,
                   StringComparison.Ordinal);
    }

    public bool TryGetPendingResumeData(out ResumeData resumeData)
    {
        resumeData = CloneSerializable(pendingResumeData);
        return resumeData != null && resumeData.Phase != ResumePhase.None;
    }

    public void ClearPendingResumeData()
    {
        pendingResumeData = null;
    }

    public void CompleteCheckpointAutosaveRestore()
    {
        suppressCheckpointAutosave = false;
    }

    public void ConsumePendingResolvedBattleRoomEntryState()
    {
        ClearPendingResolvedBattleRoomEntryState();
        suppressCheckpointAutosave = false;
    }

    private void PreparePendingResolvedBattleRoomEntryState(GameSaveData saveData)
    {
        ClearPendingResolvedBattleRoomEntryState();

        if (saveData == null || !saveData.HasResolvedBattleRoomEntryState)
            return;

        hasPendingResolvedBattleRoomEntryState = true;
        pendingBattleRoomGridEffects = CloneGridEffectSaves(saveData.BattleRoomGridEffects);
        pendingBattleRoomMonsterCommands = CloneMonsterCommandSaves(saveData.BattleRoomMonsterCommands);
    }

    private void ClearPendingResolvedBattleRoomEntryState()
    {
        hasPendingResolvedBattleRoomEntryState = false;
        pendingBattleRoomGridEffects = new List<BattleRoomGridEffectSaveData>();
        pendingBattleRoomMonsterCommands = new List<BattleRoomMonsterCommandSaveData>();
    }

    private static List<BattleRoomGridEffectSaveData> CloneGridEffectSaves(
        IEnumerable<BattleRoomGridEffectSaveData> source)
    {
        var result = new List<BattleRoomGridEffectSaveData>();
        if (source == null)
            return result;

        foreach (BattleRoomGridEffectSaveData item in source)
        {
            if (item == null)
                continue;

            result.Add(new BattleRoomGridEffectSaveData
            {
                GridIndex = item.GridIndex,
                GridEffectId = item.GridEffectId,
                RemainingDuration = item.RemainingDuration,
                HitPoints = item.HitPoints
            });
        }

        return result;
    }

    private static List<BattleRoomMonsterCommandSaveData> CloneMonsterCommandSaves(
        IEnumerable<BattleRoomMonsterCommandSaveData> source)
    {
        var result = new List<BattleRoomMonsterCommandSaveData>();
        if (source == null)
            return result;

        foreach (BattleRoomMonsterCommandSaveData item in source)
        {
            if (item == null)
                continue;

            result.Add(new BattleRoomMonsterCommandSaveData
            {
                SlotIndex = item.SlotIndex,
                RuntimeId = item.RuntimeId,
                MonsterId = item.MonsterId,
                MonsterGridIndex = item.MonsterGridIndex,
                MonsterSpawnOrder = item.MonsterSpawnOrder,
                SkillId = item.SkillId,
                MoveX = item.MoveX,
                MoveY = item.MoveY,
                ReservedDamage = item.ReservedDamage,
                ActionIndex = item.ActionIndex,
                RangeOriginGridIndex = item.RangeOriginGridIndex,
                RangeOriginCasterGridIndex = item.RangeOriginCasterGridIndex,
                HasForcedDirection = item.HasForcedDirection,
                ForcedDirection = item.ForcedDirection,
                IsPortalMove = item.IsPortalMove,
                HasExplicitRangeResult = item.HasExplicitRangeResult,
                RangeGridIndices = item.RangeGridIndices != null ? new List<int>(item.RangeGridIndices) : new List<int>(),
                TargetGridIndices = item.TargetGridIndices != null ? new List<int>(item.TargetGridIndices) : new List<int>(),
                HasSimulatedResult = item.HasSimulatedResult,
                IsSimulatedMoveBlocked = item.IsSimulatedMoveBlocked,
                SimulatedMoveX = item.SimulatedMoveX,
                SimulatedMoveY = item.SimulatedMoveY,
                UseRequestedMoveOffsetForExecution = item.UseRequestedMoveOffsetForExecution
            });
        }

        return result;
    }

    public GameSaveData ReadSaveData()
    {
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            NormalizeSaveData(saveData, saveData?.ActiveSceneName);
            return saveData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to read save file. {ex}");
            return null;
        }
    }

    private GameSaveData CreateSaveData()
    {
        DataManager dataManager = DataManager.Instance;

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            dataManager.CharacterRuntimeStore?.GetAll();
        IReadOnlyDictionary<string, SkillRuntimeData> skills =
            dataManager.SkillRuntimeStore?.GetAll();

        return CreateSaveDataSnapshot(
            dataManager.PlayerRuntimeStore?.Data,
            dataManager.PartyRuntimeStore,
            characters?.Values,
            skills?.Values,
            dataManager.MapRuntimeStore?.Get(),
            dataManager.BattleRuntimeStore?.Get(),
            SceneManager.GetActiveScene().name,
            GameManager.Instance != null && GameManager.Instance.Context != null
                ? GameManager.Instance.Context.SelectedGameMode
                : GameMode.None,
            dataManager.LobbyRuntimeStore?.GetOrCreate());
    }

    public static GameSaveData CreateSaveDataSnapshot(
        PlayerRuntimeData player,
        PartyRuntimeStore partyStore,
        IEnumerable<CharacterRuntimeData> characters,
        IEnumerable<SkillRuntimeData> skills,
        MapRuntimeData map,
        BattleRuntimeData battle,
        string activeSceneName,
        GameMode selectedGameMode,
        LobbyRuntimeData lobby = null)
    {
        var saveData = new GameSaveData
        {
            Version = CurrentSaveVersion,
            SavedAtUtc = DateTime.UtcNow.ToString("O"),
            ActiveSceneName = activeSceneName,
            SelectedGameMode = selectedGameMode,
            Player = CloneSerializable(player),
            Party = BuildPartyRuntimeData(partyStore),
            Map = CloneSerializable(map),
            Battle = CloneSerializable(battle),
            Lobby = CloneSerializable(lobby)
        };

        AddCharacters(saveData, characters);
        AddSkills(saveData, skills);
        NormalizeSaveData(saveData, activeSceneName);

        return saveData;
    }

    public static bool CanContinueBattle(GameSaveData saveData)
    {
        if (saveData == null)
            return false;

        MapRuntimeData map = saveData.Map;
        BattleRuntimeData battle = saveData.Battle;

        if (map == null || battle == null)
            return false;

        if (!map.IsRunInitialized || !battle.IsBattleRunInitialized)
            return false;

        if (string.IsNullOrWhiteSpace(map.SelectedChapterId) ||
            string.IsNullOrWhiteSpace(map.CurrentStage))
        {
            return false;
        }

        return true;
    }

    private bool WriteSaveData(GameSaveData saveData)
    {
        try
        {
            EnsureSaveDirectory();

            NormalizeSaveData(saveData, saveData?.ActiveSceneName);
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"[SaveSystem] Progress saved: {SaveFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to save progress. {ex}");
            return false;
        }
    }

    private void ApplySaveData(GameSaveData saveData)
    {
        ClearBattleRoomEntryCheckpoint();

        if (saveData == null)
            return;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] DataManager is not ready. Progress was not loaded.");
            return;
        }

        DataManager dataManager = DataManager.Instance;

        dataManager.PlayerRuntimeStore?.SetData(saveData.Player);
        ApplyPartyData(dataManager.PartyRuntimeStore, saveData.Party);
        dataManager.CharacterRuntimeStore?.SetAll(saveData.Characters);
        dataManager.SkillRuntimeStore?.SetAll(saveData.Skills);
        dataManager.MapRuntimeStore?.Set(saveData.Map);
        dataManager.BattleRuntimeStore?.Set(saveData.Battle);
        dataManager.LobbyRuntimeStore?.Set(saveData.Lobby);

        if (GameManager.Instance != null && GameManager.Instance.Context != null)
            GameManager.Instance.Context.SelectedGameMode = saveData.SelectedGameMode;

        RecordDiscoveryService.BackfillFromCurrentState(dataManager);
        Debug.Log($"[SaveSystem] Progress loaded: {SaveFilePath}");
    }

    private void EnsureSaveDirectory()
    {
        string directory = Path.GetDirectoryName(SaveFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    private static void CommitRuntimeStateContributorsForSave()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not IRuntimeSaveStateContributor contributor)
                continue;

            try
            {
                contributor.CommitRuntimeStateForSave();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to commit runtime save state from {behaviours[i].GetType().Name}. {ex}");
            }
        }
    }

    private static PartyRuntimeData BuildPartyRuntimeData(PartyRuntimeStore partyStore)
    {
        var partyData = new PartyRuntimeData();

        if (partyStore == null)
            return partyData;

        partyData.Slots.Clear();

        IReadOnlyList<PartySlotRuntimeData> slots = partyStore.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            PartySlotRuntimeData slot = slots[i];
            partyData.Slots.Add(new PartySlotRuntimeData
            {
                CharacterId = slot.CharacterId,
                SpawnGridIndex = slot.SpawnGridIndex,
                CurrentGridIndex = slot.CurrentGridIndex
            });
        }

        return partyData;
    }

    private static void AddCharacters(GameSaveData saveData, IEnumerable<CharacterRuntimeData> characters)
    {
        saveData.Characters.Clear();

        if (characters == null)
            return;

        foreach (CharacterRuntimeData character in characters)
        {
            CharacterRuntimeData snapshot = CloneSerializable(character);
            if (snapshot == null)
                continue;

            NormalizeCharacter(snapshot);
            saveData.Characters.Add(snapshot);
        }
    }

    private static void AddSkills(GameSaveData saveData, IEnumerable<SkillRuntimeData> skills)
    {
        saveData.Skills.Clear();

        if (skills == null)
            return;

        foreach (SkillRuntimeData skill in skills)
        {
            SkillRuntimeData snapshot = CloneSerializable(skill);
            if (snapshot != null)
                saveData.Skills.Add(snapshot);
        }
    }

    private static void ApplyPartyData(PartyRuntimeStore partyStore, PartyRuntimeData partyData)
    {
        if (partyStore == null)
            return;

        partyStore.Clear();

        if (partyData == null || partyData.Slots == null)
            return;

        int count = Mathf.Min(partyStore.MaxPartyCountValue, partyData.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            PartySlotRuntimeData slot = partyData.Slots[i];
            if (slot == null || string.IsNullOrWhiteSpace(slot.CharacterId))
                continue;

            partyStore.SetCharacter(i, slot.CharacterId);

            if (IsValidBattleGridIndex(slot.SpawnGridIndex))
                partyStore.SetSpawnGridIndex(i, slot.SpawnGridIndex);

            if (IsValidBattleGridIndex(slot.CurrentGridIndex))
                partyStore.SetCurrentGridIndex(i, slot.CurrentGridIndex);
        }
    }

    private static bool IsValidBattleGridIndex(int gridIndex)
    {
        return gridIndex >= 0 && gridIndex < 35;
    }

    private static T CloneSerializable<T>(T source) where T : class
    {
        if (source == null)
            return null;

        string json = JsonUtility.ToJson(source);
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<T>(json);
    }

    private static void NormalizeSaveData(GameSaveData saveData, string activeSceneName)
    {
        if (saveData == null)
            return;

        saveData.Characters ??= new List<CharacterRuntimeData>();
        saveData.Skills ??= new List<SkillRuntimeData>();
        saveData.BattleRoomGridEffects ??= new List<BattleRoomGridEffectSaveData>();
        saveData.BattleRoomMonsterCommands ??= new List<BattleRoomMonsterCommandSaveData>();
        saveData.Player ??= new PlayerRuntimeData();
        RecordDiscoveryService.Normalize(saveData.Player);
        saveData.Party ??= new PartyRuntimeData();
        saveData.Party.Slots ??= new List<PartySlotRuntimeData>();
        saveData.Lobby ??= new LobbyRuntimeData();

        NormalizeLobby(saveData.Lobby);

        NormalizeMap(saveData.Map, activeSceneName);
        NormalizeBattle(saveData.Battle);

        for (int i = 0; i < saveData.Characters.Count; i++)
            NormalizeCharacter(saveData.Characters[i]);
    }

    private static void NormalizeMap(MapRuntimeData map, string activeSceneName)
    {
        if (map == null)
            return;

        if (string.IsNullOrWhiteSpace(map.CurrentSceneName))
            map.CurrentSceneName = activeSceneName;

        map.ClearedMapIds ??= new List<string>();
        map.VisitedMapIds ??= new List<string>();
        map.GeneratedNodes ??= new List<GeneratedMapNodeData>();

        for (int i = 0; i < map.GeneratedNodes.Count; i++)
        {
            if (map.GeneratedNodes[i] != null)
                map.GeneratedNodes[i].NextNodeIndices ??= new List<int>();
        }
    }

    private static void NormalizeBattle(BattleRuntimeData battle)
    {
        if (battle == null)
            return;

        battle.OwnedRelicIds ??= new List<string>();
        battle.BagItemIds ??= new List<string>();
        battle.SkillInventoryIds ??= new List<string>();
        battle.StartingSkillInventoryIds ??= new List<string>();
        battle.AcquiredSkillIds ??= new List<string>();
        battle.CharacterStatistics ??= new List<BattleRunCharacterStatisticsData>();
        battle.LobbyLoadoutSnapshots ??= new List<BattleLobbyLoadoutSnapshotData>();
        CultureTankBattleStartEffectService.Normalize(battle);

        for (int i = 0; i < battle.LobbyLoadoutSnapshots.Count; i++)
            NormalizeLobbyLoadoutSnapshot(battle.LobbyLoadoutSnapshots[i]);
    }

    private static void NormalizeLobby(LobbyRuntimeData lobby)
    {
        if (lobby == null)
            return;

        lobby.OwnedRelicIds ??= new List<string>();
        lobby.SkillInventoryIds ??= new List<string>();
        lobby.BagItemIds ??= new List<string>();
        lobby.StoredCompoundIds ??= new List<string>();
        lobby.CharacterLoadouts ??= new List<LobbyCharacterLoadoutData>();
        lobby.RelicOfferIds ??= new List<string>();
        CultureTankResearchService.Normalize(lobby);

        if (!lobby.HasPendingResearchResult || lobby.PendingResearchResult == null)
        {
            lobby.HasPendingResearchResult = false;
            lobby.PendingResearchResult = null;
        }

        for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
        {
            LobbyCharacterLoadoutData loadout = lobby.CharacterLoadouts[i];
            if (loadout == null)
                continue;

            loadout.EquippedRelicIds = NormalizeStringArray(loadout.EquippedRelicIds, EquippedRelicSlotCount);
            loadout.EquippedSkillIds = NormalizeStringArray(loadout.EquippedSkillIds, EquippedSkillSlotCount);
        }
    }

    private static void NormalizeLobbyLoadoutSnapshot(BattleLobbyLoadoutSnapshotData snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.EquippedSkillIds = NormalizeStringArray(snapshot.EquippedSkillIds, EquippedSkillSlotCount);
        snapshot.EquippedRuneIds = NormalizeStringArray(snapshot.EquippedRuneIds, EquippedRuneSlotCount);
        snapshot.EquippedRelicIds = NormalizeStringArray(snapshot.EquippedRelicIds, EquippedRelicSlotCount);
    }

    private static void NormalizeCharacter(CharacterRuntimeData character)
    {
        if (character == null)
            return;

        character.StatusEffects ??= new List<StatusEffectRuntimeData>();
        character.EquippedSkillIds = NormalizeStringArray(character.EquippedSkillIds, EquippedSkillSlotCount);
        character.EquippedRuneIds = NormalizeStringArray(character.EquippedRuneIds, EquippedRuneSlotCount);
        character.EquippedRelicIds = NormalizeStringArray(character.EquippedRelicIds, EquippedRelicSlotCount);
        ActiveRelicRuntimeUtility.NormalizeUseEntries(character);
        character.AppliedBattleEquipmentEffectIds ??= new List<string>();
    }

    private static string[] NormalizeStringArray(string[] source, int length)
    {
        var normalized = new string[length];

        if (source == null)
            return normalized;

        Array.Copy(source, normalized, Mathf.Min(source.Length, length));
        return normalized;
    }
}

[Serializable]
public class GameSaveData
{
    public int Version;
    public string SavedAtUtc;
    public string ActiveSceneName;
    public GameMode SelectedGameMode;

    public PlayerRuntimeData Player;
    public PartyRuntimeData Party;
    public MapRuntimeData Map;
    public BattleRuntimeData Battle;
    public LobbyRuntimeData Lobby;
    public ResumeData Resume;

    public List<CharacterRuntimeData> Characters = new();
    public List<SkillRuntimeData> Skills = new();

    public bool HasResolvedBattleRoomEntryState;
    public List<BattleRoomGridEffectSaveData> BattleRoomGridEffects = new();
    public List<BattleRoomMonsterCommandSaveData> BattleRoomMonsterCommands = new();
}

[Serializable]
public class BattleRoomGridEffectSaveData
{
    public int GridIndex;
    public string GridEffectId;
    public int RemainingDuration;
    public int HitPoints;
}

[Serializable]
public class BattleRoomMonsterCommandSaveData
{
    public int SlotIndex;
    public string RuntimeId;
    public string MonsterId;
    public int MonsterGridIndex = -1;
    // 전투방에서 생성된 몬스터 목록의 고정 순번입니다. RuntimeId 재발급과 동종 몬스터 중복을 구분합니다.
    public int MonsterSpawnOrder = -1;
    public string SkillId;
    public int MoveX;
    public int MoveY;
    public int ReservedDamage;
    public int ActionIndex;
    public int RangeOriginGridIndex = -1;
    public int RangeOriginCasterGridIndex = -1;
    public bool HasForcedDirection;
    public int ForcedDirection;
    public bool IsPortalMove;
    public bool HasExplicitRangeResult;
    public List<int> RangeGridIndices = new();
    public List<int> TargetGridIndices = new();
    public bool HasSimulatedResult;
    public bool IsSimulatedMoveBlocked;
    public int SimulatedMoveX;
    public int SimulatedMoveY;
    public bool UseRequestedMoveOffsetForExecution;
}

public interface IRuntimeSaveStateContributor
{
    void CommitRuntimeStateForSave();
}
