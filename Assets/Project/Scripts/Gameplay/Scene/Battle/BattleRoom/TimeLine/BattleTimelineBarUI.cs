using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleTimelineBarUI : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private BattleTimelineController owner;

    [Header("Timeline Slots")]
    [SerializeField] private BattleTimelineGroupUI[] timelineGroups;
    [SerializeField] private BattleTimelineGroupUI[] trailingTimelineGroups;
    [SerializeField] private BattleTimelineGroupUI timelineSlotPrefab;
    [SerializeField] private RectTransform timelineSlotParent;
    [SerializeField, Min(1)] private int timelineSlotCount = 5;
    [SerializeField] private float firstTimelineSlotX = -500f;
    [SerializeField] private float timelineSlotSpacingX = 350f;
    [SerializeField] private float trailingFirstTimelineSlotX = 1130f;
    [SerializeField] private bool generateTimelineSlotsIfMissing = true;

    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private GridManager gridManager;
    private int activeSlotIndex = -1;
    private bool[] slotHasTimelineEntry;

    private void Awake()
    {
        EnsureTimelineGroups();
        EnsureTrailingTimelineGroups();
        InitGroups();
    }

    public void Init(BattleTimelineController owner)
    {
        this.owner = owner;
        EnsureTimelineGroups();
        EnsureTrailingTimelineGroups();
        InitGroups();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
    }

    public bool TryGetOwner(out BattleTimelineController result)
    {
        result = owner;
        return result != null;
    }


    public ReserveTurnSlotUI[] GetOrCreateReserveSlots(BattleTimelineController controller)
    {
        EnsureTimelineGroups();
        InitGroups();

        if (timelineGroups == null || timelineGroups.Length == 0)
            return System.Array.Empty<ReserveTurnSlotUI>();

        ReserveTurnSlotUI[] result = new ReserveTurnSlotUI[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            BattleTimelineGroupUI group = timelineGroups[i];
            if (group == null)
                continue;

            result[i] = group.GetOrCreateReserveTurnSlot(controller, i);
        }

        return result;
    }


    public ReserveTurnSlotUI[] PromoteTrailingTimelineGroupsToCurrent(BattleTimelineController controller)
    {
        EnsureTimelineGroups();
        EnsureTrailingTimelineGroups();

        if (trailingTimelineGroups == null || trailingTimelineGroups.Length == 0)
            return GetOrCreateReserveSlots(controller);

        BattleTimelineGroupUI[] oldCurrent = timelineGroups;
        timelineGroups = trailingTimelineGroups;
        trailingTimelineGroups = oldCurrent;

        activeSlotIndex = -1;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            BattleTimelineGroupUI group = timelineGroups[i];
            if (group == null)
                continue;

            group.name = "TimelineSlot" + (i + 1).ToString("00");

            CanvasGroup canvasGroup = group.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            group.Init(this, i);
            group.Clear();
            group.SetActiveTimelineSlot(false);
            group.SetOwnerIconsVisible(false);
            group.SetEmptyUseSkillSlotsVisible(false);
        }

        ApplyTimelineSlotPositions();

        if (trailingTimelineGroups != null)
        {
            for (int i = 0; i < trailingTimelineGroups.Length; i++)
            {
                BattleTimelineGroupUI group = trailingTimelineGroups[i];
                if (group == null)
                    continue;

                group.name = "TimelineSlotNext" + (i + 1).ToString("00");
                group.Init(this, i);
                group.Clear();
                group.SetActiveTimelineSlot(false);
                group.SetOwnerIconsVisible(false);
                group.SetEmptyUseSkillSlotsVisible(false);
                ConfigureTrailingAsDecoration(group);
                group.PrepareTrailingDecorationVisuals();
                ClearTrailingTurnText(group);
            }
        }

        ApplyTrailingTimelineSlotPositions();
        InitGroups();

        return GetOrCreateReserveSlots(controller);
    }

    public void SetActiveTimelineSlot(int slotIndex)
    {
        activeSlotIndex = slotIndex;

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].SetActiveTimelineSlot(i == activeSlotIndex);
        }
    }

    public void SetPlayerLockedSlot(int lockedSlotIndex)
    {
        EnsureTimelineGroups();

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            BattleTimelineGroupUI group = timelineGroups[i];

            if (group == null)
                continue;

            BattleTimelineLockedSlotOverlay overlay =
                group.GetComponent<BattleTimelineLockedSlotOverlay>();

            if (overlay == null)
                overlay = group.gameObject.AddComponent<BattleTimelineLockedSlotOverlay>();

            overlay.SetLocked(i == lockedSlotIndex);
        }
    }
    public void Refresh(
        ReserveTurnSlotUI[] reserveSlots,
        IReadOnlyList<MonsterReservedCommand>[] monsterCommandsBySlot)
    {
        EnsureTimelineGroups();
        InitGroups();

        if (timelineGroups == null)
        {
            Debug.LogWarning("[BattleTimelineBarUI] timelineGroups null");
            return;
        }

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            List<BattleTimelinePreviewEntry> entries = new();

            List<PlayerReservedCommand> playerCommands = new();

            if (reserveSlots != null && i < reserveSlots.Length && reserveSlots[i] != null)
            {
                var commands = reserveSlots[i].Commands;

                for (int j = 0; j < commands.Count; j++)
                {
                    if (commands[j] != null)
                        playerCommands.Add(commands[j]);
                }
            }

            int orderIndex = 0;

            for (int j = 0; j < playerCommands.Count; j++)
            {
                PlayerReservedCommand command = playerCommands[j];

                if (!BattleActionOrderUtility.HasSwift(command))
                    continue;

                BattleTimelinePreviewEntry entry =
                    BattleTimelinePreviewEntry.CreatePlayer(i, orderIndex, command, j);

                if (entry != null)
                    entries.Add(entry);

                orderIndex++;
            }

            if (monsterCommandsBySlot != null &&
                i < monsterCommandsBySlot.Length &&
                monsterCommandsBySlot[i] != null)
            {
                var monsterCommands = monsterCommandsBySlot[i];

                for (int j = 0; j < monsterCommands.Count; j++)
                {
                    BattleTimelinePreviewEntry entry =
                        BattleTimelinePreviewEntry.CreateMonster(i, orderIndex, monsterCommands[j]);

                    if (entry != null)
                        entries.Add(entry);

                    orderIndex++;
                }
            }

            for (int j = 0; j < playerCommands.Count; j++)
            {
                PlayerReservedCommand command = playerCommands[j];

                if (BattleActionOrderUtility.HasSwift(command))
                    continue;

                BattleTimelinePreviewEntry entry =
                    BattleTimelinePreviewEntry.CreatePlayer(i, orderIndex, command, j);

                if (entry != null)
                    entries.Add(entry);

                orderIndex++;
            }

            bool hasEntry = entries.Count > 0;
            slotHasTimelineEntry[i] = hasEntry;

            if (timelineGroups[i] != null)
            {
                timelineGroups[i].SetTimelineEntries(entries, i);

                SetTurnMarkChildrenVisible(timelineGroups[i], true);
            }
        }
    }


    public void HideOwnerIconsForSlot(int slotIndex)
    {
        EnsureTimelineGroups();

        if (timelineGroups == null || slotIndex < 0 || slotIndex >= timelineGroups.Length)
            return;

        BattleTimelineGroupUI group = timelineGroups[slotIndex];
        if (group != null)
            group.SetOwnerIconsVisible(false);
    }

    public void SetTurnMarkChildrenVisible(bool visible)
    {
        EnsureTimelineGroups();

        if (timelineGroups == null)
            return;

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            SetTurnMarkChildrenVisible(timelineGroups[i], visible);
        }
    }

    private void SetTurnMarkChildrenVisible(BattleTimelineGroupUI group, bool visible)
    {
        if (group == null)
            return;

        Transform turnMark = FindChildRecursive(group.transform, "TurnMark");

        if (turnMark == null)
            return;

        for (int childIndex = 0; childIndex < turnMark.childCount; childIndex++)
        {
            Transform child = turnMark.GetChild(childIndex);

            if (child == null)
                continue;

            bool isOwnerIcon = child.name == "Player_Icon" || child.name == "Enemy_Icon";

            if (!visible)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (!isOwnerIcon)
                child.gameObject.SetActive(true);
        }
    }

    public void SetEmptyUseSkillSlotsVisible(bool visible)
    {
        EnsureTimelineGroups();

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].SetEmptyUseSkillSlotsVisible(visible);
        }
    }

    public void Clear()
    {
        EnsureTimelineGroups();

        if (timelineGroups == null)
            return;

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            slotHasTimelineEntry[i] = false;

            if (timelineGroups[i] != null)
            {
                timelineGroups[i].Clear();
                SetGroupPlayerLocked(timelineGroups[i], false);
                SetTurnMarkChildrenVisible(timelineGroups[i], false);
            }
        }
    }

    private void SetGroupPlayerLocked(BattleTimelineGroupUI group, bool locked)
    {
        if (group == null)
            return;

        BattleTimelineLockedSlotOverlay overlay =
            group.GetComponent<BattleTimelineLockedSlotOverlay>();

        if (overlay == null)
        {
            if (!locked)
                return;

            overlay = group.gameObject.AddComponent<BattleTimelineLockedSlotOverlay>();
        }

        overlay.SetLocked(locked);
    }
    private void InitGroups()
    {
        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] == null)
                continue;

            timelineGroups[i].Init(this, i);

            ReserveTurnSlotUI clickSlot = timelineGroups[i].GetComponent<ReserveTurnSlotUI>();

            if (clickSlot == null)
                clickSlot = timelineGroups[i].GetComponentInChildren<ReserveTurnSlotUI>(true);

            if (clickSlot != null)
                clickSlot.Init(owner, i);
        }
    }

    private void EnsureTimelineGroups()
    {
        if (timelineSlotParent == null)
            timelineSlotParent = transform as RectTransform;

        List<BattleTimelineGroupUI> groups = FindExistingTimelineGroups();

        if (groups.Count == 0 && generateTimelineSlotsIfMissing && timelineSlotPrefab != null)
        {
            RectTransform parent = timelineSlotParent != null
                ? timelineSlotParent
                : transform as RectTransform;

            for (int i = 0; i < Mathf.Max(1, timelineSlotCount); i++)
            {
                BattleTimelineGroupUI instance = Instantiate(timelineSlotPrefab, parent);
                instance.name = "TimelineSlot" + (i + 1).ToString("00");
                groups.Add(instance);
            }
        }

        int expectedCount = Mathf.Max(1, timelineSlotCount);
        if (groups.Count > expectedCount)
            groups.RemoveRange(expectedCount, groups.Count - expectedCount);

        timelineGroups = groups.ToArray();
        ApplyTimelineSlotPositions();
    }

    private List<BattleTimelineGroupUI> FindExistingTimelineGroups()
    {
        List<BattleTimelineGroupUI> groups = new();

        for (int i = 1; i <= Mathf.Max(1, timelineSlotCount); i++)
        {
            Transform found = FindChildRecursive(transform, "TimelineSlot" + i.ToString("00"));

            if (found == null)
                found = FindChildRecursive(transform, "TimelineSlot" + i);

            if (found == null)
                continue;

            BattleTimelineGroupUI group = found.GetComponent<BattleTimelineGroupUI>();

            if (group == null)
                group = found.gameObject.AddComponent<BattleTimelineGroupUI>();

            groups.Add(group);
        }

        if (groups.Count == 0 && timelineGroups != null)
        {
            for (int i = 0; i < timelineGroups.Length; i++)
            {
                if (timelineGroups[i] != null && !groups.Contains(timelineGroups[i]))
                    groups.Add(timelineGroups[i]);
            }
        }

        return groups;
    }

    private void EnsureTrailingTimelineGroups()
    {
        if (timelineSlotPrefab == null)
            return;

        if (timelineSlotParent == null)
            timelineSlotParent = transform as RectTransform;

        int count = Mathf.Max(1, timelineSlotCount);
        List<BattleTimelineGroupUI> groups = new();

        for (int i = 0; i < count; i++)
        {
            Transform found = FindChildRecursive(transform, "TimelineSlotNext" + (i + 1).ToString("00"));
            BattleTimelineGroupUI group = found != null
                ? found.GetComponent<BattleTimelineGroupUI>()
                : null;

            if (group == null)
            {
                RectTransform parent = timelineSlotParent != null
                    ? timelineSlotParent
                    : transform as RectTransform;

                group = Instantiate(timelineSlotPrefab, parent);
                group.name = "TimelineSlotNext" + (i + 1).ToString("00");
            }

            group.Init(this, i);
            group.Clear();
            group.SetActiveTimelineSlot(false);
            group.SetOwnerIconsVisible(false);
            group.SetEmptyUseSkillSlotsVisible(false);
            ConfigureTrailingAsDecoration(group);
            group.PrepareTrailingDecorationVisuals();
            ClearTrailingTurnText(group);
            groups.Add(group);
        }

        trailingTimelineGroups = groups.ToArray();
        ApplyTrailingTimelineSlotPositions();
    }

    private void ApplyTrailingTimelineSlotPositions()
    {
        if (trailingTimelineGroups == null)
            return;

        for (int i = 0; i < trailingTimelineGroups.Length; i++)
        {
            BattleTimelineGroupUI group = trailingTimelineGroups[i];
            if (group == null)
                continue;

            RectTransform rect = group.transform as RectTransform;
            if (rect == null)
                continue;

            Vector2 position = rect.anchoredPosition;
            position.x = trailingFirstTimelineSlotX + timelineSlotSpacingX * i;
            rect.anchoredPosition = position;
        }
    }

    private static void ConfigureTrailingAsDecoration(BattleTimelineGroupUI group)
    {
        if (group == null)
            return;

        CanvasGroup canvasGroup = group.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = group.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ClearTrailingTurnText(BattleTimelineGroupUI group)
    {
        if (group == null)
            return;

        Transform turnTextTransform = FindChildRecursive(group.transform, "Turn_Text");
        if (turnTextTransform == null)
            return;

        TMPro.TMP_Text turnText = turnTextTransform.GetComponent<TMPro.TMP_Text>();
        if (turnText == null)
            return;

        turnText.text = string.Empty;
        turnText.gameObject.SetActive(true);
    }

    private void ApplyTimelineSlotPositions()
    {
        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            BattleTimelineGroupUI group = timelineGroups[i];
            if (group == null)
                continue;

            RectTransform rect = group.transform as RectTransform;
            if (rect == null)
                continue;

            Vector2 position = rect.anchoredPosition;
            position.x = firstTimelineSlotX + timelineSlotSpacingX * i;
            rect.anchoredPosition = position;
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    public void OnOrderClicked(int slotIndex, int orderIndex)
    {
        if (owner != null)
            owner.RemoveCommand(slotIndex, orderIndex);
    }

    public void OnEntryClicked(BattleTimelinePreviewEntry entry)
    {
        if (entry == null)
            return;

        if (!entry.IsPlayer)
            return;

        if (owner != null)
            owner.RemoveCommand(entry.SlotIndex, entry.PlayerCommandIndex);
    }

    public void ShowEntryRangePreview(BattleTimelinePreviewEntry entry)
    {
        if (entry == null || rangePreview == null || gridManager == null)
            return;

        if (!entry.IsPlayer || entry.PlayerCommand == null)
            return;

        PlayerReservedCommand command = entry.PlayerCommand;

        if (command.SkillData == null || command.UserRuntime == null)
            return;

        int casterGridIndex = owner.GetPreviewGridIndexBeforeCommand(
            command.UserRuntime,
            entry.SlotIndex,
            entry.PlayerCommandIndex
        );

        if (casterGridIndex < 0)
            return;

        List<int> rangeIndices = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                command.Direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }
        else if (command.SkillData.RangeType == RangeType.Selection)
        {
            rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                command.SelectedGridIndex >= 0 ? command.SelectedGridIndex : casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }
        else
        {
            rangeIndices = command.RangeGridIndices;
        }

        rangePreview.ShowDirectionCells(rangeIndices);
    }

    public void ClearEntryRangePreview()
    {
        if (rangePreview != null)
            rangePreview.Clear();
    }
}
public class BattleTimelineLockedSlotOverlay : MonoBehaviour
{
    private const string DefaultOverlayObjectName = "CobwebSlotLock";
    private const string DefaultEditorSpritePath = "Assets/Project/Art/Image/UI/Battle/CobwebUI.png";

