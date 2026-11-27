using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LobbyConsumableSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private LobbyConsumablePanelUI panel;
    [SerializeField, Range(0, 3)] private int slotIndex;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject placeholder;
    [SerializeField] private GameObject selection;
    private string displayedItemId;

    private void OnEnable() => displayedItemId = null;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
            panel?.UseSlot(slotIndex, eventData.button == PointerEventData.InputButton.Right);
    }

    public void Refresh(string itemId, bool selecting)
    {
        if (selection != null && selection.activeSelf != selecting) selection.SetActive(selecting);
        string normalized = itemId ?? string.Empty;
        if (displayedItemId == normalized) return;
        displayedItemId = normalized;
        Sprite sprite = null;
        if (!string.IsNullOrWhiteSpace(normalized)) DataManager.Instance?.RelicIconDatabase?.TryGetIcon(normalized, out sprite);
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.gameObject.SetActive(true);
        }
        if (placeholder != null) placeholder.SetActive(string.IsNullOrWhiteSpace(normalized));
    }
}
