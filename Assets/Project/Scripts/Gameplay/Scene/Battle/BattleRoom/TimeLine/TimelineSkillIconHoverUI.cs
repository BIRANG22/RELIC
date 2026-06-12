using UnityEngine;
using UnityEngine.EventSystems;

public class TimelineSkillIconHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TimelineReservationHoverPreview hoverPreview;

    private PlayerReservedCommand command;

    public void Setup(PlayerReservedCommand reservedCommand)
    {
        command = reservedCommand;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(
            $"[TimelineSkillIconHoverUI] Enter:{name} / " +
            $"CommandNull:{command == null} / " +
            $"RangeCount:{command?.RangeGridIndices?.Count ?? -1}"
        );

        if (hoverPreview != null)
            hoverPreview.Show(command);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverPreview != null)
            hoverPreview.Hide();
    }
}