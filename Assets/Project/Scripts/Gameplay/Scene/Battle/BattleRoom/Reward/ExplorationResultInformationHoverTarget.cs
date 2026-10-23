using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ExplorationResultInformationHoverTarget : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] private ExplorationResultCharacterRowUI informationOwner;

    public void OnPointerEnter(PointerEventData eventData)
    {
        informationOwner?.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        informationOwner?.OnPointerExit(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        informationOwner?.OnPointerClick(eventData);
    }
}
