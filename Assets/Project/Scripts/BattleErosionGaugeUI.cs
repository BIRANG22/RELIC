using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleErosionGaugeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private float animationDuration = 0.35f;

    [Header("Hover")]
    [SerializeField] private bool showValueOnlyOnHover = true;
    [Tooltip("마우스오버 판정에 사용할 Back 영역입니다. 비워두면 자식 Back을 자동으로 찾습니다.")]
    [SerializeField] private RectTransform hoverBackRect;
    private Canvas hoverCanvas;
    private bool isHovering;

    [Header("Erosion Add Animation")]
    [Tooltip("침식도 증가량을 표시할 Add 텍스트입니다. 비워두면 Erosion/Add를 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text addText;
    [Tooltip("Add 텍스트가 위로 이동하는 UI 거리입니다.")]
    [SerializeField] private float addMoveDistanceY = 55f;
    [Tooltip("증가량 표시 연출의 총 시간입니다.")]
    [SerializeField] private float addAnimationDuration = 0.7f;
    [Tooltip("연출 시작 후 사라지는 데 걸리는 시간입니다.")]
    [SerializeField] private float addFadeDuration = 0.5f;
    [Tooltip("위로 이동하는 속도의 곡선입니다.")]
    [SerializeField] private AnimationCurve addMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine addAnimationCoroutine;
    private RectTransform addRect;
    private Vector2 addOriginalPosition;
    private Color addOriginalColor;
    private bool addPositionCaptured;
    private Coroutine animationCoroutine;
    private float displayedValue;
    private bool initialized;

    private void Awake()
    {
        AutoBind();
        EnsureValueTextRuntimeOwnership();
        RefreshValueVisibility(false);
    }

    private void OnEnable()
    {
        AutoBind();
        EnsureValueTextRuntimeOwnership();

        BattleErosionRuntimeService.ValueChanged -= HandleValueChanged;
        BattleErosionRuntimeService.ValueChanged += HandleValueChanged;
        SetImmediate(BattleErosionRuntimeService.CurrentValue);
        RefreshValueVisibility(IsPointerOverBack());
    }

    private void OnDisable()
    {
        BattleErosionRuntimeService.ValueChanged -= HandleValueChanged;
        isHovering = false;
        RefreshValueVisibility(false);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        StopAddAnimation();
        ResetAddVisual();
    }

    private void LateUpdate()
    {
        // Raycast Target 설정과 관계없이 Back의 실제 사각형으로 판정합니다.
        if (!showValueOnlyOnHover)
        {
            if (valueText != null && !valueText.gameObject.activeSelf)
                RefreshValueVisibility(true);
            return;
        }

        bool hovered = IsPointerOverBack();
        if (hovered != isHovering || (valueText != null && valueText.gameObject.activeSelf != hovered))
            RefreshValueVisibility(hovered);
    }

    private bool IsPointerOverBack()
    {
        if (hoverBackRect == null)
            AutoBind();

        RectTransform target = hoverBackRect != null ? hoverBackRect : transform as RectTransform;
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        if (hoverCanvas == null || !hoverCanvas.isActiveAndEnabled)
            hoverCanvas = target.GetComponentInParent<Canvas>();

        Camera eventCamera = null;
        if (hoverCanvas != null && hoverCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = hoverCanvas.rootCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(target, Input.mousePosition, eventCamera);
    }

    public void Initialize()
    {
        AutoBind();
        EnsureValueTextRuntimeOwnership();
        SetImmediate(BattleErosionRuntimeService.CurrentValue);
        initialized = true;
    }

    private void AutoBind()
    {
        if (fillImage == null)
        {
            Transform fill = FindChildRecursive(transform, "Fill");
            if (fill != null)
                fillImage = fill.GetComponent<Image>();
        }

        if (valueText == null)
        {
            Transform value = FindChildRecursive(transform, "Value");
            if (value != null)
                valueText = value.GetComponent<TMP_Text>();
        }

        if (hoverBackRect == null)
        {
            Transform back = FindChildRecursive(transform, "Back");
            if (back != null)
                hoverBackRect = back as RectTransform;
        }

        if (addText == null)
        {
            Transform add = FindChildRecursive(transform, "Add");
            if (add != null)
                addText = add.GetComponent<TMP_Text>();
        }

        if (addText != null && (!addPositionCaptured || addRect != addText.rectTransform))
        {
            addRect = addText.rectTransform;
            addOriginalPosition = addRect.anchoredPosition;
            addOriginalColor = addText.color;
            addPositionCaptured = true;
        }

        if (fillImage != null && fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }


    private void EnsureValueTextRuntimeOwnership()
    {
        if (valueText == null)
            return;

        GameObject target = valueText.gameObject;

        // Value는 실시간 침식도 숫자 전용입니다.
        // 정적 로컬라이저가 MenuRoot 재활성화 시 "침식" 같은 문구로 덮어쓰지 못하게 합니다.
        if (target.GetComponent<LocalizationIgnore>() == null)
            target.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizedTmp = target.GetComponent<LocalizedTMPText>();
        if (localizedTmp != null)
            localizedTmp.enabled = false;

        LocalizeStringEvent legacyLocalizer = target.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
            legacyLocalizer.enabled = false;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        RefreshValueVisibility(IsPointerOverBack());
        SetImmediate(BattleErosionRuntimeService.CurrentValue);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RefreshValueVisibility(IsPointerOverBack());
    }

    private void RefreshValueVisibility(bool hovered)
    {
        if (valueText == null)
            AutoBind();

        if (valueText == null)
            return;

        isHovering = hovered;
        bool shouldShow = !showValueOnlyOnHover || hovered;
        if (valueText.gameObject.activeSelf != shouldShow)
            valueText.gameObject.SetActive(shouldShow);
    }

    private void HandleValueChanged(int before, int after)
    {
        if (!initialized)
            Initialize();

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateTo(after));

        // Only actual increases trigger the floating delta indicator.
        if (after > before)
            PlayAddAnimation(after - before);
    }

    private void PlayAddAnimation(int delta)
    {
        AutoBind();
        if (addText == null || delta <= 0)
            return;

        StopAddAnimation();
        ResetAddVisual();
        addText.text = "+" + delta.ToString();
        addText.gameObject.SetActive(true);
        addAnimationCoroutine = StartCoroutine(AnimateAdd());
    }

    private IEnumerator AnimateAdd()
    {
        float duration = Mathf.Max(0.01f, addAnimationDuration);
        float fade = Mathf.Clamp(addFadeDuration, 0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float moveT = addMoveCurve != null && addMoveCurve.length > 0
                ? addMoveCurve.Evaluate(t) : t;
            addRect.anchoredPosition = addOriginalPosition + Vector2.up * (addMoveDistanceY * moveT);
            float fadeT = Mathf.Clamp01((elapsed - (duration - fade)) / fade);
            Color color = addOriginalColor;
            color.a *= 1f - fadeT;
            addText.color = color;
            yield return null;
        }

        addAnimationCoroutine = null;
        ResetAddVisual();
    }

    private void StopAddAnimation()
    {
        if (addAnimationCoroutine == null)
            return;

        StopCoroutine(addAnimationCoroutine);
        addAnimationCoroutine = null;
    }

    private void ResetAddVisual()
    {
        if (addText == null || !addPositionCaptured)
            return;

        addRect.anchoredPosition = addOriginalPosition;
        addText.color = addOriginalColor;
        if (addText.gameObject.activeSelf)
            addText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateTo(int targetValue)
    {
        float startValue = displayedValue;
        float target = Mathf.Clamp(targetValue, BattleErosionRuntimeService.MinValue, BattleErosionRuntimeService.MaxValue);
        float duration = Mathf.Max(0.01f, animationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            ApplyDisplayedValue(Mathf.Lerp(startValue, target, eased));
            yield return null;
        }

        ApplyDisplayedValue(target);
        animationCoroutine = null;
    }

    private void SetImmediate(int value)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ApplyDisplayedValue(Mathf.Clamp(value, BattleErosionRuntimeService.MinValue, BattleErosionRuntimeService.MaxValue));
    }

    private void ApplyDisplayedValue(float value)
    {
        displayedValue = Mathf.Clamp(value, BattleErosionRuntimeService.MinValue, BattleErosionRuntimeService.MaxValue);

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(displayedValue).ToString();

        if (fillImage != null)
            fillImage.fillAmount = displayedValue / BattleErosionRuntimeService.MaxValue;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
