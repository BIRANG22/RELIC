using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public readonly struct EventSkillAwakenSelectionPanelEntry
{
    public EventSkillAwakenSelectionPanelEntry(
        EventChoiceSkillAwakenTarget target,
        string characterName,
        string slotName,
        string skillName,
        string upgradeSkillName,
        Sprite icon = null)
    {
        Target = target;
        CharacterName = Normalize(characterName);
        SlotName = Normalize(slotName);
        SkillName = Normalize(skillName);
        UpgradeSkillName = Normalize(upgradeSkillName);
        Icon = icon;
    }

    public EventChoiceSkillAwakenTarget Target { get; }
    public string CharacterName { get; }
    public string SlotName { get; }
    public string SkillName { get; }
    public string UpgradeSkillName { get; }
    public Sprite Icon { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public sealed class EventSkillAwakenSelectionPanelUI : MonoBehaviour
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

    private readonly List<EventSkillAwakenSelectionPanelEntry> entries = new();
    private readonly List<GameObject> optionObjects = new();
    private Func<EventChoiceSkillAwakenTarget, bool> selectedCallback;
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
        IEnumerable<EventSkillAwakenSelectionPanelEntry> candidates,
        Func<EventChoiceSkillAwakenTarget, bool> onSelected,
        Action onCancelled)
    {
        if (!HasRequiredSceneReferences())
        {
            Close();
            return false;
        }

        gameObject.SetActive(true);
        RegisterCancelButton();

        entries.Clear();
        if (candidates != null)
        {
            foreach (EventSkillAwakenSelectionPanelEntry entry in candidates)
            {
                if (entry.Target.IsValid)
                    entries.Add(entry);
            }
        }

        selectedCallback = onSelected;
        cancelCallback = onCancelled;

        optionTemplate.SetActive(false);
        panelRoot.SetActive(true);
        transform.SetAsLastSibling();
        RefreshOptions();
        return true;
    }

    public bool TrySelect(EventChoiceSkillAwakenTarget target)
    {
        if (!IsOpen || !ContainsTarget(target))
            return false;

        Func<EventChoiceSkillAwakenTarget, bool> callback = selectedCallback;
        bool accepted = callback == null || callback.Invoke(target);
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
        entries.Clear();
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
            titleText.text = "강화할 기억 선택";

        if (emptyText != null)
            emptyText.gameObject.SetActive(entries.Count == 0);

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject optionObject = CreateOptionObject(entries[i], i);
            optionObjects.Add(optionObject);
            VisibleOptionCount++;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private GameObject CreateOptionObject(EventSkillAwakenSelectionPanelEntry entry, int optionIndex)
    {
        GameObject optionObject = Instantiate(optionTemplate, contentRoot);
        optionObject.name = "SkillAwakenOption";
        optionObject.SetActive(true);
        PositionOptionObject(optionObject, optionIndex);

        TMP_Text skillNameText = FindText(optionObject.transform, "SkillNameText");
        if (skillNameText == null)
            skillNameText = FindText(optionObject.transform, "RelicNameText");
        TMP_Text characterNameText = FindText(optionObject.transform, "CharacterNameText");
        TMP_Text slotNameText = FindText(optionObject.transform, "SlotNameText");
        TMP_Text upgradeNameText = FindText(optionObject.transform, "UpgradeNameText");
        Image iconImage = FindImage(optionObject.transform, "Icon");

        if (skillNameText != null)
            skillNameText.text = string.IsNullOrWhiteSpace(entry.SkillName)
                ? entry.Target.SkillId
                : entry.SkillName;

        if (characterNameText != null)
            characterNameText.text = entry.CharacterName;

        if (slotNameText != null)
            slotNameText.text = entry.SlotName;

        if (upgradeNameText != null)
            upgradeNameText.text = string.IsNullOrWhiteSpace(entry.UpgradeSkillName)
                ? entry.Target.UpgradeSkillId
                : entry.UpgradeSkillName;

        if (iconImage != null)
        {
            iconImage.sprite = entry.Icon;
            iconImage.enabled = entry.Icon != null;
        }

        Button button = optionObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            EventChoiceSkillAwakenTarget captured = entry.Target;
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

    private bool ContainsTarget(EventChoiceSkillAwakenTarget target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            EventChoiceSkillAwakenTarget option = entries[i].Target;
            if (string.Equals(option.CharacterId, target.CharacterId, StringComparison.Ordinal) &&
                option.SlotKind == target.SlotKind &&
                option.SlotIndex == target.SlotIndex &&
                string.Equals(option.SkillId, target.SkillId, StringComparison.Ordinal) &&
                string.Equals(option.UpgradeSkillId, target.UpgradeSkillId, StringComparison.Ordinal))
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
}
