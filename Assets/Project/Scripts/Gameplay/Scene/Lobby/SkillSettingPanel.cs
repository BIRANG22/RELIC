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

    [Header("Select Panel Positions")]
    [SerializeField] private Vector2[] selectPanelPositions;

    private SkillSlotButton currentSelectedSlot;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private void Awake()
    {
        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            if (skillSlotButtons[i] != null)
                skillSlotButtons[i].Init(this, i);
        }

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                skillIconButtons[i].Init(this);
        }

        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(false);
    }

    public void OpenCharacterSetting(string characterId)
    {
        SaveCurrentSkillSetting();

        currentCharacterId = characterId;
        currentMasterData = null;
        currentRuntimeData = null;

        if (DataManager.Instance == null)
        {
            ClearSkillSlots();
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            ClearSkillSlots();
            return;
        }

        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            ClearSkillSlots();
            return;
        }

        LoadCurrentSkillSetting();

        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(false);
    }

    private void LoadCurrentSkillSetting()
    {
        if (currentRuntimeData == null)
            return;

        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            SkillMasterData skill = null;
            string skillId = GetRuntimeSkillId(i);

            if (!string.IsNullOrWhiteSpace(skillId))
                DataManager.Instance.SkillDatabase.TryGet(skillId, out skill);

            if (skill == null)
                skill = GetDefaultSkill(i);

            if (skillSlotButtons[i] != null)
                skillSlotButtons[i].SetSkill(skill);

            SetRuntimeSkillId(i, skill != null ? skill.SkillId : null);
        }

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    private SkillMasterData GetDefaultSkill(int slotIndex)
    {
        if (currentMasterData == null)
            return null;

        string skillId = null;

        switch (slotIndex)
        {
            case 0:
                skillId = currentMasterData.PassiveSkill1;
                break;

            case 1:
                skillId = currentMasterData.UniqueSkill1;
                break;

            case 2:
                skillId = currentMasterData.CharacterSkill1;
                break;

            case 3:
                skillId = currentMasterData.CommonSkill1;
                break;
        }

        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillDatabase.TryGet(skillId, out var skill))
            return skill;

        Debug.LogWarning($"[SkillSettingPanel] Default skill not found: {skillId}");
        return null;
    }

    private void SaveCurrentSkillSetting()
    {
        if (currentRuntimeData == null)
            return;

        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            if (skillSlotButtons[i] == null)
                continue;

            SkillMasterData skill = skillSlotButtons[i].EquippedSkill;
            SetRuntimeSkillId(i, skill != null ? skill.SkillId : null);
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(currentRuntimeData);
    }

    public void OpenSkillSelectPanel(SkillSlotButton slotButton)
    {
        Debug.Log("[SkillSettingPanel] OpenSkillSelectPanel");

        if (currentRuntimeData == null || currentMasterData == null)
        {
            Debug.LogWarning("[SkillSettingPanel] current data is null.");
            return;
        }

        if (slotButton == null)
            return;

        currentSelectedSlot = slotButton;

        List<SkillMasterData> candidates = GetSkillCandidates(slotButton.SlotIndex);
        Debug.Log($"[SkillSettingPanel] Candidates: {candidates.Count}");

        SkillMasterData equippedSkill = slotButton.EquippedSkill;

        List<SkillMasterData> changeableSkills = new();

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null)
                continue;

            if (equippedSkill != null && candidates[i].SkillId == equippedSkill.SkillId)
                continue;

            changeableSkills.Add(candidates[i]);
        }

        Debug.Log($"[SkillSettingPanel] ChangeableSkills: {changeableSkills.Count}");

        RefreshSkillIconButtons(changeableSkills);
        MoveSelectPanel(slotButton.SlotIndex);

        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(changeableSkills.Count > 0);
    }

    private List<SkillMasterData> GetSkillCandidates(int slotIndex)
    {
        List<SkillMasterData> result = new();

        if (DataManager.Instance == null)
            return result;

        if (currentMasterData == null)
            return result;

        string[] skillIds = GetCandidateSkillIds(slotIndex);

        for (int i = 0; i < skillIds.Length; i++)
        {
            string skillId = skillIds[i];

            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            if (DataManager.Instance.SkillDatabase.TryGet(skillId, out var skill))
                result.Add(skill);
            else
                Debug.LogWarning($"[SkillSettingPanel] Candidate skill not found: {skillId}");
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

    private bool IsValidSkillForSlot(SkillMasterData skill, int slotIndex)
    {
        if (skill == null)
            return false;

        if (slotIndex == 3)
            return skill.Category == Category.Unique;

        return skill.Category == Category.Ability;
    }

    private void RefreshSkillIconButtons(List<SkillMasterData> skills)
    {
        int characterLevel = currentRuntimeData != null ? currentRuntimeData.Level : 1;

        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] == null)
                continue;

            if (skills != null && i < skills.Count && skills[i] != null)
            {
                SkillMasterData skillData = skills[i];
                int requiredLevel = 1;
                bool locked = characterLevel < requiredLevel;

                skillIconButtons[i].SetSkillData(skillData, locked, requiredLevel);
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

    public void EquipSkillFromIcon(SkillMasterData skillData)
    {
        SelectSkill(skillData);
    }

    public void SelectSkill(SkillMasterData skillData)
    {
        if (currentSelectedSlot == null)
            return;

        if (skillData == null)
            return;

        currentSelectedSlot.SetSkill(skillData);

        SaveCurrentSkillSetting();

        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(false);

        currentSelectedSlot = null;
    }

    public void SaveBeforeBattle()
    {
        SaveCurrentSkillSetting();
    }

    private string GetRuntimeSkillId(int slotIndex)
    {
        if (currentRuntimeData == null)
            return null;

        if (currentRuntimeData.EquippedSkillIds == null)
            currentRuntimeData.EquippedSkillIds = new string[4];

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedSkillIds.Length)
            return null;

        return currentRuntimeData.EquippedSkillIds[slotIndex];
    }

    private void SetRuntimeSkillId(int slotIndex, string skillId)
    {
        if (currentRuntimeData == null)
            return;

        if (currentRuntimeData.EquippedSkillIds == null)
            currentRuntimeData.EquippedSkillIds = new string[4];

        if (slotIndex < 0 || slotIndex >= currentRuntimeData.EquippedSkillIds.Length)
            return;

        currentRuntimeData.EquippedSkillIds[slotIndex] = skillId;
    }

    private void ClearSkillSlots()
    {
        for (int i = 0; i < skillSlotButtons.Length; i++)
        {
            if (skillSlotButtons[i] != null)
                skillSlotButtons[i].SetSkill(null);
        }

        ClearSkillIconButtons();

        if (skillIconSelectPanel != null)
            skillIconSelectPanel.SetActive(false);
    }

    private void ClearSkillIconButtons()
    {
        for (int i = 0; i < skillIconButtons.Length; i++)
        {
            if (skillIconButtons[i] != null)
                skillIconButtons[i].SetSkillData(null, false, 0);
        }
    }
}