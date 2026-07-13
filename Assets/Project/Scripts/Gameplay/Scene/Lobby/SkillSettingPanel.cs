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
    [Header("Skill Slots")]
    [SerializeField] private SkillSlotButton[] skillSlotButtons;

    [Header("Skill Select Panels")]
    [SerializeField] private GameObject[] skillIconSelectPanels = new GameObject[4];
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
    [SerializeField] private Image skillInfoRangeImage;
    [SerializeField] private TMP_Text skillInfoCostText;
    [SerializeField] private string emptySkillInfoTitle = "스킬명";
    [SerializeField, TextArea] private string emptySkillInfoEffect = "스킬을 선택하면 정보가 표시됩니다.";
    [SerializeField] private bool autoBindSkillInfoArea = true;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private SkillSlotButton currentSelectedSlot;
    private int openedSkillSelectPanelIndex = -1;
    private Coroutine[] skillSelectPanelMoveCoroutines = new Coroutine[4];

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
            if (skillSlotButtons[i] != null)
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

        for (int panelIndex = 0; panelIndex < skillIconSelectPanels.Length; panelIndex++)
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

        if (skillIconSelectPanels == null || skillIconSelectPanels.Length != 4)
            skillIconSelectPanels = new GameObject[4];

        for (int i = 0; i < skillIconSelectPanels.Length; i++)
        {
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
            skillInfoCostText != null)
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

        if (skillInfoRangeImage == null)
        {
            Transform range = area.Find("RangeImg");
            if (range == null)
                range = area.Find("RangeImage");
            if (range == null)
                range = area.Find("RangeIcon");
            if (range != null)
                skillInfoRangeImage = range.GetComponent<Image>();
        }

        if (skillInfoCostText == null)
        {
            Transform cost = area.Find("CostText");
            if (cost == null)
                cost = area.Find("ResourceCostText");
            if (cost != null)
                skillInfoCostText = cost.GetComponent<TMP_Text>();
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

        for (int i = 0; i < skillIconSelectPanels.Length; i++)
        {
            GameObject panelObject = skillIconSelectPanels[i];
            if (panelObject == null)
                continue;

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
            ShowWarning("DataManager가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearSkillSlots();
            ShowWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearSkillSlots();
            ShowWarning("캐릭터 마스터 데이터를 찾을 수 없습니다: " + characterId);
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearSkillSlots();
            ShowWarning("캐릭터 런타임 데이터를 찾을 수 없습니다: " + characterId);
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

        for (int i = 0; i < skillSlotButtons.Length; i++)
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

        for (int i = 0; i < skillSlotButtons.Length; i++)
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
            ShowWarning("스킬 슬롯이 연결되지 않았습니다.");
            return;
        }

        if (currentRuntimeData == null || currentMasterData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
            return;
        }

        int slotIndex = slotButton.SlotIndex;

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

            case 3:
                return new string[]
                {
                    currentMasterData.CommonSkill1,
                    currentMasterData.CommonSkill2
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
                    case 3:
                        return new string[] { "S_Public_01", "S_Public_03" };
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
                    case 3:
                        return new string[] { "S_Public_05", "S_Public_07" };
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
                    case 3:
                        return new string[] { "S_Public_09", "S_Public_11" };
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

            case 3:
                validCategory = skill.Category == Category.Public;
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

        SetPlainTmpText(skillInfoTitleText, skill.Name);
        SetRichTmpText(skillInfoEffectText, BuildSkillDetailsText(skill));
        SetSkillRangeImage(skill);
        SetPlainTmpText(skillInfoCostText, BuildSkillCostText(skill));
    }

    public void ClearSkillInfoFromHover()
    {
        int hoverVersion = LobbyInfoHoverState.CurrentVersion;
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

        SetPlainTmpText(skillInfoTitleText, emptySkillInfoTitle);
        SetRichTmpText(skillInfoEffectText, emptySkillInfoEffect);
        ClearSkillRangeImage();
        SetPlainTmpText(skillInfoCostText, string.Empty);
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
            skillInfoEffectText.parseCtrlCharacters = true;
        }

        if (skillInfoCostText != null)
        {
            skillInfoCostText.richText = false;
            skillInfoCostText.parseCtrlCharacters = true;
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

        skillInfoRangeImage.sprite = rangeSprite;
        skillInfoRangeImage.enabled = rangeSprite != null;
    }

    private void ClearSkillRangeImage()
    {
        if (skillInfoRangeImage == null)
            return;

        skillInfoRangeImage.sprite = null;
        skillInfoRangeImage.enabled = false;
    }

    private string BuildSkillCostText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        string resourceName;

        switch (skill.ReferenceResource)
        {
            case ReferenceResource.HP:
                resourceName = "HP";
                break;

            case ReferenceResource.Cost:
                resourceName = "Cost";
                break;

            case ReferenceResource.UniqueResource:
                resourceName = "Ulit";
                break;

            default:
                return string.Empty;
        }

        return $"{resourceName} {Mathf.Max(0, skill.ResourceCostValue)}소모";
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

        string details = skill.Details;

        if (string.IsNullOrWhiteSpace(details))
            details = skill.ToolTip;

        if (string.IsNullOrWhiteSpace(details))
            return "";

        string normalizedDetails = NormalizeSkillInfoText(details);
        return ColorizeSkillDetailNumbersOutsideRichTags(normalizedDetails);
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
            ShowWarning("스킬을 장착할 슬롯을 먼저 선택하세요.");
            return;
        }

        if (skill == null)
        {
            ShowWarning("선택된 스킬이 없습니다.");
            return;
        }

        int requiredLevel = GetRequiredLevelForSkill(skill, currentSelectedSlot.SlotIndex);
        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        if (characterLevel < requiredLevel)
        {
            ShowWarning("아직 잠겨있는 스킬입니다. 필요 레벨: LV. " + requiredLevel);
            return;
        }

        if (!IsSkillValidForCurrentCharacterSlot(skill, currentSelectedSlot.SlotIndex))
        {
            ShowWarning("이 슬롯에 장착할 수 없는 스킬입니다.");
            return;
        }

        currentSelectedSlot.SetSkill(skill);
        ShowSkillInfo(skill);
        SaveCurrentSkillSetting();

        // 패널 안의 스킬 버튼을 선택하면 선택 패널을 다시 숨김 위치로 돌린다.
        SetSkillSelectPanelVisible(false);
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

            case 3:
                return currentRuntimeData.EquippedSkillIds != null &&
                       currentRuntimeData.EquippedSkillIds.Length > 2
                    ? currentRuntimeData.EquippedSkillIds[2]
                    : null;
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

            case 3:
                currentRuntimeData.EquippedSkillIds[2] = skillId;
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

        string defaultPublicSkillId = GetDefaultSkillIdForSlot(3);

        if (string.IsNullOrWhiteSpace(currentRuntimeData.EquippedSkillIds[2]))
            currentRuntimeData.EquippedSkillIds[2] = defaultPublicSkillId;

        string freeSkillId = currentRuntimeData.EquippedSkillIds[2];

        if (string.IsNullOrWhiteSpace(freeSkillId) || !IsPublicSkill(freeSkillId))
            currentRuntimeData.EquippedSkillIds[2] = defaultPublicSkillId;
    }

    private bool IsPublicSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return false;

        SkillMasterData skill = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skill == null)
            return false;

        return skill.Category == Category.Public;
    }

    private string GetDefaultSkillIdForSlot(int slotIndex)
    {
        string[] candidateSkillIds = GetCandidateSkillIds(slotIndex);

        if (candidateSkillIds != null && candidateSkillIds.Length > 0)
            return candidateSkillIds[0];

        return "";
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
