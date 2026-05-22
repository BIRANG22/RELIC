using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleCharacter : MonoBehaviour
{
    public CharacterRuntimeData RuntimeData { get; private set; }

    private readonly List<SkillMasterData> equippedSkills = new();
    public IReadOnlyList<SkillMasterData> EquippedSkills => equippedSkills;

    [Header("Skill Buttons Only Skill1~Skill4, Do Not Include Move")]
    [SerializeField] private SkillSelectButtonUI[] skillButtons;

    public string CharacterId => RuntimeData != null ? RuntimeData.CharacterId : null;

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;

        ApplySkillsToButtons();

        Debug.Log($"[BattleCharacter] Init: {CharacterId}");
    }

    private void ApplySkillsToButtons()
    {
        if (skillButtons == null)
            return;

        SetButtonSkill(0, RuntimeData.AbilitySkillId1);
        SetButtonSkill(1, RuntimeData.AbilitySkillId2);
        SetButtonSkill(2, RuntimeData.AbilitySkillId3);
        SetButtonSkill(3, RuntimeData.UniqueSkillId);
    }

    private void SetButtonSkill(int index, string skillId)
    {
        if (skillButtons == null || index < 0 || index >= skillButtons.Length)
            return;

        if (skillButtons[index] == null)
            return;

        if (string.IsNullOrWhiteSpace(skillId))
        {
            skillButtons[index].ClearSkill();
            skillButtons[index].gameObject.SetActive(false);
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[BattleCharacter] SkillData ¾øÀ½: {skillId}");
            skillButtons[index].ClearSkill();
            skillButtons[index].gameObject.SetActive(false);
            return;
        }

        skillButtons[index].gameObject.SetActive(true);
        skillButtons[index].SetSkill(skillData);
    }
}