using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Erosion 시트와 ErosionIconDatabase를 기준으로 침식 난이도 UI를 구성합니다.
/// 씬의 LevelXX_X 오브젝트 이름은 Erosion.SlotName과 연결됩니다.
/// </summary>
public sealed class ErosionDifficultyCatalogUI : MonoBehaviour
{
    [Header("Auto Bind")]
    [SerializeField] private Transform catalogGroup;
    [SerializeField] private TMP_Text erosionValueText;
    [SerializeField] private bool autoBindOnAwake = true;

    [Header("Colors")]
    [SerializeField] private Color selectedBaskColor = new Color32(0x76, 0x00, 0x00, 0xFF);
    [SerializeField] private Color groupDimmedColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    [Header("Selected Erosion Slots")]
    [SerializeField] private GameObject erosionSlotPrefab;
    [SerializeField] private Transform erosionSlotContent;
    [SerializeField] private ScrollRect erosionSlotScrollRect;

    [Header("Erosion Tooltip")]
    [Tooltip("각 침식도 Level에 마우스를 올렸을 때 표시할 TooltipPanel입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private GameObject tooltipPanel;
    [Tooltip("TooltipPanel 아래에서 침식 효과 문구를 표시할 ErosionText입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text tooltipErosionText;
    [Tooltip("호버한 침식도 아이콘 기준 툴팁 X 오프셋입니다.")]
    [SerializeField] private float tooltipOffsetX = 120f;
    [Tooltip("호버한 침식도 아이콘 기준 툴팁 Y 오프셋입니다.")]
    [SerializeField] private float tooltipOffsetY = 0f;
    [Tooltip("툴팁이 나타나는 페이드 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float tooltipFadeInDuration = 0.12f;
    [Tooltip("툴팁이 사라지는 페이드 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float tooltipFadeOutDuration = 0.10f;

    [Header("Hover")]
    [Min(1f)]
    [SerializeField] private float hoverIconScale = 1.1f;
    [Min(0f)]
    [SerializeField] private float hoverScaleDuration = 0.12f;

    [Header("Score Animation")]
    [Tooltip("점수가 1씩 증가/감소하는 간격입니다.")]
    [Min(0.01f)]
    [SerializeField] private float scoreStepInterval = 0.06f;
    [SerializeField] private bool useUnscaledTime = true;

    private readonly List<ErosionDifficultyLevelItemUI> levelItems = new();
    private readonly Dictionary<string, GameObject> erosionSlotInstances = new(StringComparer.OrdinalIgnoreCase);

    private int targetScore;
    private int displayedScore;
    private Coroutine scoreRoutine;
    private string erosionValueTemplate = string.Empty;
    private bool erosionValueTemplateCaptured;
    private ErosionDifficultyLevelItemUI tooltipOwnerItem;
    private CanvasGroup tooltipCanvasGroup;
    private Coroutine tooltipFadeRoutine;

    public int CurrentScore => targetScore;

    private void Awake()
    {
        AutoBindTooltipIfNeeded();
        HideErosionTooltipImmediate();

        if (autoBindOnAwake)
            AutoBind();
    }

    private void OnEnable()
    {
        AutoBindTooltipIfNeeded();
        HideErosionTooltipImmediate();
        RefreshAllVisuals();
        ApplyDisplayedScoreImmediately(targetScore);
    }

    private void OnDisable()
    {
        HideErosionTooltipImmediate();
    }

    [ContextMenu("Auto Bind Erosion Difficulty UI")]
    public void AutoBind()
    {
        if (catalogGroup == null)
            catalogGroup = FindTransformRecursive(transform, "CatalogGroup");

        if (erosionValueText == null)
            erosionValueText = FindTextAnywhereInRoot("Erosion_Value");

        CaptureErosionValueTemplate();

        AutoBindSelectedSlotScroll();

        levelItems.Clear();
        ClearAllErosionSlotInstances();

        if (catalogGroup == null)
        {
            Debug.LogWarning("[ErosionDifficultyCatalogUI] CatalogGroup을 찾지 못했습니다.", this);
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.ErosionDatabase == null)
        {
            Debug.LogWarning("[ErosionDifficultyCatalogUI] ErosionDatabase가 준비되지 않았습니다.", this);
            return;
        }

        BindCatalog("Catalog01");
        BindCatalog("Catalog02");
        BindCatalog("Catalog03");

        UpdateAllGroupDimStates();
        RecalculateTargetScore(false);
        RefreshAllVisuals();
        ApplyDisplayedScoreImmediately(targetScore);
    }

    public void OnLevelClicked(ErosionDifficultyLevelItemUI item)
    {
        if (item == null || !item.IsSelectable || item.DifficultyData == null)
            return;

        if (item.IsExclusive)
        {
            if (item.IsSelected)
            {
                item.SetSelected(false);
            }
            else
            {
                List<ErosionDifficultyLevelItemUI> peers = FindGroupPeers(item.GroupId);
                for (int i = 0; i < peers.Count; i++)
                {
                    ErosionDifficultyLevelItemUI peer = peers[i];
                    if (peer != null && peer != item && peer.IsSelected)
                        peer.SetSelected(false);
                }

                item.SetSelected(true);
            }

            UpdateGroupDimStates(item.GroupId);
        }
        else
        {
            item.SetSelected(!item.IsSelected);
        }

        RecalculateTargetScore(true);
        RefreshErosionSlotInstances();
    }

    internal void ShowErosionTooltip(ErosionDifficultyLevelItemUI item)
    {
        if (item == null || item.DifficultyData == null)
            return;

        AutoBindTooltipIfNeeded();
        if (tooltipPanel == null || tooltipErosionText == null)
            return;

        // TooltipPanel의 ErosionText는 동적으로 갱신되는 텍스트입니다.
        // 고정 LocalizedTMPText가 아니라 현재 호버한 ErosionData의 로컬라이징 결과를 직접 사용합니다.
        string effectText = GameDataLocalization.ErosionDescription(item.DifficultyData);
        if (string.IsNullOrWhiteSpace(effectText))
        {
            HideErosionTooltip(item);
            return;
        }

        tooltipOwnerItem = item;
        tooltipErosionText.text = effectText;
        UpdateTooltipPosition(item);
        FadeTooltipToVisible();
    }

    private void UpdateTooltipPosition(ErosionDifficultyLevelItemUI item)
    {
        if (tooltipPanel == null || item == null)
            return;

        RectTransform tooltipRect = tooltipPanel.transform as RectTransform;
        RectTransform itemRect = item.transform as RectTransform;
        if (tooltipRect == null || itemRect == null)
            return;

        RectTransform parentRect = tooltipRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector3 itemWorldCenter = itemRect.TransformPoint(itemRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, itemWorldCenter);
        screenPoint += new Vector2(tooltipOffsetX, tooltipOffsetY);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            tooltipRect.anchoredPosition = localPoint;
        }
    }

    internal void HideErosionTooltip(ErosionDifficultyLevelItemUI item)
    {
        if (tooltipOwnerItem != null && tooltipOwnerItem != item)
            return;

        HideErosionTooltip();
    }

    private void HideErosionTooltip()
    {
        tooltipOwnerItem = null;
        FadeTooltipToHidden();
    }

    private void FadeTooltipToVisible()
    {
        AutoBindTooltipIfNeeded();
        if (tooltipPanel == null || tooltipCanvasGroup == null)
            return;

        if (tooltipFadeRoutine != null)
        {
            StopCoroutine(tooltipFadeRoutine);
            tooltipFadeRoutine = null;
        }

        if (!tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(true);
            tooltipCanvasGroup.alpha = 0f;
        }

        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;

        if (tooltipFadeInDuration <= 0f)
        {
            tooltipCanvasGroup.alpha = 1f;
            return;
        }

        if (tooltipCanvasGroup.alpha >= 0.999f)
        {
            tooltipCanvasGroup.alpha = 1f;
            return;
        }

        tooltipFadeRoutine = StartCoroutine(FadeTooltipRoutine(1f, tooltipFadeInDuration, false));
    }

    private void FadeTooltipToHidden()
    {
        AutoBindTooltipIfNeeded();
        if (tooltipPanel == null || tooltipCanvasGroup == null)
            return;

        if (tooltipFadeRoutine != null)
        {
            StopCoroutine(tooltipFadeRoutine);
            tooltipFadeRoutine = null;
        }

        if (!tooltipPanel.activeSelf)
        {
            tooltipCanvasGroup.alpha = 0f;
            return;
        }

        if (tooltipFadeOutDuration <= 0f)
        {
            tooltipCanvasGroup.alpha = 0f;
            tooltipPanel.SetActive(false);
            return;
        }

        tooltipFadeRoutine = StartCoroutine(FadeTooltipRoutine(0f, tooltipFadeOutDuration, true));
    }

    private IEnumerator FadeTooltipRoutine(float targetAlpha, float duration, bool deactivateWhenDone)
    {
        float startAlpha = tooltipCanvasGroup != null ? tooltipCanvasGroup.alpha : 0f;
        float elapsed = 0f;

        while (tooltipCanvasGroup != null && elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        if (tooltipCanvasGroup != null)
            tooltipCanvasGroup.alpha = targetAlpha;

        if (deactivateWhenDone && tooltipPanel != null && tooltipOwnerItem == null)
            tooltipPanel.SetActive(false);

        tooltipFadeRoutine = null;
    }

    private void HideErosionTooltipImmediate()
    {
        tooltipOwnerItem = null;

        if (tooltipFadeRoutine != null)
        {
            StopCoroutine(tooltipFadeRoutine);
            tooltipFadeRoutine = null;
        }

        AutoBindTooltipIfNeeded();

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
        }

        if (tooltipPanel != null && tooltipPanel.activeSelf)
            tooltipPanel.SetActive(false);
    }

    private void AutoBindTooltipIfNeeded()
    {
        if (tooltipPanel == null)
        {
            Transform root = transform.root;
            Transform found = FindTransformRecursive(root, "TooltipPanel");
            if (found != null)
                tooltipPanel = found.gameObject;
        }

        if (tooltipErosionText == null && tooltipPanel != null)
        {
            Transform erosionText = FindTransformRecursive(tooltipPanel.transform, "ErosionText");
            if (erosionText != null)
                tooltipErosionText = erosionText.GetComponent<TMP_Text>() ?? erosionText.GetComponentInChildren<TMP_Text>(true);
        }

        if (tooltipPanel != null && tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
        }

        EnsureDynamicTooltipTextOwnership();
    }

    /// <summary>
    /// ErosionText는 현재 호버한 침식도에 따라 내용이 바뀌는 동적 텍스트입니다.
    /// Inspector에 남아 있는 고정 LocalizedTMPText가 내용을 다시 덮어쓰지 않도록 정리합니다.
    /// </summary>
    private void EnsureDynamicTooltipTextOwnership()
    {
        if (tooltipErosionText == null)
            return;

        if (tooltipErosionText.GetComponent<LocalizationIgnore>() == null)
            tooltipErosionText.gameObject.AddComponent<LocalizationIgnore>();

        LocalizedTMPText fixedLocalizer = tooltipErosionText.GetComponent<LocalizedTMPText>();
        if (fixedLocalizer != null)
        {
            fixedLocalizer.enabled = false;
            Destroy(fixedLocalizer);
        }
    }

    private void BindCatalog(string catalogName)
    {
        Transform catalog = FindDirectChild(catalogGroup, catalogName) ??
                            FindTransformRecursive(catalogGroup, catalogName);
        if (catalog == null)
        {
            Debug.LogWarning($"[ErosionDifficultyCatalogUI] {catalogName}을 찾지 못했습니다.", this);
            return;
        }

        for (int i = 0; i < catalog.childCount; i++)
        {
            Transform child = catalog.GetChild(i);
            if (child == null || !child.name.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
                continue;

            DataManager.Instance.ErosionDatabase.TryGetBySlotName(child.name, out ErosionData data);

            ErosionDifficultyLevelItemUI item = child.GetComponent<ErosionDifficultyLevelItemUI>();
            if (item == null)
                item = child.gameObject.AddComponent<ErosionDifficultyLevelItemUI>();

            Sprite icon = ResolveLevelIcon(data, item.CurrentIconSprite);
            item.Initialize(
                this,
                data,
                icon,
                selectedBaskColor,
                groupDimmedColor,
                hoverIconScale,
                hoverScaleDuration);
            levelItems.Add(item);

            if (data == null)
                Debug.LogWarning($"[ErosionDifficultyCatalogUI] Erosion 시트에 SlotName '{child.name}' 데이터가 없습니다.", child);
        }
    }

    private Sprite ResolveLevelIcon(ErosionData data, Sprite fallback)
    {
        ErosionIconDatabase iconDatabase = DataManager.Instance != null
            ? DataManager.Instance.ErosionIconDatabase
            : null;

        if (data == null)
            return iconDatabase != null && iconDatabase.UnavailableIcon != null
                ? iconDatabase.UnavailableIcon
                : fallback;

        if (!data.Selectable)
            return iconDatabase != null && iconDatabase.UnavailableIcon != null
                ? iconDatabase.UnavailableIcon
                : fallback;

        if (iconDatabase != null && iconDatabase.TryGetIcon(data.GroupId, out Sprite icon))
            return icon;

        return fallback;
    }

    private void RefreshErosionSlotInstances()
    {
        if (erosionSlotPrefab == null || erosionSlotContent == null)
            return;

        Dictionary<string, ErosionDifficultyLevelItemUI> desiredSlots =
            new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item == null || !item.IsSelectable || !item.IsSelected)
                continue;

            string slotKey = GetErosionSlotKey(item);
            if (!string.IsNullOrEmpty(slotKey))
                desiredSlots[slotKey] = item;
        }

        List<string> staleKeys = new();
        foreach (KeyValuePair<string, GameObject> pair in erosionSlotInstances)
        {
            if (!desiredSlots.ContainsKey(pair.Key))
                staleKeys.Add(pair.Key);
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            string key = staleKeys[i];
            if (!erosionSlotInstances.TryGetValue(key, out GameObject slot))
                continue;

            if (slot != null)
                Destroy(slot);

            erosionSlotInstances.Remove(key);
        }

        bool createdNewSlot = false;

        foreach (KeyValuePair<string, ErosionDifficultyLevelItemUI> pair in desiredSlots)
        {
            string slotKey = pair.Key;
            ErosionDifficultyLevelItemUI item = pair.Value;

            if (erosionSlotInstances.TryGetValue(slotKey, out GameObject existingSlot) && existingSlot != null)
            {
                existingSlot.name = $"ErosionSlot_{item.DifficultyId}";
                BindErosionSlot(existingSlot.transform, item);
                continue;
            }

            GameObject slotInstance = Instantiate(erosionSlotPrefab, erosionSlotContent);
            slotInstance.name = $"ErosionSlot_{item.DifficultyId}";
            BindErosionSlot(slotInstance.transform, item);
            erosionSlotInstances[slotKey] = slotInstance;
            StartCoroutine(RebindErosionSlotNextFrame(slotKey, slotInstance));
            createdNewSlot = true;
        }

        if (createdNewSlot)
            ScrollErosionSlotsToBottom();
    }


    private IEnumerator RebindErosionSlotNextFrame(string slotKey, GameObject slotInstance)
    {
        // 프리팹이 처음 활성화되는 프레임에는 Localization/TMP 초기화가
        // Inspector 기본 텍스트를 다시 적용할 수 있어 다음 프레임에 최종 데이터를 재적용합니다.
        yield return null;

        if (slotInstance == null || !erosionSlotInstances.TryGetValue(slotKey, out GameObject currentSlot) ||
            currentSlot != slotInstance)
        {
            yield break;
        }

        ErosionDifficultyLevelItemUI selectedItem = null;
        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI candidate = levelItems[i];
            if (candidate == null || !candidate.IsSelectable || !candidate.IsSelected)
                continue;

            if (string.Equals(GetErosionSlotKey(candidate), slotKey, StringComparison.OrdinalIgnoreCase))
            {
                selectedItem = candidate;
                break;
            }
        }

        if (selectedItem != null)
            BindErosionSlot(slotInstance.transform, selectedItem);
    }

    private static string GetErosionSlotKey(ErosionDifficultyLevelItemUI item)
    {
        if (item == null)
            return null;

        if (item.IsExclusive && !string.IsNullOrWhiteSpace(item.GroupId))
            return item.GroupId;

        return !string.IsNullOrWhiteSpace(item.DifficultyId)
            ? item.DifficultyId
            : item.name;
    }

    private void BindErosionSlot(Transform slotRoot, ErosionDifficultyLevelItemUI item)
    {
        if (slotRoot == null || item == null || item.DifficultyData == null)
            return;

        Transform iconTransform = FindTransformRecursive(slotRoot, "Icon");
        Image slotIcon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (slotIcon != null)
        {
            slotIcon.sprite = item.IconSprite;
            slotIcon.enabled = item.IconSprite != null;
        }

        SetText(slotRoot, "Value_Text", item.ScoreValue.ToString());
        SetText(slotRoot, "Catalog_Text", BuildCatalogDisplayName(item.DifficultyData));
        SetText(slotRoot, "Effect_Text", GameDataLocalization.ErosionDescription(item.DifficultyData));

        ErosionSelectedSlotInteractionUI interaction =
            slotRoot.GetComponent<ErosionSelectedSlotInteractionUI>();
        if (interaction == null)
            interaction = slotRoot.gameObject.AddComponent<ErosionSelectedSlotInteractionUI>();

        Transform lineTransform = FindTransformRecursive(slotRoot, "Line");
        Graphic lineGraphic = lineTransform != null ? lineTransform.GetComponent<Graphic>() : null;
        interaction.Initialize(this, item, lineGraphic);
    }

    internal void OnSelectedErosionSlotClicked(ErosionDifficultyLevelItemUI item)
    {
        if (item == null || !item.IsSelectable || !item.IsSelected)
            return;

        // 선택 목록의 슬롯을 클릭하면 카탈로그에서 같은 항목을 다시 누른 것과 동일하게 해제합니다.
        OnLevelClicked(item);
    }

    private static string BuildCatalogDisplayName(ErosionData data)
    {
        if (data == null)
            return string.Empty;

        string erosionName = string.IsNullOrWhiteSpace(data.ErosionName)
            ? data.EffectName
            : GameDataLocalization.ErosionName(data);

        if (string.IsNullOrWhiteSpace(erosionName))
            return string.Empty;

        if (!data.IsExclusive)
            return erosionName.Trim();

        string roman = ToRomanTier(data.Tier);
        return string.IsNullOrEmpty(roman)
            ? erosionName.Trim()
            : $"{erosionName.Trim()} {roman}";
    }

    internal static string ToRomanTier(int tier)
    {
        return tier switch
        {
            1 => "Ⅰ",
            2 => "Ⅱ",
            3 => "Ⅲ",
            _ => string.Empty
        };
    }

    private static void SetText(Transform root, string objectName, string value)
    {
        Transform target = FindTransformRecursive(root, objectName);
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private List<ErosionDifficultyLevelItemUI> FindGroupPeers(string groupId)
    {
        List<ErosionDifficultyLevelItemUI> peers = new();
        if (string.IsNullOrWhiteSpace(groupId))
            return peers;

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item != null &&
                item.IsExclusive &&
                string.Equals(item.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
            {
                peers.Add(item);
            }
        }

        return peers;
    }

    private void UpdateGroupDimStates(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return;

        ErosionDifficultyLevelItemUI selectedItem = null;
        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item != null &&
                item.IsExclusive &&
                string.Equals(item.GroupId, groupId, StringComparison.OrdinalIgnoreCase) &&
                item.IsSelected)
            {
                selectedItem = item;
                break;
            }
        }

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item == null ||
                !item.IsExclusive ||
                !string.Equals(item.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            item.SetGroupDimmed(selectedItem != null && item != selectedItem);
        }
    }

    private void UpdateAllGroupDimStates()
    {
        HashSet<string> handledGroups = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item == null || !item.IsExclusive || string.IsNullOrWhiteSpace(item.GroupId))
                continue;

            if (handledGroups.Add(item.GroupId))
                UpdateGroupDimStates(item.GroupId);
        }
    }

    private void RecalculateTargetScore(bool animate)
    {
        int nextScore = 0;

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item != null && item.IsSelectable && item.IsSelected)
                nextScore += item.ScoreValue;
        }

        targetScore = nextScore;

        if (!animate)
        {
            ApplyDisplayedScoreImmediately(targetScore);
            return;
        }

        EnsureScoreAnimationRunning();
    }

