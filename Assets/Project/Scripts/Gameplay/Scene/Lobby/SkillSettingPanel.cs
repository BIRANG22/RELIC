using Relic.Gameplay.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SkillSettingPanel : MonoBehaviour, IRuntimeSaveStateContributor
{
    private const int SetupSkillSlotCount = 3;

    [Header("Skill Slots")]
    [SerializeField, HideInInspector] private SkillSlotButton[] skillSlotButtons;

    [Header("Skill Select Panels")]
    [SerializeField] private GameObject[] skillIconSelectPanels = new GameObject[SetupSkillSlotCount];
    [SerializeField, HideInInspector, Min(0.01f)] private float skillSelectPanelMoveDuration = 0.2f;

    [Header("Skill Select Button Count")]
    [SerializeField] private bool autoBindSkillIconButtons = true;

    [Header("Shared Info Area")]
    [FormerlySerializedAs("skillInfoArea")]
    [SerializeField, HideInInspector] private GameObject sharedInfoArea;
    [SerializeField, HideInInspector] private TMP_Text skillInfoTitleText;
    [SerializeField, HideInInspector] private TMP_Text skillInfoEffectText;
    [SerializeField, HideInInspector] private TMP_Text skillInfoRarityText;
    [SerializeField, HideInInspector] private GameObject skillInfoRangeRoot;
    [SerializeField, HideInInspector] private Image skillInfoRangeImage;
    [SerializeField, HideInInspector] private TMP_Text skillInfoCostText;
    [SerializeField, HideInInspector] private TMP_Text skillInfoTypeText;
    [SerializeField, HideInInspector] private TMP_Text skillInfoValueText;

    [Header("Info Rarity Colors")]
    [SerializeField, HideInInspector] private Color commonRarityColor = Color.white;
    [SerializeField, HideInInspector] private Color rareRarityColor = Color.white;
    [SerializeField, HideInInspector] private Color epicRarityColor = Color.white;
    [SerializeField, HideInInspector] private Color uniqueRarityColor = Color.white;
    [SerializeField, HideInInspector] private Color exclusiveRarityColor = new Color(1f, 0.82f, 0.2f, 1f);

    [Header("Info Effect Value Color")]
    [Tooltip("도감과 동일하게 설명 안의 ValueRate/CountRate 치환 수치에 적용할 강조 색상입니다.")]
    [SerializeField, HideInInspector] private Color valueHighlightColor = Color.yellow;

    [Header("Shared Info Labels")]
    [SerializeField, HideInInspector] private GameObject skillInfoRangeLabel;
    [SerializeField, HideInInspector] private GameObject skillInfoTypeLabel;
    [SerializeField, HideInInspector] private GameObject skillInfoCostLabel;
    [SerializeField, HideInInspector] private GameObject skillInfoValueLabel;

    [SerializeField, HideInInspector] private string emptySkillInfoTitle = "스킬명";
    [SerializeField, HideInInspector, TextArea] private string emptySkillInfoEffect = "스킬을 선택하면 정보가 표시된다.";
    [SerializeField, HideInInspector] private bool autoBindSkillInfoArea = true;

    [Header("Skill Tooltip")]
    [Tooltip("Skill_TootipPanel 오브젝트입니다. 비어 있으면 이름으로 자동 탐색합니다.")]
    [SerializeField] private GameObject skillTooltipPanel;
    [SerializeField] private Image skillTooltipRangeImage;
    [SerializeField] private Image skillTooltipResourceImage;
    [SerializeField] private TMP_Text skillTooltipResourceText;
    [SerializeField] private TMP_Text skillTooltipNameText;
    [SerializeField] private TMP_Text skillTooltipDescriptionText;
    [Tooltip("호버한 스킬 아이콘을 기준으로 한 툴팁 X 오프셋입니다.")]
    [SerializeField] private float skillTooltipOffsetX = 50f;
    [Tooltip("호버한 스킬 아이콘을 기준으로 한 툴팁 Y 오프셋입니다.")]
    [SerializeField] private float skillTooltipOffsetY = 50f;

    [Header("Skill Tooltip Resource Icons")]
    [SerializeField] private Sprite skillTooltipCostResourceIcon;
    [SerializeField] private Sprite skillTooltipHpResourceIcon;
    [SerializeField] private Sprite skillTooltipUniqueResourceIcon;
    [SerializeField] private Sprite skillTooltipMoveResourceIcon;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private Setting settingController;

    private SkillSlotButton currentSelectedSlot;
    private int openedSkillSelectPanelIndex = -1;
    private Coroutine[] skillSelectPanelMoveCoroutines = new Coroutine[SetupSkillSlotCount];

    private bool skillSelectPanelAllowed = true;

    private bool IsDirectSelectionLayout => true;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;
    private SkillMasterData currentDisplayedSkillInfo;

    public bool IsDisplayingSkillInfo => currentDisplayedSkillInfo != null;

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindSkillIconButtonsIfNeeded();
        InitSkillIconButtons();
        BindSkillTooltipIfNeeded();
        HideSkillTooltipImmediate();
        SetAllDirectSkillPanelsVisible();
    }

    public void SetSettingController(Setting controller)
    {
        settingController = controller;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindSkillIconButtonsIfNeeded();
        InitSkillIconButtons();
        BindSkillTooltipIfNeeded();
        HideSkillTooltipImmediate();
        SetAllDirectSkillPanelsVisible();

        if (currentMasterData != null && currentRuntimeData != null)
            RefreshAllDirectSkillButtons();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        if (currentDisplayedSkillInfo != null)
            ShowSkillInfo(currentDisplayedSkillInfo);
    }

    private void InitSkillSlotButtons()
    {
        if (skillSlotButtons == null)
            return;

        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            if (skillSlotButtons[i] == null)
                continue;

            bool isSetupSlot = i < SetupSkillSlotCount;
            skillSlotButtons[i].gameObject.SetActive(isSetupSlot);

            if (isSetupSlot)
                skillSlotButtons[i].Init(this, i);
        }
    }

    private void SetSelectedSkillSlot(SkillSlotButton selectedSlot)
    {
        currentSelectedSlot = selectedSlot;

        if (skillSlotButtons == null)
            return;

        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            if (skillSlotButtons[i] != null)
                skillSlotButtons[i].SetSelected(skillSlotButtons[i] == selectedSlot);
        }
    }

    private void InitSkillIconButtons()
    {
        BindSkillIconButtonsIfNeeded();

        if (skillIconSelectPanels == null)
            return;

        int panelCount = Mathf.Min(skillIconSelectPanels.Length, SetupSkillSlotCount);

        for (int panelIndex = 0; panelIndex < panelCount; panelIndex++)
        {
            SkillIconButton[] buttons = GetSkillIconButtons(panelIndex);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].Init(this, panelIndex);
            }
        }
    }

    private void BindSkillIconButtonsIfNeeded()
    {
        if (!autoBindSkillIconButtons)
            return;

        if (skillIconSelectPanels == null)
            skillIconSelectPanels = new GameObject[SetupSkillSlotCount];

        for (int i = 0; i < skillIconSelectPanels.Length; i++)
        {
            if (i >= SetupSkillSlotCount)
            {
                if (skillIconSelectPanels[i] != null)
                    skillIconSelectPanels[i].SetActive(false);

                continue;
            }

            if (skillIconSelectPanels[i] != null)
                continue;

            Transform panel = transform.Find("SkillIconSelectPanel_" + i);

            if (panel == null)
                panel = FindChildByName(transform, "SkillIconSelectPanel_" + i);

            if (panel != null)
                skillIconSelectPanels[i] = panel.gameObject;
        }
    }

    private SkillIconButton[] GetSkillIconButtons(int panelIndex)
    {
        if (skillIconSelectPanels == null ||
            panelIndex < 0 ||
            panelIndex >= SetupSkillSlotCount ||
            panelIndex >= skillIconSelectPanels.Length ||
            skillIconSelectPanels[panelIndex] == null)
            return Array.Empty<SkillIconButton>();

        Transform panel = skillIconSelectPanels[panelIndex].transform;
        SkillIconButton[] result = new SkillIconButton[2];

        for (int i = 0; i < result.Length; i++)
        {
            Transform buttonTransform = panel.Find("SkillIconButton_" + i);
            if (buttonTransform == null)
                buttonTransform = FindChildByName(panel, "SkillIconButton_" + i);

            if (buttonTransform != null)
                result[i] = buttonTransform.GetComponent<SkillIconButton>();
        }

        return result;
    }


    private void BindSkillInfoAreaIfNeeded()
    {
        if (!autoBindSkillInfoArea)
            return;

        if (skillInfoTitleText != null &&
            skillInfoEffectText != null &&
            skillInfoRarityText != null &&
            skillInfoRangeImage != null &&
            skillInfoCostText != null &&
            skillInfoTypeText != null &&
            skillInfoValueText != null &&
            skillInfoRangeLabel != null &&
            skillInfoTypeLabel != null &&
            skillInfoCostLabel != null &&
            skillInfoValueLabel != null)
        {
            return;
        }

        Transform area = sharedInfoArea != null ? sharedInfoArea.transform : null;

        if (area == null)
            area = FindChildByName(transform.root, "InfoArea");

        if (area == null)
            return;

        sharedInfoArea = area.gameObject;

        if (skillInfoTitleText == null)
        {
            Transform title = area.Find("TitleText");
            if (title == null)
                title = area.Find("NameText");
            if (title == null)
                title = area.Find("SkillNameText");
            if (title != null)
                skillInfoTitleText = title.GetComponent<TMP_Text>();
        }

        if (skillInfoEffectText == null)
        {
            Transform effect = area.Find("EffectText");
            if (effect == null)
                effect = area.Find("DescriptionText");
            if (effect == null)
                effect = area.Find("SkillEffectText");
            if (effect != null)
                skillInfoEffectText = effect.GetComponent<TMP_Text>();
        }

        if (skillInfoRarityText == null)
        {
            Transform rarity = area.Find("RarityText");
            if (rarity != null)
                skillInfoRarityText = rarity.GetComponent<TMP_Text>();
        }

        if (skillInfoRangeRoot == null)
        {
            Transform rangeRoot = area.Find("Range");
            if (rangeRoot != null)
                skillInfoRangeRoot = rangeRoot.gameObject;
        }

        if (skillInfoRangeImage == null)
        {
            Transform rangeImage = null;

            if (skillInfoRangeRoot != null)
            {
                rangeImage = skillInfoRangeRoot.transform.Find("RangeImg");
                if (rangeImage == null)
                    rangeImage = skillInfoRangeRoot.transform.Find("RangeImage");
                if (rangeImage == null)
                    rangeImage = skillInfoRangeRoot.transform.Find("RangeIcon");
            }

            if (rangeImage == null)
                rangeImage = area.Find("RangeImg");
            if (rangeImage == null)
                rangeImage = area.Find("RangeImage");
            if (rangeImage == null)
                rangeImage = area.Find("RangeIcon");

            if (rangeImage != null)
                skillInfoRangeImage = rangeImage.GetComponent<Image>();

            if (skillInfoRangeImage == null && skillInfoRangeRoot != null)
                skillInfoRangeImage = skillInfoRangeRoot.GetComponentInChildren<Image>(true);
        }

        if (skillInfoCostText == null)
        {
            Transform cost = area.Find("CostText");
            if (cost == null)
                cost = area.Find("ResourceCostText");
            if (cost != null)
                skillInfoCostText = cost.GetComponent<TMP_Text>();
        }

        if (skillInfoTypeText == null)
        {
            Transform type = area.Find("TpyeText");
            if (type == null)
                type = area.Find("TypeText");
            if (type == null)
                type = area.Find("RangeTypeText");
            if (type != null)
                skillInfoTypeText = type.GetComponent<TMP_Text>();
        }

        if (skillInfoValueText == null)
        {
            Transform value = area.Find("ValueText");
            if (value == null)
                value = area.Find("EffectValueText");
            if (value != null)
                skillInfoValueText = value.GetComponent<TMP_Text>();
        }

        if (skillInfoRangeLabel == null)
        {
            Transform label = area.Find("Infotext_1");
            if (label != null)
                skillInfoRangeLabel = label.gameObject;
        }

        if (skillInfoTypeLabel == null)
        {
            Transform label = area.Find("Infotext_2");
            if (label != null)
                skillInfoTypeLabel = label.gameObject;
        }

        if (skillInfoCostLabel == null)
        {
            Transform label = area.Find("Infotext_3");
            if (label != null)
                skillInfoCostLabel = label.gameObject;
        }

        if (skillInfoValueLabel == null)
        {
            Transform label = area.Find("Infotext_4");
            if (label != null)
                skillInfoValueLabel = label.gameObject;
        }
    }

    private void BindSkillTooltipIfNeeded()
    {
        Transform tooltip = skillTooltipPanel != null
            ? skillTooltipPanel.transform
            : FindChildByName(transform, "Skill_TootipPanel");

        if (tooltip == null)
            tooltip = FindChildByName(transform, "Skill_TooltipPanel");

        if (tooltip == null)
            return;

        skillTooltipPanel = tooltip.gameObject;

        if (skillTooltipRangeImage == null)
        {
            Transform child = tooltip.Find("Range");
            if (child != null) skillTooltipRangeImage = child.GetComponent<Image>();
        }

        if (skillTooltipResourceImage == null)
        {
            Transform child = tooltip.Find("Resources_Image");
            if (child != null) skillTooltipResourceImage = child.GetComponent<Image>();
        }

        if (skillTooltipResourceText == null)
        {
            Transform child = tooltip.Find("Resources_Text");
            if (child != null) skillTooltipResourceText = child.GetComponent<TMP_Text>();
        }

        if (skillTooltipNameText == null)
        {
            Transform child = tooltip.Find("NameText");
            if (child != null) skillTooltipNameText = child.GetComponent<TMP_Text>();
        }

        if (skillTooltipDescriptionText == null)
        {
            Transform child = tooltip.Find("DescriptionText");
            if (child != null) skillTooltipDescriptionText = child.GetComponent<TMP_Text>();
        }

        EnsureDynamicTextOwnership(skillTooltipResourceText);
        EnsureDynamicTextOwnership(skillTooltipNameText);
        EnsureDynamicTextOwnership(skillTooltipDescriptionText);
    }

    public void ShowSkillTooltip(SkillIconButton sourceButton, SkillMasterData skill)
    {
        if (sourceButton == null || skill == null)
            return;

        BindSkillTooltipIfNeeded();
        if (skillTooltipPanel == null)
            return;

        SetPlainTmpText(skillTooltipNameText, GameDataLocalization.SkillName(skill));
        SetRichTmpText(skillTooltipDescriptionText, BuildSkillDetailsText(skill));

        if (skillTooltipResourceText != null)
            SetPlainTmpText(skillTooltipResourceText, Mathf.Max(0, skill.ResourceCostValue).ToString());

        if (skillTooltipRangeImage != null)
        {
            Sprite rangeSprite = null;
            if (!string.IsNullOrWhiteSpace(skill.RangeId) &&
                DataManager.Instance != null &&
                DataManager.Instance.SkillRangeIconDatabase != null)
            {
                DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(skill.RangeId, out rangeSprite);
            }

            skillTooltipRangeImage.sprite = rangeSprite;
            skillTooltipRangeImage.enabled = rangeSprite != null;
        }

        if (skillTooltipResourceImage != null)
        {
            Sprite resourceSprite = ResolveSkillTooltipResourceIcon(skill.ReferenceResource);
            skillTooltipResourceImage.sprite = resourceSprite;
            skillTooltipResourceImage.enabled = resourceSprite != null;
        }

        PositionSkillTooltip(sourceButton.transform as RectTransform);
        skillTooltipPanel.SetActive(true);
    }

    public void HideSkillTooltip(SkillIconButton sourceButton)
    {
        if (skillTooltipPanel == null)
            return;

        skillTooltipPanel.SetActive(false);
    }

    private void HideSkillTooltipImmediate()
    {
        if (skillTooltipPanel != null)
            skillTooltipPanel.SetActive(false);
    }

    private void PositionSkillTooltip(RectTransform sourceRect)
    {
        if (sourceRect == null || skillTooltipPanel == null)
            return;

        RectTransform tooltipRect = skillTooltipPanel.transform as RectTransform;
        RectTransform parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
        if (tooltipRect == null || parentRect == null)
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, sourceRect.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
        {
            tooltipRect.anchoredPosition = localPoint + new Vector2(skillTooltipOffsetX, skillTooltipOffsetY);
        }
    }

    private Sprite ResolveSkillTooltipResourceIcon(ReferenceResource resource)
    {
        Sprite assigned = resource switch
        {
            ReferenceResource.HP => skillTooltipHpResourceIcon,
            ReferenceResource.UniqueResource => skillTooltipUniqueResourceIcon,
            ReferenceResource.MovePoint => skillTooltipMoveResourceIcon != null ? skillTooltipMoveResourceIcon : skillTooltipCostResourceIcon,
            _ => skillTooltipCostResourceIcon,
        };

        if (assigned != null)
            return assigned;

        string statName = resource switch
        {
            ReferenceResource.HP => "HP",
            ReferenceResource.UniqueResource => "Karma",
            ReferenceResource.MovePoint => "Cost",
            _ => "Cost",
        };

        Transform statsArea = FindChildByName(transform, "Stats_Area");
        Transform statRoot = statsArea != null ? statsArea.Find(statName) : null;
        Transform icon = statRoot != null ? statRoot.Find("Icon") : null;
        Image image = icon != null ? icon.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }

    public bool ShouldClearInfoOnHoverExit => false;

    // 스킬 선택 패널이 이동하는 동안에는 지나가는 아이콘의 호버 정보를 반영하지 않습니다.
    public bool CanPreviewSkillIconHover => false;

    public void SetSkillSelectPanelEnabledForTab(bool enabled)
    {
        skillSelectPanelAllowed = true;
        SetAllDirectSkillPanelsVisible();
    }

    public void SetSkillSelectPanelVisible(bool visible)
    {
        SetSkillSelectPanelVisible(visible, false);
    }

    private void SetSkillSelectPanelVisible(bool visible, bool immediate)
    {
        SetAllDirectSkillPanelsVisible();
        openedSkillSelectPanelIndex = -1;
    }

    public void OpenCharacterSetting(string characterId)
    {
        OpenCharacterSetting(characterId, true);
    }

    public void OpenCharacterSetting(string characterId, bool saveCurrent)
    {
        if (saveCurrent)
            SaveCurrentSkillSetting();

        currentCharacterId = characterId;
        currentMasterData = null;
        currentRuntimeData = null;
        SetSelectedSkillSlot(null);

        if (DataManager.Instance == null)
        {
            ClearSkillSlots();
            ShowWarning("데이터를 사용할 수 없다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearSkillSlots();
            ShowWarning("선택된 캐릭터가 없다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearSkillSlots();
            ShowWarning(string.Format("캐릭터 데이터를 찾을 수 없다: {0}", characterId));
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearSkillSlots();
            ShowWarning(string.Format("캐릭터 데이터를 찾을 수 없다: {0}", characterId));
            return;
        }

        EnsureEquippedSkillArray();

        LoadCurrentSkillSetting();
        SetSelectedSkillSlot(null);

        if (IsDirectSelectionLayout)
        {
            RefreshAllDirectSkillButtons();
            SetAllDirectSkillPanelsVisible();
        }
        else
        {
            ClearSkillIconButtons();
            SetSkillSelectPanelVisible(false);
        }
    }

    public void RefreshByCurrentLevel()
    {
        if (currentRuntimeData == null || currentMasterData == null)
            return;

        LoadCurrentSkillSetting();

        if (IsDirectSelectionLayout)
        {
            RefreshAllDirectSkillButtons();
            SetAllDirectSkillPanelsVisible();
        }
        else if (currentSelectedSlot != null)
        {
            OpenSkillSelectPanel(currentSelectedSlot);
        }
        else
        {
            ClearSkillIconButtons();
            SetSkillSelectPanelVisible(false);
        }
    }

    private void LoadCurrentSkillSetting()
    {
        if (currentRuntimeData == null)
            return;

        EnsureEquippedSkillArray();

        int setupSlotCount = IsDirectSelectionLayout
            ? SetupSkillSlotCount
            : Mathf.Min(skillSlotButtons != null ? skillSlotButtons.Length : 0, SetupSkillSlotCount);

        for (int i = 0; i < setupSlotCount; i++)
        {
            SkillMasterData skill = null;
            string skillId = GetRuntimeSkillId(i);

            if (!string.IsNullOrWhiteSpace(skillId))
                DataManager.Instance.SkillDatabase.TryGet(skillId, out skill);

            if (!IsSkillValidForCurrentCharacterSlot(skill, i))
                skill = null;

            if (skill != null && IsSkillLockedForCurrentLevel(skill, i))
                skill = null;

            if (skill == null)
                skill = GetDefaultSkill(i);

            if (!IsDirectSelectionLayout && skillSlotButtons != null && i < skillSlotButtons.Length && skillSlotButtons[i] != null)
                skillSlotButtons[i].SetSkill(skill);

            SetRuntimeSkillId(i, skill != null ? skill.SkillId : "");
        }

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    private SkillMasterData GetDefaultSkill(int slotIndex)
    {
        List<SkillMasterData> candidates = GetSkillCandidates(slotIndex);

        for (int i = 0; i < candidates.Count; i++)
        {
            SkillMasterData skill = candidates[i];

            if (skill == null)
                continue;

            if (!IsSkillLockedForCurrentLevel(skill, slotIndex))
                return skill;
        }

        return null;
    }

    private void SaveCurrentSkillSetting()
    {
        if (currentRuntimeData == null)
            return;

        if (IsDirectSelectionLayout)
        {
            if (DataManager.Instance != null)
                DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
            return;
        }

        if (skillSlotButtons == null)
            return;

        EnsureEquippedSkillArray();

        int setupSlotCount = Mathf.Min(skillSlotButtons.Length, SetupSkillSlotCount);

        for (int i = 0; i < setupSlotCount; i++)
        {
            if (skillSlotButtons[i] == null)
                continue;

            SkillMasterData skill = skillSlotButtons[i].EquippedSkill;

            if (!IsSkillValidForCurrentCharacterSlot(skill, i))
                skill = null;

            if (skill != null && IsSkillLockedForCurrentLevel(skill, i))
                skill = null;

            SetRuntimeSkillId(i, skill != null ? skill.SkillId : "");
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    public void OpenSkillSelectPanel(SkillSlotButton slotButton)
    {
        if (slotButton == null)
        {
            ShowWarning("스킬 슬롯이 연결되지 않았다.");
            return;
        }

        if (currentRuntimeData == null || currentMasterData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        if (!skillSelectPanelAllowed)
        {
            OpenSkillSettingFromPreview(slotButton);
            return;
        }

        int slotIndex = slotButton.SlotIndex;

        if (slotIndex < 0 || slotIndex >= SetupSkillSlotCount)
            return;

        // 이미 열려 있는 같은 스킬 버튼을 다시 누르면 선택 패널을 닫는다.
        if (openedSkillSelectPanelIndex == slotIndex)
        {
            SetSelectedSkillSlot(null);
            SetSkillSelectPanelVisible(false);
            return;
        }

        SetSelectedSkillSlot(slotButton);
        ShowSkillInfo(slotButton.EquippedSkill);

        List<SkillMasterData> candidates = GetSkillCandidates(slotIndex);
        RefreshSkillIconButtons(candidates, slotIndex);

        // 다른 패널은 X 230으로 복귀하고 선택한 패널만 X -15로 이동한다.
        SetSkillSelectPanelVisible(true);
    }

    public void OpenDefaultSkillSlot()
    {
        if (IsDirectSelectionLayout)
        {
            RefreshAllDirectSkillButtons();
            SetAllDirectSkillPanelsVisible();
            return;
        }
        if (skillSlotButtons == null ||
            skillSlotButtons.Length == 0 ||
            skillSlotButtons[0] == null)
        {
            ShowWarning("스킬 슬롯이 연결되지 않았다.");
            return;
        }

        // 이미 0번 목록이 열려 있다면 상단 스킬 버튼을 다시 눌러도 닫지 않는다.
        if (openedSkillSelectPanelIndex == 0 && currentSelectedSlot == skillSlotButtons[0])
            return;

        OpenSkillSelectPanel(skillSlotButtons[0]);
    }

    private void OpenSkillSettingFromPreview(SkillSlotButton slotButton)
    {
        if (settingController == null)
            settingController = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);

        settingController?.OpenSkillSettingForSlot(slotButton);
    }

    private List<SkillMasterData> GetSkillCandidates(int slotIndex)
    {
        List<SkillMasterData> result = new();

        if (DataManager.Instance == null || currentMasterData == null)
            return result;

        string[] skillIds = GetCandidateSkillIds(slotIndex);

        for (int i = 0; i < skillIds.Length; i++)
        {
            string skillId = skillIds[i];

            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            if (DataManager.Instance.SkillDatabase.TryGet(skillId, out var skill))
                AddUniqueSkill(result, skill);
            else
                Debug.LogWarning("[SkillSettingPanel] Candidate skill not found: " + skillId);
        }

        return result;
    }

    private string[] GetCandidateSkillIds(int slotIndex)
    {
        if (currentMasterData == null)
            return Array.Empty<string>();

        return slotIndex switch
        {
            0 => new[] { currentMasterData.PassiveSkill1, currentMasterData.PassiveSkill2 },
            1 => new[] { currentMasterData.UniqueSkill1, currentMasterData.UniqueSkill2 },
            2 => new[] { currentMasterData.CharacterSkill1, currentMasterData.CharacterSkill2 },
            _ => Array.Empty<string>()
        };
    }

    private void AddUniqueSkill(List<SkillMasterData> result, SkillMasterData skill)
    {
        if (result == null || skill == null)
            return;

        for (int i = 0; i < result.Count; i++)
        {
            if (IsSameSkill(result[i], skill))
                return;
        }

        result.Add(skill);
    }

    private bool IsSkillValidForCurrentCharacterSlot(SkillMasterData skill, int slotIndex)
    {
        if (skill == null)
            return false;

        bool validCategory = false;

        switch (slotIndex)
        {
            case 0:
                validCategory = skill.Category == Category.Passive;
                break;

            case 1:
                validCategory = skill.Category == Category.Unique;
                break;

            case 2:
                validCategory = skill.Category == Category.Ability;
                break;

        }

        if (!validCategory)
            return false;

        return IsSkillInCurrentCandidateSet(skill, slotIndex);
    }

    private bool IsSkillInCurrentCandidateSet(SkillMasterData skill, int slotIndex)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            return false;

        string[] candidateSkillIds = GetCandidateSkillIds(slotIndex);

        if (candidateSkillIds == null || candidateSkillIds.Length == 0)
            return true;

        for (int i = 0; i < candidateSkillIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(candidateSkillIds[i]))
                continue;

            if (candidateSkillIds[i] == skill.SkillId)
                return true;
        }

        return false;
    }

    private bool IsSkillLockedForCurrentLevel(SkillMasterData skill, int slotIndex)
    {
        if (skill == null)
            return false;

        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;
        int requiredLevel = GetRequiredLevelForSkill(skill, slotIndex);

        return characterLevel < requiredLevel;
    }

    private int GetRequiredLevelForSkill(SkillMasterData skill, int slotIndex)
    {
        if (skill == null)
            return 1;

        if (slotIndex < 0 || slotIndex >= SetupSkillSlotCount)
            return 1;

        int candidateIndex = GetCandidateSkillIndex(skill, slotIndex);
        if (candidateIndex < 0)
            return 1;

        return CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(
            currentMasterData,
            slotIndex,
            candidateIndex);
    }

    private int GetCandidateSkillIndex(SkillMasterData skill, int slotIndex)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            return -1;

        string[] candidateSkillIds = GetCandidateSkillIds(slotIndex);
        if (candidateSkillIds == null)
            return -1;

        for (int i = 0; i < candidateSkillIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(candidateSkillIds[i]))
                continue;

            if (string.Equals(
                    candidateSkillIds[i].Trim(),
                    skill.SkillId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsSameSkill(SkillMasterData a, SkillMasterData b)
    {
        if (a == b)
            return true;

        if (a == null || b == null)
            return false;

        if (!string.IsNullOrWhiteSpace(a.SkillId) &&
            !string.IsNullOrWhiteSpace(b.SkillId))
            return a.SkillId == b.SkillId;

        return false;
    }

    private void RefreshSkillIconButtons(List<SkillMasterData> skills, int slotIndex)
    {
        BindSkillIconButtonsIfNeeded();

        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;
        SkillIconButton[] buttons = GetSkillIconButtons(slotIndex);

        for (int i = 0; i < buttons.Length; i++)
        {
            SkillIconButton button = buttons[i];
            if (button == null)
                continue;

            if (!button.gameObject.activeSelf)
                button.gameObject.SetActive(true);

            SkillMasterData skill = skills != null && i < skills.Count ? skills[i] : null;
            if (skill == null)
            {
                button.SetSkillData(null, false, 0);
                continue;
            }

            int requiredLevel = GetRequiredLevelForSkill(skill, slotIndex);
            bool locked = characterLevel < requiredLevel;
            button.SetSkillData(skill, locked, requiredLevel);
        }
    }


    public void SelectSkillDirect(int slotIndex, SkillMasterData skill)
    {
        if (!IsDirectSelectionLayout)
        {
            SelectSkill(skill);
            return;
        }

        if (currentRuntimeData == null || currentMasterData == null || skill == null)
            return;

        if (slotIndex < 0 || slotIndex >= SetupSkillSlotCount)
            return;

        int requiredLevel = GetRequiredLevelForSkill(skill, slotIndex);
        if (currentRuntimeData.Level < requiredLevel)
        {
            ShowWarning(SettingWarningUI.GetSkillMemoryUnlockLevelMessage(requiredLevel));
            return;
        }

        if (!IsSkillValidForCurrentCharacterSlot(skill, slotIndex))
            return;

        SetRuntimeSkillId(slotIndex, skill.SkillId);
        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
        RefreshDirectSkillSelection(slotIndex);
    }

    private void RefreshDirectSkillSelection(int slotIndex)
    {
        if (!IsDirectSelectionLayout)
            return;

        if (slotIndex < 0 || slotIndex >= SetupSkillSlotCount)
            return;

        string equippedId = GetRuntimeSkillId(slotIndex);
        SkillIconButton[] buttons = GetSkillIconButtons(slotIndex);

        for (int i = 0; i < buttons.Length; i++)
        {
            SkillMasterData data = buttons[i] != null ? buttons[i].CurrentSkillData : null;
            bool selected = data != null && !string.IsNullOrWhiteSpace(equippedId) &&
                string.Equals(data.SkillId, equippedId, StringComparison.OrdinalIgnoreCase);
            buttons[i]?.SetEquippedSelected(selected);
        }
    }

    private void RefreshAllDirectSkillButtons()
    {
        if (!IsDirectSelectionLayout)
            return;

        bool appliedDefaultSkill = false;

        for (int slotIndex = 0; slotIndex < SetupSkillSlotCount; slotIndex++)
        {
            List<SkillMasterData> candidates = GetSkillCandidates(slotIndex);
            RefreshSkillIconButtons(candidates, slotIndex);

            string equippedId = GetRuntimeSkillId(slotIndex);
            SkillIconButton[] buttons = GetSkillIconButtons(slotIndex);

            // 저장된 장착 정보가 없으면 각 분류의 Button_0을 기본 스킬로 사용합니다.
            if (string.IsNullOrWhiteSpace(equippedId) && buttons.Length > 0)
            {
                SkillMasterData defaultSkill = buttons[0] != null ? buttons[0].CurrentSkillData : null;
                if (defaultSkill != null && !IsSkillLockedForCurrentLevel(defaultSkill, slotIndex))
                {
                    equippedId = defaultSkill.SkillId;
                    SetRuntimeSkillId(slotIndex, equippedId);
                    appliedDefaultSkill = true;
                }
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                SkillMasterData data = buttons[i] != null ? buttons[i].CurrentSkillData : null;
                bool selected = data != null && !string.IsNullOrWhiteSpace(equippedId) &&
                    string.Equals(data.SkillId, equippedId, StringComparison.OrdinalIgnoreCase);
                buttons[i]?.SetEquippedSelected(selected);
            }
        }

        if (appliedDefaultSkill && currentRuntimeData != null && DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    private void SetAllDirectSkillPanelsVisible()
    {
        if (skillIconSelectPanels == null)
            return;

        for (int i = 0; i < Mathf.Min(skillIconSelectPanels.Length, SetupSkillSlotCount); i++)
        {
            if (skillIconSelectPanels[i] != null)
                skillIconSelectPanels[i].SetActive(true);
        }
    }

    public void ShowSkillInfo(SkillMasterData skill)
    {
        // 룬 위에 마우스가 있는 동안에는 뒤늦게 들어온 스킬 호버가 룬 정보를 덮어쓰지 않습니다.
        if (LobbyInfoHoverState.IsRuneHovered)
            return;

        LobbyInfoHoverState.NotifyInfoShown();
        BindSkillInfoAreaIfNeeded();
        EnsureDynamicTextOwnership();
        ConfigureSkillInfoTextComponents();

        if (skill == null)
        {
            ClearSkillInfo();
            return;
        }

        currentDisplayedSkillInfo = skill;

        if (sharedInfoArea != null)
            sharedInfoArea.SetActive(true);

        SetSkillInfoLabelsVisible(true);
        SetSkillInfoValueObjectsVisible(true);

        SetPlainTmpText(skillInfoTitleText, GameDataLocalization.SkillName(skill));
        SetPlainTmpText(skillInfoRarityText, SkillRarityUtility.GetMemoryTypeDisplayName(skill));
        ApplySkillInfoRarityColor(skill.Rarity);
        SetRichTmpText(skillInfoEffectText, BuildSkillDetailsText(skill));
        SetSkillRangeImage(skill);
        SetPlainTmpText(skillInfoCostText, GameLocalization.Format(LocalizationKeys.SkillInfo.Cost, BuildSkillCostText(skill)));
        SetPlainTmpText(skillInfoTypeText, GameLocalization.Format(LocalizationKeys.SkillInfo.Type, BuildSkillRangeTypeText(skill)));
        SetPlainTmpText(skillInfoValueText, GameLocalization.Format(LocalizationKeys.SkillInfo.Effect, BuildSkillValueText(skill)));
    }

    public void ClearSkillInfoFromHover()
    {
        ClearSkillInfoFromHover(LobbyInfoHoverState.CurrentVersion);
    }

    public void ClearSkillInfoFromHover(int hoverVersion)
    {
        StartCoroutine(ClearSkillInfoAfterHoverDelay(hoverVersion));
    }

    private IEnumerator ClearSkillInfoAfterHoverDelay(int hoverVersion)
    {
        yield return new WaitForSecondsRealtime(LobbyInfoHoverState.ClearDelaySeconds);

        if (LobbyInfoHoverState.IsCurrent(hoverVersion))
            ClearSkillInfo();
    }

    public void SetEmptyInfoText(string title, string effect)
    {
        emptySkillInfoTitle = title ?? string.Empty;
        emptySkillInfoEffect = effect ?? string.Empty;
        ClearSkillInfo();
    }

    public void ClearSkillInfo()
    {
        currentDisplayedSkillInfo = null;
        BindSkillInfoAreaIfNeeded();
        EnsureDynamicTextOwnership();
        ConfigureSkillInfoTextComponents();

        if (sharedInfoArea != null)
            sharedInfoArea.SetActive(true);

        SetSkillInfoLabelsVisible(false);
        SetSkillInfoValueObjectsVisible(false);

        SetPlainTmpText(skillInfoTitleText, emptySkillInfoTitle);
        SetPlainTmpText(skillInfoRarityText, string.Empty);
        RestoreSkillInfoRarityColor();
        SetRichTmpText(skillInfoEffectText, emptySkillInfoEffect);
        ClearSkillRangeImage();
        SetPlainTmpText(skillInfoCostText, string.Empty);
        SetPlainTmpText(skillInfoTypeText, string.Empty);
        SetPlainTmpText(skillInfoValueText, string.Empty);
    }

    private void SetSkillInfoLabelsVisible(bool visible)
    {
        // Infotext_1~4는 더 이상 사용하지 않습니다.
        // 방식/소모/효과 라벨은 각 값 텍스트에 직접 포함합니다.
        if (skillInfoRangeLabel != null)
            skillInfoRangeLabel.SetActive(false);

        if (skillInfoTypeLabel != null)
            skillInfoTypeLabel.SetActive(false);

        if (skillInfoCostLabel != null)
            skillInfoCostLabel.SetActive(false);

        if (skillInfoValueLabel != null)
            skillInfoValueLabel.SetActive(false);
    }

    /// <summary>
    /// 룬 정보가 표시될 때 스킬 전용 정보 오브젝트를 정확한 참조로 숨깁니다.
    /// 이름 검색이 아니라 현재 SkillSettingPanel이 사용하는 실제 오브젝트를 제어합니다.
    /// </summary>
    public void SetSkillInfoExtrasVisible(bool visible)
    {
        BindSkillInfoAreaIfNeeded();
        SetSkillInfoLabelsVisible(visible);
        SetSkillInfoValueObjectsVisible(visible);

        if (!visible)
        {
            ClearSkillRangeImage();
            SetPlainTmpText(skillInfoCostText, string.Empty);
            SetPlainTmpText(skillInfoTypeText, string.Empty);
            SetPlainTmpText(skillInfoValueText, string.Empty);
        }
    }

    private void SetSkillInfoValueObjectsVisible(bool visible)
    {
        if (skillInfoRangeRoot != null)
            skillInfoRangeRoot.SetActive(visible);
        else if (skillInfoRangeImage != null)
            skillInfoRangeImage.gameObject.SetActive(visible);

        if (skillInfoTypeText != null)
            skillInfoTypeText.gameObject.SetActive(visible);

        if (skillInfoCostText != null)
            skillInfoCostText.gameObject.SetActive(visible);

        if (skillInfoValueText != null)
            skillInfoValueText.gameObject.SetActive(visible);
    }

    private void ConfigureSkillInfoTextComponents()
    {
        if (skillInfoTitleText != null)
        {
            skillInfoTitleText.richText = false;
            skillInfoTitleText.parseCtrlCharacters = true;
        }

        if (skillInfoEffectText != null)
        {
            skillInfoEffectText.richText = true;
            skillInfoEffectText.overrideColorTags = false;
            skillInfoEffectText.parseCtrlCharacters = true;
        }

        if (skillInfoRarityText != null)
        {
            skillInfoRarityText.richText = false;
            skillInfoRarityText.parseCtrlCharacters = true;
        }

        if (skillInfoCostText != null)
        {
            skillInfoCostText.richText = false;
            skillInfoCostText.parseCtrlCharacters = true;
        }

        if (skillInfoTypeText != null)
        {
            skillInfoTypeText.richText = false;
            skillInfoTypeText.parseCtrlCharacters = true;
        }

        if (skillInfoValueText != null)
        {
            skillInfoValueText.richText = false;
            skillInfoValueText.parseCtrlCharacters = true;
        }
    }

    /// <summary>These TMPs are populated by this presenter and must never be claimed by static localization.</summary>
    private void EnsureDynamicTextOwnership()
    {
        TMP_Text[] dynamicTexts =
        {
            skillInfoTitleText, skillInfoEffectText, skillInfoRarityText,
            skillInfoCostText, skillInfoTypeText, skillInfoValueText,
        };

        foreach (TMP_Text text in dynamicTexts)
            EnsureDynamicTextOwnership(text);
    }

    private static void EnsureDynamicTextOwnership(TMP_Text text)
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

    private void SetSkillRangeImage(SkillMasterData skill)
    {
        if (skillInfoRangeImage == null)
            return;

        Sprite rangeSprite = null;

        if (skill != null &&
            !string.IsNullOrWhiteSpace(skill.RangeId) &&
            DataManager.Instance != null &&
            DataManager.Instance.SkillRangeIconDatabase != null)
        {
            DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(skill.RangeId, out rangeSprite);
        }

        bool hasRangeSprite = rangeSprite != null;

        if (skillInfoRangeRoot != null)
            skillInfoRangeRoot.SetActive(hasRangeSprite);

        skillInfoRangeImage.gameObject.SetActive(hasRangeSprite);
        skillInfoRangeImage.sprite = rangeSprite;
        skillInfoRangeImage.enabled = hasRangeSprite;
    }

    private void ClearSkillRangeImage()
    {
        if (skillInfoRangeImage == null)
            return;

        skillInfoRangeImage.sprite = null;
        skillInfoRangeImage.enabled = false;
        skillInfoRangeImage.gameObject.SetActive(false);

        if (skillInfoRangeRoot != null)
            skillInfoRangeRoot.SetActive(false);
    }

    private string BuildSkillCostText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        if (skill.ResourceCostValue <= 0)
            return GameLocalization.Get(LocalizationKeys.SkillInfo.NoCost);

        string resourceName;
        switch (skill.ReferenceResource)
        {
            case ReferenceResource.HP:
                resourceName = GameLocalization.Get(LocalizationKeys.Resource.Hp);
                break;
            case ReferenceResource.Cost:
                resourceName = GameLocalization.Get(LocalizationKeys.Resource.Mana);
                break;
            case ReferenceResource.UniqueResource:
                resourceName = GameLocalization.Get(LocalizationKeys.Resource.Karma);
                break;
            case ReferenceResource.MovePoint:
                resourceName = GameLocalization.Get(LocalizationKeys.Resource.Move);
                break;
            default:
                resourceName = string.Empty;
                break;
        }

        int costValue = Mathf.Max(0, skill.ResourceCostValue);
        return string.IsNullOrEmpty(resourceName)
            ? costValue.ToString()
            : $"{resourceName} {costValue}";
    }

    private int GetCurrentCharacterNumber()
    {
        if (string.IsNullOrWhiteSpace(currentCharacterId))
            return 0;

        string id = currentCharacterId.Trim();
        int endIndex = id.Length - 1;

        while (endIndex >= 0 && !char.IsDigit(id[endIndex]))
            endIndex--;

        if (endIndex < 0)
            return 0;

        int startIndex = endIndex;

        while (startIndex >= 0 && char.IsDigit(id[startIndex]))
            startIndex--;

        string numberText = id.Substring(startIndex + 1, endIndex - startIndex);

        return int.TryParse(numberText, out int characterNumber)
            ? characterNumber
            : 0;
    }

    private string GetCurrentUniqueResourceName()
    {
        switch (GetCurrentCharacterNumber())
        {
            case 1:
                return GameLocalization.Get("resource.rage");

            case 2:
                return GameLocalization.Get("resource.momentum");

            case 3:
                return GameLocalization.Get("resource.aether");

            case 4:
                return GameLocalization.Get("resource.faith");

            case 5:
                return GameLocalization.Get("resource.blood");

            default:
                return GameLocalization.Get(LocalizationKeys.Resource.Karma);
        }
    }

    private string BuildSkillRangeTypeText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        switch (skill.RangeType)
        {
            case RangeType.Direction:
                return GameLocalization.Get(LocalizationKeys.SkillInfo.RangeDirection);
            case RangeType.Selection:
                return GameLocalization.Get(LocalizationKeys.SkillInfo.RangeSelection);
            case RangeType.Passive:
                return GameLocalization.Get(LocalizationKeys.SkillInfo.RangePassive);
            default:
                return string.Empty;
        }
    }

    private string BuildPassiveActivationTypeText()
    {
        switch (GetCurrentCharacterNumber())
        {
            case 1:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.PassiveActivation, GameLocalization.Get("resource.rage"), 3);

            case 2:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.PassiveActivation, GameLocalization.Get("resource.momentum"), 5);

            case 3:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.PassiveActivation, GameLocalization.Get("resource.aether"), 3);

            case 4:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.PassiveActivation, GameLocalization.Get("resource.faith"), 3);

            case 5:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.PassiveActivation, GameLocalization.Get("resource.blood"), 5);

            default:
                return GameLocalization.Format(LocalizationKeys.SkillInfo.RangePassive, GameLocalization.Get(LocalizationKeys.Resource.Karma));
        }
    }

    private string BuildSkillValueText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        List<SkillEffectEntry> entries = skill.EffectEntries;
        if ((entries == null || entries.Count == 0) &&
            DataManager.Instance != null &&
            DataManager.Instance.EffectDatabase != null)
        {
            entries = SkillEffectParser.Parse(skill, DataManager.Instance.EffectDatabase);
        }

        if (entries == null || entries.Count == 0)
            return GameLocalization.Get(LocalizationKeys.SkillInfo.NoEffect);

        List<string> parts = new List<string>(2);
        int count = Mathf.Min(2, entries.Count);

        for (int i = 0; i < count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectName = entry.EffectData != null
                ? GameDataLocalization.EffectName(entry.EffectData)
                : entry.EffectId;

            int effectValue = entry.ValueAmount != 0 ? entry.ValueAmount : entry.CountAmount;
            string valueText = Mathf.Abs(effectValue).ToString();
            parts.Add(string.IsNullOrWhiteSpace(effectName) ? valueText : $"{effectName} {valueText}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : GameLocalization.Get(LocalizationKeys.SkillInfo.NoEffect);
    }

    private void SetPlainTmpText(TMP_Text targetText, string rawText)
    {
        if (targetText == null)
            return;

        targetText.text = NormalizeSkillInfoText(rawText);
    }

    private void SetRichTmpText(TMP_Text targetText, string rawText)
    {
        if (targetText == null)
            return;

        targetText.text = NormalizeSkillInfoText(rawText);
    }

    private string NormalizeSkillInfoText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        return text
            .Replace("\\n", "\n")
            .Replace("\\r", "")
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n");
    }

    private string BuildSkillDetailsText(SkillMasterData skill)
    {
        if (skill == null)
            return "";

        // 도감과 동일하게 토큰 치환 전 원본 Details를 사용합니다.
        // 스킬 상세 문구는 먼저 {ValueRate}/{CountRate}를 숫자로 바꾸므로
        // 이후에는 어떤 숫자가 치환값인지 알 수 없어 색상 태그를 적용할 수 없습니다.
        string details = GameDataLocalization.SkillDetailsTemplate(skill);

        if (string.IsNullOrWhiteSpace(details))
            return "";

        return FormatHighlightedEffectDescription(
            NormalizeSkillInfoText(details),
            skill.ValueRate,
            skill.CountRate);
    }

    private string FormatHighlightedEffectDescription(string description, string valueRate, string countRate)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        string result = description;
        string colorHex = ColorUtility.ToHtmlStringRGB(valueHighlightColor);

        result = ReplaceIndexedHighlightedValues(result, "ValueRate", valueRate, colorHex);
        result = ReplaceIndexedHighlightedValues(result, "CountRate", countRate, colorHex);
        result = ReplaceHighlightedValue(result, "{ValueRate}", valueRate, colorHex);
        result = ReplaceHighlightedValue(result, "{CountRate}", countRate, colorHex);

        return result;
    }

    private static string ReplaceIndexedHighlightedValues(string source, string tokenName, string values, string colorHex)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
            return source;

        string[] splitValues = string.IsNullOrWhiteSpace(values)
            ? System.Array.Empty<string>()
            : values.Split(';');

        for (int i = 0; i < splitValues.Length; i++)
        {
            string token = $"{{{tokenName}{i + 1}}}";
            if (!source.Contains(token))
                continue;

            string displayValue = GetHighlightedDisplayRateValue(splitValues[i]);
            source = source.Replace(token, $"<color=#{colorHex}>{displayValue}</color>");
        }

        return source;
    }

    private static string ReplaceHighlightedValue(string source, string token, string value, string colorHex)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
            return source;

        string displayValue = GetHighlightedDisplayRateValue(value);
        return source.Replace(token, $"<color=#{colorHex}>{displayValue}</color>");
    }

    private static string GetHighlightedDisplayRateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        string displayValue = value.Trim();
        if (displayValue.Length > 1 &&
            displayValue[0] == '-' &&
            float.TryParse(
                displayValue.Substring(1),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out _))
        {
            return displayValue.Substring(1);
        }

        return displayValue;
    }

    private void ApplySkillInfoRarityColor(SkillRarity rarity)
    {
        if (skillInfoRarityText == null)
            return;

        skillInfoRarityText.color = GetSkillInfoRarityColor(rarity);
    }

    private void RestoreSkillInfoRarityColor()
    {
        if (skillInfoRarityText != null)
            skillInfoRarityText.color = commonRarityColor;
    }

    private Color GetSkillInfoRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Exclusive:
                return exclusiveRarityColor;
            case SkillRarity.Rare:
                return rareRarityColor;
            case SkillRarity.Epic:
                return epicRarityColor;
            case SkillRarity.Unique:
                return uniqueRarityColor;
            default:
                return commonRarityColor;
        }
    }

    private string ColorizeSkillDetailNumbersOutsideRichTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        StringBuilder builder = new StringBuilder(text.Length + 64);
        StringBuilder plainBuffer = new StringBuilder();
        bool insideRichTextTag = false;

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            if (current == '<')
            {
                AppendColorizedSkillInfoPlainSegment(builder, plainBuffer.ToString());
                plainBuffer.Length = 0;
                insideRichTextTag = true;
                builder.Append(current);
                continue;
            }

            if (insideRichTextTag)
            {
                builder.Append(current);

                if (current == '>')
                    insideRichTextTag = false;

                continue;
            }

            plainBuffer.Append(current);
        }

        AppendColorizedSkillInfoPlainSegment(builder, plainBuffer.ToString());
        return builder.ToString();
    }

    private void AppendColorizedSkillInfoPlainSegment(StringBuilder builder, string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return;

        const string orangeColor = "#FF9A00";
        const string pattern = @"(?<![A-Za-z0-9_])([+-]?\d+(?:\.\d+)?%?|[+-])";

        string colorized = Regex.Replace(segment, pattern, match =>
        {
            string value = match.Value;

            if (string.IsNullOrEmpty(value))
                return value;

            return "<color=" + orangeColor + ">" + value + "</color>";
        });

        builder.Append(colorized);
    }

    public void EquipSkillFromIcon(SkillMasterData skill)
    {
        SelectSkill(skill);
    }

    public void SelectSkill(SkillMasterData skill)
    {
        if (currentSelectedSlot == null)
        {
            ShowWarning("스킬을 장착할 슬롯을 먼저 선택해야 한다.");
            return;
        }

        if (skill == null)
        {
            ShowWarning("선택된 스킬이 없다.");
            return;
        }

        int requiredLevel = GetRequiredLevelForSkill(skill, currentSelectedSlot.SlotIndex);
        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        if (characterLevel < requiredLevel)
        {
            ShowWarning(string.Format("아직 잠겨있는 스킬이다. 필요 레벨: LV. {0}", requiredLevel));
            return;
        }

        if (!IsSkillValidForCurrentCharacterSlot(skill, currentSelectedSlot.SlotIndex))
        {
            ShowWarning("이 슬롯에 장착할 수 없는 스킬이다.");
            return;
        }

        currentSelectedSlot.SetSkill(skill);
        ShowSkillInfo(skill);
        SaveCurrentSkillSetting();

        // 패널 안의 스킬 버튼을 선택하면 선택 패널을 다시 숨김 위치로 돌린다.
        SetSkillSelectPanelVisible(false);

        // 패널이 이동하며 다른 아이콘의 PointerEnter가 발생해도
        // 최종적으로 실제 선택한 스킬 정보가 남도록 다시 고정합니다.
        StartCoroutine(RestoreSelectedSkillInfoAfterPanelClose(skill));
    }

    private IEnumerator RestoreSelectedSkillInfoAfterPanelClose(SkillMasterData selectedSkill)
    {
        float waitTime = Mathf.Max(0f, skillSelectPanelMoveDuration) + 0.02f;

        if (waitTime > 0f)
            yield return new WaitForSecondsRealtime(waitTime);
        else
            yield return null;

        if (selectedSkill != null && currentSelectedSlot != null &&
            IsSameSkill(currentSelectedSlot.EquippedSkill, selectedSkill))
        {
            ShowSkillInfo(selectedSkill);
        }

    }

    public void SaveBeforeBattle()
    {
        SaveCurrentSkillSetting();
    }

    public void CommitRuntimeStateForSave()
    {
        SaveCurrentSkillSetting();
    }

    private string GetRuntimeSkillId(int slotIndex)
    {
        if (currentRuntimeData == null)
            return null;

        switch (slotIndex)
        {
            case 0:
                return currentRuntimeData.PassiveSkillId;

            case 1:
                return currentRuntimeData.UniqueSkillId;

            case 2:
                return currentRuntimeData.AbilitySkillId;

        }

        return null;
    }

    private void SetRuntimeSkillId(int slotIndex, string skillId)
    {
        if (currentRuntimeData == null)
            return;

        EnsureEquippedSkillArray();

        switch (slotIndex)
        {
            case 0:
                currentRuntimeData.PassiveSkillId = skillId;
                break;

            case 1:
                currentRuntimeData.UniqueSkillId = skillId;
                currentRuntimeData.EquippedSkillIds[0] = skillId;
                break;

            case 2:
                currentRuntimeData.AbilitySkillId = skillId;
                currentRuntimeData.EquippedSkillIds[1] = skillId;
                break;

        }
    }

    private void EnsureEquippedSkillArray()
    {
        if (currentRuntimeData == null)
            return;

        if (currentRuntimeData.EquippedSkillIds == null ||
            currentRuntimeData.EquippedSkillIds.Length != 4)
        {
            currentRuntimeData.EquippedSkillIds = new string[4];
        }

        if (string.IsNullOrWhiteSpace(currentRuntimeData.EquippedSkillIds[0]))
            currentRuntimeData.EquippedSkillIds[0] = currentRuntimeData.UniqueSkillId;

        if (string.IsNullOrWhiteSpace(currentRuntimeData.EquippedSkillIds[1]))
            currentRuntimeData.EquippedSkillIds[1] = currentRuntimeData.AbilitySkillId;

        // 탐사 시작 전에는 코어 스킬 슬롯을 비워 둡니다.
        // EquippedSkillIds[2], [3]은 탐사 중 획득한 코어 스킬 장착에 사용됩니다.
        currentRuntimeData.EquippedSkillIds[2] = string.Empty;
        currentRuntimeData.EquippedSkillIds[3] = string.Empty;
    }

    private void ClearSkillSlots()
    {
        if (skillSlotButtons != null)
        {
            for (int i = 0; i < skillSlotButtons.Length; i++)
            {
                if (skillSlotButtons[i] != null)
                    skillSlotButtons[i].SetSkill(null);
            }
        }

        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSelectedSkillSlot(null);
        SetSkillSelectPanelVisible(false);
    }

    private void ClearSkillIconButtons()
    {
        BindSkillIconButtonsIfNeeded();

        if (skillIconSelectPanels == null)
            return;

        for (int panelIndex = 0; panelIndex < Mathf.Min(skillIconSelectPanels.Length, SetupSkillSlotCount); panelIndex++)
        {
            SkillIconButton[] buttons = GetSkillIconButtons(panelIndex);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                if (!buttons[i].gameObject.activeSelf)
                    buttons[i].gameObject.SetActive(true);

                buttons[i].SetSkillData(null, false, 0);
                buttons[i].SetEquippedSelected(false);
            }
        }

        SetAllDirectSkillPanelsVisible();
    }

    public void ClearForEmptyCharacter()
    {
        currentCharacterId = null;
        currentMasterData = null;
        currentRuntimeData = null;
        SetSelectedSkillSlot(null);

        ClearSkillSlots();
        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(false);
    }

    public void ShowWarning(string message)
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
            warningUI.Show(message);
        else
            Debug.LogWarning("[SkillSettingPanel] " + message);
    }
}
