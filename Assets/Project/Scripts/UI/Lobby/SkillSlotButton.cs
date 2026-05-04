using UnityEngine;

public class SkillSlotButton : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private GameObject skillListPanel;
    public void Execute()
    {
        if (CharacterSelectionState.Instance == null)
            return;

        CharacterSelectionState.Instance.SelectSkillSlot(slotIndex);

        Debug.Log($"[SkillSlot] Selected Slot: {slotIndex}");

        if (skillListPanel != null)
        {
            skillListPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SkillSlotButton] Skill List Panel not assigned.");
        }
    }
}