    private void EnsureScoreAnimationRunning()
    {
        if (erosionValueText == null)
            return;

        if (scoreRoutine == null)
            scoreRoutine = StartCoroutine(AnimateScoreRoutine());
    }

    private IEnumerator AnimateScoreRoutine()
    {
        while (displayedScore != targetScore)
        {
            displayedScore += displayedScore < targetScore ? 1 : -1;
            erosionValueText.text = FormatErosionValueText(displayedScore);

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(scoreStepInterval);
            else
                yield return new WaitForSeconds(scoreStepInterval);
        }

        scoreRoutine = null;
    }

    private void ApplyDisplayedScoreImmediately(int score)
    {
        if (scoreRoutine != null)
        {
            StopCoroutine(scoreRoutine);
            scoreRoutine = null;
        }

        displayedScore = score;

        if (erosionValueText != null)
            erosionValueText.text = FormatErosionValueText(displayedScore);
    }

    private void CaptureErosionValueTemplate()
    {
        if (erosionValueTemplateCaptured || erosionValueText == null)
            return;

        erosionValueTemplate = erosionValueText.text ?? string.Empty;
        erosionValueTemplateCaptured = true;
    }

    private string FormatErosionValueText(int value)
    {
        CaptureErosionValueTemplate();

        if (string.IsNullOrEmpty(erosionValueTemplate))
            return value.ToString();

        if (erosionValueTemplate.IndexOf("{0}", StringComparison.Ordinal) >= 0)
            return erosionValueTemplate.Replace("{0}", value.ToString());

        string trimmed = erosionValueTemplate.Trim();
        if (int.TryParse(trimmed, out _))
        {
            int index = erosionValueTemplate.IndexOf(trimmed, StringComparison.Ordinal);
            return erosionValueTemplate.Remove(index, trimmed.Length).Insert(index, value.ToString());
        }

        return erosionValueTemplate;
    }

