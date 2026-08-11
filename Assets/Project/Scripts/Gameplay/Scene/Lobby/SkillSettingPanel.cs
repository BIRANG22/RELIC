using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class SkillSettingPanel : MonoBehaviour, IRuntimeSaveStateContributor
{
    private const int SetupSkillSlotCount = 3;

    [Header("Skill Slots")]
    [SerializeField] private SkillSlotButton[] skillSlotButtons;

    [Header("Skill Select Panels")]
    [SerializeField] private GameObject[] skillIconSelectPanels = new GameObject[SetupSkillSlotCount];
    [SerializeField] private float skillSelectPanelHiddenX = 235f;
    [SerializeField] private float skillSelectPanelVisibleX = -15f;
    [SerializeField, Min(0.01f)] private float skillSelectPanelMoveDuration = 0.2f;

    [Header("Skill Select Button Count")]
    [SerializeField] private int maxVisibleSkillIconButtonCount = 2;
    [SerializeField] private bool autoBindSkillIconButtons = true;

    [Header("Shared Info Area")]
    [FormerlySerializedAs("skillInfoArea")]
    [SerializeField] private GameObject sharedInfoArea;
    [SerializeField] private TMP_Text skillInfoTitleText;
    [SerializeField] private TMP_Text skillInfoEffectText;
    [SerializeField] private GameObject skillInfoRangeRoot;
    [SerializeField] private Image skillInfoRangeImage;
    [SerializeField] private TMP_Text skillInfoCostText;
    [SerializeField] private TMP_Text skillInfoTypeText;
    [SerializeField] private TMP_Text skillInfoValueText;

    [Header("Shared Info Labels")]
    [SerializeField] private GameObject skillInfoRangeLabel;
    [SerializeField] private GameObject skillInfoTypeLabel;
    [SerializeField] private GameObject skillInfoCostLabel;
    [SerializeField] private GameObject skillInfoValueLabel;

    [SerializeField] private string emptySkillInfoTitle = "스킬명";
    [SerializeField, TextArea] private string emptySkillInfoEffect = "스킬을 선택하면 정보가 표시됩니다.";
    [SerializeField] private bool autoBindSkillInfoArea = true;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private Setting settingController;

    private SkillSlotButton currentSelectedSlot;
    private int openedSkillSelectPanelIndex = -1;
    private bool suppressSkillIconHover;
    private Coroutine[] skillSelectPanelMoveCoroutines = new Coroutine[SetupSkillSlotCount];

    private bool skillSelectPanelAllowed = true;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindSkillIconButtonsIfNeeded();
        BindSkillInfoAreaIfNeeded();
        InitSkillSlotButtons();
        InitSkillIconButtons();

        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(false, true);
    }

    public void SetSettingController(Setting controller)
    {
        settingController = controller;
    }

    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindSkillIconButtonsIfNeeded();
        BindSkillInfoAreaIfNeeded();
        InitSkillSlotButtons();
        InitSkillIconButtons();
        SetSelectedSkillSlot(null);
        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(false, true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxVisibleSkillIconButtonCount < 1)
            maxVisibleSkillIconButtonCount = 1;
    }
