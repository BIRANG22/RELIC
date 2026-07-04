using UnityEngine;

public class EventRoomRelicRewardItem : MonoBehaviour
{
    private ChestOpenButton owner;
    private string relicId;
    private bool isInteractable;

    public void Setup(ChestOpenButton chestOwner, string id)
    {
        owner = chestOwner;
        relicId = id;
        isInteractable = owner != null && !string.IsNullOrWhiteSpace(relicId);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    private void OnMouseEnter()
    {
        if (!CanInteract())
            return;

        owner.NotifyRewardPointerEnter();
    }

    private void OnMouseExit()
    {
        if (owner != null)
            owner.NotifyRewardPointerExit();
    }

    private void OnMouseDown()
    {
        if (!CanInteract())
            return;

        owner.NotifyRewardClicked();
    }

    private bool CanInteract()
    {
        return isInteractable && owner != null && !string.IsNullOrWhiteSpace(relicId);
    }
}
