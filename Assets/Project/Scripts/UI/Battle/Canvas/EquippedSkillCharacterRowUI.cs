using UnityEngine;
using Relic.Gameplay.Data;

public class EquippedSkillCharacterRowUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private EquippedSkillSlotUI abilitySkillSlot1;
    [SerializeField] private EquippedSkillSlotUI abilitySkillSlot2;
    [SerializeField] private EquippedSkillSlotUI abilitySkillSlot3;

    public void Setup(CharacterRuntimeData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        SetSlot(abilitySkillSlot1, characterData.AbilitySkillId1);
        SetSlot(abilitySkillSlot2, characterData.AbilitySkillId2);
        SetSlot(abilitySkillSlot3, characterData.AbilitySkillId3);
    }

    public void Clear()
    {
        if (abilitySkillSlot1 != null)
            abilitySkillSlot1.Clear();

        if (abilitySkillSlot2 != null)
            abilitySkillSlot2.Clear();

        if (abilitySkillSlot3 != null)
            abilitySkillSlot3.Clear();
    }

    private void SetSlot(EquippedSkillSlotUI slot, string skillId)
    {
        if (slot == null)
            return;

        if (string.IsNullOrWhiteSpace(skillId))
        {
            slot.Clear();
            return;
        }

        SkillIconDatabase iconDatabase = DataManager.Instance.SkillIconDatabase;

        if (iconDatabase != null &&
            iconDatabase.TryGetIcon(skillId, out Sprite icon))
        {
            slot.SetSkill(icon);
        }
        else
        {
            Debug.LogWarning($"[EquippedSkillCharacterRowUI] SkillIcon ¾øÀ½: {skillId}");
            slot.Clear();
        }
    }
}