#endif

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
                    buttons[i].Init(this);
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
            return new SkillIconButton[0];

        Transform panel = skillIconSelectPanels[panelIndex].transform;
        Transform buttonRoot = panel.Find("ButtonRoot");
        Transform root = buttonRoot != null ? buttonRoot : panel;
        return root.GetComponentsInChildren<SkillIconButton>(true);
    }


    private void BindSkillInfoAreaIfNeeded()
    {
        if (!autoBindSkillInfoArea)
            return;

        if (skillInfoTitleText != null &&
            skillInfoEffectText != null &&
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
            Transform type = area.Find("TypeText");
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

    public bool ShouldClearInfoOnHoverExit => !skillSelectPanelAllowed;

    // 스킬 선택 패널이 이동하는 동안에는 지나가는 아이콘의 호버 정보를 반영하지 않습니다.
    public bool CanPreviewSkillIconHover => !suppressSkillIconHover;

    public void SetSkillSelectPanelEnabledForTab(bool enabled)
    {
        skillSelectPanelAllowed = enabled;

        if (!enabled)
        {
            SetSelectedSkillSlot(null);
            ClearSkillInfo();
            SetSkillSelectPanelVisible(false);
            return;
        }

        // 스킬 탭으로 들어온 직후에는 슬롯을 선택하기 전까지 선택 패널을 열지 않는다.
        SetSkillSelectPanelVisible(currentSelectedSlot != null);
    }

    public void SetSkillSelectPanelVisible(bool visible)
    {
        SetSkillSelectPanelVisible(visible, false);
    }

    private void SetSkillSelectPanelVisible(bool visible, bool immediate)
    {
        BindSkillIconButtonsIfNeeded();

        if (skillIconSelectPanels == null)
            return;

        EnsureMoveCoroutineArray();

        int selectedIndex = currentSelectedSlot != null ? currentSelectedSlot.SlotIndex : -1;
        bool canShow = skillSelectPanelAllowed && visible && selectedIndex >= 0;
        openedSkillSelectPanelIndex = canShow ? selectedIndex : -1;

        if (canShow)
            suppressSkillIconHover = false;

        for (int i = 0; i < skillIconSelectPanels.Length; i++)
        {
            GameObject panelObject = skillIconSelectPanels[i];
            if (panelObject == null)
                continue;

            if (i >= SetupSkillSlotCount)
            {
                panelObject.SetActive(false);
                continue;
            }

            // 선택 패널은 비활성화하지 않고 X 좌표 이동으로 화면 안팎을 전환한다.
            if (!panelObject.activeSelf)
                panelObject.SetActive(true);

            float targetX = canShow && i == selectedIndex
                ? skillSelectPanelVisibleX
                : skillSelectPanelHiddenX;

            MoveSkillSelectPanelX(i, panelObject, targetX, immediate);
        }
    }

    private void EnsureMoveCoroutineArray()
    {
        int panelCount = skillIconSelectPanels != null ? skillIconSelectPanels.Length : 0;

        if (skillSelectPanelMoveCoroutines == null || skillSelectPanelMoveCoroutines.Length != panelCount)
            skillSelectPanelMoveCoroutines = new Coroutine[panelCount];
    }

    private void MoveSkillSelectPanelX(int panelIndex, GameObject panelObject, float targetX, bool immediate)
    {
        if (panelObject == null)
            return;

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        if (panelIndex >= 0 &&
            skillSelectPanelMoveCoroutines != null &&
            panelIndex < skillSelectPanelMoveCoroutines.Length &&
            skillSelectPanelMoveCoroutines[panelIndex] != null)
        {
            StopCoroutine(skillSelectPanelMoveCoroutines[panelIndex]);
            skillSelectPanelMoveCoroutines[panelIndex] = null;
        }

        if (immediate || skillSelectPanelMoveDuration <= 0f || !isActiveAndEnabled)
        {
            Vector2 position = rectTransform.anchoredPosition;
            position.x = targetX;
            rectTransform.anchoredPosition = position;
            return;
        }

        skillSelectPanelMoveCoroutines[panelIndex] = StartCoroutine(
            MoveSkillSelectPanelXCoroutine(panelIndex, rectTransform, targetX));
    }

    private IEnumerator MoveSkillSelectPanelXCoroutine(
        int panelIndex,
        RectTransform rectTransform,
        float targetX)
    {
        float startX = rectTransform.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < skillSelectPanelMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / skillSelectPanelMoveDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            Vector2 position = rectTransform.anchoredPosition;
            position.x = Mathf.LerpUnclamped(startX, targetX, easedProgress);
            rectTransform.anchoredPosition = position;

            yield return null;
        }

        Vector2 finalPosition = rectTransform.anchoredPosition;
        finalPosition.x = targetX;
        rectTransform.anchoredPosition = finalPosition;

        if (skillSelectPanelMoveCoroutines != null &&
            panelIndex >= 0 &&
            panelIndex < skillSelectPanelMoveCoroutines.Length)
        {
            skillSelectPanelMoveCoroutines[panelIndex] = null;
        }
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
            ShowWarning(GameLocalization.Get("common.data_unavailable", "데이터를 사용할 수 없습니다."));
            return;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearSkillSlots();
            ShowWarning(GameLocalization.Get("lobby.no_character_selected", "선택된 캐릭터가 없습니다."));
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearSkillSlots();
            ShowWarning(GameLocalization.Format("lobby.character_data_not_found_id", "캐릭터 데이터를 찾을 수 없습니다: {0}", characterId));
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearSkillSlots();
            ShowWarning(GameLocalization.Format("lobby.character_data_not_found_id", "캐릭터 데이터를 찾을 수 없습니다: {0}", characterId));
            return;
        }

        EnsureEquippedSkillArray();

        LoadCurrentSkillSetting();
        SetSelectedSkillSlot(null);
        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(false);
    }

    public void RefreshByCurrentLevel()
    {
        if (currentRuntimeData == null || currentMasterData == null)
            return;

        LoadCurrentSkillSetting();

        if (currentSelectedSlot != null)
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
        if (currentRuntimeData == null || skillSlotButtons == null)
            return;

        EnsureEquippedSkillArray();

        int setupSlotCount = Mathf.Min(skillSlotButtons.Length, SetupSkillSlotCount);

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

            if (skillSlotButtons[i] != null)
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
        if (currentRuntimeData == null || skillSlotButtons == null)
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
            ShowWarning(GameLocalization.Get("lobby.skill_slot_not_connected", "스킬 슬롯이 연결되지 않았습니다."));
            return;
        }

        if (currentRuntimeData == null || currentMasterData == null)
        {
            ShowWarning(GameLocalization.Get("lobby.select_character_first", "캐릭터를 먼저 선택해야 합니다."));
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
        if (skillSlotButtons == null ||
            skillSlotButtons.Length == 0 ||
            skillSlotButtons[0] == null)
        {
            ShowWarning(GameLocalization.Get("lobby.skill_slot_not_connected", "스킬 슬롯이 연결되지 않았습니다."));
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
        int characterNumber = GetCurrentCharacterNumber();

        if (characterNumber > 0)
        {
            string[] fixedSkillIds = GetFixedCandidateSkillIds(characterNumber, slotIndex);

            if (fixedSkillIds != null && fixedSkillIds.Length > 0)
                return fixedSkillIds;
        }

        if (currentMasterData == null)
            return new string[0];

        switch (slotIndex)
        {
            case 0:
                return new string[]
                {
                    currentMasterData.PassiveSkill1,
                    currentMasterData.PassiveSkill2
                };

            case 1:
                return new string[]
                {
                    currentMasterData.UniqueSkill1,
                    currentMasterData.UniqueSkill2
                };

            case 2:
                return new string[]
                {
                    currentMasterData.CharacterSkill1,
                    currentMasterData.CharacterSkill2
                };

            default:
                return new string[0];
        }
    }

    private string[] GetFixedCandidateSkillIds(int characterNumber, int slotIndex)
    {
        switch (characterNumber)
        {
            case 1:
                switch (slotIndex)
                {
                    case 0:
                        return new string[] { "S_Passive_01", "S_Passive_02" };
                    case 1:
                        return new string[] { "S_Unique_01", "S_Unique_02" };
                    case 2:
                        return new string[] { "S_Ability_01", "S_Ability_03" };
                }
                break;

            case 2:
                switch (slotIndex)
                {
                    case 0:
                        return new string[] { "S_Passive_03", "S_Passive_04" };
                    case 1:
                        return new string[] { "S_Unique_03", "S_Unique_04" };
                    case 2:
                        return new string[] { "S_Ability_05", "S_Ability_07" };
                }
                break;

            case 3:
                switch (slotIndex)
                {
                    case 0:
                        return new string[] { "S_Passive_05", "S_Passive_06" };
                    case 1:
                        return new string[] { "S_Unique_05", "S_Unique_06" };
                    case 2:
                        return new string[] { "S_Ability_09", "S_Ability_11" };
                }
                break;
        }

        return new string[0];
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

        if (int.TryParse(numberText, out int characterNumber))
            return characterNumber;

        return 0;
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
        return 1;
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

        if (buttons.Length == 0)
            return;

        int visibleCount = Mathf.Clamp(maxVisibleSkillIconButtonCount, 1, buttons.Length);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            bool canUseButton = i < visibleCount;

            if (canUseButton && skills != null && i < skills.Count && skills[i] != null)
            {
                SkillMasterData skill = skills[i];
                int requiredLevel = GetRequiredLevelForSkill(skill, slotIndex);
                bool locked = characterLevel < requiredLevel;

                buttons[i].SetSkillData(skill, locked, requiredLevel);
            }
            else
            {
                buttons[i].SetSkillData(null, false, 0);
            }
        }
    }


    public void ShowSkillInfo(SkillMasterData skill)
    {
        // 룬 위에 마우스가 있는 동안에는 뒤늦게 들어온 스킬 호버가 룬 정보를 덮어쓰지 않습니다.
        if (LobbyInfoHoverState.IsRuneHovered)
            return;

        LobbyInfoHoverState.NotifyInfoShown();
        BindSkillInfoAreaIfNeeded();
        ConfigureSkillInfoTextComponents();

        if (skill == null)
        {
            ClearSkillInfo();
            return;
        }

        if (sharedInfoArea != null)
            sharedInfoArea.SetActive(true);

        SetSkillInfoLabelsVisible(true);
        SetSkillInfoValueObjectsVisible(true);

        SetPlainTmpText(skillInfoTitleText, GameDataLocalization.SkillName(skill));
        SetRichTmpText(skillInfoEffectText, BuildSkillDetailsText(skill));
        SetSkillRangeImage(skill);
        SetPlainTmpText(skillInfoCostText, BuildSkillCostText(skill));
        SetPlainTmpText(skillInfoTypeText, BuildSkillRangeTypeText(skill));
        SetPlainTmpText(skillInfoValueText, BuildSkillValueText(skill));
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
        BindSkillInfoAreaIfNeeded();
        ConfigureSkillInfoTextComponents();

        if (sharedInfoArea != null)
            sharedInfoArea.SetActive(true);

        SetSkillInfoLabelsVisible(false);
        SetSkillInfoValueObjectsVisible(false);

        SetPlainTmpText(skillInfoTitleText, emptySkillInfoTitle);
        SetRichTmpText(skillInfoEffectText, emptySkillInfoEffect);
        ClearSkillRangeImage();
        SetPlainTmpText(skillInfoCostText, string.Empty);
        SetPlainTmpText(skillInfoTypeText, string.Empty);
        SetPlainTmpText(skillInfoValueText, string.Empty);
    }

    private void SetSkillInfoLabelsVisible(bool visible)
    {
        if (skillInfoRangeLabel != null)
            skillInfoRangeLabel.SetActive(visible);

        if (skillInfoTypeLabel != null)
            skillInfoTypeLabel.SetActive(visible);

        if (skillInfoCostLabel != null)
            skillInfoCostLabel.SetActive(visible);

        if (skillInfoValueLabel != null)
            skillInfoValueLabel.SetActive(visible);
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
            // 스킬 설명의 숫자에 별도 색상을 적용하지 않고 CSV 원문 그대로 표시합니다.
            skillInfoEffectText.richText = false;
            skillInfoEffectText.parseCtrlCharacters = true;
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

        int costValue = Mathf.Max(0, skill.ResourceCostValue);
        if (costValue <= 0)
            return GameLocalization.Get("common.no_cost", "소모 없음");

        string resourceName;

        switch (skill.ReferenceResource)
        {
            case ReferenceResource.HP:
                resourceName = GameLocalization.Get("common.hp", "체력");
                break;

            case ReferenceResource.Cost:
                resourceName = GameLocalization.Get("common.cost", "코스트");
                break;

            case ReferenceResource.UniqueResource:
                resourceName = GetCurrentUniqueResourceName();
                break;

            case ReferenceResource.MovePoint:
                resourceName = GameLocalization.Get("common.move_point", "이동력");
                break;

            default:
                return string.Empty;
        }

        return $"{resourceName} {costValue}";
    }

    private string GetCurrentUniqueResourceName()
    {
        switch (GetCurrentCharacterNumber())
        {
            case 1:
                return GameLocalization.Get("resource.rage", "분노");

            case 2:
                return GameLocalization.Get("resource.momentum", "기세");

            case 3:
                return GameLocalization.Get("resource.aether", "에테르");

            case 4:
                return GameLocalization.Get("resource.faith", "신앙");

            case 5:
                return GameLocalization.Get("resource.blood", "혈기");

            default:
                return GameLocalization.Get("resource.unique", "고유자원");
        }
    }

    private string BuildSkillRangeTypeText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        // 패시브 스킬은 범위 사용 방식 대신 고유자원 유지 조건을 표시합니다.
        if (skill.Category == Category.Passive)
            return BuildPassiveActivationTypeText();

        switch (skill.RangeType)
        {
            case RangeType.Selection:
                return GameLocalization.Get("skill.range_selection", "그리드 선택");

            case RangeType.Direction:
                return GameLocalization.Get("skill.range_caster_position", "시전자 위치");

            default:
                return string.Empty;
        }
    }

    private string BuildPassiveActivationTypeText()
    {
        switch (GetCurrentCharacterNumber())
        {
            case 1:
                return GameLocalization.Get("skill.passive_rage_3", "분노 3 유지 시 지속");

            case 2:
                return GameLocalization.Get("skill.passive_momentum_5", "기세 5 유지 시 지속");

            case 3:
                return GameLocalization.Get("skill.passive_aether_3", "에테르 3 유지 시 지속");

            case 4:
                return GameLocalization.Get("skill.passive_faith_3", "신앙 3 유지 시 지속");

            case 5:
                return GameLocalization.Get("skill.passive_blood_5", "혈기 5 유지 시 지속");

            default:
                return GameLocalization.Get("skill.passive_unique_max", "고유자원 최대치 유지 시 지속");
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
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry == null)
                continue;

            string effectName = entry.EffectData != null &&
                                !string.IsNullOrWhiteSpace(entry.EffectData.Name)
                ? GameDataLocalization.EffectName(entry.EffectData)
                : entry.EffectId;

            effectName = GetEffectDisplayName(effectName, entry.EffectId);

            if (string.IsNullOrWhiteSpace(effectName))
                continue;

            int value = SkillValueCalculator.GetValue(entry);
            int count = Mathf.Max(0, SkillValueCalculator.GetCount(entry));

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append(effectName);

            if (value != 0)
            {
                builder.Append(' ');
                builder.Append(value);
            }

            if (count > 1)
            {
                builder.Append(" × ");
                builder.Append(count);
            }
        }

        return builder.ToString();
    }

    private string GetEffectDisplayName(string effectName, string effectId)
    {
        string source = !string.IsNullOrWhiteSpace(effectName)
            ? effectName.Trim()
            : string.Empty;

        string id = !string.IsNullOrWhiteSpace(effectId)
            ? effectId.Trim()
            : string.Empty;

        if (string.Equals(source, "타격", System.StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get("common.damage", "피해");

        if (string.Equals(source, "관통", System.StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get("effect.piercing_damage", "관통피해");

        if (string.Equals(source, "E_Move", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(id, "E_Move", System.StringComparison.OrdinalIgnoreCase))
        {
            return GameLocalization.Get("common.move", "이동");
        }

        return source;
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

        string details = GameDataLocalization.SkillDetails(skill);

        if (string.IsNullOrWhiteSpace(details))
            details = GameDataLocalization.SkillTooltip(skill);

        if (string.IsNullOrWhiteSpace(details))
            return "";

        // 설명에 포함된 숫자를 색상 태그로 감싸지 않고 원문 그대로 표시합니다.
        return NormalizeSkillInfoText(details);
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
            ShowWarning(GameLocalization.Get("lobby.select_skill_slot_first", "스킬을 장착할 슬롯을 먼저 선택하세요."));
            return;
        }

        if (skill == null)
        {
            ShowWarning(GameLocalization.Get("lobby.no_skill_selected", "선택된 스킬이 없습니다."));
            return;
        }

        int requiredLevel = GetRequiredLevelForSkill(skill, currentSelectedSlot.SlotIndex);
        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        if (characterLevel < requiredLevel)
        {
            ShowWarning(GameLocalization.Format("lobby.skill_locked_level", "아직 잠겨있는 스킬입니다. 필요 레벨: LV. {0}", requiredLevel));
            return;
        }

        if (!IsSkillValidForCurrentCharacterSlot(skill, currentSelectedSlot.SlotIndex))
        {
            ShowWarning(GameLocalization.Get("lobby.skill_not_available_for_slot", "이 슬롯에 장착할 수 없는 스킬입니다."));
            return;
        }

        currentSelectedSlot.SetSkill(skill);
        ShowSkillInfo(skill);
        SaveCurrentSkillSetting();

        // 패널이 닫히며 다른 아이콘 위를 지나갈 때 발생하는 PointerEnter를 무시합니다.
        suppressSkillIconHover = true;

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

        suppressSkillIconHover = false;
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

        for (int panelIndex = 0; panelIndex < skillIconSelectPanels.Length; panelIndex++)
        {
            SkillIconButton[] buttons = GetSkillIconButtons(panelIndex);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].SetSkillData(null, false, 0);
            }
        }
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
