using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneSettingPanel : MonoBehaviour
{
    [Header("Rune Slots")]
    [SerializeField] private RuneSlotButton[] runeSlotButtons;

    [Header("Rune Icon List Panel")]
    [SerializeField] private GameObject runeIconSelectPanel;
    [SerializeField] private RuneIconButton[] runeIconButtons;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    public event Action OnRuneChanged;

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        InitRuneSlots();
        InitRuneIconButtons();
        ApplyDefaultLockedState();

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);

        if (currentRuntimeData != null)
            RefreshCurrentRuneView();
        else
            ApplyDefaultLockedState();
    }

    private void InitRuneSlots()
    {
        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].Init(this, i);
        }
    }

    private void InitRuneIconButtons()
    {
        if (runeIconButtons == null)
            return;

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] != null)
                runeIconButtons[i].Init(this);
        }
    }

    public void OpenCharacterSetting(string characterId)
    {
        OpenCharacterSetting(characterId, true);
    }

    public void OpenCharacterSetting(string characterId, bool saveCurrent)
    {
        if (saveCurrent)
            SaveCurrentRuneSetting();

        currentCharacterId = characterId;
        currentMasterData = null;
        currentRuntimeData = null;

        if (DataManager.Instance == null)
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("DataManager가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("캐릭터 마스터 데이터를 찾을 수 없습니다: " + characterId);
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("캐릭터 런타임 데이터를 찾을 수 없습니다: " + characterId);
            return;
        }

        RefreshCurrentRuneView();

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    public void RefreshByCurrentLevel()
    {
        RefreshCurrentRuneView();
    }

    public void RefreshByPlayerLevel()
    {
        RefreshCurrentRuneView();
    }

    private void RefreshCurrentRuneView()
    {
        ApplyRuneSlotUnlockState();
        LoadCurrentRuneSetting();
        RefreshRuneIconButtons();
    }

    private void LoadCurrentRuneSetting()
    {
        if (currentRuntimeData == null)
            return;

        if (runeSlotButtons == null)
            return;

        bool changed = false;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            RuneData runeData = null;
            string runeId = GetRuntimeRuneId(i);

            if (!string.IsNullOrWhiteSpace(runeId))
                DataManager.Instance.RuneDatabase.TryGet(runeId, out runeData);

            if (!IsRuneValidForCurrentCharacter(runeData))
            {
                runeData = null;
                changed = true;
            }

            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].SetRune(runeData);

            SetRuntimeRuneId(i, runeData != null ? runeData.RuneId : null);
        }

        if (changed)
        {
            SaveCurrentRuneSetting();
            OnRuneChanged?.Invoke();
        }
    }

    private void ApplyRuneSlotUnlockState()
    {
        if (currentRuntimeData == null)
        {
            ApplyDefaultLockedState();
            return;
        }

        int characterLevel = currentRuntimeData.Level;
        int unlockedSlotCount = GetUnlockedRuneSlotCount(characterLevel);

        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            bool unlocked = i < unlockedSlotCount;
            runeSlotButtons[i].SetLocked(!unlocked);

            if (!unlocked)
                runeSlotButtons[i].SetRune(null);
        }

        SaveCurrentRuneSetting();
    }

    private void ApplyDefaultLockedState()
    {
        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].SetLocked(true);
        }
    }

    private int GetUnlockedRuneSlotCount(int level)
    {
        int baseSlot = 4;
        return Mathf.Clamp(baseSlot + (level - 1), 0, runeSlotButtons.Length);
    }

    private void SaveCurrentRuneSetting()
    {
        if (currentRuntimeData == null)
            return;

        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            RuneData rune = runeSlotButtons[i].EquippedRune;

            if (runeSlotButtons[i].IsLocked)
                rune = null;

            if (!IsRuneValidForCurrentCharacter(rune))
                rune = null;

            SetRuntimeRuneId(i, rune != null ? rune.RuneId : null);
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    private void RefreshRuneIconButtons()
    {
        RuneData[] candidates = GetCurrentRuneCandidates();

        if (runeIconButtons != null)
        {
            for (int i = 0; i < runeIconButtons.Length; i++)
            {
                if (runeIconButtons[i] == null)
                    continue;

                if (candidates != null && i < candidates.Length)
                {
                    RuneData runeData = candidates[i];
                    bool locked = IsRuneLockedForCurrentState(runeData);
                    int requiredLevel = GetRequiredLevelForRune(runeData);

                    runeIconButtons[i].SetRuneData(runeData, locked, requiredLevel);
                }
                else
                {
                    runeIconButtons[i].SetRuneData(null, false, 0);
                }
            }
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);

        RefreshRuneIconEquippedStates();
    }

    private RuneData[] GetCurrentRuneCandidates()
    {
        List<RuneData> result = new();

        if (DataManager.Instance == null)
            return result.ToArray();

        if (string.IsNullOrWhiteSpace(currentCharacterId))
            return result.ToArray();

        List<RuneData> allRunes = DataManager.Instance.RuneDatabase.GetAll();

        for (int i = 0; i < allRunes.Count; i++)
        {
            RuneData rune = allRunes[i];

            if (rune == null)
                continue;

            if (IsRuneCandidateForCurrentCharacter(rune))
                AddRuneIfNotExists(result, rune);
        }

        return result.ToArray();
    }

    private bool IsRuneCandidateForCurrentCharacter(RuneData rune)
    {
        if (rune == null)
            return false;

        bool isCommonRune = rune.TargetCharacterId == "All";
        bool isCharacterRune = rune.TargetCharacterId == currentCharacterId;

        return isCommonRune || isCharacterRune;
    }

    private void AddRuneIfNotExists(List<RuneData> list, RuneData rune)
    {
        if (list == null || rune == null)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].RuneId == rune.RuneId)
                return;
        }

        list.Add(rune);
    }

    public void TryEquipRuneToFirstEmptySlot(RuneData runeData)
    {
        if (currentRuntimeData == null || currentMasterData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
            return;
        }

        if (runeData == null)
        {
            ShowWarning("선택된 룬이 없습니다.");
            return;
        }

        if (IsRuneLockedForCurrentState(runeData))
        {
            ShowRuneLockedWarning(runeData);
            return;
        }

        if (!IsRuneValidForCurrentCharacter(runeData))
        {
            ShowWarning("현재 캐릭터가 사용할 수 없는 룬입니다.");
            return;
        }

        if (IsRuneEquipped(runeData))
        {
            UnequipRune(runeData);
            return;
        }

        RuneSlotButton emptySlot = FindFirstEmptyUnlockedSlot();

        if (emptySlot == null)
        {
            ShowWarning("비어있는 룬 슬롯이 없습니다.");
            return;
        }

        emptySlot.SetRune(runeData);
        SetRuntimeRuneId(emptySlot.SlotIndex, runeData.RuneId);

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);

        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    public void TrySelectRuneIcon(RuneData runeData, bool locked, int requiredLevel)
    {
        if (runeData == null)
            return;

        if (locked)
        {
            if (runeData.TargetCharacterId == "All")
                ShowWarning("플레이어 또는 계정 레벨 조건이 필요한 공용룬입니다.");
            else
                ShowWarning("캐릭터 LV." + requiredLevel + "에 해금되는 전용룬입니다.");

            return;
        }

        TryEquipRuneToFirstEmptySlot(runeData);
    }

    public void UnequipRune(RuneData runeData)
    {
        if (runeData == null)
        {
            ShowWarning("해제할 룬이 없습니다.");
            return;
        }

        bool removed = false;

        if (runeSlotButtons != null)
        {
            for (int i = 0; i < runeSlotButtons.Length; i++)
            {
                if (runeSlotButtons[i] == null)
                    continue;

                RuneData equippedRune = runeSlotButtons[i].EquippedRune;

                if (equippedRune == null)
                    continue;

                if (equippedRune.RuneId == runeData.RuneId)
                {
                    runeSlotButtons[i].SetRune(null);
                    removed = true;
                    break;
                }
            }
        }

        if (!removed)
        {
            ShowWarning("장착 중인 룬이 아닙니다.");
            return;
        }

        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    public void UnequipRuneFromSlot(RuneSlotButton slotButton)
    {
        if (slotButton == null)
        {
            ShowWarning("룬 슬롯이 연결되지 않았습니다.");
            return;
        }

        if (slotButton.IsLocked)
        {
            ShowWarning("아직 잠겨있는 룬 슬롯입니다.");
            return;
        }

        if (slotButton.EquippedRune == null)
        {
            ShowWarning("해제할 룬이 없습니다.");
            return;
        }

        slotButton.SetRune(null);

        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    private RuneSlotButton FindFirstEmptyUnlockedSlot()
    {
        if (runeSlotButtons == null)
            return null;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            if (runeSlotButtons[i].IsLocked)
                continue;

            if (runeSlotButtons[i].EquippedRune != null)
                continue;

            return runeSlotButtons[i];
        }

        return null;
    }

    private bool IsRuneEquipped(RuneData runeData)
    {
        if (runeData == null || runeSlotButtons == null)
            return false;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            RuneData equippedRune = runeSlotButtons[i].EquippedRune;

            if (equippedRune == null)
                continue;

            if (equippedRune.RuneId == runeData.RuneId)
                return true;
        }

        return false;
    }

    private bool IsRuneValidForCurrentCharacter(RuneData runeData)
    {
        if (runeData == null)
            return false;

        if (string.IsNullOrWhiteSpace(currentCharacterId))
            return false;

        if (!IsRuneCandidateForCurrentCharacter(runeData))
            return false;

        if (IsRuneLockedForCurrentState(runeData))
            return false;

        return true;
    }

    private bool IsRuneLockedForCurrentState(RuneData runeData)
    {
        if (runeData == null)
            return false;

        int requiredLevel = GetRequiredLevelForRune(runeData);

        if (requiredLevel <= 1)
            return false;

        int currentLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        return currentLevel < requiredLevel;
    }

    private int GetRequiredLevelForRune(RuneData runeData)
    {
        if (runeData == null)
            return 0;

        /*
         * 현재 RuneData에는 unlockLevel, category, common/unique 구분 enum이 없음.
         * 그래서 TargetCharacterId 기준으로 임시 해금 레벨을 계산함.
         *
         * 공용룬 TargetCharacterId == "All" : 기본 LV.1
         * 전용룬 TargetCharacterId == currentCharacterId : EnhancementLevel 기준 임시 계산
         */

        if (runeData.TargetCharacterId == "All")
            return 1;

        if (runeData.TargetCharacterId == currentCharacterId)
        {
            switch (runeData.EnhancementLevel)
            {
                case 0:
                case 1:
                    return 1;
                case 2:
                    return 5;
                case 3:
                    return 9;
                case 4:
                    return 13;
                case 5:
                    return 17;
                default:
                    return 20;
            }
        }

        return 0;
    }

    private void ShowRuneLockedWarning(RuneData runeData)
    {
        int requiredLevel = GetRequiredLevelForRune(runeData);

        if (runeData != null && runeData.TargetCharacterId == "All")
        {
            ShowWarning("아직 잠겨있는 공용룬입니다.");
            return;
        }

        ShowWarning("캐릭터 LV." + requiredLevel + "에 해금되는 전용룬입니다.");
    }

    private void RefreshRuneIconEquippedStates()
    {
        if (runeIconButtons == null)
            return;

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] == null)
                continue;

            RuneData runeData = runeIconButtons[i].CurrentRuneData;
            bool equipped = IsRuneEquipped(runeData);

            runeIconButtons[i].SetEquippedState(equipped);
        }
    }

    private string GetRuntimeRuneId(int slotIndex)
    {
        if (currentRuntimeData == null)
            return null;

        if (currentRuntimeData.EquippedRuneIds == null)
            currentRuntimeData.EquippedRuneIds = new string[4];

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedRuneIds.Length)
            return null;

        return currentRuntimeData.EquippedRuneIds[slotIndex];
    }

    private void SetRuntimeRuneId(int slotIndex, string runeId)
    {
        if (currentRuntimeData == null)
            return;

        if (currentRuntimeData.EquippedRuneIds == null)
            currentRuntimeData.EquippedRuneIds = new string[4];

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedRuneIds.Length)
            return;

        currentRuntimeData.EquippedRuneIds[slotIndex] = runeId;
    }

    private void ClearRuneSlots()
    {
        if (runeSlotButtons != null)
        {
            for (int i = 0; i < runeSlotButtons.Length; i++)
            {
                if (runeSlotButtons[i] != null)
                    runeSlotButtons[i].SetRune(null);
            }
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void ClearRuneSlotsAndLockAll()
    {
        if (runeSlotButtons != null)
        {
            for (int i = 0; i < runeSlotButtons.Length; i++)
            {
                if (runeSlotButtons[i] == null)
                    continue;

                runeSlotButtons[i].SetLocked(true);
                runeSlotButtons[i].SetRune(null);
            }
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void ClearRuneIconButtons()
    {
        if (runeIconButtons != null)
        {
            for (int i = 0; i < runeIconButtons.Length; i++)
            {
                if (runeIconButtons[i] != null)
                    runeIconButtons[i].SetRuneData(null, false, 0);
            }
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    public void SaveBeforeBattle()
    {
        SaveCurrentRuneSetting();
    }

    public void ShowWarning(string message)
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
            warningUI.Show(message);
        else
            Debug.LogWarning("[RuneSettingPanel] " + message);
    }
}