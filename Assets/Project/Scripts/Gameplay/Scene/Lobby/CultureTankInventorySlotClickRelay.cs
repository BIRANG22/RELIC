using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CultureTankInventorySlotClickRelay : MonoBehaviour
{
    private Button button;
    private string itemId;
    private bool selectionEnabled;
    private Action<string> onSelected;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        BindButton();
    }

    public void Configure(Button targetButton, string configuredItemId, bool enabled, Action<string> callback)
    {
        button = targetButton != null ? targetButton : GetComponent<Button>();
        itemId = string.IsNullOrWhiteSpace(configuredItemId) ? string.Empty : configuredItemId.Trim();
        selectionEnabled = enabled && !string.IsNullOrWhiteSpace(itemId);
        onSelected = callback;
        BindButton();
        if (button != null) button.interactable = selectionEnabled;
    }

    private void BindButton()
    {
        if (button == null) return;
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (!selectionEnabled || string.IsNullOrWhiteSpace(itemId)) return;
        onSelected?.Invoke(itemId);
    }
}
