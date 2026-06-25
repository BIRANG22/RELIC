using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class RuneSettingPanel : MonoBehaviour
{
    [Header("Rune Slots")]
    [SerializeField] private RuneSlotButton[] runeSlotButtons;

    [Header("Rune Slot Unlock Settings")]
    [SerializeField] private int defaultUnlockedRuneSlotCount = 4;
    [SerializeField] private int levelUpsPerRuneSlotUnlock = 2;

    [Header("Rune Icon List Panel")]
    [SerializeField] private GameObject runeIconSelectPanel;
    [SerializeField] private RuneIconButton[] runeIconButtons;

    [Header("Rune Info Area")]
    [SerializeField] private TMP_Text runeInfoTitleText;
    [SerializeField] private TMP_Text runeInfoEffectText;
    [SerializeField] private string emptyRuneInfoTitle = "룬 정보";
    [SerializeField, TextArea] private string emptyRuneInfoEffect = "룬을 선택하면 정보가 표시됩니다.";

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

        AutoBindRuneInfoTexts();
        ClearRuneInfo();

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

        AutoBindRuneInfoTexts();

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

        ClearRuneInfo();

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
        int totalSlotCount = runeSlotButtons != null ? runeSlotButtons.Length : 0;
        int safeBaseSlotCount = Mathf.Max(0, defaultUnlockedRuneSlotCount);
        int safeUnlockInterval = Mathf.Max(1, levelUpsPerRuneSlotUnlock);
        int safeLevel = Mathf.Max(1, level);
        int extraUnlockedSlotCount = (safeLevel - 1) / safeUnlockInterval;

        return Mathf.Clamp(safeBaseSlotCount + extraUnlockedSlotCount, 0, totalSlotCount);
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

        result.Sort(CompareRuneDisplayOrder);

        return result.ToArray();
    }

    private bool IsRuneCandidateForCurrentCharacter(RuneData rune)
    {
        if (rune == null)
            return false;

        if (string.IsNullOrWhiteSpace(currentCharacterId))
            return false;

        int runeNumber = GetRuneNumber(rune.RuneId);

        if (IsCommonRuneNumber(runeNumber))
            return true;

        if (IsCurrentCharacterRuneNumber(runeNumber))
            return true;

        bool isCommonRuneByData = rune.TargetCharacterId == "All";
        bool isCharacterRuneByData = rune.TargetCharacterId == currentCharacterId;

        return isCommonRuneByData || isCharacterRuneByData;
    }

    private int CompareRuneDisplayOrder(RuneData a, RuneData b)
    {
        int groupA = GetRuneDisplayGroup(a);
        int groupB = GetRuneDisplayGroup(b);

        if (groupA != groupB)
            return groupA.CompareTo(groupB);

        int numberA = GetRuneNumber(a != null ? a.RuneId : null);
        int numberB = GetRuneNumber(b != null ? b.RuneId : null);

        if (numberA != numberB)
        {
            if (numberA <= 0)
                return 1;

            if (numberB <= 0)
                return -1;

            return numberA.CompareTo(numberB);
        }

        string idA = a != null ? a.RuneId : string.Empty;
        string idB = b != null ? b.RuneId : string.Empty;

        return string.CompareOrdinal(idA, idB);
    }

    private int GetRuneDisplayGroup(RuneData rune)
    {
        if (rune == null)
            return 99;

        int runeNumber = GetRuneNumber(rune.RuneId);

        if (IsCommonRuneNumber(runeNumber))
            return 0;

        if (IsCurrentCharacterRuneNumber(runeNumber))
            return 1;

        if (rune.TargetCharacterId == "All")
            return 2;

        if (rune.TargetCharacterId == currentCharacterId)
            return 3;

        return 99;
    }

    private bool IsCommonRuneNumber(int runeNumber)
    {
        return runeNumber >= 16 && runeNumber <= 25;
    }

    private bool IsCurrentCharacterRuneNumber(int runeNumber)
    {
        int characterNumber = GetCurrentCharacterNumber();

        if (characterNumber < 1 || characterNumber > 3)
            return false;

        int start = ((characterNumber - 1) * 5) + 1;
        int end = start + 4;

        return runeNumber >= start && runeNumber <= end;
    }

    private int GetCurrentCharacterNumber()
    {
        return GetTrailingNumber(currentCharacterId);
    }

    private int GetRuneNumber(string runeId)
    {
        return GetTrailingNumber(runeId);
    }

    private int GetTrailingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return -1;

        int end = value.Length - 1;

        while (end >= 0 && char.IsWhiteSpace(value[end]))
            end--;

        if (end < 0 || !char.IsDigit(value[end]))
            return -1;

        int start = end;

        while (start >= 0 && char.IsDigit(value[start]))
            start--;

        string numberText = value.Substring(start + 1, end - start);

        if (int.TryParse(numberText, out int number))
            return number;

        return -1;
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
        // 테스트를 위해 룬 아이템 자체는 전부 해금 상태로 둔다.
        // 장착 가능 여부는 룬 슬롯 잠금 상태에서만 제한한다.
        return false;
    }

    private int GetRequiredLevelForRune(RuneData runeData)
    {
        // 현재 테스트 단계에서는 캐릭터 룬과 공용룬을 모두 LV.1부터 사용할 수 있게 둔다.
        return 1;
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

        EnsureRuntimeRuneSlots();

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedRuneIds.Length)
            return null;

        return currentRuntimeData.EquippedRuneIds[slotIndex];
    }

    private void SetRuntimeRuneId(int slotIndex, string runeId)
    {
        if (currentRuntimeData == null)
            return;

        EnsureRuntimeRuneSlots();

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedRuneIds.Length)
            return;

        currentRuntimeData.EquippedRuneIds[slotIndex] = runeId;
    }

    private void EnsureRuntimeRuneSlots()
    {
        if (currentRuntimeData == null)
            return;

        int requiredLength = Mathf.Max(12, runeSlotButtons != null ? runeSlotButtons.Length : 0);

        if (currentRuntimeData.EquippedRuneIds != null &&
            currentRuntimeData.EquippedRuneIds.Length >= requiredLength)
        {
            return;
        }

        string[] expanded = new string[requiredLength];

        if (currentRuntimeData.EquippedRuneIds != null)
        {
            int copyLength = Mathf.Min(
                currentRuntimeData.EquippedRuneIds.Length,
                expanded.Length);

            for (int i = 0; i < copyLength; i++)
                expanded[i] = currentRuntimeData.EquippedRuneIds[i];
        }

        currentRuntimeData.EquippedRuneIds = expanded;
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
        ClearRuneInfo();
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

    public void ClearForEmptyCharacter()
    {
        currentCharacterId = null;
        currentMasterData = null;
        currentRuntimeData = null;

        ClearRuneSlotsAndLockAll();
        ClearRuneIconButtons();
        ClearRuneInfo();

        if (runeIconSelectPanel != null)
            runeIconSelectPanel.SetActive(true);
    }

    public void ShowRuneInfo(RuneData runeData)
    {
        AutoBindRuneInfoTexts();

        if (runeData == null)
        {
            ClearRuneInfo();
            return;
        }

        if (runeInfoTitleText != null)
            runeInfoTitleText.text = string.IsNullOrWhiteSpace(runeData.Name) ? runeData.RuneId : runeData.Name;

        if (runeInfoEffectText != null)
            runeInfoEffectText.text = BuildRuneEffectText(runeData);
    }

    private void ClearRuneInfo()
    {
        AutoBindRuneInfoTexts();

        if (runeInfoTitleText != null)
            runeInfoTitleText.text = emptyRuneInfoTitle;

        if (runeInfoEffectText != null)
            runeInfoEffectText.text = emptyRuneInfoEffect;
    }

    private void AutoBindRuneInfoTexts()
    {
        if (runeInfoTitleText != null && runeInfoEffectText != null)
            return;

        Transform infoArea = FindDeepChild(transform, "RuneInfoArea");

        if (infoArea == null)
            infoArea = FindDeepChild(transform, "RuenInfoArea");

        TMP_Text[] texts = infoArea != null
            ? infoArea.GetComponentsInChildren<TMP_Text>(true)
            : GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string objectName = texts[i].gameObject.name;

            if (runeInfoTitleText == null && objectName == "TitleText")
                runeInfoTitleText = texts[i];
            else if (runeInfoEffectText == null && objectName == "EffectText")
                runeInfoEffectText = texts[i];
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private string BuildRuneEffectText(RuneData runeData)
    {
        if (runeData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(runeData.EffectDesc))
            return ColorizeRuneEffectDesc(NormalizeRuneEffectDesc(runeData.EffectDesc));

        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(runeData.EffectIds))
        {
            builder.Append("- ");
            builder.Append(runeData.EffectIds);
        }

        if (builder.Length <= 0)
            builder.Append("등록된 효과 설명이 없습니다.");

        return ColorizeRuneEffectDesc(builder.ToString());
    }

    private string NormalizeRuneEffectDesc(string effectDesc)
    {
        if (string.IsNullOrEmpty(effectDesc))
            return string.Empty;

        return effectDesc
            .Replace("\\n", "\n")
            .Replace("\\r", "")
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n");
    }

    private string ColorizeRuneEffectDesc(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        const string orangeColor = "#FF9A00";
        const string pattern = @"(?<![A-Za-z0-9_])([+-]?\d+(?:\.\d+)?%?|[+-])";

        return Regex.Replace(text, pattern, match =>
        {
            string value = match.Value;

            if (string.IsNullOrEmpty(value))
                return value;

            return "<color=" + orangeColor + ">" + value + "</color>";
        });
    }

    private string GetEffectDisplayName(SkillEffectEntry entry)
    {
        if (entry == null)
            return string.Empty;

        if (entry.EffectData != null && !string.IsNullOrWhiteSpace(entry.EffectData.Name))
            return entry.EffectData.Name;

        return entry.EffectId;
    }

    private string BuildEffectAmountText(SkillEffectEntry entry)
    {
        if (entry == null)
            return string.Empty;

        List<string> parts = new List<string>();

        if (entry.ValueAmount != 0)
        {
            string valueCalcTypeName = entry.ValueCalcType.ToString();
            string valueText = valueCalcTypeName == "Percent"
                ? entry.ValueAmount + "%"
                : entry.ValueAmount.ToString();

            parts.Add("수치 " + valueText);
        }

        if (entry.CountAmount > 0)
            parts.Add("횟수 " + entry.CountAmount);

        return parts.Count > 0 ? "(" + string.Join(", ", parts) + ")" : string.Empty;
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
