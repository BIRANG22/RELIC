using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCharacter : MonoBehaviour
{
    public CharacterRuntimeData RuntimeData { get; private set; }

    private readonly List<SkillMasterData> equippedSkills = new();
    public IReadOnlyList<SkillMasterData> EquippedSkills => equippedSkills;

    public string CharacterId => RuntimeData != null ? RuntimeData.CharacterId : null;

    public int CurrentGridIndex { get; private set; } = -1;

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;
        LoadEquippedSkills();
    }

    public void SetGridIndex(int gridIndex)
    {
        CurrentGridIndex = gridIndex;
    }

    private void OnMouseDown()
    {
        if (RuntimeData == null)
            return;

        if (IsPointerOverUI())
            return;

        BattleRoomLoader roomLoader = FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        if (roomLoader == null)
        {
            Debug.LogWarning("[BattleCharacter] BattleRoomLoader가 없습니다.");
            return;
        }

        roomLoader.OnPlayerCharacterClicked(RuntimeData);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return false;
    }

    private void LoadEquippedSkills()
    {
        equippedSkills.Clear();

        if (RuntimeData == null)
            return;

        AddSkill(RuntimeData.MoveSkillId);

        if (RuntimeData.EquippedSkillIds == null ||
            RuntimeData.EquippedSkillIds.Length != 4)
        {
            RuntimeData.EquippedSkillIds = new string[4]
            {
                RuntimeData.UniqueSkillId,
                RuntimeData.AbilitySkillId,
                "",
                ""
            };
        }

        for (int i = 0; i < RuntimeData.EquippedSkillIds.Length; i++)
        {
            AddSkill(RuntimeData.EquippedSkillIds[i]);
        }
    }

    private void AddSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (DataManager.Instance == null)
            return;

        for (int i = 0; i < equippedSkills.Count; i++)
        {
            if (equippedSkills[i] != null && equippedSkills[i].SkillId == skillId)
                return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[BattleCharacter] SkillData 없음: {skillId}");
            return;
        }

        equippedSkills.Add(skillData);
    }
}
