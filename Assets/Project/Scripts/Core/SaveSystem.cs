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
            return WriteSaveData(checkpointSave);

        CommitRuntimeStateContributorsForSave();
        RecordDiscoveryService.BackfillFromCurrentState(DataManager.Instance);

        GameSaveData saveData = CreateSaveData();
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
    }

    public void ClearBattleRoomEntryCheckpoint()
    {
        battleRoomEntryCheckpoint = null;
        battleRoomEntryCheckpointNodeIndex = -1;
        battleRoomEntryCheckpointMapId = string.Empty;
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

        ApplySaveData(saveData);
        return true;
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

    public List<CharacterRuntimeData> Characters = new();
    public List<SkillRuntimeData> Skills = new();
}

public interface IRuntimeSaveStateContributor
{
    void CommitRuntimeStateForSave();
}
