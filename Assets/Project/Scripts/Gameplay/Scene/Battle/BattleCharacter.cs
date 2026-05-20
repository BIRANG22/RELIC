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

        equippedSkills.Clear();

        foreach (string skillId in runtimeData.EquippedSkillIds)
        {
            SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

            if (skillData != null)
                equippedSkills.Add(skillData);
            else
                Debug.LogWarning($"[BattleCharacter] SkillData ¾øÀ½: {skillId}");
        }

        ApplySkillsToButtons();

        Debug.Log($"[BattleCharacter] Init: {CharacterId} / Skills:{equippedSkills.Count}");
    }

    private void ApplySkillsToButtons()
    {
        if (skillButtons == null)
            return;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null)
                continue;

            if (i < equippedSkills.Count)
            {
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].SetSkill(equippedSkills[i]);
            }
            else
            {
                skillButtons[i].ClearSkill();
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }
}