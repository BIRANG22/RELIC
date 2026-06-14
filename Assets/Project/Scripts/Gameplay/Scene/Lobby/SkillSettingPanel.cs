using Relic.Gameplay.Data;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class SkillSettingPanel : MonoBehaviour
{
    [Header("Skill Slots")]
    [SerializeField] private SkillSlotButton[] skillSlotButtons;

    [Header("Skill Select Panel")]
    [SerializeField] private GameObject skillIconSelectPanel;
    [SerializeField] private SkillIconButton[] skillIconButtons;

    [Header("Skill Select Button Count")]
    [SerializeField] private int maxVisibleSkillIconButtonCount = 2;
    [SerializeField] private bool autoBindSkillIconButtons = true;

    [Header("Select Panel")]
    [SerializeField] private bool moveSelectPanelToSlot = false;
    [SerializeField] private Vector2[] selectPanelPositions;

    [Header("Always Visible")]
    [SerializeField] private bool keepSkillSelectPanelVisible = true;

    [Header("Skill Info Area")]
    [SerializeField] private GameObject skillInfoArea;
    [SerializeField] private TMP_Text skillInfoTitleText;
    [SerializeField] private TMP_Text skillInfoEffectText;
    [SerializeField] private bool autoBindSkillInfoArea = true;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private SkillSlotButton currentSelectedSlot;

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

        SetSkillSelectPanelVisible(true);
        ClearSkillIconButtons();
        ClearSkillInfo();
    }

    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        BindSkillIconButtonsIfNeeded();
        BindSkillInfoAreaIfNeeded();
        InitSkillIconButtons();
        SetSkillSelectPanelVisible(true);
        ClearSkillInfo();
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

    private void InitSkillIconButtons()
    {
        if (skillIconButtons == null)
            return;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                skillIconButtons[i].Init(this);
        }
    }

    private void BindSkillIconButtonsIfNeeded()
    {
        if (!autoBindSkillIconButtons)
            return;

        if (!NeedsSkillIconButtonBinding())
            return;

        Transform searchRoot = null;

        if (skillIconSelectPanel != null)
            searchRoot = skillIconSelectPanel.transform;
        else
            searchRoot = transform.Find("SkillIconSelectPanel");

        if (searchRoot == null)
            return;

        Transform buttonRoot = searchRoot.Find("ButtonRoot");
        Transform root = buttonRoot != null ? buttonRoot : searchRoot;
        skillIconButtons = root.GetComponentsInChildren<SkillIconButton>(true);
    }

    private bool NeedsSkillIconButtonBinding()
    {
        if (skillIconButtons == null || skillIconButtons.Length == 0)
            return true;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                return false;
        }

        return true;
    }


    private void BindSkillInfoAreaIfNeeded()
    {
        if (!autoBindSkillInfoArea)
            return;

        if (skillInfoTitleText != null && skillInfoEffectText != null)
            return;

        Transform area = skillInfoArea != null ? skillInfoArea.transform : null;

        if (area == null)
            area = transform.Find("SkillInfoArea");

        if (area == null && transform.parent != null)
            area = transform.parent.Find("SkillInfoArea");

        if (area == null)
            area = FindChildByName(transform.root, "SkillInfoArea");

        if (area == null)
            return;

        skillInfoArea = area.gameObject;

        if (skillInfoTitleText == null)
        {
            Transform title = area.Find("TitleText");
            if (title != null)
                skillInfoTitleText = title.GetComponent<TMP_Text>();
        }

        if (skillInfoEffectText == null)
        {
            Transform effect = area.Find("EffectText");
            if (effect != null)
                skillInfoEffectText = effect.GetComponent<TMP_Text>();
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

    public void SetSkillSelectPanelVisible(bool visible)
    {
        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(visible || keepSkillSelectPanelVisible);
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
        currentSelectedSlot = null;

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
        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(true);
    }

    public void RefreshByCurrentLevel()
    {
        if (currentRuntimeData == null || currentMasterData == null)
            return;

        LoadCurrentSkillSetting();

        if (currentSelectedSlot != null)
            OpenSkillSelectPanel(currentSelectedSlot);
        else
            ClearSkillIconButtons();
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

        currentSelectedSlot = slotButton;
        ShowSkillInfo(slotButton.EquippedSkill);

        List<SkillMasterData> candidates = GetSkillCandidates(slotButton.SlotIndex);
        RefreshSkillIconButtons(candidates, slotButton.SlotIndex);

        if (moveSelectPanelToSlot)
            MoveSelectPanel(slotButton.SlotIndex);

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

        if (skillIconButtons == null)
            return;

        int visibleCount = Mathf.Clamp(maxVisibleSkillIconButtonCount, 1, skillIconButtons.Length);

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] == null)
                continue;

            bool canUseButton = i < visibleCount;

            if (canUseButton && skills != null && i < skills.Count && skills[i] != null)
            {
                SkillMasterData skill = skills[i];
                int requiredLevel = GetRequiredLevelForSkill(skill, slotIndex);
                bool locked = characterLevel < requiredLevel;

                skillIconButtons[i].SetSkillData(skill, locked, requiredLevel);
            }
            else
            {
                skillIconButtons[i].SetSkillData(null, false, 0);
            }
        }
    }

    private void MoveSelectPanel(int slotIndex)
    {
        if (skillIconSelectPanel == null)
            return;

        RectTransform rect = skillIconSelectPanel.GetComponent<RectTransform>();

        if (rect == null)
            return;

        if (selectPanelPositions != null &&
            slotIndex >= 0 &&
            slotIndex < selectPanelPositions.Length)
        {
            rect.anchoredPosition = selectPanelPositions[slotIndex];
        }
    }


    public void ShowSkillInfo(SkillMasterData skill)
    {
        BindSkillInfoAreaIfNeeded();
        ConfigureSkillInfoTextComponents();

        if (skillInfoArea != null)
            skillInfoArea.SetActive(true);

        SetPlainTmpText(skillInfoTitleText, skill != null ? skill.Name : "");
        SetRichTmpText(skillInfoEffectText, skill != null ? BuildSkillDetailsText(skill) : "");
    }

    public void ClearSkillInfo()
    {
        BindSkillInfoAreaIfNeeded();
        ConfigureSkillInfoTextComponents();

        SetPlainTmpText(skillInfoTitleText, "");
        SetRichTmpText(skillInfoEffectText, "");
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
        SetSkillSelectPanelVisible(true);
    }

    public void SaveBeforeBattle()
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
        currentSelectedSlot = null;
        SetSkillSelectPanelVisible(true);
    }

    private void ClearSkillIconButtons()
    {
        BindSkillIconButtonsIfNeeded();

        if (skillIconButtons == null)
            return;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                skillIconButtons[i].SetSkillData(null, false, 0);
        }
    }

    public void ClearForEmptyCharacter()
    {
        currentCharacterId = null;
        currentMasterData = null;
        currentRuntimeData = null;
        currentSelectedSlot = null;

        ClearSkillSlots();
        ClearSkillIconButtons();
        ClearSkillInfo();
        SetSkillSelectPanelVisible(true);
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
