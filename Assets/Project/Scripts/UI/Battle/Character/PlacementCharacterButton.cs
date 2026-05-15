using UnityEngine;
using UnityEngine.UI;

public class PlacementCharacterButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private int partySlotIndex = -1;
    private BattlePlacementController controller;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
    }

    public void Setup(int slotIndex, Sprite icon, BattlePlacementController placementController)
    {
        partySlotIndex = slotIndex;
        controller = placementController;

        gameObject.SetActive(true);

        if (iconImage == null)
        {
            Debug.LogWarning("[PlacementCharacterButton] Icon Image is missing.");
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
            Debug.LogWarning($"[PlacementCharacterButton] Icon is null. Slot: {slotIndex}");
    }

    public void Hide()
    {
        partySlotIndex = -1;
        controller = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        gameObject.SetActive(false);
    }

    public void Execute()
    {
        if (controller == null)
        {
            Debug.LogWarning("[PlacementCharacterButton] Controller missing.");
            return;
        }

        if (partySlotIndex < 0)
        {
            Debug.LogWarning("[PlacementCharacterButton] Invalid party slot index.");
            return;
        }

        controller.PlaceCharacter(partySlotIndex);
    }
}