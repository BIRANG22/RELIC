using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleNextNodeChoiceButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    private int nodeIndex = -1;
    private Action<int> onSelected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (iconImage == null)
            iconImage = transform.Find("NodeIcon")?.GetComponent<Image>();

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    public void ConfigureGeneratedUi(Image generatedIcon)
    {
        iconImage = generatedIcon;
    }

    public void Bind(GeneratedMapNodeData node, Action<int> selectionCallback)
    {
        nodeIndex = node != null ? node.NodeIndex : -1;
        onSelected = selectionCallback;

        if (iconImage != null)
        {
            Sprite icon = null;
            bool hasIcon = node != null &&
                           DataManager.Instance != null &&
                           DataManager.Instance.MapNodeIconDatabase != null &&
                           DataManager.Instance.MapNodeIconDatabase.TryGetIcon(node.Type, out icon);
            iconImage.sprite = icon;
            iconImage.enabled = hasIcon;
        }

        gameObject.SetActive(node != null);
    }

    public void Clear()
    {
        nodeIndex = -1;
        onSelected = null;
        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (nodeIndex < 0 || UIPanelButton.IsMenuPanelOpen)
            return;

        onSelected?.Invoke(nodeIndex);
    }
}
