using UnityEngine;
using UnityEngine.EventSystems;

public class TimelineSkillIconHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Range Preview")]
    [SerializeField] private TimelineReservationHoverPreview hoverPreview;
    [SerializeField] private bool autoFindHoverPreview = true;

    [Header("Skill Info Popup")]
    [SerializeField] private TimelineSkillHoverPopupUI hoverPopup;
    [SerializeField] private bool autoFindHoverPopup = true;

    private PlayerReservedCommand command;
    private BattleTimelinePreviewEntry entry;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        FindReferencesIfNeeded();
    }

    public void Setup(PlayerReservedCommand reservedCommand)
    {
        command = reservedCommand;
        entry = null;
    }

    public void Setup(BattleTimelinePreviewEntry previewEntry)
    {
        entry = previewEntry;
        command = previewEntry != null ? previewEntry.PlayerCommand : null;
    }

    public void Clear()
    {
        command = null;
        entry = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FindReferencesIfNeeded();

        if (hoverPreview != null && command != null)
            hoverPreview.Show(command);

        if (hoverPopup != null)
            hoverPopup.Show(entry, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverPreview != null)
            hoverPreview.Hide();

        if (hoverPopup != null)
            hoverPopup.Hide();
    }

    private void FindReferencesIfNeeded()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (hoverPreview == null && autoFindHoverPreview)
            hoverPreview = FindFirstObjectByType<TimelineReservationHoverPreview>(FindObjectsInactive.Include);

        if (hoverPopup == null && autoFindHoverPopup)
            hoverPopup = FindFirstObjectByType<TimelineSkillHoverPopupUI>(FindObjectsInactive.Include);
    }
}
