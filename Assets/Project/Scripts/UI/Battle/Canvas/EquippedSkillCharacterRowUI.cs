using UnityEngine;
using Relic.Gameplay.Data;

public class EquippedSkillCharacterRowUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private EquippedSkillSlotUI slot1;
    [SerializeField] private EquippedSkillSlotUI slot2;
    [SerializeField] private EquippedSkillSlotUI slot3;
    [SerializeField] private EquippedSkillSlotUI slot4;

    public void Setup(CharacterRuntimeData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        SetSlot(slot1, characterData.UniqueSkillId);
        SetSlot(slot2, characterData.AbilitySkillId);

        SetSlot(
            slot3,
            characterData.EquippedSkillIds != null &&
            characterData.EquippedSkillIds.Length > 2
                ? characterData.EquippedSkillIds[2]
                : null);

        SetSlot(
            slot4,
            characterData.EquippedSkillIds != null &&
            characterData.EquippedSkillIds.Length > 3
                ? characterData.EquippedSkillIds[3]
                : null);
    }

    public void Clear()
    {
        if (slot1 != null) slot1.Clear();
        if (slot2 != null) slot2.Clear();
        if (slot3 != null) slot3.Clear();
        if (slot4 != null) slot4.Clear();
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
            Debug.LogWarning(
                $"[EquippedSkillCharacterRowUI] SkillIcon ¾øÀ½: {skillId}");

            slot.Clear();
        }
    }
}