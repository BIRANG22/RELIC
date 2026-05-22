using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;
//로비 스킬 장착 스크립트
public class SkillSelectButton : MonoBehaviour
{
    [Header("Skill")]
    [SerializeField] private string skillId;

    [Header("UI")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image targetSlotImage;
    [SerializeField] private GameObject skillListPanel;

    [SerializeField] private SkillEquipSlotType targetSlotType;

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

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[SkillSelectButton] No character selected");
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
        
        if (cachedIcon == null)
        {
            if (!DB.TryGetIcon(skillId, out cachedIcon))
            {
                Debug.LogWarning($"[SkillSelectButton] Icon not found: {skillId}");
                return;
            }
        }

        var equipService = new SkillEquipService(DataManager.Instance.CharacterRuntimeStore);
        equipService.EquipSkill(characterId, targetSlotType, skillId);

        targetSlotImage.sprite = cachedIcon;
        targetSlotImage.enabled = true;

        if (skillListPanel != null)
            skillListPanel.SetActive(false);

    }
}