using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapCategoryHighlightController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private MapViewSpawner mapViewSpawner;

    [Header("Category Hover Areas")]
    [SerializeField, Min(1f)] private float categoryImageHoverScale = 1.4f;

    [SerializeField] private Button bossButton;
    [SerializeField] private Button eliteButton;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button eventButton;
    [SerializeField] private Button startButton;

    private readonly List<RegisteredHoverEvent> registeredHoverEvents = new();
    private Button hoveredButton;

    private sealed class RegisteredHoverEvent
    {
        public EventTrigger Trigger;
        public EventTrigger.Entry EnterEntry;
        public EventTrigger.Entry ExitEntry;
        public RectTransform VisualTransform;
        public Vector3 BaseVisualScale;
    }

    private void Awake()
    {
        FindReferencesIfNeeded();
        RegisterHoverEvents();
    }

    private void OnDisable()
    {
        ClearHighlight();
    }

    private void OnDestroy()
    {
        UnregisterHoverEvents();
    }

    private void FindReferencesIfNeeded()
    {
        if (mapViewSpawner == null)
            mapViewSpawner = FindFirstObjectByType<MapViewSpawner>();

        if (bossButton == null)
            bossButton = FindChildButton("BossImg");

        if (eliteButton == null)
            eliteButton = FindChildButton("EliteImg");

        if (battleButton == null)
            battleButton = FindChildButton("BattleImg");

        if (restButton == null)
            restButton = FindChildButton("RestImg");

        if (eventButton == null)
            eventButton = FindChildButton("EventImg");

        if (startButton == null)
            startButton = FindChildButton("StartImg");
    }

    private Button FindChildButton(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != objectName)
                continue;

            return children[i].GetComponent<Button>();
        }

        return null;
    }

    private void RegisterHoverEvents()
    {
        RegisterHoverEvent(bossButton, "Boss");
        RegisterHoverEvent(eliteButton, "Elite");
        RegisterHoverEvent(battleButton, "Common");
        RegisterHoverEvent(restButton, "Rest");
        RegisterHoverEvent(eventButton, "Special");
        RegisterHoverEvent(startButton, "Start");
    }

    private void RegisterHoverEvent(Button targetButton, string nodeType)
    {
        if (targetButton == null)
            return;

        EventTrigger trigger = targetButton.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = targetButton.gameObject.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();

        RectTransform visualTransform = targetButton.transform.Find("Image") as RectTransform;
        Vector3 baseVisualScale = visualTransform != null ? visualTransform.localScale : Vector3.one;

        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener(_ => EnterCategory(targetButton, nodeType));

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener(_ => ExitCategory(targetButton));

        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);

        registeredHoverEvents.Add(new RegisteredHoverEvent
        {
            Trigger = trigger,
            EnterEntry = enterEntry,
            ExitEntry = exitEntry,
            VisualTransform = visualTransform,
            BaseVisualScale = baseVisualScale
        });
    }

    private void UnregisterHoverEvents()
    {
        for (int i = 0; i < registeredHoverEvents.Count; i++)
        {
            RegisteredHoverEvent registered = registeredHoverEvents[i];

            if (registered?.Trigger == null || registered.Trigger.triggers == null)
                continue;

            registered.Trigger.triggers.Remove(registered.EnterEntry);
            registered.Trigger.triggers.Remove(registered.ExitEntry);
        }

        registeredHoverEvents.Clear();
    }

    private void EnterCategory(Button targetButton, string nodeType)
    {
        hoveredButton = targetButton;
        SetCategoryImageScale(targetButton, true);

        if (mapViewSpawner != null)
            mapViewSpawner.HighlightCategory(nodeType);
    }

    private void ExitCategory(Button targetButton)
    {
        if (hoveredButton != targetButton)
            return;

        ClearHighlight();
    }

    private void ClearHighlight()
    {
        hoveredButton = null;
        ResetAllCategoryImageScales();

        if (mapViewSpawner != null)
            mapViewSpawner.ClearCategoryHighlight();
    }

    private void SetCategoryImageScale(Button targetButton, bool hovered)
    {
        for (int i = 0; i < registeredHoverEvents.Count; i++)
        {
            RegisteredHoverEvent registered = registeredHoverEvents[i];
            if (registered?.Trigger == null || registered.Trigger.gameObject != targetButton.gameObject)
                continue;

            RectTransform visualTransform = registered.VisualTransform;
            if (visualTransform == null)
                return;

            visualTransform.localScale = hovered
                ? registered.BaseVisualScale * categoryImageHoverScale
                : registered.BaseVisualScale;
            return;
        }
    }

    private void ResetAllCategoryImageScales()
    {
        for (int i = 0; i < registeredHoverEvents.Count; i++)
        {
            RegisteredHoverEvent registered = registeredHoverEvents[i];
            RectTransform visualTransform = registered?.VisualTransform;
            if (visualTransform != null)
                visualTransform.localScale = registered.BaseVisualScale;
        }
    }

    /// <summary>
    /// 방 클리어 후 지도가 다시 생성될 때 남아 있는 카테고리 호버 강조를 초기화합니다.
    /// </summary>
    public void ResetHighlightForMapRefresh()
    {
        ClearHighlight();
    }
}
