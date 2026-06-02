using System;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private GeneratedMapNodeData nodeData;
    private Action<GeneratedMapNodeData> onClicked;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Setup(
        GeneratedMapNodeData data,
        MapNodeIconDatabase iconDatabase,
        Action<GeneratedMapNodeData> clickCallback,
        bool canClick)
    {
        nodeData = data;
        onClicked = clickCallback;

        if (iconImage != null &&
            iconDatabase != null &&
            iconDatabase.TryGetIcon(data.Type, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }

        SetClickable(canClick);
    }

    public void SetClickable(bool canClick)
    {
        if (button != null)
            button.interactable = canClick;

        if (iconImage != null)
        {
            Color color = iconImage.color;
            color.a = canClick ? 1f : 0.45f;
            iconImage.color = color;
        }
    }

    public void OnClick()
    {
        if (nodeData == null)
            return;

        if (button != null && !button.interactable)
            return;

        onClicked?.Invoke(nodeData);
    }
}