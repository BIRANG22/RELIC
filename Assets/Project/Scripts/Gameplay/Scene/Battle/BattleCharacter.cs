using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleCharacter : MonoBehaviour
{
    public CharacterRuntimeData RuntimeData { get; private set; }

    private readonly List<SkillMasterData> equippedSkills = new();
    public IReadOnlyList<SkillMasterData> EquippedSkills => equippedSkills;

    public string CharacterId => RuntimeData != null ? RuntimeData.CharacterId : null;

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;

        LoadEquippedSkills();
    }

    private void LoadEquippedSkills()
    {
        equippedSkills.Clear();

        if (RuntimeData == null)
            return;

        AddSkill(RuntimeData.AbilitySkillId1);
        AddSkill(RuntimeData.AbilitySkillId2);
        AddSkill(RuntimeData.AbilitySkillId3);
        AddSkill(RuntimeData.UniqueSkillId);
    }

    private void AddSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (DataManager.Instance == null)
            return;

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[BattleCharacter] SkillData ¾øÀ½: {skillId}");
            return;
        }

        equippedSkills.Add(skillData);
    }
}