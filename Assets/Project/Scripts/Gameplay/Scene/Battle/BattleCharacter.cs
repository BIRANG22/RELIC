using UnityEngine;
using Relic.Gameplay.Data;

public class BattleCharacter : MonoBehaviour
{
    public string CharacterId { get; private set; }
    public CharacterRuntimeData RuntimeData { get; private set; }

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;
        CharacterId = runtimeData.CharacterId;

        Debug.Log($"[BattleCharacter] Init: {CharacterId}");

        SetupSkills(runtimeData.EquippedSkillIds);
    }

    private void SetupSkills(string[] skillIds)
    {
        if (skillIds == null)
            return;

        foreach (string skillId in skillIds)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            Debug.Log($"[BattleCharacter] Equipped Skill: {skillId}");
        }
    }
}