using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyBlueDustiumHudUI : MonoBehaviour
{
    private static readonly HashSet<LobbyBlueDustiumHudUI> Instances = new();

    [SerializeField] private Sprite blueDustiumIcon;
    private TMP_Text valueText;

    private void Awake()
    {
        EnsureVisuals();
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
        int value = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate()?.BlueDustium ?? 0;
        SetValueImmediate(value);
    }

    public void SetValueImmediate(int value)
    {
        EnsureVisuals();
        valueText.text = Mathf.Max(0, value).ToString();
    }

    private void EnsureVisuals()
    {
        if (valueText != null)
            return;

        var iconObject = new GameObject("BlueDustiumIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(transform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(48f, 48f);
        Image image = iconObject.GetComponent<Image>();
        image.sprite = blueDustiumIcon;
        image.preserveAspect = true;

        var textObject = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(55f, 0f);
        textRect.offsetMax = Vector2.zero;
        valueText = textObject.GetComponent<TMP_Text>();
        valueText.alignment = TextAlignmentOptions.MidlineLeft;
        valueText.fontSize = 30f;
    }
}
