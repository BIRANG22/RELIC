using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Data-driven TutorialGuide index list and per-index page viewer.</summary>
public sealed class TutorialGuideController : MonoBehaviour
{
    private const string UnlockKey = "Dustium.TutorialGuide.HighestUnlockedIndex";
    private const string IndividualKeyPrefix = "Dustium.TutorialGuide.Seen.";
    private const string MigrationKey = "Dustium.TutorialGuide.SeenMigrated";
    [Header("Auto-bind by the supplied TutorialGuidePanel hierarchy")]
    [SerializeField] private Transform indexContent;
    [Header("Assign the new TutorialIndex prefab (not a child in Content)")]
    [SerializeField] private GameObject tutorialIndexPrefab;
    [SerializeField] private TMP_Text infoName;
    [SerializeField] private Image infoPreview;
    [SerializeField] private TMP_Text infoExplanation;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private Button beforeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button cancelButton;
    [Header("Preview sprites (assign a TutorialGuideSpriteDatabase asset)")]
    [SerializeField] private TutorialGuideSpriteDatabase spriteDatabase;

    [Header("Index appearance")]
    [SerializeField] private Color lockedIndexTextColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    [SerializeField] private Color unlockedIndexTextColor = Color.white;
    [SerializeField] private Color selectedIndexBackColor = new Color32(0x24, 0x39, 0x52, 0xFF);
    [SerializeField] private Color hoveredIndexBackColor = new Color32(0x1A, 0x23, 0x2E, 0xFF);

    private readonly List<GameObject> generatedIndexes = new();
    private readonly Dictionary<string, Image> indexBacks = new();
    private readonly Dictionary<string, Color> indexDefaultBackColors = new();
    private string hoveredIndex;
    private string selectedIndex;
    private int selectedPage;
    private bool bound;
    private GameObject modalBlocker;
    // OnMouseEnter is driven by Physics, not UI GraphicRaycaster.
    // World hover scripts check this flag while the guide is open.
    public static bool IsModalOpen { get; private set; }


    public static int HighestUnlockedIndex => PlayerPrefs.GetInt(UnlockKey, 0);

    private static string GetSeenKey(string indexId) => IndividualKeyPrefix + indexId.Trim();

    // One-time migration from the previous version that tracked a highest unlocked index.
    // New unlocks use individual flags, so unlocking Index03 does not implicitly unlock Index02.
    private static void MigrateLegacyProgress()
    {
        if (PlayerPrefs.GetInt(MigrationKey, 0) != 0) return;
        int legacyHighest = HighestUnlockedIndex;
        for (int number = 1; number <= legacyHighest; number++)
            PlayerPrefs.SetInt(GetSeenKey($"Index{number:00}"), 1);
        PlayerPrefs.SetInt(MigrationKey, 1);
        PlayerPrefs.Save();
    }

    // Called by the full progress reset shortcut. Guide unlocks are stored in
    // PlayerPrefs separately from the save file and tutorial-seen flag.
    public static void ResetSeenProgress()
    {
        // Index IDs use the IndexNN format. Clear existing and future guide
        // flags without depending on whether the database is initialized yet.
        for (int number = 1; number <= 999; number++)
            PlayerPrefs.DeleteKey(GetSeenKey($"Index{number:00}"));
        PlayerPrefs.DeleteKey(UnlockKey);
        PlayerPrefs.DeleteKey(MigrationKey);
        PlayerPrefs.Save();
    }

    public static bool HasSeenIndex(string indexId)
    {
        if (string.IsNullOrWhiteSpace(indexId)) return false;
        MigrateLegacyProgress();
        return PlayerPrefs.GetInt(GetSeenKey(indexId), 0) == 1;
    }

    public static void UnlockIndex(string indexId)
    {
        if (string.IsNullOrWhiteSpace(indexId) || TutorialGuideDatabase.ParseIndexNumber(indexId) < 1) return;
        MigrateLegacyProgress();
        if (HasSeenIndex(indexId)) return;
        PlayerPrefs.SetInt(GetSeenKey(indexId), 1);
        int number = TutorialGuideDatabase.ParseIndexNumber(indexId);
        if (number > HighestUnlockedIndex) PlayerPrefs.SetInt(UnlockKey, number);
        PlayerPrefs.Save();
    }

    public void OpenIndex(string indexId)
    {
        UnlockIndex(indexId);
        gameObject.SetActive(true);
        EnsureBound();
        RebuildIndexes();
        SelectIndex(indexId);
    }

    private void Awake() => EnsureBound();

    private void Start() => CreateModalBlocker();

