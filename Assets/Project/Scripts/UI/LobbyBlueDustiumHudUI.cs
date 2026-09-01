using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class LobbyBlueDustiumHudUI : MonoBehaviour
{
    private static readonly HashSet<LobbyBlueDustiumHudUI> Instances = new();

    [Header("HUD 연결")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;

    [Header("아이콘")]
    [SerializeField] private Sprite blueDustiumIcon;
    [SerializeField] private Color iconColor = Color.white;

    [Header("숫자 변화 연출")]
    [Tooltip("현재 표시값에서 실제 블루 더스티움 보유량까지 숫자가 변화하는 시간입니다.")]
    [SerializeField, Min(0f)] private float numberChangeDuration = 0.2f;

    [Header("에디터 미리보기")]
    [SerializeField, Min(0)] private int editorPreviewValue = 0;

    private Coroutine numberChangeCoroutine;
    private int displayedValue;
    private bool hasDisplayedValue;

    private void Awake()
    {
        BindExistingChildren();
        ApplyIconSettings();
    }

    private void OnEnable()
    {
        BindExistingChildren();
        ApplyIconSettings();

        if (Application.isPlaying)
        {
            Instances.Add(this);
            Refresh();
        }
        else
        {
            SetValueImmediate(editorPreviewValue);
        }
    }

    private void OnDisable()
    {
        Instances.Remove(this);
        StopNumberChangeCoroutine();
        hasDisplayedValue = false;
    }

    public static void RefreshAll()
    {
        foreach (LobbyBlueDustiumHudUI instance in Instances)
        {
            if (instance != null)
                instance.Refresh();
        }
    }

    public void Refresh()
    {
        BindExistingChildren();
        ApplyIconSettings();

        if (!Application.isPlaying)
        {
            SetValueImmediate(editorPreviewValue);
            return;
        }

        int targetValue = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate()?.BlueDustium ?? 0;

        // HUD가 처음 표시될 때는 현재 보유량을 즉시 보여줍니다.
        if (!hasDisplayedValue)
        {
            StopNumberChangeCoroutine();
            displayedValue = Mathf.Max(0, targetValue);
            hasDisplayedValue = true;
            ApplyDisplayedValue();
            return;
        }

        if (displayedValue == targetValue)
        {
            StopNumberChangeCoroutine();
            ApplyDisplayedValue();
            return;
        }

        StopNumberChangeCoroutine();
        numberChangeCoroutine = StartCoroutine(AnimateNumberChange(targetValue));
    }

    public void SetValueImmediate(int value)
    {
        BindExistingChildren();
        StopNumberChangeCoroutine();
        displayedValue = Mathf.Max(0, value);
        hasDisplayedValue = true;
        ApplyDisplayedValue();
    }

    private IEnumerator AnimateNumberChange(int targetValue)
    {
        int startValue = displayedValue;
        int safeTargetValue = Mathf.Max(0, targetValue);

        if (numberChangeDuration <= 0f)
        {
            displayedValue = safeTargetValue;
            ApplyDisplayedValue();
            numberChangeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < numberChangeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / numberChangeDuration);
            displayedValue = Mathf.RoundToInt(Mathf.Lerp(startValue, safeTargetValue, progress));
            ApplyDisplayedValue();
            yield return null;
        }

        displayedValue = safeTargetValue;
        ApplyDisplayedValue();
        numberChangeCoroutine = null;
    }

    private void ApplyDisplayedValue()
    {
        if (valueText != null)
            valueText.text = Mathf.Max(0, displayedValue).ToString();
    }

    private void StopNumberChangeCoroutine()
    {
        if (numberChangeCoroutine == null)
            return;

        StopCoroutine(numberChangeCoroutine);
        numberChangeCoroutine = null;
    }

    private void BindExistingChildren()
    {
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (valueText == null)
        {
            Transform valueTransform = transform.Find("Value");
            if (valueTransform != null)
                valueText = valueTransform.GetComponent<TMP_Text>();
        }
    }

    private void ApplyIconSettings()
    {
        if (iconImage == null)
            return;

        if (blueDustiumIcon != null)
            iconImage.sprite = blueDustiumIcon;

        iconImage.color = iconColor;
        iconImage.preserveAspect = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        editorPreviewValue = Mathf.Max(0, editorPreviewValue);
        numberChangeDuration = Mathf.Max(0f, numberChangeDuration);

        BindExistingChildren();
        ApplyIconSettings();

        if (!Application.isPlaying && valueText != null)
            valueText.text = editorPreviewValue.ToString();
    }
#endif
}
