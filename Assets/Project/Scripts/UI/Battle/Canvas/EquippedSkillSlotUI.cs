using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedSkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icon")]
    [SerializeField] private Image iconImage;

    [Header("Interaction")]
    [SerializeField] private Button button;
    [SerializeField] private bool hideEmptySlotIcon = true;

    private EquippedSkillPanelUI ownerPanel;
    private SkillMasterData skillData;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        if (ownerPanel != null)
            ownerPanel.HideSkillTooltip();
    }

    public void SetSkill(Sprite icon)
    {
        SetSkill(null, null, icon);
    }

    public void SetSkill(EquippedSkillPanelUI owner, SkillMasterData data, Sprite icon)
    {
        ownerPanel = owner;
        skillData = data;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null || !hideEmptySlotIcon;
            iconImage.raycastTarget = true;
        }

        if (button != null)
            button.interactable = skillData != null;
    }

    public void Clear()
    {
        ownerPanel = null;
        skillData = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = !hideEmptySlotIcon;
            iconImage.raycastTarget = true;
        }

        if (button != null)
            button.interactable = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerPanel == null || skillData == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        ownerPanel.ShowSkillTooltip(skillData, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ownerPanel == null)
            return;

        ownerPanel.HideSkillTooltip();
    }
}
