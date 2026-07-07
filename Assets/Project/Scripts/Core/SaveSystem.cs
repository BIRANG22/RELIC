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

    public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public void Initialize()
    {
        EnsureSaveDirectory();
    }

    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    public bool SaveCurrentProgress()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] DataManager is not ready. Progress was not saved.");
            return false;
        }

        GameSaveData saveData = CreateSaveData();
        return WriteSaveData(saveData);
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

    public GameSaveData ReadSaveData()
    {
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            return JsonUtility.FromJson<GameSaveData>(json);
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

        var saveData = new GameSaveData
        {
            Version = CurrentSaveVersion,
            SavedAtUtc = DateTime.UtcNow.ToString("O"),
            ActiveSceneName = SceneManager.GetActiveScene().name,
            Player = dataManager.PlayerRuntimeStore?.Data,
            Party = BuildPartyRuntimeData(dataManager.PartyRuntimeStore),
            Map = dataManager.MapRuntimeStore?.Get(),
            Battle = dataManager.BattleRuntimeStore?.Get(),
            SelectedGameMode = GameManager.Instance != null && GameManager.Instance.Context != null
                ? GameManager.Instance.Context.SelectedGameMode
                : GameMode.None
        };

        AddCharacters(saveData, dataManager.CharacterRuntimeStore);
        AddSkills(saveData, dataManager.SkillRuntimeStore);

        return saveData;
    }

    private bool WriteSaveData(GameSaveData saveData)
    {
        try
        {
            EnsureSaveDirectory();

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

        if (GameManager.Instance != null && GameManager.Instance.Context != null)
            GameManager.Instance.Context.SelectedGameMode = saveData.SelectedGameMode;

        Debug.Log($"[SaveSystem] Progress loaded: {SaveFilePath}");
    }

    private void EnsureSaveDirectory()
    {
        string directory = Path.GetDirectoryName(SaveFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
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

    private static void AddCharacters(GameSaveData saveData, CharacterRuntimeStore characterStore)
    {
        saveData.Characters.Clear();

        if (characterStore == null)
            return;

        foreach (CharacterRuntimeData character in characterStore.GetAll().Values)
        {
            if (character != null)
                saveData.Characters.Add(character);
        }
    }

    private static void AddSkills(GameSaveData saveData, SkillRuntimeStore skillStore)
    {
        saveData.Skills.Clear();

        if (skillStore == null)
            return;

        foreach (SkillRuntimeData skill in skillStore.GetAll().Values)
        {
            if (skill != null)
                saveData.Skills.Add(skill);
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

    public List<CharacterRuntimeData> Characters = new();
    public List<SkillRuntimeData> Skills = new();
}
