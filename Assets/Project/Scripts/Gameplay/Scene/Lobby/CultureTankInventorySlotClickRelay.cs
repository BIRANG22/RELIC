using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CultureTankInventorySlotClickRelay : MonoBehaviour, IPointerClickHandler
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
        InvokeSelection();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 슬롯 루트에 Button이 없는 StorageSlotUI 프리팹도 클릭할 수 있게 합니다.
        // Button이 있는 경우에는 Button.onClick에서 처리하므로 중복 실행하지 않습니다.
        if (button != null)
            return;

        InvokeSelection();
    }

    private void InvokeSelection()
    {
        if (!selectionEnabled || string.IsNullOrWhiteSpace(itemId))
            return;

        onSelected?.Invoke(itemId);
    }
}
