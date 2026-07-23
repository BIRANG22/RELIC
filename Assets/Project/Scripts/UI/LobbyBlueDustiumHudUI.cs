using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyBlueDustiumHudUI : MonoBehaviour
{
    private static readonly HashSet<LobbyBlueDustiumHudUI> Instances = new();

    [Header("아이콘")]
    [SerializeField] private Sprite blueDustiumIcon;
    [SerializeField] private Color iconColor = Color.white;

    private TMP_Text valueText;
    private Image iconImage;

    private void Awake()
    {
        EnsureVisuals();
        ApplyIconSettings();
    }

    private void OnEnable()
    {
        Instances.Add(this);
        Refresh();
    }

    private void OnDisable()
    {
        Instances.Remove(this);
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
        EnsureVisuals();
        ApplyIconSettings();

        int value = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate()?.BlueDustium ?? 0;
        SetValueImmediate(value);
    }

    public void SetValueImmediate(int value)
    {
        EnsureVisuals();

        if (valueText != null)
            valueText.text = Mathf.Max(0, value).ToString();
    }

    private void EnsureVisuals()
    {
        if (iconImage == null)
        {
            Transform existingIcon = transform.Find("BlueDustiumIcon");
            if (existingIcon != null)
                iconImage = existingIcon.GetComponent<Image>();
        }

        if (valueText == null)
        {
            Transform existingValue = transform.Find("Value");
            if (existingValue != null)
                valueText = existingValue.GetComponent<TMP_Text>();
        }

        if (iconImage == null)
        {
            var iconObject = new GameObject(
                "BlueDustiumIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(48f, 48f);

            iconImage = iconObject.GetComponent<Image>();
            iconImage.preserveAspect = true;
        }

        if (valueText == null)
        {
            var textObject = new GameObject(
                "Value",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.SetParent(transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 0f);
            textRect.offsetMax = Vector2.zero;

            valueText = textObject.GetComponent<TMP_Text>();
            valueText.alignment = TextAlignmentOptions.MidlineLeft;
            valueText.fontSize = 30f;
        }
    }

    private void ApplyIconSettings()
    {
        if (iconImage == null)
            return;

        iconImage.sprite = blueDustiumIcon;
        iconImage.color = iconColor;
        iconImage.preserveAspect = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // OnValidate에서는 GameObject 또는 Component를 생성하지 않는다.
        // 이미 생성된 아이콘이 있을 때만 설정값을 갱신한다.
        if (iconImage == null)
        {
            Transform existingIcon = transform.Find("BlueDustiumIcon");
            if (existingIcon != null)
                iconImage = existingIcon.GetComponent<Image>();
        }

        ApplyIconSettings();
    }
#endif
}
