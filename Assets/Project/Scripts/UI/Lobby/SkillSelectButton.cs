using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SkillSelectButton : MonoBehaviour
{
    [Header("Skill")]
    [SerializeField] private string skillId;

    [Header("UI")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image targetSlotImage;
    [SerializeField] private GameObject skillListPanel;

    private Sprite cachedIcon;

    private SkillIconDatabase DB => DataManager.Instance.SkillIconDatabase;

    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        RefreshButtonIcon();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        RefreshButtonIcon();
    }
#endif

    private void RefreshButtonIcon()
    {
        if (!Application.isPlaying)
            return;

        if (DataManager.Instance == null)
            return;

        var db = DataManager.Instance.SkillIconDatabase;

        if (db == null || string.IsNullOrWhiteSpace(skillId))
            return;

        if (!db.TryGetIcon(skillId, out var icon))
        {
            Debug.LogWarning($"[SkillSelectButton] Icon not found: {skillId}");
            return;
        }

        cachedIcon = icon;

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.sprite = icon;
            buttonImage.enabled = true;
        }
    }

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
            Debug.LogWarning("[SkillSelectButton] Target slot image missing");
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

        if (cachedIcon == null)
        {
            if (!DB.TryGetIcon(skillId, out cachedIcon))
            {
                Debug.LogWarning($"[SkillSelectButton] Icon not found: {skillId}");
                return;
            }
        }

        character.EquippedSkillIds[slotIndex] = skillId;

        targetSlotImage.sprite = cachedIcon;
        targetSlotImage.enabled = true;

        if (skillListPanel != null)
            skillListPanel.SetActive(false);

        Debug.Log($"[SkillEquip] {characterId} Slot {slotIndex} = {skillId}");
    }
}