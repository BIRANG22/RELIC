using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleNextNodeChoiceButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    [Header("Node Type Sprites")]
    [SerializeField] private Sprite eventSprite;
    [SerializeField] private Sprite restSprite;
    [SerializeField] private Sprite battleSprite;
    [SerializeField] private Sprite eliteBattleSprite;
    [SerializeField] private Sprite bossBattleSprite;

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

    public void Bind(
        GeneratedMapNodeData node,
        Action<int> selectionCallback)
    {
        nodeIndex = node != null ? node.NodeIndex : -1;
        onSelected = selectionCallback;

        if (iconImage != null)
        {
            Sprite sprite = GetNodeTypeSprite(node);

            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
            iconImage.preserveAspect = true;
        }

        gameObject.SetActive(node != null);
    }

    private Sprite GetNodeTypeSprite(GeneratedMapNodeData node)
    {
        if (node == null)
            return null;

        return node.Type switch
        {
            "Special" => eventSprite,
            "Rest" => restSprite,
            "Common" => battleSprite,
            "Elite" => eliteBattleSprite,
            "Boss" => bossBattleSprite,
            _ => null
        };
    }

    public void Clear()
    {
        nodeIndex = -1;
        onSelected = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (nodeIndex < 0 || UIPanelButton.IsMenuPanelOpen)
            return;

        onSelected?.Invoke(nodeIndex);
    }
}