using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Data-driven TutorialGuide index list and per-index page viewer.</summary>
public sealed class TutorialGuideController : MonoBehaviour
{
    private const string UnlockKey = "Dustium.TutorialGuide.HighestUnlockedIndex";
    [Header("Auto-bind by the supplied TutorialGuidePanel hierarchy")]
    [SerializeField] private Transform indexContent;
    [SerializeField] private GameObject index01Template;
    [SerializeField] private TMP_Text infoName;
    [SerializeField] private Image infoPreview;
    [SerializeField] private TMP_Text infoExplanation;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private Button beforeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button cancelButton;
    [Header("Preview sprites (assign a TutorialGuideSpriteDatabase asset)")]
    [SerializeField] private TutorialGuideSpriteDatabase spriteDatabase;

    private readonly List<GameObject> generatedIndexes = new();
    private string selectedIndex;
    private int selectedPage;
    private bool bound;

    public static int HighestUnlockedIndex => PlayerPrefs.GetInt(UnlockKey, 0);
    public static void UnlockIndex(string indexId)
    {
        int number = TutorialGuideDatabase.ParseIndexNumber(indexId);
        if (number < 1 || number <= HighestUnlockedIndex) return;
        PlayerPrefs.SetInt(UnlockKey, number);
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
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        EnsureBound();
        RebuildIndexes();
        if (string.IsNullOrEmpty(selectedIndex))
        {
            var database = DataManager.Instance?.TutorialGuideDatabase;
            if (database != null)
                foreach (string index in database.IndexIds)
                    if (TutorialGuideDatabase.ParseIndexNumber(index) <= HighestUnlockedIndex)
                    { SelectIndex(index); break; }
        }
        RefreshPage();
    }
    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    private void OnDestroy()
    {
        if (beforeButton != null) beforeButton.onClick.RemoveListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextPage);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
    }

    private void EnsureBound()
    {
        if (bound) return;
        Transform root = transform;
        indexContent ??= Find(root, "Index/Viewport/Content");
        index01Template ??= Find(indexContent, "Index01")?.gameObject;
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
        // The original Index01 is the inactive template, not a visible index.
        if (index01Template != null) index01Template.SetActive(false);
        bound = true;
    }

    private void RebuildIndexes()
    {
        foreach (GameObject obj in generatedIndexes) if (obj != null) Destroy(obj);
        generatedIndexes.Clear();
        if (index01Template == null || indexContent == null) return;
        TutorialGuideDatabase db = DataManager.Instance?.TutorialGuideDatabase;
        if (db == null) return;
        foreach (string indexId in db.IndexIds)
        {
            if (TutorialGuideDatabase.ParseIndexNumber(indexId) > HighestUnlockedIndex) continue;
            string capturedId = indexId;
            GameObject item = Instantiate(index01Template, indexContent, false);
            item.name = indexId;
            item.SetActive(true);
            TMP_Text text = Find(item.transform, "Text")?.GetComponent<TMP_Text>();
            if (text != null)
            {
                Protect(text);
                var indexInfo = db.GetIndexInfo(indexId);
                string fallback = indexInfo?.IndexNameKO;
                if (string.IsNullOrWhiteSpace(fallback)) fallback = indexId;
                SetLocalized(text, indexInfo?.IndexNameKey, fallback);
            }
            Button button = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>(true);
            if (button != null) button.onClick.AddListener(() => SelectIndex(capturedId));
            generatedIndexes.Add(item);
        }
        index01Template.SetActive(false);
    }

    public void SelectIndex(string indexId)
    {
        if (TutorialGuideDatabase.ParseIndexNumber(indexId) > HighestUnlockedIndex) return;
        var pages = DataManager.Instance?.TutorialGuideDatabase?.GetPages(indexId);
        if (pages == null || pages.Count == 0) return;
        selectedIndex = indexId;
        selectedPage = 0;
        RefreshPage();
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
