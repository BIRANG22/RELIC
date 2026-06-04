using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TimelineOrderClickTarget : MonoBehaviour, IPointerClickHandler
{
    private BattleTimelineGroupUI owner;
    private int orderIndex;

    public void Init(BattleTimelineGroupUI owner, int orderIndex)
    {
        this.owner = owner;
        this.orderIndex = orderIndex;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnOrderClicked(orderIndex);
    }
}