    [SerializeField] private Image overlayImage;
    [SerializeField] private Sprite overlaySprite;
    [SerializeField, Range(0f, 1f)] private float overlayAlpha = 0.55f;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        Refresh();
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        Refresh();
    }

    private void Refresh()
    {
        EnsureOverlayImage();

        if (overlayImage == null)
            return;

        bool visible = IsLocked && overlaySprite != null;
        overlayImage.sprite = overlaySprite;
        overlayImage.preserveAspect = false;
        overlayImage.raycastTarget = false;
        overlayImage.color = new Color(1f, 1f, 1f, overlayAlpha);
        overlayImage.enabled = visible;
        overlayImage.gameObject.SetActive(visible);
        overlayImage.transform.SetAsLastSibling();
    }

    private void EnsureOverlayImage()
    {
        ResolveOverlaySpriteIfNeeded();

        if (overlayImage == null)
        {
            Transform found = FindChildRecursive(transform, DefaultOverlayObjectName);

            if (found != null)
                overlayImage = found.GetComponent<Image>();
        }

        if (overlayImage != null)
        {
            ApplyOverlayRectTransformSettings(overlayImage.rectTransform);
            overlayImage.raycastTarget = false;
            return;
        }

        RectTransform parentRect = transform as RectTransform;

        if (parentRect == null)
            return;

        GameObject overlayObject = new GameObject(
            DefaultOverlayObjectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        overlayObject.layer = gameObject.layer;
        overlayObject.transform.SetParent(transform, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        ApplyOverlayRectTransformSettings(overlayRect);

        overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.raycastTarget = false;
    }

    private static void ApplyOverlayRectTransformSettings(RectTransform overlayRect)
    {
        if (overlayRect == null)
            return;

        Vector2 middleCenter = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMin = middleCenter;
        overlayRect.anchorMax = middleCenter;
        overlayRect.pivot = middleCenter;
        overlayRect.anchoredPosition = new Vector2(160f, 0f);
        overlayRect.sizeDelta = new Vector2(300f, 80f);
    }

    private void ResolveOverlaySpriteIfNeeded()
    {
        if (overlaySprite != null)
            return;

#if UNITY_EDITOR
        overlaySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DefaultEditorSpritePath);
#endif
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }
}
