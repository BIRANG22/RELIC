using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public sealed class BattleErosionGaugeUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private float animationDuration = 0.35f;

    private Coroutine animationCoroutine;
    private float displayedValue;
    private bool initialized;

    private void Awake()
    {
        AutoBind();
        EnsureValueTextRuntimeOwnership();
    }

    private void OnEnable()
    {
        AutoBind();
        EnsureValueTextRuntimeOwnership();

        BattleErosionRuntimeService.ValueChanged -= HandleValueChanged;
        BattleErosionRuntimeService.ValueChanged += HandleValueChanged;
        SetImmediate(BattleErosionRuntimeService.CurrentValue);
    }

    private void OnDisable()
    {
        BattleErosionRuntimeService.ValueChanged -= HandleValueChanged;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
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

    private void HandleValueChanged(int before, int after)
    {
        if (!initialized)
            Initialize();

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateTo(after));
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