    private void CreateModalBlocker()
    {
        if (modalBlocker != null || transform.parent == null) return;
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogWarning("[TutorialGuideController] Parent must be a Canvas RectTransform for modal raycast blocking.", this);
            return;
        }

        // A full-screen invisible UI graphic behind the guide, but in front of battle UI.
        // Do not put it inside Background: a centered background image may not cover the screen.
        modalBlocker = new GameObject("__TutorialGuideModalBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var blockRect = (RectTransform)modalBlocker.transform;
        blockRect.SetParent(parentRect, false);
        blockRect.anchorMin = Vector2.zero;
        blockRect.anchorMax = Vector2.one;
        blockRect.offsetMin = Vector2.zero;
        blockRect.offsetMax = Vector2.zero;
        blockRect.localScale = Vector3.one;
        var image = modalBlocker.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        // Guarantee the blocker and the panel are above siblings in the same Canvas.
        blockRect.SetAsLastSibling();
        transform.SetAsLastSibling();
    }

    private void OnEnable()
    {
        IsModalOpen = true;
        if (modalBlocker != null) modalBlocker.SetActive(true);
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        EnsureBound();
        RebuildIndexes();
        if (string.IsNullOrEmpty(selectedIndex))
        {
            var database = DataManager.Instance?.TutorialGuideDatabase;
            if (database != null)
                foreach (string index in database.IndexIds)
                    if (HasSeenIndex(index))
                    { SelectIndex(index); break; }
        }
        RefreshPage();
    }
    private void OnDisable()
    {
        IsModalOpen = false;
        if (modalBlocker != null) modalBlocker.SetActive(false);
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }
    private void OnDestroy()
    {
        IsModalOpen = false;
        if (modalBlocker != null) Destroy(modalBlocker);
        if (beforeButton != null) beforeButton.onClick.RemoveListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextPage);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
    }

    private void EnsureBound()
    {
        if (bound) return;
        Transform root = transform;
        indexContent ??= Find(root, "Index/Viewport/Content");
        // Remove the old embedded Index01 template if it remains in an older panel prefab.
        // Content should be empty in the new prefab; only seen indexes are instantiated.
        if (indexContent != null)
        {
            Transform legacy = indexContent.Find("Index01");
            if (legacy != null && !generatedIndexes.Contains(legacy.gameObject))
            {
                legacy.gameObject.SetActive(false);
                Destroy(legacy.gameObject);
            }
        }
        infoName ??= Find(root, "Info/Name")?.GetComponent<TMP_Text>();
        infoPreview ??= Find(root, "Info/Preview")?.GetComponent<Image>();
        infoExplanation ??= Find(root, "Info/Explanation")?.GetComponent<TMP_Text>();
        pageLabel ??= Find(root, "Page/Page")?.GetComponent<TMP_Text>();
        beforeButton ??= Find(root, "Page/BeforeButton")?.GetComponent<Button>();
        nextButton ??= Find(root, "Page/NextButton")?.GetComponent<Button>();
        cancelButton ??= Find(root, "CancelButton")?.GetComponent<Button>();
        if (beforeButton != null) beforeButton.onClick.AddListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        Protect(infoName); Protect(infoExplanation); Protect(pageLabel);
        bound = true;
    }

    private void RebuildIndexes()
    {
        foreach (GameObject obj in generatedIndexes) if (obj != null) Destroy(obj);
        generatedIndexes.Clear();
        indexBacks.Clear();
        indexDefaultBackColors.Clear();
        hoveredIndex = null;
        if (indexContent == null || tutorialIndexPrefab == null)
        {
            Debug.LogWarning("[TutorialGuideController] Assign Tutorial Index Prefab and Index/Viewport/Content.", this);
            return;
        }
        TutorialGuideDatabase db = DataManager.Instance?.TutorialGuideDatabase;
        if (db == null) return;
        foreach (string indexId in db.IndexIds)
        {
            bool seen = HasSeenIndex(indexId);
            string capturedId = indexId;
            GameObject item = Instantiate(tutorialIndexPrefab, indexContent, false);
            item.name = indexId;
            item.SetActive(true);
            TMP_Text text = Find(item.transform, "Text")?.GetComponent<TMP_Text>();
            if (text == null) text = item.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                Protect(text);
                if (seen)
                {
                    var indexInfo = db.GetIndexInfo(indexId);
                    string fallback = indexInfo?.IndexNameKO;
                    if (string.IsNullOrWhiteSpace(fallback)) fallback = indexId;
                    SetLocalized(text, indexInfo?.IndexNameKey, fallback);
                    text.color = unlockedIndexTextColor;
                }
                else
                {
                    text.text = "???";
                    text.color = lockedIndexTextColor;
                }
            }

            Image back = Find(item.transform, "Back")?.GetComponent<Image>();
            if (back != null)
            {
                indexBacks[indexId] = back;
                indexDefaultBackColors[indexId] = back.color;
            }
            Button button = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                // Locked indices remain visible but cannot open unrevealed information.
                button.interactable = seen;
                if (seen) button.onClick.AddListener(() => SelectIndex(capturedId));
                // Avoid Button tint overriding the inspector-controlled Back color.
                button.transition = Selectable.Transition.None;
            }
            if (seen)
            {
                EventTrigger trigger = item.GetComponent<EventTrigger>();
                if (trigger == null) trigger = item.AddComponent<EventTrigger>();
                AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => SetIndexHovered(capturedId, true));
                AddPointerEvent(trigger, EventTriggerType.PointerExit, () => SetIndexHovered(capturedId, false));
            }
            generatedIndexes.Add(item);
        }
        RefreshIndexBackColors();
    }

    private static void AddPointerEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private void SetIndexHovered(string indexId, bool entered)
    {
        if (entered) hoveredIndex = indexId;
        else if (hoveredIndex == indexId) hoveredIndex = null;
        RefreshIndexBackColors();
    }

    private void RefreshIndexBackColors()
    {
        foreach (var pair in indexBacks)
        {
            if (pair.Value == null) continue;
            string id = pair.Key;
            if (id == selectedIndex && HasSeenIndex(id))
                pair.Value.color = selectedIndexBackColor;
            else if (id == hoveredIndex && HasSeenIndex(id))
                pair.Value.color = hoveredIndexBackColor;
            else
                pair.Value.color = indexDefaultBackColors[id];
        }
    }

    public void SelectIndex(string indexId)
    {
        if (!HasSeenIndex(indexId)) return;
        var pages = DataManager.Instance?.TutorialGuideDatabase?.GetPages(indexId);
        if (pages == null || pages.Count == 0) return;
        selectedIndex = indexId;
        selectedPage = 0;
        RefreshPage();
        RefreshIndexBackColors();
    }
    public void ShowPreviousPage() { selectedPage--; RefreshPage(); }
    public void ShowNextPage() { selectedPage++; RefreshPage(); }
    public void Close() => Destroy(gameObject);

    private void RefreshPage()
    {
        var pages = DataManager.Instance?.TutorialGuideDatabase?.GetPages(selectedIndex);
        int count = pages?.Count ?? 0;
        if (count == 0)
        {
            if (infoName != null) infoName.text = string.Empty;
            if (infoExplanation != null) infoExplanation.text = string.Empty;
            if (infoPreview != null) { infoPreview.sprite = null; infoPreview.enabled = false; }
            if (pageLabel != null) pageLabel.text = "0/0";
            if (beforeButton != null) beforeButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            return;
        }
        selectedPage = Mathf.Clamp(selectedPage, 0, count - 1);
        TutorialGuideData page = pages[selectedPage];
        if (infoName != null) SetLocalized(infoName, page.PageNameKey, page.PageNameKO);
        if (infoExplanation != null) SetLocalized(infoExplanation, page.ExplanationKey, page.ExplanationKO);
        Sprite sprite = spriteDatabase != null ? spriteDatabase.GetSprite(selectedIndex, page.Page) : null;
        if (infoPreview != null) { infoPreview.sprite = sprite; infoPreview.enabled = sprite != null; }
        if (pageLabel != null) pageLabel.text = $"{selectedPage + 1}/{count}";
        if (beforeButton != null) beforeButton.interactable = selectedPage > 0;
        if (nextButton != null) nextButton.interactable = selectedPage < count - 1;
    }
    private static void SetLocalized(TMP_Text target, string key, string fallback)
    {
        target.text = string.IsNullOrWhiteSpace(key) ? fallback ?? string.Empty : GameLocalization.Get(key, fallback ?? string.Empty);
        LocaleTMPFontGuard.Prepare(target);
    }
    private void HandleLocaleChanged(Locale _) { RebuildIndexes(); RefreshPage(); }
    private static Transform Find(Transform root, string path) => root != null ? root.Find(path) : null;
    private static void Protect(TMP_Text target)
    {
        if (target == null) return;
        if (target.GetComponent<LocalizationAutoBindingIgnore>() == null) target.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        var old = target.GetComponent<LocalizedTMPText>(); if (old != null) old.enabled = false;
        var dynamic = target.GetComponent<DynamicLocalizedTMPText>(); if (dynamic != null) dynamic.enabled = false;
        var local = target.GetComponent<LocalizeStringEvent>(); if (local != null) local.enabled = false;
    }
}
