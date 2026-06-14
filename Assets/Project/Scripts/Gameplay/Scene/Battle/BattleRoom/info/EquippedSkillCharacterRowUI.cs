using UnityEngine;
using Relic.Gameplay.Data;

public class EquippedSkillCharacterRowUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private EquippedSkillSlotUI passiveSlot;   // 1
    [SerializeField] private EquippedSkillSlotUI uniqueSlot;    // 2
    [SerializeField] private EquippedSkillSlotUI abilitySlot;   // 3
    [SerializeField] private EquippedSkillSlotUI freeSlot1;     // 4 Common/Core
    [SerializeField] private EquippedSkillSlotUI freeSlot2;     // 5 Common/Core

    public void Setup(CharacterRuntimeData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        SetSlot(passiveSlot, characterData.PassiveSkillId);
        SetSlot(uniqueSlot, characterData.UniqueSkillId);
        SetSlot(abilitySlot, characterData.AbilitySkillId);

        SetSlot(
            freeSlot1,
            characterData.EquippedSkillIds != null &&
            characterData.EquippedSkillIds.Length > 2
                ? characterData.EquippedSkillIds[2]
                : null);

        SetSlot(
            freeSlot2,
            characterData.EquippedSkillIds != null &&
            characterData.EquippedSkillIds.Length > 3
                ? characterData.EquippedSkillIds[3]
                : null);
    }

    public void Clear()
    {
        if (passiveSlot != null) passiveSlot.Clear();
        if (uniqueSlot != null) uniqueSlot.Clear();
        if (abilitySlot != null) abilitySlot.Clear();
        if (freeSlot1 != null) freeSlot1.Clear();
        if (freeSlot2 != null) freeSlot2.Clear();
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

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[EquippedSkillCharacterRowUI] DataManager 없음");
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
            Debug.LogWarning($"[EquippedSkillCharacterRowUI] SkillIcon 없음: {skillId}");
            slot.Clear();
        }
    }
}