using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TimelineReservationDragContext
{
    public CharacterRuntimeData Runtime { get; private set; }
    public SkillMasterData Skill { get; private set; }
    public PlayerReservedCommand Command { get; private set; }
    public bool IsExistingReservation => Command != null;
    public bool IsFacingFlip { get; private set; }
    public bool DropHandled { get; private set; }
    public bool HasSwift => Command != null
        ? BattleActionOrderUtility.HasSwift(Command)
        : (Skill != null && Skill.IsFastSkill == 1) || BattleActionOrderUtility.HasSwift(Runtime);
    public int OccupiedOrders => IsFacingFlip ? 0 : Command != null
        ? BattleTimelineCapacityPolicy.NormalizeOccupiedOrders(Command.TimelineEntryCost)
        : ResolveSkillOccupiedOrders(Skill);

    public static TimelineReservationDragContext Active { get; private set; }

    private static RectTransform dragVisual;

    private static int ResolveSkillOccupiedOrders(SkillMasterData skill)
    {
        if (skill == null || skill.Category == Category.Move)
            return 1;

        return BattleTimelineCapacityPolicy.NormalizeOccupiedOrders(skill.Entry);
    }

    public static void BeginNewSkill(
        CharacterRuntimeData runtime,
        SkillMasterData skill,
        Sprite icon,
        PointerEventData eventData)
    {
        if (runtime == null || skill == null)
            return;

        Active = new TimelineReservationDragContext { Runtime = runtime, Skill = skill };
        CreateVisual(icon, eventData);
    }

    public static void BeginNewFacingFlip(
        CharacterRuntimeData runtime,
        SkillMasterData moveSkill,
        Sprite icon,
        PointerEventData eventData)
    {
        if (runtime == null || moveSkill == null)
            return;

        Active = new TimelineReservationDragContext
        {
            Runtime = runtime,
            Skill = moveSkill,
            IsFacingFlip = true
        };
        CreateVisual(icon, eventData);
    }

    public static void BeginExisting(
        PlayerReservedCommand command,
        Sprite icon,
        PointerEventData eventData)
    {
        if (command == null)
            return;

        Active = new TimelineReservationDragContext
        {
            Runtime = command.UserRuntime,
            Skill = command.SkillData,
            Command = command
        };
        CreateVisual(icon, eventData);
    }

    public static void UpdateVisual(PointerEventData eventData)
    {
        if (dragVisual != null && eventData != null)
            dragVisual.position = eventData.position;

        UpdateTimelinePreview(eventData);
    }

    public static void CompleteDropAtPointer(PointerEventData eventData)
    {
        if (Active == null || Active.DropHandled || eventData == null)
            return;

        BattleTimelineGroupUI[] groups = Object.FindObjectsByType<BattleTimelineGroupUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < groups.Length; i++)
        {
            BattleTimelineGroupUI group = groups[i];
            if (group == null || !group.ContainsScreenPointInDropArea(
                    eventData.position,
                    eventData.pressEventCamera))
            {
                continue;
            }

            if (group.TryHandleReservationDrop(eventData))
                return;
        }
    }

    public static void MarkDropHandled()
    {
        if (Active != null)
            Active.DropHandled = true;
    }

    public static void End()
    {
        ClearTimelinePreviews();
        if (dragVisual != null)
            Object.Destroy(dragVisual.gameObject);
        dragVisual = null;
        Active = null;
    }

    private static void UpdateTimelinePreview(PointerEventData eventData)
    {
        if (Active == null || eventData == null)
        {
            ClearTimelinePreviews();
            return;
        }

        BattleTimelineGroupUI[] groups = Object.FindObjectsByType<BattleTimelineGroupUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        BattleTimelineGroupUI target = null;
        for (int i = 0; i < groups.Length; i++)
        {
            BattleTimelineGroupUI group = groups[i];
            if (group != null && group.ContainsScreenPointInDropArea(
                    eventData.position,
                    eventData.pressEventCamera))
            {
                target = group;
                break;
            }
        }

        for (int i = 0; i < groups.Length; i++)
        {
            BattleTimelineGroupUI group = groups[i];
            if (group == null)
                continue;

            if (group == target)
                group.ShowReservationDragPreview(Active, eventData);
            else
                group.ClearReservationDragPreview();
        }
    }

    private static void ClearTimelinePreviews()
    {
        BattleTimelineGroupUI[] groups = Object.FindObjectsByType<BattleTimelineGroupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < groups.Length; i++)
            groups[i]?.ClearReservationDragPreview();
    }

    private static void CreateVisual(Sprite icon, PointerEventData eventData)
    {
        EndVisualOnly();
        Canvas canvas = eventData != null && eventData.pointerPress != null
            ? eventData.pointerPress.GetComponentInParent<Canvas>()?.rootCanvas
            : null;
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude);
        if (canvas == null)
            return;

        GameObject visual = new("TimelineReservationDragVisual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragVisual = visual.GetComponent<RectTransform>();
        dragVisual.SetParent(canvas.transform, false);
        dragVisual.SetAsLastSibling();
        dragVisual.sizeDelta = new Vector2(64f, 64f);
        Image image = visual.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        group.alpha = 0.8f;
        group.blocksRaycasts = false;
        UpdateVisual(eventData);
    }

    private static void EndVisualOnly()
    {
        if (dragVisual != null)
            Object.Destroy(dragVisual.gameObject);
        dragVisual = null;
    }
}

public sealed class TimelineReservationDragSource : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private BattleTimelinePreviewEntry entry;
    private Image iconImage;

    public void Setup(BattleTimelinePreviewEntry previewEntry, Image image)
    {
        entry = previewEntry;
        iconImage = image;
    }

    public void Clear()
    {
        entry = null;
        iconImage = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (entry == null || !entry.IsPlayer || entry.PlayerCommand == null)
            return;
        TimelineReservationDragContext.BeginExisting(
            entry.PlayerCommand,
            iconImage != null ? iconImage.sprite : null,
            eventData);
    }

    public void OnDrag(PointerEventData eventData) =>
        TimelineReservationDragContext.UpdateVisual(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        TimelineReservationDragContext.CompleteDropAtPointer(eventData);
        TimelineReservationDragContext.End();
    }
}

public sealed class BattleCharacterSkillDragSource : MonoBehaviour,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private BattleCharacterPanelUI owner;
    private int displaySlotIndex;
    private Image iconImage;
    private bool isFacingFlip;

    public void Configure(BattleCharacterPanelUI panel, int slotIndex, Image icon)
    {
        owner = panel;
        displaySlotIndex = slotIndex;
        iconImage = icon;
        isFacingFlip = false;
    }

    public void ConfigureFacingFlip(BattleCharacterPanelUI panel, Image icon)
    {
        owner = panel;
        displaySlotIndex = -1;
        iconImage = icon;
        isFacingFlip = true;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (eventData != null)
            eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFacingFlip)
            owner?.TryBeginTimelineFacingFlipDrag(iconImage, eventData);
        else
            owner?.TryBeginTimelineSkillDrag(displaySlotIndex, iconImage, eventData);
    }

    public void OnDrag(PointerEventData eventData) =>
        TimelineReservationDragContext.UpdateVisual(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        TimelineReservationDragContext.CompleteDropAtPointer(eventData);
        TimelineReservationDragContext.End();
    }
}
