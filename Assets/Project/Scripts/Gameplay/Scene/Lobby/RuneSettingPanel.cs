using Relic.Gameplay.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneSettingPanel : MonoBehaviour
{
    [Header("Rune Slots")]
    [SerializeField] private RuneSlotButton[] runeSlotButtons;

    [Header("Rune Slot Unlock Settings")]
    [SerializeField] private int[] runeSlotUnlockLevels = { 1, 1, 3, 5, 7, 10 };

    [Header("Locked Rune Slot Visual")]
    [Tooltip("잠긴 룬 슬롯에 표시할 자물쇠 스프라이트입니다.")]
    [SerializeField] private Sprite lockedRuneSlotSprite;
    [Tooltip("룬 슬롯의 아이콘 Image를 순서대로 연결합니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private Image[] runeSlotIconImages;
    [Tooltip("잠긴 룬 슬롯 자물쇠 이미지의 알파값입니다. 0~255 기준입니다.")]
    [SerializeField, Range(0, 255)] private int lockedRuneSlotAlpha = 150;

    [Header("Rune Icon List Panel")]
    [SerializeField] private GameObject runeIconSelectPanel;
    [SerializeField] private RuneIconButton[] runeIconButtons;

    [Header("Rune Icon Select Panel Move Effect")]
    [Tooltip("룬 선택 패널이 닫혀 있을 때의 X 좌표입니다.")]
    [SerializeField] private float runeSelectPanelHiddenX = 1200f;
    [Tooltip("룬 탭에서 표시될 때의 X 좌표입니다.")]
    [SerializeField] private float runeSelectPanelShownX = 790f;
    [Tooltip("룬 선택 패널이 목표 위치까지 이동하는 시간입니다.")]
    [SerializeField] private float runeSelectPanelMoveDuration = 0.25f;

    [Header("Shared Info Area")]
    [SerializeField] private GameObject sharedInfoArea;
    [SerializeField] private TMP_Text runeInfoTitleText;
    [SerializeField] private TMP_Text runeInfoEffectText;
    [Tooltip("같은 InfoArea를 사용하는 SkillSettingPanel입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private SkillSettingPanel sharedSkillSettingPanel;
    [SerializeField] private string emptyRuneInfoTitle = "룬 정보";
    [SerializeField, TextArea] private string emptyRuneInfoEffect = "룬을 선택하면 정보가 표시됩니다.";

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private bool runeSelectPanelAllowed = true;
    private RectTransform runeIconSelectPanelRect;
    private Coroutine runeSelectPanelMoveCoroutine;

    public bool IsRuneInteractionEnabled => runeSelectPanelAllowed;
    public bool ShouldClearInfoOnHoverExit => !runeSelectPanelAllowed;

    public event Action OnRuneChanged;

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        AutoBindRuneInfoTexts();
        BindSharedSkillSettingPanel();
        BindRuneSelectPanelRect();
        ClearRuneInfo();

        InitRuneSlots();
        AutoBindRuneSlotIconImages();
        InitRuneIconButtons();
        ApplyDefaultLockedState();

        // 탭 전환 시 활성화/비활성화하지 않고 X 좌표 이동만 사용한다.
        SetRuneSelectPanelActive();
        SetRuneSelectPanelXImmediate(runeSelectPanelHiddenX);
    }

    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        AutoBindRuneInfoTexts();
        BindSharedSkillSettingPanel();
        BindRuneSelectPanelRect();
        SetRuneSelectPanelActive();
        MoveRuneSelectPanel(runeSelectPanelAllowed);

        if (currentRuntimeData != null)
            RefreshCurrentRuneView();
        else
            ApplyDefaultLockedState();

    }


    private void OnDisable()
    {
        if (runeSelectPanelMoveCoroutine != null)
        {
            StopCoroutine(runeSelectPanelMoveCoroutine);
            runeSelectPanelMoveCoroutine = null;
        }
    }

    private void LateUpdate()
    {
        // 스킬에서 룬으로 빠르게 이동할 때 늦게 실행된 스킬 호버가
        // TypeText 같은 스킬 전용 오브젝트를 다시 켜는 경우가 있습니다.
        // 룬을 호버하는 동안에는 매 프레임 룬 전용 표시 상태를 유지합니다.
        if (LobbyInfoHoverState.IsRuneHovered)
            HideSkillOnlyInfoObjects();
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


    private void AutoBindRuneSlotIconImages()
    {
        int slotCount = runeSlotButtons != null ? runeSlotButtons.Length : 0;

        if (slotCount <= 0)
            return;

        if (runeSlotIconImages == null || runeSlotIconImages.Length != slotCount)
            Array.Resize(ref runeSlotIconImages, slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            if (runeSlotIconImages[i] != null || runeSlotButtons[i] == null)
                continue;

            Transform iconTransform = FindDeepChild(runeSlotButtons[i].transform, "RuneImg");

            if (iconTransform == null)
                iconTransform = FindDeepChild(runeSlotButtons[i].transform, "IconImg");

            if (iconTransform == null)
                iconTransform = FindDeepChild(runeSlotButtons[i].transform, "Icon");

            if (iconTransform != null)
                runeSlotIconImages[i] = iconTransform.GetComponent<Image>();
        }
    }

    private void RefreshLockedRuneSlotVisuals()
    {
        AutoBindRuneSlotIconImages();

        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            bool locked = runeSlotButtons[i] != null && runeSlotButtons[i].IsLocked;
            ApplyLockedRuneSlotVisual(i, locked);
        }
    }

    private void ApplyLockedRuneSlotVisual(int slotIndex, bool locked)
    {
        if (runeSlotIconImages == null || slotIndex < 0 || slotIndex >= runeSlotIconImages.Length)
            return;

        Image iconImage = runeSlotIconImages[slotIndex];

        if (iconImage == null)
            return;

        if (!locked)
        {
            RuneData equippedRune = null;

            if (runeSlotButtons != null && slotIndex < runeSlotButtons.Length && runeSlotButtons[slotIndex] != null)
                equippedRune = runeSlotButtons[slotIndex].EquippedRune;

            if (equippedRune == null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconImage.color = Color.white;
            }

            return;
        }

        iconImage.sprite = lockedRuneSlotSprite;
        iconImage.enabled = lockedRuneSlotSprite != null;

        Color color = Color.white;
        color.a = Mathf.Clamp01(lockedRuneSlotAlpha / 255f);
        iconImage.color = color;
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

    public void SetRuneSelectPanelEnabledForTab(bool enabled)
    {
        runeSelectPanelAllowed = enabled;

        if (!enabled)
        {
            ClearRuneInfo();
            SetRuneSelectPanelVisible(false);
            return;
        }

        // 룬 탭에서는 기존처럼 룬 선택 목록을 표시한다.
        SetRuneSelectPanelVisible(true);
    }

    public void SetRuneSelectPanelVisible(bool visible)
    {
        MoveRuneSelectPanel(runeSelectPanelAllowed && visible);
    }

    private void BindRuneSelectPanelRect()
    {
        if (runeIconSelectPanelRect != null)
            return;

        if (runeIconSelectPanel != null)
            runeIconSelectPanelRect = runeIconSelectPanel.transform as RectTransform;
    }

    private void SetRuneSelectPanelActive()
    {
        if (runeIconSelectPanel != null && !runeIconSelectPanel.activeSelf)
            runeIconSelectPanel.SetActive(true);
    }

    private void MoveRuneSelectPanel(bool show)
    {
        BindRuneSelectPanelRect();
        SetRuneSelectPanelActive();

        if (runeIconSelectPanelRect == null)
            return;

        if (runeSelectPanelMoveCoroutine != null)
            StopCoroutine(runeSelectPanelMoveCoroutine);

        float targetX = show ? runeSelectPanelShownX : runeSelectPanelHiddenX;
        runeSelectPanelMoveCoroutine = StartCoroutine(MoveRuneSelectPanelRoutine(targetX));
    }

    private IEnumerator MoveRuneSelectPanelRoutine(float targetX)
    {
        Vector2 startPosition = runeIconSelectPanelRect.anchoredPosition;
        float duration = Mathf.Max(0.01f, runeSelectPanelMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 position = runeIconSelectPanelRect.anchoredPosition;
            position.x = Mathf.Lerp(startPosition.x, targetX, easedT);
            runeIconSelectPanelRect.anchoredPosition = position;

            yield return null;
        }

        SetRuneSelectPanelXImmediate(targetX);
        runeSelectPanelMoveCoroutine = null;
    }

    private void SetRuneSelectPanelXImmediate(float x)
    {
        BindRuneSelectPanelRect();

        if (runeIconSelectPanelRect == null)
            return;

        Vector2 position = runeIconSelectPanelRect.anchoredPosition;
        position.x = x;
        runeIconSelectPanelRect.anchoredPosition = position;
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

        SetRuneSelectPanelVisible(true);
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
        RefreshLockedRuneSlotVisuals();
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

            bool slotLocked = runeSlotButtons[i] != null && runeSlotButtons[i].IsLocked;

            if (slotLocked || !IsRuneValidForCurrentCharacter(runeData))
            {
                if (!string.IsNullOrWhiteSpace(runeId))
                    changed = true;

                runeData = null;
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
    }

    private void ApplyDefaultLockedState()
    {
        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] != null)
                runeSlotButtons[i].SetLocked(true);

            ApplyLockedRuneSlotVisual(i, true);
        }
    }

    private int GetUnlockedRuneSlotCount(int level)
    {
        int totalSlotCount = Mathf.Min(
            runeSlotButtons != null ? runeSlotButtons.Length : 0,
            runeSlotUnlockLevels != null ? runeSlotUnlockLevels.Length : 0);

        int safeLevel = Mathf.Max(1, level);
        int unlockedSlotCount = 0;

        for (int i = 0; i < totalSlotCount; i++)
        {
            int requiredLevel = Mathf.Max(1, runeSlotUnlockLevels[i]);

            if (safeLevel >= requiredLevel)
                unlockedSlotCount++;
        }

        return unlockedSlotCount;
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

        SetRuneSelectPanelVisible(true);

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
        // Rune_01 ~ Rune_20은 모든 캐릭터가 사용하는 공용룬입니다.
        return runeNumber >= 1 && runeNumber <= 20;
    }

    private bool IsCurrentCharacterRuneNumber(int runeNumber)
    {
        int characterNumber = GetCurrentCharacterNumber();

        // 캐릭터 전용룬은 Rune_51부터 캐릭터 순서대로 5개씩 배정됩니다.
        // 1: 힐트(51~55), 2: 카야(56~60), 3: 헤이즈(61~65),
        // 4: 이네스(66~70), 5: 레이나(71~75)
        if (characterNumber < 1 || characterNumber > 5)
            return false;

        int start = 51 + ((characterNumber - 1) * 5);
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
        // 프리뷰 탭에서는 장착 룬 정보만 확인하며 클릭 동작은 막는다.
        if (!runeSelectPanelAllowed)
            return;

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

        const int maxRuneSlotCount = 6;
        int connectedSlotCount = runeSlotButtons != null ? runeSlotButtons.Length : 0;
        int requiredLength = Mathf.Min(maxRuneSlotCount, connectedSlotCount > 0 ? connectedSlotCount : maxRuneSlotCount);

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

        SetRuneSelectPanelVisible(true);

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
                ApplyLockedRuneSlotVisual(i, true);
            }
        }

        SetRuneSelectPanelVisible(true);

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

        SetRuneSelectPanelVisible(true);
    }

    public void ClearForEmptyCharacter()
    {
        currentCharacterId = null;
        currentMasterData = null;
        currentRuntimeData = null;

        ClearRuneSlotsAndLockAll();
        ClearRuneIconButtons();
        ClearRuneInfo();

        SetRuneSelectPanelVisible(true);
    }

    public void ShowRuneSlotInfo(int slotIndex, RuneData runeData, bool isLocked)
    {
        LobbyInfoHoverState.NotifyRuneInfoShown();
        AutoBindRuneInfoTexts();
        HideSkillOnlyInfoObjects();

        if (isLocked)
        {
            int requiredLevel = GetRuneSlotRequiredLevel(slotIndex);

            if (runeInfoTitleText != null)
                runeInfoTitleText.text = string.Empty;

            if (runeInfoEffectText != null)
                runeInfoEffectText.text = requiredLevel > 0
                    ? $"캐릭터 {requiredLevel}레벨에 오픈됩니다."
                    : "잠긴 룬 슬롯입니다.";

            return;
        }

        ShowRuneInfo(runeData);
    }

    private int GetRuneSlotRequiredLevel(int slotIndex)
    {
        if (runeSlotUnlockLevels == null || slotIndex < 0 || slotIndex >= runeSlotUnlockLevels.Length)
            return 0;

        return Mathf.Max(1, runeSlotUnlockLevels[slotIndex]);
    }

    public void ShowRuneInfo(RuneData runeData)
    {
        LobbyInfoHoverState.NotifyRuneInfoShown();
        AutoBindRuneInfoTexts();
        HideSkillOnlyInfoObjects();

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

    public void ClearRuneInfoFromHover()
    {
        ClearRuneInfoFromHover(LobbyInfoHoverState.CurrentVersion);
    }

    public void ClearRuneInfoFromHover(int hoverVersion)
    {
        StartCoroutine(ClearRuneInfoAfterHoverDelay(hoverVersion));
    }

    private IEnumerator ClearRuneInfoAfterHoverDelay(int hoverVersion)
    {
        yield return new WaitForSecondsRealtime(LobbyInfoHoverState.ClearDelaySeconds);

        if (LobbyInfoHoverState.CanClearRuneInfo(hoverVersion))
            ClearRuneInfo();
    }

    public void SetEmptyInfoText(string title, string effect)
    {
        emptyRuneInfoTitle = title ?? string.Empty;
        emptyRuneInfoEffect = effect ?? string.Empty;
        ClearRuneInfo();
    }

    private void ClearRuneInfo()
    {
        AutoBindRuneInfoTexts();
        HideSkillOnlyInfoObjects();

        if (runeInfoTitleText != null)
            runeInfoTitleText.text = emptyRuneInfoTitle;

        if (runeInfoEffectText != null)
            runeInfoEffectText.text = emptyRuneInfoEffect;
    }

    private void AutoBindRuneInfoTexts()
    {
        if (runeInfoTitleText != null && runeInfoEffectText != null)
            return;

        Transform infoAreaTransform = sharedInfoArea != null
            ? sharedInfoArea.transform
            : FindDeepChild(transform.root, "InfoArea");

        if (sharedInfoArea == null && infoAreaTransform != null)
            sharedInfoArea = infoAreaTransform.gameObject;

        TMP_Text[] texts = infoAreaTransform != null
            ? infoAreaTransform.GetComponentsInChildren<TMP_Text>(true)
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

    private void BindSharedSkillSettingPanel()
    {
        if (sharedSkillSettingPanel == null)
            sharedSkillSettingPanel = FindFirstObjectByType<SkillSettingPanel>(FindObjectsInactive.Include);
    }

    private void HideSkillOnlyInfoObjects()
    {
        // SkillSettingPanel이 실제로 참조하는 TypeText, Range, CostText, ValueText를 먼저 숨깁니다.
        // 동일한 이름의 오브젝트가 여러 개 있어도 잘못된 대상을 끄지 않도록 합니다.
        BindSharedSkillSettingPanel();
        if (sharedSkillSettingPanel != null)
            sharedSkillSettingPanel.SetSkillInfoExtrasVisible(false);

        Transform infoArea = sharedInfoArea != null
            ? sharedInfoArea.transform
            : FindDeepChild(transform.root, "InfoArea");

        if (infoArea == null)
            return;

        // 룬 정보에는 스킬 전용 라벨과 값 오브젝트를 모두 표시하지 않습니다.
        SetChildActive(infoArea, "Infotext_1", false);
        SetChildActive(infoArea, "Infotext_2", false);
        SetChildActive(infoArea, "Infotext_3", false);
        SetChildActive(infoArea, "Infotext_4", false);

        SetChildActive(infoArea, "CostText", false);
        SetChildActive(infoArea, "TypeText", false);
        SetChildActive(infoArea, "ValueText", false);

        // 실제 구조: InfoArea/Range/RangeImg
        Transform rangeRoot = FindDeepChild(infoArea, "Range");
        Transform rangeImageTransform = rangeRoot != null
            ? FindDeepChild(rangeRoot, "RangeImg")
            : FindDeepChild(infoArea, "RangeImg");

        if (rangeImageTransform == null)
            rangeImageTransform = FindDeepChild(infoArea, "RangeImage");

        if (rangeImageTransform != null)
        {
            Image image = rangeImageTransform.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.enabled = false;
            }

            rangeImageTransform.gameObject.SetActive(false);
        }

        if (rangeRoot != null)
            rangeRoot.gameObject.SetActive(false);
    }

    private void SetChildActive(Transform root, string childName, bool active)
    {
        Transform child = FindDeepChild(root, childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private void ClearChildText(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);
        if (child == null)
            return;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null)
            text.text = string.Empty;
    }

    private string StripRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return Regex.Replace(text, "<[^>]+>", string.Empty);
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
            return StripRichTextTags(NormalizeRuneEffectDesc(runeData.EffectDesc));

        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(runeData.EffectIds))
        {
            builder.Append("- ");
            builder.Append(runeData.EffectIds);
        }

        if (builder.Length <= 0)
            builder.Append("등록된 효과 설명이 없습니다.");

        return StripRichTextTags(builder.ToString());
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
            string valueText = entry.ValueAmount.ToString();

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
