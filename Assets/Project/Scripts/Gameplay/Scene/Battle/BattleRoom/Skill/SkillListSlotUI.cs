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
    [SerializeField] private Color usableTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color usableImageColor = Color.white;
    [SerializeField] private Color emptyImageColor = new Color(1f, 1f, 1f, 0.15f);

    private SkillListPanel owner;
    private string skillId;
    private SkillMasterData skillData;
    private bool isPointerOver;
    private RectTransform rectTransform;
    private string detailText = "";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        ApplyVisualState();
    }

    public void Setup(SkillListPanel ownerPanel, string skillId)
    {
        owner = ownerPanel;
        this.skillId = skillId;
        skillData = null;
        isPointerOver = false;

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

        if (skillNameText != null)
            skillNameText.text = skillData.Name;

        detailText = BuildDetailText(skillData);

        if (button != null)
            button.interactable = true;

        ApplyVisualState();
    }

    private void SetEmpty()
    {
        skillId = "";
        skillData = null;
        detailText = "";

        if (skillIconImage != null)
        {
            skillIconImage.sprite = null;
            skillIconImage.enabled = false;
        }

        if (skillRangeImage != null)
            skillRangeImage.enabled = false;

        if (skillCostImage != null)
            skillCostImage.enabled = false;

        if (skillNameText != null)
            skillNameText.text = "스킬 없음";

        if (button != null)
            button.interactable = false;

        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillData == null)
            return;

        isPointerOver = true;
        ApplyVisualState();

        if (owner != null)
            owner.ShowSkillDetail(detailText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyVisualState();

        if (owner != null)
            owner.HideSkillDetail();
    }

    private void OnClick()
    {
        if (skillData == null || owner == null)
            return;

        owner.SelectSkill(skillId);
        owner.ShowSkillDetail(detailText);
    }

    private void ApplyVisualState()
    {
        bool hasSkill = skillData != null;

        if (backgroundImage != null)
            backgroundImage.color = isPointerOver && hasSkill
                ? hoverBackgroundColor
                : normalBackgroundColor;

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

    private string BuildDetailText(SkillMasterData data)
    {
        if (data == null)
            return "";

        string text = data.Name;

        text += $"\n분류: {data.Category}";
        text += $"\n대상: {data.Target}";
        text += $"\n타입: {data.SkillType}";

        if (!string.IsNullOrWhiteSpace(data.EffectIds))
            text += $"\n효과: {data.EffectIds}";

        if (!string.IsNullOrWhiteSpace(data.ValueRate))
            text += $"\n수치: {data.ValueRate}";

        if (!string.IsNullOrWhiteSpace(data.CountRate))
            text += $"\n횟수: {data.CountRate}";

        if (data.ResourceCostType != ResourceCostType.None)
            text += $"\n소모: {data.ReferenceResource} {data.ResourceCostType} {data.ResourceCostValue}";

        if (data.GridMove != 0)
            text += $"\n이동: {data.GridMove}";

        if (data.RangeType != RangeType.None)
            text += $"\n범위: {data.RangeType}";

        if (!string.IsNullOrWhiteSpace(data.RangeId))
            text += $"\nRangeId: {data.RangeId}";

        if (data.TimelineNotation != TimelineActionType.None)
            text += $"\n타임라인: {data.TimelineNotation}";

        return text;
    }
}