using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class SkillSettingPanel : MonoBehaviour
{
    [Header("Skill Slots")]
    [SerializeField] private SkillSlotButton[] skillSlotButtons;

    [Header("Skill Select Panel")]
    [SerializeField] private GameObject skillIconSelectPanel;
    [SerializeField] private SkillIconButton[] skillIconButtons;

    [Header("Select Panel")]
    [SerializeField] private bool moveSelectPanelToSlot = false;
    [SerializeField] private Vector2[] selectPanelPositions;

    [Header("Always Visible")]
    [SerializeField] private bool keepSkillSelectPanelVisible = true;

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

        InitSkillSlotButtons();
        InitSkillIconButtons();

        SetSkillSelectPanelVisible(true);
        ClearSkillIconButtons();
    }

    private void OnEnable()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        SetSkillSelectPanelVisible(true);
    }

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

        switch (slotIndex)
        {
            case 0:
                return skill.Category == Category.Passive;

            case 1:
                return skill.Category == Category.Unique;

            case 2:
                return skill.Category == Category.Ability;

            case 3:
                return skill.Category == Category.Public;

            default:
                return false;
        }
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
        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        if (skillIconButtons == null)
            return;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] == null)
                continue;

            if (skills != null && i < skills.Count && skills[i] != null)
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

        if (string.IsNullOrWhiteSpace(currentRuntimeData.EquippedSkillIds[2]) &&
        currentMasterData != null)
        {
            currentRuntimeData.EquippedSkillIds[2] = currentMasterData.CommonSkill1;
        }

        if (currentMasterData != null)
        {
            string freeSkillId = currentRuntimeData.EquippedSkillIds[2];

            if (string.IsNullOrWhiteSpace(freeSkillId) ||
                !IsPublicSkill(freeSkillId))
            {
                currentRuntimeData.EquippedSkillIds[2] = currentMasterData.CommonSkill1;
            }
        }
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
        currentSelectedSlot = null;
        SetSkillSelectPanelVisible(true);
    }

    private void ClearSkillIconButtons()
    {
        if (skillIconButtons == null)
            return;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                skillIconButtons[i].SetSkillData(null, false, 0);
        }
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