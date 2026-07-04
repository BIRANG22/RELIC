using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillInventoryIconUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Selection")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private float selectedScale = 1.08f;

    private string skillId;
    private SkillInventoryPanelUI owner;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private bool isSelected;

    public string SkillId => skillId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        CaptureBaseScaleOnce();
    }

    public void Setup(string skillId, SkillInventoryPanelUI owner)
    {
        this.skillId = skillId;
        this.owner = owner;
        isSelected = false;
        RefreshIcon();
        ApplyScale();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && !string.IsNullOrWhiteSpace(skillId);
        ApplyScale();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        owner?.SelectInventorySkillIcon(this);
        owner?.SelectSkill(skillId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        owner?.ShowSkillTooltip(skillId, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HideSkillTooltip();
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        iconImage.raycastTarget = true;

        Sprite icon = null;

        if (!string.IsNullOrWhiteSpace(skillId) &&
            DataManager.Instance != null &&
            DataManager.Instance.SkillIconDatabase != null)
        {
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out icon);
        }

        if (icon == null &&
            !string.IsNullOrWhiteSpace(skillId) &&
            DataManager.Instance != null &&
            DataManager.Instance.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData))
        {
            icon = skillData.Icon;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.color = SkillRarityUtility.GetSkillIconColor(skillId);
    }

    private void ApplyScale()
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();
        scaleTarget.localScale = isSelected ? baseScale * selectedScale : baseScale;
    }

    private void CaptureBaseScaleOnce()
    {
        if (hasCapturedBaseScale || scaleTarget == null)
            return;

        baseScale = scaleTarget.localScale;
        hasCapturedBaseScale = true;
    }
}
