using UnityEngine;

public class TimelineReservationHoverPreview : MonoBehaviour
{
    [SerializeField] private RangePreview rangePreview;

    public void Show(PlayerReservedCommand command)
    {
        if (command == null || rangePreview == null)
            return;

        rangePreview.ShowRangeCells(command.RangeGridIndices);
    }

    public void Hide()
    {
        if (rangePreview != null)
            rangePreview.ClearRangeOnly();
    }
}