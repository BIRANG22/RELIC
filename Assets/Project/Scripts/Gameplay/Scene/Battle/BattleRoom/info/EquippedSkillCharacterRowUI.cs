using UnityEngine;
using Relic.Gameplay.Data;

public class EquippedSkillCharacterRowUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private EquippedSkillSlotUI passiveSlot;
    [SerializeField] private EquippedSkillSlotUI uniqueSlot;
    [SerializeField] private EquippedSkillSlotUI abilitySlot;
    [SerializeField] private EquippedSkillSlotUI freeSlot1;
    [SerializeField] private EquippedSkillSlotUI freeSlot2;

    private EquippedSkillPanelUI ownerPanel;

    public void Setup(CharacterRuntimeData characterData)
    {
        Setup(ownerPanel, characterData);
    }

    public void Setup(EquippedSkillPanelUI owner, CharacterRuntimeData characterData)
    {
        ownerPanel = owner;

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
            Debug.LogWarning("[EquippedSkillCharacterRowUI] DataManager가 없습니다.");
            slot.Clear();
            return;
        }

        SkillDatabase skillDatabase = DataManager.Instance.SkillDatabase;
        if (skillDatabase == null || !skillDatabase.TryGet(skillId, out SkillMasterData skillData))
        {
            Debug.LogWarning($"[EquippedSkillCharacterRowUI] SkillData가 없습니다: {skillId}");
            slot.Clear();
            return;
        }

        Sprite icon = null;
        SkillIconDatabase iconDatabase = DataManager.Instance.SkillIconDatabase;

        if (iconDatabase != null)
            iconDatabase.TryGetIcon(skillId, out icon);

        slot.SetSkill(ownerPanel, skillData, icon);
    }
}
