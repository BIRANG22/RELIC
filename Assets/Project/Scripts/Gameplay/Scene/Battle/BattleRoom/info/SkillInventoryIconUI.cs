using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillInventoryIconUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;


    [Header("Scale Effect")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private float hoverMaxScale = 1.1f;
    [SerializeField] private float hoverBreathSpeed = 2f;
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float scaleLerpSpeed = 14f;

    private string skillId;
    private SkillInventoryPanelUI owner;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private bool isSelected;
    private bool isPointerOver;

    public string SkillId => skillId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        CaptureBaseScaleOnce();
        ApplyScale(true);
    }

    private void Update()
    {
        ApplyScale(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        ApplyScale(true);
        owner?.HideSkillTooltip();
    }

    public void Setup(string skillId, SkillInventoryPanelUI owner)
    {
        this.skillId = skillId;
        this.owner = owner;
        isSelected = false;
        isPointerOver = false;
        RefreshIcon();
        ApplyScale(true);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && !string.IsNullOrWhiteSpace(skillId);
        ApplyScale(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (string.IsNullOrWhiteSpace(skillId))
            return;

        owner?.SelectInventorySkillIcon(this);
        owner?.SelectSkill(skillId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (string.IsNullOrWhiteSpace(skillId))
            return;

        isPointerOver = true;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        owner?.ShowSkillTooltip(skillId, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
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
        iconImage.color = Color.white;
        SkillUpgradeMarkStyle.ApplyShared(iconImage, skillId);
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();

        float scaleMultiplier = 1f;

        if (isSelected && !string.IsNullOrWhiteSpace(skillId))
        {
            scaleMultiplier = selectedScale;
        }
        else if (isPointerOver && !string.IsNullOrWhiteSpace(skillId))
        {
            // 1.0과 1.1 사이를 반복합니다.
            float breathT = (Mathf.Sin(Time.unscaledTime * hoverBreathSpeed) + 1f) * 0.5f;
            scaleMultiplier = Mathf.Lerp(1f, hoverMaxScale, breathT);
        }

        Vector3 targetScale = baseScale * scaleMultiplier;

        if (instant)
        {
            scaleTarget.localScale = targetScale;
            return;
        }

        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
    }

    private void CaptureBaseScaleOnce()
    {
        if (hasCapturedBaseScale || scaleTarget == null)
            return;

        baseScale = scaleTarget.localScale;
        hasCapturedBaseScale = true;
    }
}
