using Relic.Gameplay.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class RuneSettingPanel : MonoBehaviour
{
    [Header("Rune Slots")]
    [SerializeField] private RuneSlotButton[] runeSlotButtons;

    [Header("Rune Icon List Panel")]
    [SerializeField] private GameObject runeIconSelectPanel;
    [SerializeField] private RuneIconButton[] runeIconButtons;
    [Tooltip("RuneIconSelectPanel/Shared 아래에 배치한 RuneIconButtonSlot 프리팹(또는 템플릿)입니다.")]
    [SerializeField] private RuneIconButton runeIconButtonSlotPrefab;
    [SerializeField] private Transform exclusiveRuneRoot;
    [SerializeField] private Transform sharedRuneRoot;

    private readonly List<RuneIconButton> generatedRuneIconButtons = new();

    [Header("Common Rune Purchase")]
    [SerializeField] private GameObject buyButtonRoot;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyPriceText;
    [SerializeField] private string emptyBuyPriceText = "0";
    [SerializeField] private Color buyButtonInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color buyButtonReadyColor = Color.white;

    private GameObject sharedInfoArea;
    private TMP_Text runeInfoTitleText;
    private TMP_Text runeInfoEffectText;
    private TMP_Text runeInfoRarityText;
    private SkillSettingPanel sharedSkillSettingPanel;
    private string emptyRuneInfoTitle;
    private string emptyRuneInfoEffect;

    private Color commonRarityColor = Color.white;
    private Color rareRarityColor = Color.white;
    private Color epicRarityColor = Color.white;
    private Color uniqueRarityColor = Color.white;
    private Color exclusiveRarityColor = new Color(1f, 0.82f, 0.2f, 1f);

    private Color valueHighlightColor = Color.yellow;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    [Header("Rune Tooltip")]
    [SerializeField] private GameObject runeTooltipPanel;
    [SerializeField] private TMP_Text runeTooltipNameText;
    [SerializeField] private TMP_Text runeTooltipDescriptionText;
    [SerializeField] private float runeTooltipFadeInDuration = 0.12f;
    [SerializeField] private float runeTooltipFadeOutDuration = 0.05f;
    [SerializeField] private float runeTooltipOffsetX = 190f;
    [SerializeField] private float runeTooltipOffsetY = -20f;
    [SerializeField] private float runeTooltipScreenPadding = 20f;

    private CanvasGroup runeTooltipCanvasGroup;
    private RectTransform runeTooltipRectTransform;
    private Coroutine runeTooltipFadeCoroutine;

    private Setting settingController;
    private RecordPanelUI recordPanelUI;
    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private bool runeSelectPanelAllowed = true;
    private RuneSlotButton selectedRuneSlot;
    private RuneData selectedPurchaseRune;
    private RuneData currentDisplayedRune;

    // RuneIconSelectPanel <-> RuneSettingPanel drag & drop state
    private RuneData draggedRune;
    private RuneSlotButton draggedSourceSlot;
    private RectTransform runeDragVisual;
    private Canvas runeDragCanvas;
    private bool dragDroppedOnSlot;

    public bool IsDisplayingRuneInfo => false;

    public bool IsRuneInteractionEnabled => runeSelectPanelAllowed;
    public bool ShouldClearInfoOnHoverExit => false;

    public event Action OnRuneChanged;

    public void SetSettingController(Setting controller)
    {
        settingController = controller;
    }

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindDynamicRuneListLayout();
        BindRuneTooltipUI();
        SetRuneTooltipImmediate(false);
        ClearRuneInfo();

        InitRuneSlots();
        InitRuneIconButtons();
        BindCommonRunePurchaseUI();
        ApplyDefaultLockedState();

        // 새 CharacterSettingPanel에서는 룬 영역이 고정 배치되므로 항상 활성 상태만 유지한다.
        SetRuneSelectPanelActive();
    }


    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindDynamicRuneListLayout();
        BindRuneTooltipUI();
        SetRuneTooltipImmediate(false);
        BindCommonRunePurchaseUI();
        SetRuneSelectPanelActive();

        if (currentRuntimeData != null)
            RefreshCurrentRuneView();
        else
            ApplyDefaultLockedState();

    }


    private void OnDisable()
    {
        if (runeTooltipFadeCoroutine != null)
        {
            StopCoroutine(runeTooltipFadeCoroutine);
            runeTooltipFadeCoroutine = null;
        }

        SetRuneTooltipImmediate(false);
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

    private void BindCommonRunePurchaseUI()
    {
        if (buyButtonRoot == null && runeIconSelectPanel != null)
        {
            Transform found = FindDeepChild(runeIconSelectPanel.transform, "BuyButton");
            if (found != null)
                buyButtonRoot = found.gameObject;
        }

        if (buyButtonRoot == null && transform.parent != null)
        {
            Transform found = FindDeepChild(transform.parent, "BuyButton");
            if (found != null)
                buyButtonRoot = found.gameObject;
        }

        if (buyButton == null && buyButtonRoot != null)
        {
            Transform buttonTransform = FindDeepChild(buyButtonRoot.transform, "Button");
            if (buttonTransform != null)
                buyButton = buttonTransform.GetComponent<Button>();

            if (buyButton == null)
                buyButton = buyButtonRoot.GetComponentInChildren<Button>(true);
        }

        if (buyPriceText == null && buyButtonRoot != null)
        {
            Transform priceTransform = FindDeepChild(buyButtonRoot.transform, "price_text");
            if (priceTransform != null)
                buyPriceText = priceTransform.GetComponent<TMP_Text>();
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuySelectedCommonRune);
            buyButton.onClick.AddListener(BuySelectedCommonRune);
        }

        RefreshCommonRunePurchaseUI();
    }

    public void SetRuneSelectPanelEnabledForTab(bool enabled)
    {
        // 현재 CharacterSettingPanel에서는 탭 전환으로 룬 영역을 숨기거나 이동하지 않는다.
        runeSelectPanelAllowed = true;
        SetRuneSelectPanelActive();
    }

    public void SetRuneSelectPanelVisible(bool visible)
    {
        // 호환용 API. 룬 목록은 고정 배치이므로 위치/활성 상태를 변경하지 않는다.
        SetRuneSelectPanelActive();
    }

    private void SetRuneSelectPanelActive()
    {
        if (runeIconSelectPanel != null && !runeIconSelectPanel.activeSelf)
            runeIconSelectPanel.SetActive(true);
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

        ClearSelectedPurchaseRune();
        ClearRuneInfo();

        if (DataManager.Instance == null)
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("데이터를 사용할 수 없다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning("선택된 캐릭터가 없다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning(string.Format("캐릭터 데이터를 찾을 수 없다: {0}", characterId));
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearRuneSlotsAndLockAll();
            ClearRuneIconButtons();
            ShowWarning(string.Format("캐릭터 데이터를 찾을 수 없다: {0}", characterId));
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

        if (runeSlotButtons == null)
            return;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            if (runeSlotButtons[i] == null)
                continue;

            int requiredLevel = GetRuneSlotRequiredLevel(i);
            bool unlocked = requiredLevel <= 0 || characterLevel >= requiredLevel;
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
            if (runeSlotButtons[i] == null)
                continue;

            bool locked = GetRuneSlotRequiredLevel(i) > 1;
            runeSlotButtons[i].SetLocked(locked);
        }
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

        if (HasDynamicRuneListLayout())
        {
            RebuildDynamicRuneIconButtons(candidates);
        }
        else if (runeIconButtons != null)
        {
            for (int i = 0; i < runeIconButtons.Length; i++)
            {
                if (runeIconButtons[i] == null)
                    continue;

                if (candidates != null && i < candidates.Length)
                {
                    RuneData runeData = candidates[i];
                    bool locked = !IsCommonRune(runeData) && IsRuneLockedForCurrentState(runeData);
                    int requiredLevel = GetRequiredLevelForRune(runeData);

                    runeIconButtons[i].SetRuneData(runeData, locked, requiredLevel);
                    bool isCommon = IsCommonRune(runeData);
                    bool purchased = !isCommon || IsCommonRunePurchased(runeData);
                    runeIconButtons[i].SetPurchaseState(isCommon, purchased, false);
                }
                else
                {
                    runeIconButtons[i].SetRuneData(null, false, 0);
                    runeIconButtons[i].SetPurchaseState(false, true, false);
                }
            }
        }

        SetRuneSelectPanelVisible(true);
        RefreshRuneIconEquippedStates();
    }

    private void BindDynamicRuneListLayout()
    {
        Transform searchRoot = runeIconSelectPanel != null ? runeIconSelectPanel.transform : transform;

        if (exclusiveRuneRoot == null)
            exclusiveRuneRoot = FindDeepChild(searchRoot, "Exclusive");

        if (sharedRuneRoot == null)
            sharedRuneRoot = FindDeepChild(searchRoot, "Shared");

        if (runeIconButtonSlotPrefab == null)
        {
            Transform template = null;

            if (sharedRuneRoot != null)
                template = FindDeepChild(sharedRuneRoot, "RuneIconButtonSlot");

            if (template == null && exclusiveRuneRoot != null)
                template = FindDeepChild(exclusiveRuneRoot, "RuneIconButtonSlot");

            if (template != null)
            {
                runeIconButtonSlotPrefab = template.GetComponent<RuneIconButton>();
            }
            else
            {
                // 새 RuneIconSelectPanel 구조에서는 Exclusive/Shared가 비어 있어도
                // 기존에 연결되어 있던 RuneIconButton 중 하나를 슬롯 템플릿으로 재사용합니다.
                // 따라서 프리팹 참조를 다시 지정하지 않아도 목록이 새 Root 아래에 생성됩니다.
                runeIconButtonSlotPrefab = GetLegacyRuneButtonTemplate();

                if (runeIconButtonSlotPrefab != null)
                    HideLegacyRuneIconButtons();
            }
        }

        if (runeIconButtonSlotPrefab != null &&
            (runeIconButtonSlotPrefab.transform.parent == sharedRuneRoot ||
             runeIconButtonSlotPrefab.transform.parent == exclusiveRuneRoot))
        {
            runeIconButtonSlotPrefab.gameObject.SetActive(false);
        }
    }


    private RuneIconButton GetLegacyRuneButtonTemplate()
    {
        if (runeIconButtons == null)
            return null;

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            if (runeIconButtons[i] != null)
                return runeIconButtons[i];
        }

        return null;
    }

    private void HideLegacyRuneIconButtons()
    {
        if (runeIconButtons == null)
            return;

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            RuneIconButton button = runeIconButtons[i];
            if (button == null)
                continue;

            // 새 Exclusive/Shared 아래에 이미 배치된 슬롯은 건드리지 않습니다.
            Transform parent = button.transform.parent;
            if (parent == exclusiveRuneRoot || parent == sharedRuneRoot)
                continue;

            button.gameObject.SetActive(false);
        }
    }

    private bool HasDynamicRuneListLayout()
    {
        BindDynamicRuneListLayout();
        return runeIconButtonSlotPrefab != null && exclusiveRuneRoot != null && sharedRuneRoot != null;
    }

    private void RebuildDynamicRuneIconButtons(RuneData[] candidates)
    {
        ClearGeneratedRuneIconButtons();

        if (candidates == null || runeIconButtonSlotPrefab == null)
        {
            runeIconButtons = Array.Empty<RuneIconButton>();
            return;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            RuneData runeData = candidates[i];
            if (runeData == null)
                continue;

            Transform parent = IsCommonRune(runeData) ? sharedRuneRoot : exclusiveRuneRoot;
            if (parent == null)
                continue;

            RuneIconButton item = Instantiate(runeIconButtonSlotPrefab, parent);
            item.name = "RuneIconButtonSlot_" + runeData.RuneId;
            item.gameObject.SetActive(true);
            item.Init(this);

            bool locked = !IsCommonRune(runeData) && IsRuneLockedForCurrentState(runeData);
            int requiredLevel = GetRequiredLevelForRune(runeData);
            item.SetRuneData(runeData, locked, requiredLevel);

            bool isCommon = IsCommonRune(runeData);
            bool purchased = !isCommon || IsCommonRunePurchased(runeData);
            item.SetPurchaseState(isCommon, purchased, false);

            generatedRuneIconButtons.Add(item);
        }

        runeIconButtons = generatedRuneIconButtons.ToArray();
    }

    private void ClearGeneratedRuneIconButtons()
    {
        for (int i = 0; i < generatedRuneIconButtons.Count; i++)
        {
            RuneIconButton item = generatedRuneIconButtons[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        generatedRuneIconButtons.Clear();
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

        // 현재 캐릭터의 전용룬은 ID가 아니라 해금 레벨이 낮은 순서대로 표시합니다.
        // 같은 해금 레벨일 때만 RuneId를 보조 정렬 기준으로 사용합니다.
        if (groupA == 1 || groupA == 3)
        {
            int unlockLevelA = a != null ? GetRequiredLevelForRune(a) : int.MaxValue;
            int unlockLevelB = b != null ? GetRequiredLevelForRune(b) : int.MaxValue;

            if (unlockLevelA != unlockLevelB)
                return unlockLevelA.CompareTo(unlockLevelB);
        }

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

    public bool TryBeginRuneDrag(RuneData runeData, RuneSlotButton sourceSlot, Image sourceImage, PointerEventData eventData)
    {
        if (!runeSelectPanelAllowed || currentRuntimeData == null || currentMasterData == null)
            return false;

        if (sourceSlot != null)
        {
            if (sourceSlot.IsLocked || sourceSlot.EquippedRune == null)
                return false;

            runeData = sourceSlot.EquippedRune;
        }
        else
        {
            if (runeData == null || IsRuneLockedForCurrentState(runeData) || !IsRuneValidForCurrentCharacter(runeData))
                return false;

            if (IsCommonRune(runeData) && IsCommonRuneEquippedByOtherCharacter(runeData))
            {
                ShowWarning("다른 캐릭터가 장착 중인 파편입니다.");
                return false;
            }
        }

        if (runeData == null || sourceImage == null || sourceImage.sprite == null)
            return false;

        ClearRuneDragState(false);

        draggedRune = runeData;
        draggedSourceSlot = sourceSlot;
        dragDroppedOnSlot = false;
        CreateRuneDragVisual(sourceImage);
        UpdateRuneDrag(eventData);
        return true;
    }

    public void UpdateRuneDrag(PointerEventData eventData)
    {
        if (runeDragVisual == null || runeDragCanvas == null || eventData == null)
            return;

        RectTransform canvasRect = runeDragCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera eventCamera = runeDragCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera ?? runeDragCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            runeDragVisual.anchoredPosition = localPoint;
        }
    }

    public void DropDraggedRuneOnSlot(RuneSlotButton targetSlot)
    {
        if (draggedRune == null || targetSlot == null)
            return;

        // 슬롯 위에 놓은 경우에는 장착 성공 여부와 관계없이 '슬롯 밖 드랍'으로 처리하지 않습니다.
        dragDroppedOnSlot = true;

        if (targetSlot.IsLocked)
        {
            ShowRuneSlotLockedWarning(targetSlot.SlotIndex);
            return;
        }

        RuneSlotButton sourceSlot = draggedSourceSlot ?? FindEquippedRuneSlot(draggedRune);
        if (sourceSlot == targetSlot)
            return;

        RuneData replacedRune = targetSlot.EquippedRune;

        if (sourceSlot != null)
            sourceSlot.SetRune(replacedRune);

        targetSlot.SetRune(draggedRune);
        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();
        OnRuneChanged?.Invoke();
    }

    public void EndRuneDrag(PointerEventData eventData)
    {
        RuneSlotButton sourceSlot = draggedSourceSlot;
        bool unequipByOutsideDrop = sourceSlot != null && !dragDroppedOnSlot;

        ClearRuneDragState(false);

        if (unequipByOutsideDrop && sourceSlot != null && sourceSlot.EquippedRune != null)
            UnequipRuneFromSlot(sourceSlot);
    }

    private RuneSlotButton FindEquippedRuneSlot(RuneData runeData)
    {
        if (runeData == null || runeSlotButtons == null)
            return null;

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            RuneSlotButton slot = runeSlotButtons[i];
            if (slot == null || slot.EquippedRune == null)
                continue;

            if (string.Equals(slot.EquippedRune.RuneId, runeData.RuneId, StringComparison.OrdinalIgnoreCase))
                return slot;
        }

        return null;
    }

    private void CreateRuneDragVisual(Image sourceImage)
    {
        Canvas rootCanvas = sourceImage.canvas != null
            ? sourceImage.canvas.rootCanvas
            : GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            return;

        // 화면에 실제로 표시 중인 IconImg 자체를 복제해 드래그 비주얼로 사용합니다.
        // Sprite/Image를 새로 조립하지 않으므로 원본의 머티리얼, 이미지 타입,
        // preserveAspect 등 실제 렌더링 상태를 그대로 유지할 수 있습니다.
        GameObject dragCanvasObject = new GameObject(
            "RuneDragCanvas",
            typeof(RectTransform),
            typeof(Canvas));

        RectTransform dragCanvasRect = dragCanvasObject.GetComponent<RectTransform>();
        dragCanvasRect.SetParent(rootCanvas.transform, false);
        dragCanvasRect.anchorMin = Vector2.zero;
        dragCanvasRect.anchorMax = Vector2.one;
        dragCanvasRect.offsetMin = Vector2.zero;
        dragCanvasRect.offsetMax = Vector2.zero;
        dragCanvasRect.localScale = Vector3.one;
        dragCanvasRect.SetAsLastSibling();

        runeDragCanvas = dragCanvasObject.GetComponent<Canvas>();
        runeDragCanvas.overrideSorting = true;
        runeDragCanvas.sortingOrder = 9030;

        GameObject dragObject = Instantiate(sourceImage.gameObject, dragCanvasRect, false);
        dragObject.name = "RuneDragIcon";
        dragObject.SetActive(true);

        runeDragVisual = dragObject.GetComponent<RectTransform>();
        if (runeDragVisual == null)
        {
            Destroy(dragObject);
            Destroy(dragCanvasObject);
            runeDragCanvas = null;
            return;
        }

        runeDragVisual.anchorMin = new Vector2(0.5f, 0.5f);
        runeDragVisual.anchorMax = new Vector2(0.5f, 0.5f);
        runeDragVisual.pivot = new Vector2(0.5f, 0.5f);
        runeDragVisual.anchoredPosition = Vector2.zero;
        runeDragVisual.localRotation = Quaternion.identity;
        runeDragVisual.localScale = Vector3.one;

        Vector2 sourceSize = sourceImage.rectTransform.rect.size;
        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
            sourceSize = new Vector2(100f, 100f);
        runeDragVisual.sizeDelta = sourceSize;

        // 드래그 복제본과 그 자식은 드랍 판정을 가로채면 안 됩니다.
        Graphic[] graphics = dragObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        CanvasGroup canvasGroup = dragObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = dragObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = true;

        runeDragVisual.SetAsLastSibling();
    }

    private void ClearRuneDragState(bool keepData)
    {
        if (runeDragCanvas != null)
            Destroy(runeDragCanvas.gameObject);
        else if (runeDragVisual != null)
            Destroy(runeDragVisual.gameObject);

        runeDragVisual = null;
        runeDragCanvas = null;

        if (keepData)
            return;

        draggedRune = null;
        draggedSourceSlot = null;
        dragDroppedOnSlot = false;
    }

    public void TryEquipRuneToFirstEmptySlot(RuneData runeData)
    {
        if (currentRuntimeData == null || currentMasterData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        if (runeData == null)
        {
            ShowWarning("선택된 파편이 없다.");
            return;
        }

        currentDisplayedRune = runeData;

        if (IsRuneLockedForCurrentState(runeData))
        {
            ShowRuneLockedWarning(runeData);
            return;
        }

        if (!IsRuneValidForCurrentCharacter(runeData))
        {
            ShowWarning("현재 캐릭터가 사용할 수 없는 파편이다.");
            return;
        }

        if (IsRuneEquipped(runeData))
        {
            UnequipRune(runeData);
            return;
        }

        // 공용룬은 파티 전체에서 하나만 장착할 수 있습니다.
        // 현재 캐릭터가 아닌 다른 캐릭터가 이미 사용 중이면 새로 장착하지 않습니다.
        if (IsCommonRune(runeData) && IsCommonRuneEquippedByOtherCharacter(runeData))
        {
            ShowWarning("다른 캐릭터가 장착 중인 파편입니다.");
            return;
        }

        RuneSlotButton emptySlot = GetRuneEquipDestinationSlot();

        if (emptySlot == null)
        {
            ShowWarning("비어있는 파편 슬롯이 없다.");
            return;
        }

        emptySlot.SetRune(runeData);
        SetRuntimeRuneId(emptySlot.SlotIndex, runeData.RuneId);

        if (DataManager.Instance != null)
        {
            RecordDiscoveryService.RegisterRune(DataManager.Instance, runeData.RuneId);
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
        }

        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    private RuneSlotButton GetRuneEquipDestinationSlot()
    {
        if (selectedRuneSlot != null && !selectedRuneSlot.IsLocked)
            return selectedRuneSlot;

        return FindFirstEmptyUnlockedSlot();
    }

    public void TrySelectRuneIcon(RuneData runeData, bool locked, int requiredLevel)
    {
        if (runeData == null)
            return;

        if (IsCommonRune(runeData) && !IsCommonRunePurchased(runeData))
        {
            SelectCommonRuneForPurchase(runeData);
            return;
        }

        if (locked)
        {
            if (runeData.TargetCharacterId == "All")
                ShowWarning("플레이어 또는 계정 레벨 조건이 필요한 공용 파편이다.");
            else
                ShowWarning(string.Format("캐릭터 LV.{0}에 해금되는 전용 파편이다.", requiredLevel));

            return;
        }

        ClearSelectedPurchaseRune();
        TryEquipRuneToFirstEmptySlot(runeData);
    }

    private void SelectCommonRuneForPurchase(RuneData runeData)
    {
        selectedPurchaseRune = runeData;
        RefreshRuneIconPurchaseStates();
        RefreshCommonRunePurchaseUI();
        ShowRuneInfo(runeData);
    }

    private void ClearSelectedPurchaseRune()
    {
        if (selectedPurchaseRune == null)
        {
            RefreshCommonRunePurchaseUI();
            return;
        }

        selectedPurchaseRune = null;
        RefreshRuneIconPurchaseStates();
        RefreshCommonRunePurchaseUI();
    }

    private void RefreshRuneIconPurchaseStates()
    {
        if (runeIconButtons == null)
            return;

        for (int i = 0; i < runeIconButtons.Length; i++)
        {
            RuneIconButton iconButton = runeIconButtons[i];
            if (iconButton == null)
                continue;

            RuneData runeData = iconButton.CurrentRuneData;
            bool isCommon = IsCommonRune(runeData);
            bool purchased = !isCommon || IsCommonRunePurchased(runeData);
            bool selected = selectedPurchaseRune != null && runeData != null &&
                            selectedPurchaseRune.RuneId == runeData.RuneId;

            iconButton.SetPurchaseState(isCommon, purchased, selected);
        }
    }

    private void RefreshCommonRunePurchaseUI()
    {
        bool hasSelection = selectedPurchaseRune != null &&
                            IsCommonRune(selectedPurchaseRune) &&
                            !IsCommonRunePurchased(selectedPurchaseRune);

        int price = hasSelection
            ? Mathf.Max(0, selectedPurchaseRune.BlueDustiumCost)
            : 0;

        if (buyPriceText != null)
            buyPriceText.text = hasSelection
                ? price.ToString()
                : (string.IsNullOrWhiteSpace(emptyBuyPriceText) ? "0" : emptyBuyPriceText);

        bool hasEnoughBlueDustium = false;
        if (hasSelection && DataManager.Instance != null && DataManager.Instance.LobbyRuntimeStore != null)
        {
            LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
            hasEnoughBlueDustium = lobby != null && lobby.BlueDustium >= price;
        }

        if (buyButton != null)
        {
            // 선택된 룬이 있으면 클릭은 허용해 부족 경고를 표시할 수 있게 유지합니다.
            buyButton.interactable = hasSelection;
            ApplyBuyButtonVisual(hasSelection && hasEnoughBlueDustium);
        }
    }

    private void ApplyBuyButtonVisual(bool purchaseAvailable)
    {
        if (buyButton == null)
            return;

        // Button 오브젝트 자체의 Image 색상은 변경하지 않습니다.
        // 구매 가능 여부는 Button의 자식 Image와 TMP_Text만 표시합니다.
        Color targetColor = purchaseAvailable
            ? Color.white
            : new Color(0x77 / 255f, 0x77 / 255f, 0x77 / 255f, 1f);

        Image[] buttonImages = buyButton.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < buttonImages.Length; i++)
        {
            Image image = buttonImages[i];
            if (image == null || image.gameObject == buyButton.gameObject)
                continue;

            Color imageColor = targetColor;
            imageColor.a = image.color.a;
            image.color = imageColor;
        }

        TMP_Text[] buttonTexts = buyButton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < buttonTexts.Length; i++)
        {
            TMP_Text text = buttonTexts[i];
            if (text == null)
                continue;

            Color textColor = targetColor;
            textColor.a = text.color.a;
            text.color = textColor;
        }
    }

    private void BuySelectedCommonRune()
    {
        if (selectedPurchaseRune == null || !IsCommonRune(selectedPurchaseRune))
            return;

        if (IsCommonRunePurchased(selectedPurchaseRune))
        {
            ClearSelectedPurchaseRune();
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
        {
            ShowWarning("데이터를 불러올 수 없습니다.");
            return;
        }

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        int price = Mathf.Max(0, selectedPurchaseRune.BlueDustiumCost);

        if (lobby.BlueDustium < price)
        {
            ShowWarning(SettingWarningUI.GetInsufficientBlueDustiumMessage());
            return;
        }

        lobby.BlueDustium -= price;
        RecordDiscoveryService.RegisterRune(DataManager.Instance, selectedPurchaseRune.RuneId);
        LobbyBlueDustiumHudUI.RefreshAll();

        RuneData purchasedRune = selectedPurchaseRune;
        selectedPurchaseRune = null;
        RefreshRuneIconPurchaseStates();
        RefreshCommonRunePurchaseUI();
        ShowRuneInfo(purchasedRune);
    }

    private bool IsCommonRuneEquippedByOtherCharacter(RuneData runeData)
    {
        if (runeData == null || string.IsNullOrWhiteSpace(runeData.RuneId))
            return false;

        if (DataManager.Instance == null || DataManager.Instance.CharacterRuntimeStore == null)
            return false;

        IReadOnlyDictionary<string, CharacterRuntimeData> allCharacters =
            DataManager.Instance.CharacterRuntimeStore.GetAll();

        if (allCharacters == null)
            return false;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in allCharacters)
        {
            CharacterRuntimeData character = pair.Value;
            if (character == null)
                continue;

            if (!string.IsNullOrWhiteSpace(currentCharacterId) &&
                string.Equals(character.CharacterId, currentCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] equippedRuneIds = character.EquippedRuneIds;
            if (equippedRuneIds == null)
                continue;

            for (int i = 0; i < equippedRuneIds.Length; i++)
            {
                if (string.Equals(equippedRuneIds[i], runeData.RuneId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private bool IsCommonRunePurchased(RuneData runeData)
    {
        if (!IsCommonRune(runeData))
            return true;

        return DataManager.Instance != null &&
               RecordDiscoveryService.IsRuneDiscovered(DataManager.Instance, runeData.RuneId);
    }

    private bool IsCommonRune(RuneData runeData)
    {
        if (runeData == null)
            return false;

        int runeNumber = GetRuneNumber(runeData.RuneId);
        return IsCommonRuneNumber(runeNumber) ||
               string.Equals(runeData.TargetCharacterId, "All", StringComparison.OrdinalIgnoreCase);
    }

    public void HandleRuneIconDeselected(RuneIconButton iconButton)
    {
        if (iconButton == null || selectedPurchaseRune == null)
            return;

        RuneData runeData = iconButton.CurrentRuneData;
        if (runeData == null || runeData.RuneId != selectedPurchaseRune.RuneId)
            return;

        StartCoroutine(ClearPurchaseSelectionAfterDeselect(runeData.RuneId));
    }

    private IEnumerator ClearPurchaseSelectionAfterDeselect(string runeId)
    {
        yield return null;

        if (selectedPurchaseRune == null || selectedPurchaseRune.RuneId != runeId)
            yield break;

        GameObject selectedObject = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        if (buyButtonRoot != null && selectedObject != null &&
            (selectedObject == buyButtonRoot || selectedObject.transform.IsChildOf(buyButtonRoot.transform)))
        {
            yield break;
        }

        // BuyButton으로 포커스가 이동한 경우에만 구매 선택을 유지합니다.
        // 다른 룬이나 다른 UI를 선택하면 기존 Selected 표시를 해제합니다.
        ClearSelectedPurchaseRune();
    }

    public void UnequipRune(RuneData runeData)
    {
        if (runeData == null)
        {
            ShowWarning("해체할 파편이 없다.");
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
            ShowWarning("장착 중인 파편이 아니다.");
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
            ShowWarning("파편 슬롯이 연결되지 않았다.");
            return;
        }

        if (slotButton.IsLocked)
        {
            ShowRuneSlotLockedWarning(slotButton.SlotIndex);
            return;
        }

        if (slotButton.EquippedRune == null)
        {
            ShowWarning("해체할 파편이 없다.");
            return;
        }

        slotButton.SetRune(null);

        SaveCurrentRuneSetting();
        RefreshRuneIconEquippedStates();

        OnRuneChanged?.Invoke();
    }

    public void HandleRuneSlotClick(RuneSlotButton slotButton)
    {
        if (slotButton == null)
            return;

        if (!runeSelectPanelAllowed)
        {
            OpenRuneSettingFromPreview(slotButton);
            return;
        }

        UnequipRuneFromSlot(slotButton);
    }

    private void OpenRuneSettingFromPreview(RuneSlotButton slotButton)
    {
        if (slotButton.IsLocked)
        {
            ShowRuneSlotLockedWarning(slotButton.SlotIndex);
            return;
        }

        if (settingController == null)
            settingController = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);

        settingController?.OpenRuneSettingForSlot(slotButton);
    }

    public void SelectRuneSlotForSetting(RuneSlotButton slotButton)
    {
        if (slotButton == null)
            return;

        if (slotButton.IsLocked)
        {
            ShowRuneSlotLockedWarning(slotButton.SlotIndex);
            return;
        }

        selectedRuneSlot = slotButton;
        ShowRuneSlotInfo(slotButton.SlotIndex, slotButton.EquippedRune, false);
        SetRuneSelectPanelVisible(true);
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
            return true;

        // 공용 파편은 블루 더스티움으로 구매해 영구 해금해야 장착할 수 있다.
        // 구매 전에는 장착만 막고, RuneIconButton에서는 별도의 구매 선택/호버 UI를 사용한다.
        if (IsCommonRune(runeData))
            return !IsCommonRunePurchased(runeData);

        // 전용 파편은 해당 캐릭터의 레벨이 UnlockLevel에 도달해야 사용할 수 있다.
        int requiredLevel = GetRequiredLevelForRune(runeData);

        if (requiredLevel <= 0)
            return false;

        if (currentRuntimeData == null)
            return true;

        return currentRuntimeData.Level < requiredLevel;
    }

    private int GetRequiredLevelForRune(RuneData runeData)
    {
        if (runeData == null)
            return 0;

        return CharacterLevelUnlockService.GetRuneUnlockLevel(
            currentMasterData,
            runeData,
            GetCurrentCharacterRuneIndex(runeData));
    }

    private int GetCurrentCharacterRuneIndex(RuneData runeData)
    {
        if (runeData == null || currentMasterData == null)
            return -1;

        string[] runeIds = currentMasterData.GetRuneIds();
        for (int i = 0; i < runeIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(runeIds[i]))
                continue;

            if (string.Equals(
                    runeIds[i].Trim(),
                    runeData.RuneId?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void ShowRuneLockedWarning(RuneData runeData)
    {
        int requiredLevel = GetRequiredLevelForRune(runeData);

        if (runeData != null && runeData.TargetCharacterId == "All")
        {
            ShowWarning("아직 잠겨있는 공용 파편이다.");
            return;
        }

        ShowWarning(string.Format("캐릭터 LV.{0}에 해금되는 전용 파편이다.", requiredLevel));
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
            bool equipped = IsRuneEquipped(runeData) ||
                (IsCommonRune(runeData) && IsCommonRuneEquippedByOtherCharacter(runeData));

            runeIconButtons[i].SetEquippedState(equipped);
        }

        RefreshRuneIconPurchaseStates();
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
                runeIconButtons[i].SetPurchaseState(false, true, false);
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

    public void ShowRuneTooltip(RuneData runeData)
    {
        ShowRuneTooltip(runeData, null);
    }

    public void ShowRuneTooltip(RuneData runeData, RectTransform sourceRect)
    {
        if (runeData == null)
        {
            HideRuneTooltip();
            return;
        }

        BindRuneTooltipUI();
        if (runeTooltipPanel == null || runeTooltipCanvasGroup == null)
            return;

        currentDisplayedRune = runeData;

        if (runeTooltipNameText != null)
            runeTooltipNameText.text = GameDataLocalization.RuneName(runeData);

        if (runeTooltipDescriptionText != null)
            runeTooltipDescriptionText.text = BuildRuneEffectText(runeData);

        if (!runeTooltipPanel.activeSelf)
            runeTooltipPanel.SetActive(true);

        PositionRuneTooltip(sourceRect);
        StartRuneTooltipFade(1f, runeTooltipFadeInDuration, false);
    }

    private void PositionRuneTooltip(RectTransform sourceRect)
    {
        if (sourceRect == null || runeTooltipRectTransform == null)
            return;

        RectTransform tooltipParent = runeTooltipRectTransform.parent as RectTransform;
        if (tooltipParent == null)
            return;

        Vector3 sourceWorldCenter = sourceRect.TransformPoint(sourceRect.rect.center);
        Vector3 sourceLocalCenter = tooltipParent.InverseTransformPoint(sourceWorldCenter);
        Vector3 currentLocalPosition = runeTooltipRectTransform.localPosition;

        runeTooltipRectTransform.localPosition = new Vector3(
            sourceLocalCenter.x + runeTooltipOffsetX,
            sourceLocalCenter.y + runeTooltipOffsetY,
            currentLocalPosition.z);

        // 텍스트 변경 직후 실제 툴팁 크기를 반영한 뒤 화면 밖으로 나가지 않도록 보정합니다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(runeTooltipRectTransform);
        ClampRuneTooltipToCanvas(tooltipParent);
    }

    private void ClampRuneTooltipToCanvas(RectTransform tooltipParent)
    {
        if (runeTooltipRectTransform == null || tooltipParent == null)
            return;

        Canvas rootCanvas = runeTooltipRectTransform.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            return;

        rootCanvas = rootCanvas.rootCanvas;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Vector3[] tooltipCorners = new Vector3[4];
        runeTooltipRectTransform.GetWorldCorners(tooltipCorners);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < tooltipCorners.Length; i++)
        {
            Vector3 localCorner = canvasRect.InverseTransformPoint(tooltipCorners[i]);
            minX = Mathf.Min(minX, localCorner.x);
            maxX = Mathf.Max(maxX, localCorner.x);
            minY = Mathf.Min(minY, localCorner.y);
            maxY = Mathf.Max(maxY, localCorner.y);
        }

        Rect bounds = canvasRect.rect;
        float padding = Mathf.Max(0f, runeTooltipScreenPadding);
        float left = bounds.xMin + padding;
        float right = bounds.xMax - padding;
        float bottom = bounds.yMin + padding;
        float top = bounds.yMax - padding;

        float deltaX = 0f;
        float deltaY = 0f;

        if (minX < left)
            deltaX = left - minX;
        else if (maxX > right)
            deltaX = right - maxX;

        if (minY < bottom)
            deltaY = bottom - minY;
        else if (maxY > top)
            deltaY = top - maxY;

        if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f))
            return;

        // Canvas 로컬 보정량을 툴팁 부모의 로컬 축으로 변환해 현재 위치에 더합니다.
        Vector3 worldDelta = canvasRect.TransformVector(new Vector3(deltaX, deltaY, 0f));
        Vector3 parentLocalDelta = tooltipParent.InverseTransformVector(worldDelta);
        runeTooltipRectTransform.localPosition += parentLocalDelta;
    }

    public void HideRuneTooltip()
    {
        BindRuneTooltipUI();
        if (runeTooltipPanel == null || runeTooltipCanvasGroup == null)
            return;

        if (!runeTooltipPanel.activeSelf)
        {
            runeTooltipCanvasGroup.alpha = 0f;
            return;
        }

        StartRuneTooltipFade(0f, runeTooltipFadeOutDuration, true);
    }

    private void BindRuneTooltipUI()
    {
        if (runeTooltipPanel == null)
        {
            Transform tooltipTransform = FindDeepChild(transform.root, "Rune_TooltipPanel");
            if (tooltipTransform != null)
                runeTooltipPanel = tooltipTransform.gameObject;
        }

        if (runeTooltipPanel == null)
            return;

        if (runeTooltipRectTransform == null)
            runeTooltipRectTransform = runeTooltipPanel.transform as RectTransform;

        if (runeTooltipCanvasGroup == null)
        {
            runeTooltipCanvasGroup = runeTooltipPanel.GetComponent<CanvasGroup>();
            if (runeTooltipCanvasGroup == null)
                runeTooltipCanvasGroup = runeTooltipPanel.AddComponent<CanvasGroup>();
        }

        runeTooltipCanvasGroup.interactable = false;
        runeTooltipCanvasGroup.blocksRaycasts = false;

        if (runeTooltipNameText == null)
        {
            Transform nameTransform = FindDeepChild(runeTooltipPanel.transform, "NameText");
            if (nameTransform != null)
                runeTooltipNameText = nameTransform.GetComponent<TMP_Text>();
        }

        if (runeTooltipDescriptionText == null)
        {
            Transform descriptionTransform = FindDeepChild(runeTooltipPanel.transform, "DescriptionText");
            if (descriptionTransform != null)
                runeTooltipDescriptionText = descriptionTransform.GetComponent<TMP_Text>();
        }

        EnsureDynamicInfoTextOwnership(runeTooltipNameText);
        EnsureDynamicInfoTextOwnership(runeTooltipDescriptionText);
    }

    private void StartRuneTooltipFade(float targetAlpha, float duration, bool deactivateWhenFinished)
    {
        if (runeTooltipCanvasGroup == null)
            return;

        if (runeTooltipFadeCoroutine != null)
            StopCoroutine(runeTooltipFadeCoroutine);

        runeTooltipFadeCoroutine = StartCoroutine(
            FadeRuneTooltip(targetAlpha, Mathf.Max(0f, duration), deactivateWhenFinished));
    }

    private IEnumerator FadeRuneTooltip(float targetAlpha, float duration, bool deactivateWhenFinished)
    {
        if (runeTooltipCanvasGroup == null)
            yield break;

        float startAlpha = runeTooltipCanvasGroup.alpha;

        if (duration <= 0f || Mathf.Approximately(startAlpha, targetAlpha))
        {
            runeTooltipCanvasGroup.alpha = targetAlpha;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                runeTooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            runeTooltipCanvasGroup.alpha = targetAlpha;
        }

        if (deactivateWhenFinished && targetAlpha <= 0f && runeTooltipPanel != null)
            runeTooltipPanel.SetActive(false);

        runeTooltipFadeCoroutine = null;
    }

    private void SetRuneTooltipImmediate(bool visible)
    {
        BindRuneTooltipUI();
        if (runeTooltipPanel == null || runeTooltipCanvasGroup == null)
            return;

        if (runeTooltipFadeCoroutine != null)
        {
            StopCoroutine(runeTooltipFadeCoroutine);
            runeTooltipFadeCoroutine = null;
        }

        runeTooltipCanvasGroup.alpha = visible ? 1f : 0f;
        runeTooltipPanel.SetActive(visible);
    }

    public bool ShowFirstOwnedRuneInfo()
    {
        return false;
    }

    public void ShowRuneSlotInfo(int slotIndex, RuneData runeData, bool isLocked)
    {
        // 룬 상세 정보 UI는 다음 단계에서 새 구조로 연결한다.
    }

    private void ShowRuneSlotLockedWarning(int slotIndex)
    {
        int requiredLevel = GetRuneSlotRequiredLevel(slotIndex);

        if (requiredLevel > 0)
        {
            ShowWarning(SettingWarningUI.GetRuneSlotUnlockLevelMessage(requiredLevel));
            return;
        }

        ShowWarning("아직 잠겨있는 파편 슬롯이다.");
    }

    private int GetRuneSlotRequiredLevel(int slotIndex)
    {
        return CharacterLevelUnlockService.GetRuneSlotUnlockLevel(currentMasterData, slotIndex);
    }

    public void ShowRuneInfo(RuneData runeData)
    {
        // 호환용 API. 현재 CharacterSettingPanel에는 룬 상세 정보 영역이 없다.
        currentDisplayedRune = null;
    }

    public void ClearRuneInfoFromHover()
    {
        currentDisplayedRune = null;
    }

    public void ClearRuneInfoFromHover(int hoverVersion)
    {
        currentDisplayedRune = null;
    }

    public void SetEmptyInfoText(string title, string effect)
    {
        // 룬 상세 정보 UI는 다음 단계에서 새 구조로 연결한다.
    }

    private void ClearRuneInfo()
    {
        currentDisplayedRune = null;
        HideRuneTooltip();
    }

    private void AutoBindRuneInfoTexts()
    {
        if (runeInfoTitleText != null && runeInfoEffectText != null && runeInfoRarityText != null)
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
            else if (runeInfoRarityText == null && objectName == "RarityText")
                runeInfoRarityText = texts[i];
        }
    }

    private void EnsureDynamicInfoTextOwnership()
    {
        foreach (TMP_Text text in new[] { runeInfoTitleText, runeInfoEffectText, runeInfoRarityText })
            EnsureDynamicInfoTextOwnership(text);
    }

    private static void EnsureDynamicInfoTextOwnership(TMP_Text text)
    {
        if (text == null)
            return;

        if (text.GetComponent<LocalizationIgnore>() == null)
            text.gameObject.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        if (localizer != null)
        {
            localizer.enabled = false;
            Destroy(localizer);
        }

        LocalizeStringEvent legacyLocalizer = text.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
        {
            legacyLocalizer.enabled = false;
            Destroy(legacyLocalizer);
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
        SetChildActive(infoArea, "TpyeText", false);
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

    private static string BuildRuneRarityText(RuneData runeData)
    {
        if (runeData == null || string.IsNullOrWhiteSpace(runeData.Rarity))
            return string.Empty;

        string rarity = runeData.Rarity.Trim();

        if (string.Equals(rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get(LocalizationKeys.FragmentRarity.Exclusive);
        if (string.Equals(rarity, "Common", StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get(LocalizationKeys.FragmentRarity.Common);
        if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get(LocalizationKeys.FragmentRarity.Rare);
        if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get(LocalizationKeys.FragmentRarity.Unique);

        return string.Empty;
    }

    private Color GetRuneInfoRarityColor(string rarity)
    {
        // 로비의 파편 레어리티 색상은 도감에서 실제로 사용하는 색상을 그대로 가져옵니다.
        // 이렇게 하면 도감 프리팹에서 색상을 수정해도 로비 InfoArea가 자동으로 동일한 색상을 사용합니다.
        if (recordPanelUI == null)
        {
            recordPanelUI = FindFirstObjectByType<RecordPanelUI>(FindObjectsInactive.Include);
        }

        if (recordPanelUI != null)
            return recordPanelUI.GetRarityDisplayColor(rarity);

        // 도감 패널을 찾을 수 없는 경우에만 기존 인스펙터 색상을 예비값으로 사용합니다.
        if (string.Equals(rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
            return exclusiveRarityColor;
        if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase))
            return rareRarityColor;
        if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase))
            return epicRarityColor;
        if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase))
            return uniqueRarityColor;

        return commonRarityColor;
    }

    private string BuildRuneEffectText(RuneData runeData)
    {
        if (runeData == null)
            return string.Empty;

        string localizedDescription = GameDataLocalization.RuneDescription(runeData);
        if (!string.IsNullOrWhiteSpace(localizedDescription))
        {
            string effectDesc = NormalizeRuneEffectDesc(localizedDescription);
            return ReplaceRuneEffectTokens(effectDesc, runeData.ValueRate, runeData.CountRate);
        }

        return GameLocalization.Get(LocalizationKeys.Rune.NoEffectDescription);
    }

    private string ReplaceRuneEffectTokens(string source, string valueRate, string countRate)
    {
        string result = source ?? string.Empty;
        string colorHex = ColorUtility.ToHtmlStringRGB(valueHighlightColor);
        result = ReplaceRuneIndexedTokens(result, "ValueRate", valueRate, colorHex);
        result = ReplaceRuneIndexedTokens(result, "CountRate", countRate, colorHex);

        if (result.Contains("{ValueRate}"))
            result = result.Replace("{ValueRate}", $"<color=#{colorHex}>{GetRuneDisplayRateValue(valueRate)}</color>");
        if (result.Contains("{CountRate}"))
            result = result.Replace("{CountRate}", $"<color=#{colorHex}>{GetRuneDisplayRateValue(countRate)}</color>");

        return result;
    }

    private static string ReplaceRuneIndexedTokens(string source, string tokenName, string values, string colorHex)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
            return source;

        string[] splitValues = string.IsNullOrWhiteSpace(values)
            ? Array.Empty<string>()
            : values.Split(';');

        for (int i = 0; i < splitValues.Length; i++)
        {
            string token = $"{{{tokenName}{i + 1}}}";
            if (source.Contains(token))
                source = source.Replace(token, $"<color=#{colorHex}>{GetRuneDisplayRateValue(splitValues[i])}</color>");
        }

        return source;
    }

    private static string GetRuneDisplayRateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        string result = value.Trim();
        if (result.Length > 1 && result[0] == '-' && float.TryParse(
            result.Substring(1),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out _))
        {
            return result.Substring(1);
        }

        return result;
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

        return entry.EffectData != null ? GameDataLocalization.EffectName(entry.EffectData) : string.Empty;
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
