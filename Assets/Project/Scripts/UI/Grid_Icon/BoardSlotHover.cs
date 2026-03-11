using UnityEngine;
using UnityEngine.EventSystems;

public class BoardSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PerspectiveBoardController controller;
    [SerializeField] private int slotIndex = -1;

    public void Setup(PerspectiveBoardController targetController, int index)
    {
        controller = targetController;
        slotIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"ENTER : {name} / slotIndex = {slotIndex}");

        if (controller != null && slotIndex >= 0)
            controller.SetHoveredSlot(slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"EXIT : {name} / slotIndex = {slotIndex}");

        if (controller != null)
            controller.ClearHoveredSlot(slotIndex);
    }
}