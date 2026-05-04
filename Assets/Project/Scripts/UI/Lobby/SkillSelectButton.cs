using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SkillSelectButton : MonoBehaviour
{
    [SerializeField] private string skillId;

    [Header("UI")]
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Image targetSlotImage;
    [SerializeField] private GameObject skillListPanel;

    public void Execute()
    {
        var state = CharacterSelectionState.Instance;

        if (state == null)
        {
            Debug.LogWarning("[SkillSelectButton] State missing");
            return;
        }

        string characterId = state.CurrentCharacterId;
        int slotIndex = state.CurrentSkillSlotIndex;

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[SkillSelectButton] No character selected");
            return;
        }

        if (slotIndex < 0)
        {
            Debug.LogWarning("[SkillSelectButton] No slot selected");
            return;
        }

        if (targetSlotImage == null)
        {
            Debug.LogWarning($"[SkillSelectButton] Invalid slot index: {slotIndex}");
            return;
        }

        var store = DataManager.Instance.CharacterRuntimeStore;

        if (!store.TryGet(characterId, out var character))
        {
            Debug.LogWarning("[SkillSelectButton] Character runtime not found");
            return;
        }

        if (character.EquippedSkillIds == null || character.EquippedSkillIds.Length <= slotIndex)
        {
            Debug.LogWarning("[SkillSelectButton] EquippedSkillIds is invalid");
            return;
        }

        character.EquippedSkillIds[slotIndex] = skillId;

        if (targetSlotImage != null)
        {
            targetSlotImage.sprite = skillIcon;
            targetSlotImage.enabled = skillIcon != null;
        }

        if (skillListPanel != null)
        {
            skillListPanel.SetActive(false);
        }

        Debug.Log($"[SkillEquip] {characterId} Slot {slotIndex} = {skillId}");
    }
}