    private void RefreshAllVisuals()
    {
        for (int i = 0; i < levelItems.Count; i++)
        {
            if (levelItems[i] != null)
                levelItems[i].RefreshVisuals();
        }
    }

    private void AutoBindSelectedSlotScroll()
    {
        // 활성 침식 슬롯 Content는 Inspector에서 명시적으로 연결된 대상만 사용합니다.
        // "Scroll View / Viewport / Content" 이름을 재귀 검색하면 Erosion_Catalog의
        // 가로 스크롤 Content를 잘못 잡아 카탈로그 스크롤이 초기화될 수 있습니다.
        if (erosionSlotContent == null)
        {
            erosionSlotScrollRect = null;
            return;
        }

        // 카탈로그 내부 Content가 잘못 연결되어 있으면 선택 슬롯 대상으로 사용하지 않습니다.
        if (IsUnderNamedAncestor(erosionSlotContent, "Erosion_Catalog"))
        {
            Debug.LogWarning(
                "[ErosionDifficultyCatalogUI] erosionSlotContent가 Erosion_Catalog 내부를 가리키고 있어 연결을 해제합니다. " +
                "활성 침식 슬롯용 Content를 Inspector에 직접 연결해 주세요.",
                this);
            erosionSlotContent = null;
            erosionSlotScrollRect = null;
            return;
        }

        ScrollRect parentScrollRect = erosionSlotContent.GetComponentInParent<ScrollRect>();
        if (parentScrollRect == null || parentScrollRect.content != erosionSlotContent as RectTransform)
        {
            erosionSlotScrollRect = null;
            return;
        }

        erosionSlotScrollRect = parentScrollRect;
    }

