using System;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private GeneratedMapNodeData nodeData;
    private Action<GeneratedMapNodeData> onClicked;

    public void Setup(
        GeneratedMapNodeData data,
        MapNodeIconDatabase iconDatabase,
        Action<GeneratedMapNodeData> clickCallback)
    {
        nodeData = data;
        onClicked = clickCallback;

        if (iconImage == null)
            return;

        if (iconDatabase != null &&
            iconDatabase.TryGetIcon(data.Type, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }

    public void OnClick()
    {
        if (nodeData == null)
            return;

        onClicked?.Invoke(nodeData);
    }
}