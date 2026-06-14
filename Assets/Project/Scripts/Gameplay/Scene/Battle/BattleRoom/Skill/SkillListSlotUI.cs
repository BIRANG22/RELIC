using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillListSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Root")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Summary UI")]
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image skillRangeImage;
    [SerializeField] private Image skillCostImage;

    [Header("Color")]
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color hoverBackgroundColor = new Color(0.243f, 0.318f, 0.698f, 1f);
    [SerializeField] private Color selectedBackgroundColor = new Color(1f, 0.86f, 0.35f, 0.35f);
    [SerializeField] private Color usableTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color usableImageColor = Color.white;
    [SerializeField] private Color emptyImageColor = new Color(1f, 1f, 1f, 0.15f);

    [Header("Hover Breath Effect")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private bool useHoverBreathEffect = true;
    [SerializeField] private float hoverBaseScale = 1.08f;
    [SerializeField] private float breathAmount = 0.04f;
    [SerializeField] private float breathSpeed = 4f;
    [SerializeField] private float scaleLerpSpeed = 14f;

    [Header("Selected Effect")]
    [SerializeField] private bool useSelectedScale = true;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private bool boostSortingOnHoverOrSelected = true;
    [SerializeField] private int sortingOrderBoost = 2000;

    [Header("Sound")]
    [SerializeField] private bool playClickSfx = true;
    [SerializeField] private SfxType clickSfxType = SfxType.NormalButtonClick;

    private SkillListPanel owner;
    private string skillId;
    private SkillMasterData skillData;
    private bool isPointerOver;
    private bool isSelected;
    private RectTransform rectTransform;
    private string detailText = "";
    private bool canClick;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private Canvas sortingCanvas;
    private bool hadSortingCanvas;
    private bool originalOverrideSorting;
    private int originalSortingOrder;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        CaptureSortingCanvas();
        ApplyVisualState();
        ApplyScale(true);
        ApplySortingState();
    }

    private void Update()
    {
        ApplyScale(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        ResetScale();
        ApplySortingState();
    }

    public void Setup(SkillListPanel ownerPanel, string skillId, bool interactable)
    {
        owner = ownerPanel;
        this.skillId = skillId;
        canClick = interactable;
        skillData = null;
        isPointerOver = false;
        isSelected = false;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();

        CaptureSortingCanvas();

        if (string.IsNullOrWhiteSpace(skillId))
        {
            SetEmpty();
            return;
        }

        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            !DataManager.Instance.SkillDatabase.TryGet(skillId, out skillData))
        {
            SetEmpty();
            return;
        }

        if (skillIconImage != null)
        {
            skillIconImage.sprite = GetSkillIcon(skillId);
            skillIconImage.enabled = skillIconImage.sprite != null;
        }

        if (skillRangeImage != null)
        {
            skillRangeImage.sprite = GetSkillRangeIcon(skillData.RangeId);
            skillRangeImage.enabled = skillRangeImage.sprite != null;
        }

        if (skillCostImage != null)
            skillCostImage.enabled = true;

        if (skillNameText != null)
            skillNameText.text = skillData.Name;

        detailText = BuildDetailText(skillData);

        if (button != null)
            button.interactable = interactable && skillData != null;

        ApplyVisualState();
        ApplyScale(true);
        ApplySortingState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && skillData != null;
        ApplyVisualState();
        ApplyScale(false);
        ApplySortingState();
    }

    private void SetEmpty()
    {
        skillId = "";
        skillData = null;
        detailText = "";
        isPointerOver = false;
        isSelected = false;

        if (skillIconImage != null)
        {
            skillIconImage.sprite = null;
            skillIconImage.enabled = false;
        }

        if (skillRangeImage != null)
        {
            skillRangeImage.sprite = null;
            skillRangeImage.enabled = false;
        }

        if (skillCostImage != null)
            skillCostImage.enabled = false;

        if (skillNameText != null)
            skillNameText.text = "스킬 없음";

        if (button != null)
            button.interactable = false;

        ApplyVisualState();
        ApplyScale(true);
        ApplySortingState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillData == null)
            return;

        isPointerOver = true;
        ApplyVisualState();
        ApplySortingState();

        if (owner != null)
            owner.ShowSkillDetail(detailText, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyVisualState();
        ApplySortingState();

        if (owner != null)
            owner.HideSkillDetail();
    }

    private void OnClick()
    {
        if (!canClick)
            return;

        if (skillData == null || owner == null)
            return;

        PlayClickSfx();
        owner.SelectSkillSlot(this);
        owner.SelectSkill(skillId);
        owner.ShowSkillDetail(detailText, rectTransform);
    }

    private void ApplyVisualState()
    {
        bool hasSkill = skillData != null;

        if (backgroundImage != null)
        {
            if (isSelected && hasSkill)
                backgroundImage.color = selectedBackgroundColor;
            else if (isPointerOver && hasSkill)
                backgroundImage.color = hoverBackgroundColor;
            else
                backgroundImage.color = normalBackgroundColor;
        }

        Color textColor = hasSkill ? usableTextColor : disabledTextColor;
        Color imageColor = hasSkill ? usableImageColor : emptyImageColor;

        if (skillNameText != null)
            skillNameText.color = textColor;

        ApplyImageColor(skillIconImage, imageColor);
        ApplyImageColor(skillRangeImage, imageColor);
        ApplyImageColor(skillCostImage, imageColor);
    }

    private void ApplyImageColor(Image targetImage, Color color)
    {
        if (targetImage == null || !targetImage.enabled)
            return;

        targetImage.color = color;
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();

        float scaleMultiplier = 1f;

        if (useSelectedScale && isSelected && skillData != null)
        {
            scaleMultiplier = selectedScale;
        }
        else if (useHoverBreathEffect && isPointerOver && skillData != null)
        {
            scaleMultiplier = hoverBaseScale + Mathf.Sin(Time.unscaledTime * breathSpeed) * breathAmount;
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

    private void ResetScale()
    {
        CaptureBaseScaleOnce();

        if (scaleTarget != null)
            scaleTarget.localScale = baseScale;
    }

    private void CaptureBaseScaleOnce()
    {
        if (hasCapturedBaseScale || scaleTarget == null)
            return;

        baseScale = scaleTarget.localScale;
        hasCapturedBaseScale = true;
    }

    private void CaptureSortingCanvas()
    {
        if (!boostSortingOnHoverOrSelected || scaleTarget == null)
            return;

        if (sortingCanvas != null)
            return;

        sortingCanvas = scaleTarget.GetComponent<Canvas>();
        hadSortingCanvas = sortingCanvas != null;

        if (sortingCanvas == null)
            sortingCanvas = scaleTarget.gameObject.AddComponent<Canvas>();

        if (sortingCanvas.GetComponent<GraphicRaycaster>() == null)
            sortingCanvas.gameObject.AddComponent<GraphicRaycaster>();

        originalOverrideSorting = sortingCanvas.overrideSorting;
        originalSortingOrder = sortingCanvas.sortingOrder;
    }

    private void ApplySortingState()
    {
        if (!boostSortingOnHoverOrSelected || sortingCanvas == null)
            return;

        bool shouldBoost = (isPointerOver || isSelected) && skillData != null;

        if (shouldBoost)
        {
            sortingCanvas.overrideSorting = true;
            sortingCanvas.sortingOrder = sortingOrderBoost;
        }
        else
        {
            sortingCanvas.overrideSorting = hadSortingCanvas && originalOverrideSorting;
            sortingCanvas.sortingOrder = hadSortingCanvas ? originalSortingOrder : 0;
        }
    }

    private void PlayClickSfx()
    {
        if (!playClickSfx || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfxType);
    }

    private Sprite GetSkillIcon(string skillId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite icon))
            return icon;

        return null;
    }

    private Sprite GetSkillRangeIcon(string rangeId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon))
            return icon;

        return null;
    }

    private string BuildDetailText(SkillMasterData data)
    {
        if (data == null)
            return "";

        string skillName = string.IsNullOrWhiteSpace(data.Name) ? data.SkillId : data.Name;
        string description = !string.IsNullOrWhiteSpace(data.ToolTip) ? data.ToolTip : data.Details;

        if (string.IsNullOrWhiteSpace(description))
            return skillName;

        return skillName + "\n" + description;
    }
}