    private static bool IsUnderNamedAncestor(Transform target, string ancestorName)
    {
        if (target == null || string.IsNullOrEmpty(ancestorName))
            return false;

        Transform current = target;
        while (current != null)
        {
            if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void ScrollErosionSlotsToBottom()
    {
        if (erosionSlotScrollRect != null)
            StartCoroutine(ScrollErosionSlotsToBottomNextFrame());
    }

    private IEnumerator ScrollErosionSlotsToBottomNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (erosionSlotContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();
        erosionSlotScrollRect.StopMovement();
        erosionSlotScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ClearAllErosionSlotInstances()
    {
        foreach (KeyValuePair<string, GameObject> pair in erosionSlotInstances)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        erosionSlotInstances.Clear();
    }

    private TMP_Text FindTextAnywhereInRoot(string objectName)
    {
        Transform root = transform.root;
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.gameObject.name == objectName)
                return text;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformRecursive(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}

/// <summary>
/// Erosion_Select 목록에 생성되는 선택 완료 슬롯의 호버와 해제 입력을 담당합니다.
/// </summary>
public sealed class ErosionSelectedSlotInteractionUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private static readonly Color NormalLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    private static readonly Color HoverLineColor = Color.white;

    private ErosionDifficultyCatalogUI owner;
    private ErosionDifficultyLevelItemUI item;
    private Graphic lineGraphic;

    public void Initialize(
        ErosionDifficultyCatalogUI owner,
        ErosionDifficultyLevelItemUI item,
        Graphic lineGraphic)
    {
        this.owner = owner;
        this.item = item;
        this.lineGraphic = lineGraphic;
        SetLineColor(NormalLineColor);
    }

    private void OnDisable()
    {
        SetLineColor(NormalLineColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetLineColor(HoverLineColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetLineColor(NormalLineColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        SetLineColor(NormalLineColor);
        owner?.OnSelectedErosionSlotClicked(item);
    }

    private void SetLineColor(Color color)
    {
        if (lineGraphic != null)
            lineGraphic.color = color;
    }
}

/// <summary>
/// Level01_1 같은 개별 난이도 슬롯의 입력과 시각 상태를 담당합니다.
/// </summary>
public sealed class ErosionDifficultyLevelItemUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private const byte BaskAlpha = 200;

    private ErosionDifficultyCatalogUI owner;
    private Graphic baskGraphic;
    private Graphic lineGraphic;
    private Graphic iconGraphic;
    private Graphic valueGraphic;
    private TMP_Text valueText;
    private Transform iconTransform;

    private Color originalBaskColor = Color.white;
    private Color originalLineColor = Color.white;
    private Color originalIconColor = Color.white;
    private Color originalValueColor = Color.white;
    private Color selectedBaskColor;
    private Color groupDimmedColor;
    private Vector3 originalIconScale = Vector3.one;

    private bool initialized;
    private bool isHovered;
    private bool isSelected;
    private bool isSelectable;
    private bool isGroupDimmed;
    private float hoverIconScale = 1.1f;
    private float hoverScaleDuration = 0.12f;
    private Coroutine hoverScaleRoutine;
    private ErosionData difficultyData;

    public bool IsSelected => isSelected;
    public bool IsSelectable => isSelectable;
    public bool IsExclusive => difficultyData != null && difficultyData.IsExclusive;
    public int ScoreValue => difficultyData != null ? difficultyData.Score : 0;
    public string DifficultyId => difficultyData?.DifficultyId;
    public string GroupId => difficultyData?.GroupId;
    public string SelectionMode => difficultyData?.SelectionMode;
    public ErosionData DifficultyData => difficultyData;
    public Sprite IconSprite => iconGraphic is Image image ? image.sprite : null;
    public Sprite CurrentIconSprite => iconGraphic is Image image ? image.sprite : GetComponentInChildren<Image>(true)?.sprite;

    public void Initialize(
        ErosionDifficultyCatalogUI owner,
        ErosionData data,
        Sprite iconSprite,
        Color selectedBaskColor,
        Color groupDimmedColor,
        float hoverIconScale,
        float hoverScaleDuration)
    {
        this.owner = owner;
        difficultyData = data;
        isSelectable = data != null && data.Selectable;
        this.selectedBaskColor = selectedBaskColor;
        this.groupDimmedColor = groupDimmedColor;
        this.hoverIconScale = Mathf.Max(1f, hoverIconScale);
        this.hoverScaleDuration = Mathf.Max(0f, hoverScaleDuration);

        Transform bask = FindTransformRecursive(transform, "Bask");
        Transform line = FindTransformRecursive(transform, "Line");
        Transform icon = FindTransformRecursive(transform, "Icon");
        Transform value = FindTransformRecursive(transform, "Value");

        baskGraphic = bask != null ? bask.GetComponent<Graphic>() : null;
        lineGraphic = line != null ? line.GetComponent<Graphic>() : null;
        iconGraphic = icon != null ? icon.GetComponent<Graphic>() : null;
        valueGraphic = value != null ? value.GetComponent<Graphic>() : null;
        valueText = value != null ? value.GetComponent<TMP_Text>() : null;
        iconTransform = icon;

        if (!initialized)
        {
            if (baskGraphic != null)
            {
                Color baseBaskColor = baskGraphic.color;
                baseBaskColor.a = BaskAlpha / 255f;
                originalBaskColor = baseBaskColor;
            }

            if (lineGraphic != null)
                originalLineColor = lineGraphic.color;

            if (iconGraphic != null)
                originalIconColor = iconGraphic.color;

            if (valueGraphic != null)
                originalValueColor = valueGraphic.color;

            if (iconTransform != null)
                originalIconScale = iconTransform.localScale;

            initialized = true;
        }

        if (iconGraphic is Image iconImage && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.enabled = true;
        }

        if (valueText != null)
            valueText.text = isSelectable ? ErosionDifficultyCatalogUI.ToRomanTier(difficultyData.Tier) : string.Empty;

        if (!isSelectable)
        {
            isSelected = false;
            isHovered = false;
            isGroupDimmed = false;
        }

        RefreshVisuals();
        ApplyHoverScale(false);
    }

    public void SetSelected(bool selected)
    {
        if (!isSelectable)
            selected = false;

        isSelected = selected;
        RefreshVisuals();
    }

    public void SetGroupDimmed(bool dimmed)
    {
        isGroupDimmed = isSelectable && dimmed;
        RefreshVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 실제로 선택 가능한 침식도 데이터가 연결된 아이콘에서만 툴팁을 표시합니다.
        // 데이터가 없거나 비활성 난이도 슬롯은 호버해도 TooltipPanel을 열지 않습니다.
        if (!isSelectable || difficultyData == null)
        {
            owner?.HideErosionTooltip(this);
            return;
        }

        owner?.ShowErosionTooltip(this);
        isHovered = true;
        ApplyHoverScale(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HideErosionTooltip(this);

        if (!isSelectable)
            return;

        isHovered = false;
        ApplyHoverScale(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.OnLevelClicked(this);
    }

    public void RefreshVisuals()
    {
        // Bask는 선택 여부만 표현하고 그룹 비활성 색상에는 포함하지 않습니다.
        if (baskGraphic != null)
        {
            if (isSelectable && isSelected)
            {
                Color selectedColor = selectedBaskColor;
                selectedColor.a = BaskAlpha / 255f;
                baskGraphic.color = selectedColor;
            }
            else
            {
                Color originalColor = originalBaskColor;
                originalColor.a = BaskAlpha / 255f;
                baskGraphic.color = originalColor;
            }
        }

        if (isGroupDimmed)
        {
            // RGB만 변경하고 기존 Alpha는 유지합니다.
            ApplyRgbPreserveCurrentAlpha(lineGraphic, groupDimmedColor);
            ApplyRgbPreserveCurrentAlpha(iconGraphic, groupDimmedColor);
            ApplyRgbPreserveCurrentAlpha(valueGraphic, groupDimmedColor);
            return;
        }

        ApplyRgbPreserveCurrentAlpha(lineGraphic, originalLineColor);
        ApplyRgbPreserveCurrentAlpha(iconGraphic, originalIconColor);
        ApplyRgbPreserveCurrentAlpha(valueGraphic, originalValueColor);
    }

    private static void ApplyRgbPreserveCurrentAlpha(Graphic graphic, Color rgbSource)
    {
        if (graphic == null)
            return;

        Color result = rgbSource;
        result.a = graphic.color.a;
        graphic.color = result;
    }

    private void ApplyHoverScale(bool animate)
    {
        if (iconTransform == null)
            return;

        Vector3 targetScale = isSelectable && isHovered
            ? Vector3.Scale(originalIconScale, Vector3.one * hoverIconScale)
            : originalIconScale;

        if (hoverScaleRoutine != null)
        {
            StopCoroutine(hoverScaleRoutine);
            hoverScaleRoutine = null;
        }

        if (!animate || hoverScaleDuration <= 0f || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            iconTransform.localScale = targetScale;
            return;
        }

        hoverScaleRoutine = StartCoroutine(AnimateIconScale(targetScale));
    }

    private IEnumerator AnimateIconScale(Vector3 targetScale)
    {
        Vector3 startScale = iconTransform.localScale;
        float elapsed = 0f;

        while (elapsed < hoverScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hoverScaleDuration);
            iconTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        iconTransform.localScale = targetScale;
        hoverScaleRoutine = null;
    }

    private void OnDisable()
    {
        owner?.HideErosionTooltip(this);

        if (hoverScaleRoutine != null)
        {
            StopCoroutine(hoverScaleRoutine);
            hoverScaleRoutine = null;
        }

        isHovered = false;

        if (iconTransform != null)
            iconTransform.localScale = originalIconScale;
    }

    private static Transform FindTransformRecursive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformRecursive(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
