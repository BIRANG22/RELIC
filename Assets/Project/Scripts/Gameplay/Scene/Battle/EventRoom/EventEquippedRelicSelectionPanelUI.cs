using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public readonly struct EventEquippedRelicSelectionPanelEntry
{
    public EventEquippedRelicSelectionPanelEntry(
        EventChoiceEquippedRelicCost cost,
        string characterName,
        string slotName,
        string relicName,
        Sprite icon = null)
    {
        Cost = cost;
        CharacterName = Normalize(characterName);
        SlotName = Normalize(slotName);
        RelicName = Normalize(relicName);
        Icon = icon;
    }

    public EventChoiceEquippedRelicCost Cost { get; }
    public string CharacterName { get; }
    public string SlotName { get; }
    public string RelicName { get; }
    public Sprite Icon { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public sealed class EventEquippedRelicSelectionPanelUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject optionTemplate;
    [SerializeField] private int columnCount = 3;
    [SerializeField] private Vector2 optionSpacing = new(12f, 12f);

    private readonly List<EventChoiceEquippedRelicCost> options = new();
    private readonly List<GameObject> optionObjects = new();
    private Func<EventChoiceEquippedRelicCost, EventEquippedRelicSelectionPanelEntry> entryFactory;
    private Func<EventChoiceEquippedRelicCost, bool> selectedCallback;
    private Action cancelCallback;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public int VisibleOptionCount { get; private set; }

    private void Awake()
    {
        RegisterCancelButton();
        Close();
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelSelection);
    }

    public bool Open(
        IEnumerable<EventChoiceEquippedRelicCost> equippedRelics,
        Func<EventChoiceEquippedRelicCost, EventEquippedRelicSelectionPanelEntry> createEntry,
        Func<EventChoiceEquippedRelicCost, bool> onSelected,
        Action onCancelled)
    {
        if (!HasRequiredSceneReferences())
        {
            Close();
            return false;
        }

        gameObject.SetActive(true);
        RegisterCancelButton();

        options.Clear();
        if (equippedRelics != null)
        {
            foreach (EventChoiceEquippedRelicCost cost in equippedRelics)
            {
                if (cost.IsValid)
                    options.Add(cost);
            }
        }

        entryFactory = createEntry;
        selectedCallback = onSelected;
        cancelCallback = onCancelled;

        optionTemplate.SetActive(false);
        panelRoot.SetActive(true);
        transform.SetAsLastSibling();
        RefreshOptions();
        return true;
    }

    public bool TrySelect(EventChoiceEquippedRelicCost cost)
    {
        if (!IsOpen || !ContainsCost(cost))
            return false;

        Func<EventChoiceEquippedRelicCost, bool> callback = selectedCallback;
        bool accepted = callback == null || callback.Invoke(cost);
        if (accepted && IsOpen)
            Close();

        return accepted;
    }

    public void CancelSelection()
    {
        if (!IsOpen)
            return;

        Action callback = cancelCallback;
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        ClearOptionObjects();
        options.Clear();
        entryFactory = null;
        selectedCallback = null;
        cancelCallback = null;
        VisibleOptionCount = 0;

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);

        if (optionTemplate != null)
            optionTemplate.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private bool HasRequiredSceneReferences()
    {
        return panelRoot != null &&
               contentRoot != null &&
               optionTemplate != null;
    }

    private void RefreshOptions()
    {
        ClearOptionObjects();
        VisibleOptionCount = 0;

        if (titleText != null)
            titleText.text = "삭제할 장착 유물 선택";

        if (emptyText != null)
            emptyText.gameObject.SetActive(options.Count == 0);

        for (int i = 0; i < options.Count; i++)
        {
            EventChoiceEquippedRelicCost cost = options[i];
            EventEquippedRelicSelectionPanelEntry entry = entryFactory != null
                ? entryFactory(cost)
                : new EventEquippedRelicSelectionPanelEntry(
                    cost,
                    cost.CharacterId,
                    GetFallbackSlotName(cost.RelicSlotIndex),
                    cost.RelicId);

            GameObject optionObject = CreateOptionObject(entry, i);
            optionObjects.Add(optionObject);
            VisibleOptionCount++;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private GameObject CreateOptionObject(EventEquippedRelicSelectionPanelEntry entry, int optionIndex)
    {
        GameObject optionObject = Instantiate(optionTemplate, contentRoot);
        optionObject.name = "EquippedRelicOption";
        optionObject.SetActive(true);
        PositionOptionObject(optionObject, optionIndex);

        TMP_Text relicNameText = FindText(optionObject.transform, "RelicNameText");
        TMP_Text characterNameText = FindText(optionObject.transform, "CharacterNameText");
        TMP_Text slotNameText = FindText(optionObject.transform, "SlotNameText");
        Image iconImage = FindImage(optionObject.transform, "Icon");

        if (relicNameText != null)
            relicNameText.text = string.IsNullOrWhiteSpace(entry.RelicName)
                ? entry.Cost.RelicId
                : entry.RelicName;

        if (characterNameText != null)
            characterNameText.text = entry.CharacterName;

        if (slotNameText != null)
            slotNameText.text = entry.SlotName;

        if (iconImage != null)
        {
            iconImage.sprite = entry.Icon;
            iconImage.enabled = entry.Icon != null;
        }

        Button button = optionObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            EventChoiceEquippedRelicCost captured = entry.Cost;
            button.onClick.AddListener(() => TrySelect(captured));
        }

        return optionObject;
    }

    private void PositionOptionObject(GameObject optionObject, int optionIndex)
    {
        RectTransform templateRect = optionTemplate.GetComponent<RectTransform>();
        RectTransform optionRect = optionObject.GetComponent<RectTransform>();
        if (templateRect == null || optionRect == null)
            return;

        int columns = Mathf.Max(1, columnCount);
        int row = optionIndex / columns;
        int column = optionIndex % columns;
        Vector2 cellSize = templateRect.sizeDelta;
        optionRect.anchorMin = templateRect.anchorMin;
        optionRect.anchorMax = templateRect.anchorMax;
        optionRect.pivot = templateRect.pivot;
        optionRect.sizeDelta = cellSize;
        optionRect.anchoredPosition = templateRect.anchoredPosition +
                                      new Vector2(
                                          column * (cellSize.x + optionSpacing.x),
                                          -row * (cellSize.y + optionSpacing.y));

        int requiredRows = row + 1;
        float requiredHeight = requiredRows * cellSize.y +
                               Mathf.Max(0, requiredRows - 1) * optionSpacing.y;
        Vector2 contentSize = contentRoot.sizeDelta;
        contentRoot.sizeDelta = new Vector2(
            contentSize.x,
            Mathf.Max(contentSize.y, requiredHeight));
    }

    private void ClearOptionObjects()
    {
        for (int i = optionObjects.Count - 1; i >= 0; i--)
        {
            GameObject optionObject = optionObjects[i];
            if (optionObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(optionObject);
            else
                DestroyImmediate(optionObject);
        }

        optionObjects.Clear();
    }

    private bool ContainsCost(EventChoiceEquippedRelicCost cost)
    {
        for (int i = 0; i < options.Count; i++)
        {
            EventChoiceEquippedRelicCost option = options[i];
            if (string.Equals(option.CharacterId, cost.CharacterId, StringComparison.Ordinal) &&
                option.RelicSlotIndex == cost.RelicSlotIndex &&
                string.Equals(option.RelicId, cost.RelicId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterCancelButton()
    {
        if (cancelButton == null)
            return;

        cancelButton.onClick.RemoveListener(CancelSelection);
        cancelButton.onClick.AddListener(CancelSelection);
    }

    private static TMP_Text FindText(Transform root, string targetName)
    {
        Transform target = FindChildRecursive(root, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImage(Transform root, string targetName)
    {
        Transform target = FindChildRecursive(root, targetName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static string GetFallbackSlotName(int slotIndex)
    {
        return slotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex
            ? "액티브 유물 슬롯"
            : $"유물 슬롯 {slotIndex + 1}";
    }
}
