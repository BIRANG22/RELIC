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

    [Header("에디터 미리보기")]
    [SerializeField, Min(0)] private int editorPreviewValue = 0;

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

        int value = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate()?.BlueDustium ?? 0;
        SetValueImmediate(value);
    }

    public void SetValueImmediate(int value)
    {
        BindExistingChildren();

        if (valueText != null)
            valueText.text = Mathf.Max(0, value).ToString();
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

        BindExistingChildren();
        ApplyIconSettings();

        if (!Application.isPlaying && valueText != null)
            valueText.text = editorPreviewValue.ToString();
    }
#endif
}
