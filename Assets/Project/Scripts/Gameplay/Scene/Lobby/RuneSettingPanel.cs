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

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    public event Action OnRuneChanged;

    private void Awake()
    {
        InitRuneSlots();
        InitRuneIconButtons();

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void InitRuneSlots()
    {
        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].Init(this, i);
        }
    }

    private void InitRuneIconButtons()
    {
        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] != null)
                runeIconButtons[i].Init(this);
        }
    }

    public void OpenCharacterSetting(string characterId)
    {
        SaveCurrentRuneSetting();

        currentCharacterId = characterId;
        currentMasterData = null;
        currentRuntimeData = null;

        if (DataManager.Instance == null)
        {
            ClearRuneSlots();
            ClearRuneIconButtons();
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearRuneSlots();
            ClearRuneIconButtons();
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearRuneSlots();
            ClearRuneIconButtons();
            return;
        }

        LoadCurrentRuneSetting();
        ApplyRuneSlotUnlockState();
        RefreshRuneIconButtons();

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void LoadCurrentRuneSetting()
    {
        if (currentRuntimeData == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            RuneData runeData = null;
            string runeId = GetRuntimeRuneId(i);

            if (!string.IsNullOrWhiteSpace(runeId))
                DataManager.Instance.RuneDatabase.TryGet(runeId, out runeData);

            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].SetRune(runeData);
        }
    }

    private void ApplyRuneSlotUnlockState()
    {
        if (currentRuntimeData == null)
            return;

        int characterLevel = currentRuntimeData.Level;
        int unlockedSlotCount = GetUnlockedRuneSlotCount(characterLevel);

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            bool unlocked = i < unlockedSlotCount;
            runeSlotButtons[i].SetLocked(!unlocked);
        }

        SaveCurrentRuneSetting();
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

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            RuneData rune = runeSlotButtons[i].EquippedRune;
            SetRuntimeRuneId(i, rune != null ? rune.RuneId : null);
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    private void RefreshRuneIconButtons()
    {
        RuneData[] candidates = GetCurrentRuneCandidates();

        Debug.Log($"[RuneSettingPanel] Rune Candidates Count: {(candidates != null ? candidates.Length : -1)}");

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] == null)
                continue;

            if (candidates != null && i < candidates.Length)
                runeIconButtons[i].SetRuneData(candidates[i]);
            else
                runeIconButtons[i].SetRuneData(null);
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

        if (currentCharacterId == null)
            return result.ToArray();

        List<RuneData> allRunes =
            DataManager.Instance.RuneDatabase.GetAll();

        for (int i = 0; i < allRunes.Count; i++)
        {
            RuneData rune = allRunes[i];

            if (rune == null)
                continue;

            // 공유 룬
            bool isCommonRune =
                rune.TargetCharacterId == "All";

            // 캐릭터 전용 룬
            bool isCharacterRune =
                rune.TargetCharacterId == currentCharacterId;

            if (isCommonRune || isCharacterRune)
                AddRuneIfNotExists(result, rune);
        }

        return result.ToArray();
    }

    private void AddRuneIfNotExists(List<RuneData> list, RuneData rune)
    {
        if (rune == null)
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
        Debug.Log(runeData != null
            ? $"[RuneSettingPanel] TryEquip: {runeData.RuneId}"
            : "[RuneSettingPanel] TryEquip: null");

        if (runeData == null)
            return;

        if (IsRuneEquipped(runeData))
        {
            UnequipRune(runeData);
            return;
        }

        RuneSlotButton emptySlot = FindFirstEmptyUnlockedSlot();

        if (emptySlot == null)
        {
            Debug.LogWarning("[RuneSettingPanel] 비어 있는 언락 룬 슬롯이 없습니다.");
            return;
        }

        Debug.Log($"[RuneSettingPanel] Equip to Slot: {emptySlot.SlotIndex}");
        Debug.Log($"[RuneSettingPanel] Send Rune To Slot: {runeData.RuneId}");

        emptySlot.SetRune(runeData);

        SetRuntimeRuneId(emptySlot.SlotIndex, runeData.RuneId);

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);

        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    public void UnequipRune(RuneData runeData)
    {
        if (runeData == null)
            return;

        bool removed = false;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            if (runeSlotButtons[i].EquippedRune == runeData)
            {
                runeSlotButtons[i].SetRune(null);
                removed = true;
                break;
            }
        }

        if (!removed)
            return;

        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    public void UnequipRuneFromSlot(RuneSlotButton slotButton)
    {
        if (slotButton == null)
            return;

        if (slotButton.IsLocked)
            return;

        if (slotButton.EquippedRune == null)
            return;

        slotButton.SetRune(null);

        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    private RuneSlotButton FindFirstEmptyUnlockedSlot()
    {
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
        if (runeData == null)
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

    private void RefreshRuneIconEquippedStates()
    {
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
        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].SetRune(null);
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    private void ClearRuneIconButtons()
    {
        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] != null)
                runeIconButtons[i].SetRuneData(null);
        }

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    public void SaveBeforeBattle()
    {
        SaveCurrentRuneSetting();
    }
}