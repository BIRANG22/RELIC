using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;

/// <summary>
/// ErosionSelect를 클릭하면 선택된 침식도 난이도를 5개씩 배치하고 순차적으로 표시합니다.
/// 다시 클릭하면 모든 항목을 동시에 아래로 이동시키며 숨깁니다.
/// </summary>
public sealed class BattleErosionSelectedLevelsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject erosionLevelPrefab;
    [Tooltip("침식도 아이콘 DB입니다. DataManager에 연결되어 있지 않으면 이곳에 직접 지정하세요.")]
    [SerializeField] private ErosionIconDatabase erosionIconDatabase;

    [Header("Sequential Appearance")]
    [Min(0f)][SerializeField] private float startOffsetY = -40f;
    [Min(0.01f)][SerializeField] private float appearDuration = 0.15f;
    [Min(0f)][SerializeField] private float nextInterval = 0.06f;
    [Min(0.01f)][SerializeField] private float fadeDuration = 0.125f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Simultaneous Disappearance")]
    [Min(0.01f)][SerializeField] private float hideDuration = 0.15f;
    [Min(0f)][SerializeField] private float hideOffsetY = 40f;

    [Header("Erosion Tooltip")]
    [SerializeField] private RectTransform erosionTooltip;
    [SerializeField] private TMP_Text tooltipName;
    [SerializeField] private TMP_Text tooltipDetail;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, 0f);
    [Min(0.01f)][SerializeField] private float tooltipFadeIn = 0.12f;
    [Min(0.01f)][SerializeField] private float tooltipFadeOut = 0.05f;

    private CanvasGroup tooltipGroup;
    private Entry hoveredEntry;
    private float tooltipAlpha;
    private bool tooltipVisible;

    private sealed class Entry
    {
        public GameObject gameObject;
        public RectTransform rect;
        public ErosionData data;
        public CanvasGroup group;
        public RectTransform[] visuals;
        public Vector2[] origins;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake() { Bind(); }

    private void Update()
    {
        UpdateTooltipHover();
        UpdateTooltipFade();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        if (hoveredEntry != null) SetTooltipText(hoveredEntry.data);
    }

    private void OnEnable()
    {
        Bind();
        ResetList();
    }

    private void OnDisable()
    {
        StopAnimation();
        ResetList();
    }

    // Previous external callers may still request a data refresh.
    public void Refresh()
    {
        if (!isActiveAndEnabled) return;
        if (isOpen) Show();
    }

    public void Toggle()
    {
        if (!isActiveAndEnabled) return;
        if (isOpen) Hide();
        else Show();
    }

    public void HideImmediate()
    {
        StopAnimation();
        ResetList();
    }

    private void Show()
    {
        Bind();
        StopAnimation();
        ResetList();
        if (content == null || erosionLevelPrefab == null) return;

        DataManager manager = DataManager.Instance;
        if (manager == null || manager.ErosionDatabase == null) return;
        List<string> ids = manager.BattleRuntimeStore?.Get()?.SelectedErosionDifficultyIds;
        if (ids == null || ids.Count == 0)
            ids = manager.LobbyRuntimeStore?.GetOrCreate()?.SelectedErosionDifficultyIds;
        if (ids == null) return;

        // 템플릿은 Grid Layout Group에서 제외하고 복제본만 셀로 계산합니다.
        erosionLevelPrefab.SetActive(false);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 5;
        }

        foreach (string id in new List<string>(ids))
        {
            if (string.IsNullOrWhiteSpace(id) ||
                !manager.ErosionDatabase.TryGet(id, out ErosionData data) || data == null)
                continue;

            GameObject item = Instantiate(erosionLevelPrefab, content, false);
            item.name = "ErosionLevel_" + data.DifficultyId;
            item.SetActive(true);

            Transform iconTransform = item.transform.Find("Icon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (iconImage != null)
            {
                Sprite icon = ResolveSelectedIcon(data);
                if (iconTransform != null)
                    iconTransform.gameObject.SetActive(true);
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                if (icon != null)
                {
                    Color iconColor = iconImage.color;
                    iconColor.a = 1f;
                    iconImage.color = iconColor;
                    iconImage.preserveAspect = true;
                }

            }

            CanvasGroup group = item.GetComponent<CanvasGroup>();
            if (group == null) group = item.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            entries.Add(new Entry { gameObject = item, rect = item.transform as RectTransform, data = data, group = group });
        }

        // 모든 셀을 먼저 배치하여 등장 도중 줄/열이 바뀌지 않도록 합니다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        foreach (Entry entry in entries)
        {
            int count = entry.gameObject.transform.childCount;
            entry.visuals = new RectTransform[count];
            entry.origins = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform visual = entry.gameObject.transform.GetChild(i) as RectTransform;
                entry.visuals[i] = visual;
                if (visual == null) continue;
                entry.origins[i] = visual.anchoredPosition;
                visual.anchoredPosition = entry.origins[i] + Vector2.down * startOffsetY;
            }
        }
        isOpen = true;
        animationRoutine = StartCoroutine(AnimateShow());
    }


    private Sprite ResolveSelectedIcon(ErosionData data)
    {
        if (data == null) return null;

        // Preserve the icon already displayed in the lobby catalog if available.
        if (ErosionDifficultyCatalogUI.TryGetDisplayedErosionIcon(data.DifficultyId, out Sprite cached) && cached != null)
            return cached;

        // A direct Inspector reference is reliable across scene changes.
        ErosionIconDatabase database = erosionIconDatabase;
        if (database == null && DataManager.Instance != null)
            database = DataManager.Instance.ErosionIconDatabase;

        if (database == null)
        {
            // Only assets placed in a Resources directory can be resolved this way.
            ErosionIconDatabase[] available = Resources.LoadAll<ErosionIconDatabase>(string.Empty);
            if (available != null && available.Length == 1)
                database = available[0];
        }

        if (database != null)
        {
            erosionIconDatabase = database;
            if (database.TryGetIcon(data, out Sprite resolved) && resolved != null)
                return resolved;
            if (database.UnavailableIcon != null)
                return database.UnavailableIcon;
        }

        Debug.LogWarning($"[BattleErosionSelectedLevelsUI] 침식도 아이콘을 찾지 못했습니다. DifficultyId='{data.DifficultyId}', GroupId='{data.GroupId}'. ErosionSelect의 Erosion Icon Database 필드에 DB를 연결하세요.", this);
        return null;
    }

    private IEnumerator AnimateShow()
    {
        for (int index = 0; index < entries.Count; index++)
        {
            Entry entry = entries[index];
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, appearDuration);
            float fade = Mathf.Clamp(fadeDuration, 0.01f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = moveCurve != null && moveCurve.length > 0 ? moveCurve.Evaluate(t) : t;
                SetEntry(entry, -startOffsetY * (1f - eased), Mathf.Clamp01(elapsed / fade));
                yield return null;
            }
            SetEntry(entry, 0f, 1f);
            entry.group.blocksRaycasts = true;
            entry.group.interactable = true;
            if (index + 1 < entries.Count && nextInterval > 0f)
                yield return new WaitForSecondsRealtime(nextInterval);
        }
        animationRoutine = null;
    }

    private void Hide()
    {
        StopAnimation();
        isOpen = false;
        animationRoutine = StartCoroutine(AnimateHide());
    }

    private IEnumerator AnimateHide()
    {
        float duration = Mathf.Max(0.01f, hideDuration);
        float[] startingAlpha = new float[entries.Count];
        float[] startingOffset = new float[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            startingAlpha[i] = entry.group.alpha;
            startingOffset[i] = entry.visuals != null && entry.visuals.Length > 0 && entry.visuals[0] != null
                ? entry.visuals[0].anchoredPosition.y - entry.origins[0].y : 0f;
            entry.group.blocksRaycasts = false;
            entry.group.interactable = false;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = moveCurve != null && moveCurve.length > 0 ? moveCurve.Evaluate(t) : t;
            for (int i = 0; i < entries.Count; i++)
                SetEntry(entries[i], Mathf.Lerp(startingOffset[i], -hideOffsetY, eased), startingAlpha[i] * (1f - t));
            yield return null;
        }
        animationRoutine = null;
        ResetList();
    }

    private static void SetEntry(Entry entry, float offsetY, float alpha)
    {
        entry.group.alpha = alpha;
        if (entry.visuals == null) return;
        for (int i = 0; i < entry.visuals.Length; i++)
            if (entry.visuals[i] != null)
                entry.visuals[i].anchoredPosition = entry.origins[i] + Vector2.up * offsetY;
    }

    private void StopAnimation()
    {
        if (animationRoutine == null) return;
        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private void ResetList()
    {
        isOpen = false;
        foreach (Entry entry in entries)
            if (entry.gameObject != null) Destroy(entry.gameObject);
        entries.Clear();
    }

    private void Bind()
    {
        if (content == null)
        {
            Transform found = transform.Find("Content");
            if (found != null) content = found as RectTransform;
        }
        if (erosionLevelPrefab == null && content != null)
        {
            Transform found = content.Find("ErosionLevel");
            if (found != null) erosionLevelPrefab = found.gameObject;
        }
        if (erosionLevelPrefab != null) erosionLevelPrefab.SetActive(false);
        if (erosionTooltip == null)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            Transform found = root.Find("ErosionTooltip");
            if (found != null) erosionTooltip = found as RectTransform;
        }
        if (erosionTooltip != null)
        {
            if (tooltipName == null)
            {
                Transform name = erosionTooltip.Find("Name");
                if (name != null) tooltipName = name.GetComponent<TMP_Text>();
            }
            if (tooltipDetail == null)
            {
                Transform detail = erosionTooltip.Find("Detail");
                if (detail != null) tooltipDetail = detail.GetComponent<TMP_Text>();
            }
            tooltipGroup = erosionTooltip.GetComponent<CanvasGroup>();
            if (tooltipGroup == null) tooltipGroup = erosionTooltip.gameObject.AddComponent<CanvasGroup>();
            tooltipGroup.blocksRaycasts = false;
            tooltipGroup.interactable = false;
            if (!tooltipVisible)
            {
                tooltipAlpha = 0f;
                tooltipGroup.alpha = 0f;
                erosionTooltip.gameObject.SetActive(false);
            }
            DisableFixedLocalizer(tooltipName);
            DisableFixedLocalizer(tooltipDetail);
        }
    }
    private static void DisableFixedLocalizer(TMP_Text text)
    {
        if (text == null) return;
        if (text.GetComponent<LocalizationIgnore>() == null)
            text.gameObject.AddComponent<LocalizationIgnore>();
        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        if (localizer != null) localizer.enabled = false;
        LocalizeStringEvent legacy = text.GetComponent<LocalizeStringEvent>();
        if (legacy != null) legacy.enabled = false;
    }

    private void UpdateTooltipHover()
    {
        if (!isOpen || erosionTooltip == null)
        {
            if (hoveredEntry != null) HideTooltip(false);
            return;
        }

        Canvas canvas = content != null ? content.GetComponentInParent<Canvas>() : null;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Entry target = null;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.rect == null || e.group == null || !e.group.blocksRaycasts || e.group.alpha < 0.99f) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(e.rect, Input.mousePosition, cam))
            {
                target = e;
                break;
            }
        }
        if (target != hoveredEntry)
        {
            hoveredEntry = target;
            if (target == null) HideTooltip(false);
            else
            {
                tooltipVisible = true;
                erosionTooltip.gameObject.SetActive(true);
                // Populate after activation so fixed localizers and enable hooks cannot
                // overwrite the selected difficulty on the first presentation.
                DisableFixedLocalizer(tooltipName);
                DisableFixedLocalizer(tooltipDetail);
                SetTooltipText(target.data);
            }
        }
        if (hoveredEntry != null)
        {
            // Protect dynamically generated tooltip text from late localization
            // initialization while keeping locale changes handled normally.
            SetTooltipText(hoveredEntry.data);
            PositionTooltip(hoveredEntry.rect);
        }
    }

    private void SetTooltipText(ErosionData data)
    {
        if (data == null) return;
        string name = GameDataLocalization.ErosionName(data);
        if (string.IsNullOrWhiteSpace(name)) name = data.ErosionName;
        int tier = 0;
        string[] parts = (data.DifficultyId ?? string.Empty).Split('_');
        if (parts.Length > 1) int.TryParse(parts[1], out tier);
        if (tier < 1 || tier > 3) tier = data.Tier;
        string roman = tier == 1 ? "I" : tier == 2 ? "II" : tier == 3 ? "III" : string.Empty;
        if (tooltipName != null) tooltipName.text = string.IsNullOrEmpty(roman) ? name : name.Trim() + " " + roman;
        if (tooltipDetail != null) tooltipDetail.text = GameDataLocalization.ErosionDescription(data);
    }

    private void PositionTooltip(RectTransform source)
    {
        if (source == null || erosionTooltip == null) return;
        RectTransform parent = erosionTooltip.parent as RectTransform;
        if (parent == null) return;
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Canvas targetCanvas = erosionTooltip.GetComponentInParent<Canvas>();
        Camera sourceCam = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? sourceCanvas.worldCamera : null;
        Camera targetCam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 rightCenter = (corners[2] + corners[3]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCam, rightCenter);
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screen, targetCam, out Vector3 world)) return;
        Vector3 localOffset = parent.TransformVector(new Vector3(tooltipOffset.x, tooltipOffset.y, 0f));
        erosionTooltip.position = world + localOffset;
    }

    private void HideTooltip(bool immediate)
    {
        hoveredEntry = null;
        tooltipVisible = false;
        if (immediate)
        {
            tooltipAlpha = 0f;
            if (tooltipGroup != null) tooltipGroup.alpha = 0f;
            if (erosionTooltip != null) erosionTooltip.gameObject.SetActive(false);
        }
    }

    private void UpdateTooltipFade()
    {
        if (tooltipGroup == null || erosionTooltip == null) return;
        float duration = tooltipVisible ? tooltipFadeIn : tooltipFadeOut;
        float target = tooltipVisible ? 1f : 0f;
        tooltipAlpha = Mathf.MoveTowards(tooltipAlpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
        tooltipGroup.alpha = tooltipAlpha;
        if (!tooltipVisible && tooltipAlpha <= 0f && erosionTooltip.gameObject.activeSelf)
            erosionTooltip.gameObject.SetActive(false);
    }

}
