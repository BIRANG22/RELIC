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
    private BattleTimelinePreviewEntry hoveredEntry;
    private BattleTimelineGroupUI timelineGroup;
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
        HideHoverElements();

        command = null;
        entry = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FindReferencesIfNeeded();

        if (hoverPreview != null && entry != null)
            hoverPreview.Show(entry);

        if (hoverPopup != null)
            hoverPopup.Show(entry, rectTransform, this);

        if (entry != null)
        {
            timelineGroup ??= GetComponentInParent<BattleTimelineGroupUI>();
            hoveredEntry = entry;
            timelineGroup?.SetHoveredOwner(entry);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHoverElements();
    }

    private void OnDisable()
    {
        HideHoverElements();
    }

    private void OnDestroy()
    {
        HideHoverElements();
    }

    private void HideHoverElements()
    {
        FindReferencesIfNeeded();

        if (hoverPreview != null)
            hoverPreview.Hide();

        if (hoverPopup != null)
            hoverPopup.Hide(this);

        if (timelineGroup == null)
            timelineGroup = GetComponentInParent<BattleTimelineGroupUI>();

        if (hoveredEntry != null)
            timelineGroup?.ClearHoveredOwner(hoveredEntry);

        hoveredEntry = null;
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
