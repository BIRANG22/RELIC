using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillListSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
{
    [Header("Root")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Summary UI")]
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image skillRangeImage;
    [SerializeField] private TMP_Text skillCostTypeText;
    [SerializeField] private TMP_Text skillCostValueText;

    [Header("Legacy UI")]
    [SerializeField] private Image skillCostImage;

    [Header("Auto Bind Names")]
    [SerializeField] private string skillIconObjectName = "SkillIcon";
    [SerializeField] private string skillNameObjectName = "SkillName";
    [SerializeField] private string skillRangeObjectName = "RangeIcon";
    [SerializeField] private string skillCostTypeObjectName = "CostType";
    [SerializeField] private string skillCostValueObjectName = "CostValue";

    [Header("Color")]
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color hoverBackgroundColor = new Color(0.243f, 0.318f, 0.698f, 1f);
    [SerializeField] private Color selectedBackgroundColor = new Color(1f, 0.86f, 0.35f, 0.35f);
    [SerializeField] private Color usableTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color usableImageColor = Color.white;
    [SerializeField] private Color emptyImageColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color insufficientResourceColor = new Color32(0x55, 0x55, 0x55, 0xFF);

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
    private CharacterRuntimeData runtimeData;
    private bool isPointerOver;
    private bool isSelected;
    private RectTransform rectTransform;
    private string detailText = "";
    private bool canClick;
    private int displayedCostValue;
    private int lastSelectFrame = -1;
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
        BindMissingReferences();

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
        // 예약 비용과 고유 자원 예약량은 슬롯 생성 이후에도 계속 변할 수 있으므로
        // 매 프레임 현재 사용 가능 여부를 다시 반영합니다.
        ApplyVisualState();
        ApplyScale(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        ResetScale();
        ApplySortingState();
    }

    public void Setup(
        SkillListPanel ownerPanel,
        string skillId,
        bool interactable,
        int displayedCostValue = -1,
        CharacterRuntimeData runtimeData = null)
    {
        owner = ownerPanel;
        this.skillId = skillId;
        this.runtimeData = runtimeData;
        canClick = interactable;
        skillData = null;
        isPointerOver = false;
        isSelected = false;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();
        BindMissingReferences();
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

        int payAmount = displayedCostValue >= 0
            ? displayedCostValue
            : skillData.ResourceCostValue;

        this.displayedCostValue = Mathf.Max(0, payAmount);

        ApplySkillMasterData(skillData, displayedCostValue);

        detailText = BuildDetailText(skillData, payAmount);

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

    public bool CanSelectByKeyboard => canClick && skillData != null;
    public string SkillId => skillId;
    public SkillMasterData SkillData => skillData;
    public string DetailText => detailText;
    public RectTransform SlotRectTransform => rectTransform;

    public void PlayKeyboardSelectSfx()
    {
        PlayClickSfx();
    }

    private void ApplySkillMasterData(SkillMasterData data, int displayedCostValue = -1)
    {
        if (data == null)
        {
            SetEmpty();
            return;
        }

        if (skillIconImage != null)
        {
            skillIconImage.sprite = GetSkillIcon(data);
            skillIconImage.enabled = skillIconImage.sprite != null;
        }

        if (skillRangeImage != null)
        {
            skillRangeImage.sprite = GetSkillRangeIcon(data.RangeId);
            skillRangeImage.enabled = skillRangeImage.sprite != null;
        }

        if (skillCostImage != null)
            skillCostImage.enabled = false;

        if (skillNameText != null)
            skillNameText.text = string.IsNullOrWhiteSpace(data.Name)
                ? data.SkillId
                : GameDataLocalization.SkillName(data);

        if (skillCostTypeText != null)
            skillCostTypeText.text = GetReferenceResourceDisplayName(data.ReferenceResource);

        SetDisplayedCostValue(displayedCostValue >= 0
            ? displayedCostValue
            : data.ResourceCostValue);
    }

    public void SetDisplayedCostValue(int costValue)
    {
        BindMissingReferences();

        displayedCostValue = Mathf.Max(0, costValue);

        if (skillCostValueText != null)
            skillCostValueText.text = displayedCostValue.ToString();

        ApplyVisualState();
    }

    private void SetEmpty()
    {
        skillId = "";
        skillData = null;
        runtimeData = null;
        detailText = "";
        displayedCostValue = 0;
        isPointerOver = false;
        isSelected = false;

        BindMissingReferences();

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
            skillNameText.text = GameLocalization.Get("battle.no_skill", "스킬 없음");

        if (skillCostTypeText != null)
            skillCostTypeText.text = "";

        if (skillCostValueText != null)
            skillCostValueText.text = "";

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
        {
            owner.ShowSkillDetail(detailText, rectTransform);
            owner.ShowSkillHoverRangePreview(skillData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyVisualState();
        ApplySortingState();

        if (owner != null)
        {
            owner.HideSkillDetail();
            owner.ClearSkillHoverRangePreview();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null)
            owner.IgnoreOutsideCloseForFrames(2);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        SelectThisSlot();
    }

    private void OnClick()
    {
        SelectThisSlot();
    }

    private void SelectThisSlot()
    {
        if (lastSelectFrame == Time.frameCount)
            return;

        lastSelectFrame = Time.frameCount;

        if (!canClick)
            return;

        if (skillData == null || owner == null)
            return;

        if (owner != null)
            owner.IgnoreOutsideCloseForFrames(2);

        PlayClickSfx();
        owner.SelectSkillSlot(this);
        owner.SelectSkill(skillId);
        owner.ShowSkillDetail(detailText, rectTransform);
        owner.ShowSkillHoverRangePreview(skillData);
    }

    private void ApplyVisualState()
    {
        bool hasSkill = skillData != null;
        bool isResourceInsufficient = hasSkill && !CanAffordDisplayedCost();

        if (backgroundImage != null)
        {
            if (isResourceInsufficient)
                backgroundImage.color = insufficientResourceColor;
            else if (isSelected && hasSkill)
                backgroundImage.color = selectedBackgroundColor;
            else if (isPointerOver && hasSkill)
                backgroundImage.color = hoverBackgroundColor;
            else
                backgroundImage.color = normalBackgroundColor;
        }

        Color textColor = hasSkill ? usableTextColor : disabledTextColor;
        Color imageColor = hasSkill ? usableImageColor : emptyImageColor;

        if (skillNameText != null)
            skillNameText.color = isResourceInsufficient ? insufficientResourceColor : textColor;

        if (skillCostTypeText != null)
            skillCostTypeText.color = textColor;

        if (skillCostValueText != null)
            skillCostValueText.color = textColor;

        Color skillIconColor = isResourceInsufficient
            ? insufficientResourceColor
            : hasSkill
                ? SkillRarityUtility.GetSkillIconColor(skillData.SkillId, usableImageColor)
                : emptyImageColor;

        ApplyImageColor(skillIconImage, skillIconColor);
        ApplyImageColor(skillRangeImage, imageColor);
        ApplyImageColor(skillCostImage, imageColor);
    }


    private bool CanAffordDisplayedCost()
    {
        if (skillData == null || runtimeData == null)
            return true;

        int cost = Mathf.Max(0, displayedCostValue);

        return skillData.ReferenceResource switch
        {
            ReferenceResource.HP => runtimeData.CanReserveHP(cost),
            ReferenceResource.Cost => runtimeData.CanReserveCost(cost),
            ReferenceResource.MovePoint => runtimeData.CanReserveCost(cost),
            ReferenceResource.UniqueResource => runtimeData.CanReserveResource(cost),
            _ => true
        };
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


    private string GetReferenceResourceDisplayName(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.HP:
                return "HP";

            case ReferenceResource.UniqueResource:
                return "Ulti";

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return "Cost";

            default:
                return resource.ToString();
        }
    }

    private Sprite GetSkillIcon(SkillMasterData data)
    {
        if (data == null)
            return null;

        if (data.Icon != null)
            return data.Icon;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase.TryGetIcon(data.SkillId, out Sprite icon))
            return icon;

        return null;
    }

    private Sprite GetSkillRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon))
            return icon;

        return null;
    }

    private string BuildDetailText(SkillMasterData data, int payAmount)
    {
        if (data == null)
            return "";

        // SkillListPanel의 detailsBackground에는 Details만 그대로 표시합니다.
        // ValueRate를 설명 앞에 자동으로 붙이지 않습니다.
        return !string.IsNullOrWhiteSpace(data.Details)
            ? GameDataLocalization.SkillDetails(data)
            : "효과 설명이 없습니다.";
    }

    private void BindMissingReferences()
    {
        if (skillIconImage == null)
            skillIconImage = FindChildComponentByName<Image>(skillIconObjectName);

        if (skillNameText == null)
            skillNameText = FindChildComponentByName<TMP_Text>(skillNameObjectName);

        if (skillRangeImage == null)
            skillRangeImage = FindChildComponentByName<Image>(skillRangeObjectName);

        if (skillCostTypeText == null)
            skillCostTypeText = FindChildComponentByName<TMP_Text>(skillCostTypeObjectName);

        if (skillCostValueText == null)
            skillCostValueText = FindChildComponentByName<TMP_Text>(skillCostValueObjectName);
    }

    private T FindChildComponentByName<T>(string objectName) where T : Component
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
                continue;

            if (child.name == objectName)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
                continue;

            if (child.name.Contains(objectName))
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }

        return null;
    }
}
