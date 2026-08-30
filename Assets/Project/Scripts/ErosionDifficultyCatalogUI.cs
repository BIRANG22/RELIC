using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 새 침식도 카탈로그 UI의 선택/호버/점수 표시를 관리합니다.
/// Catalog01~03 아래의 LevelXX_X 오브젝트를 이름 규칙으로 자동 연결합니다.
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

    [Header("Blocked Levels")]
    [SerializeField]
    private string[] blockedLevelNames =
    {
        "Level01_6",
        "Level02_4",
        "Level03_2",
        "Level03_5"
    };

    private readonly List<ErosionDifficultyLevelItemUI> levelItems = new List<ErosionDifficultyLevelItemUI>();
    private readonly HashSet<string> blockedNames = new HashSet<string>();
    private readonly Dictionary<string, GameObject> erosionSlotInstances = new Dictionary<string, GameObject>();

    private int targetScore;
    private int displayedScore;
    private Coroutine scoreRoutine;

    public int CurrentScore => targetScore;

    private void Awake()
    {
        if (autoBindOnAwake)
            AutoBind();
    }

    private void OnEnable()
    {
        RefreshAllVisuals();
        ApplyDisplayedScoreImmediately(targetScore);
    }

    [ContextMenu("Auto Bind Erosion Difficulty UI")]
    public void AutoBind()
    {
        RebuildBlockedNameSet();

        if (catalogGroup == null)
            catalogGroup = FindTransformRecursive(transform, "CatalogGroup");

        if (erosionValueText == null)
            erosionValueText = FindTextAnywhereInRoot("Erosion_Value");

        if (erosionSlotContent == null || erosionSlotScrollRect == null)
        {
            Transform erosionSelect = FindTransformRecursive(transform.root, "Erosion_Select");
            if (erosionSelect != null)
            {
                Transform scrollView = FindTransformRecursive(erosionSelect, "Scroll View");
                Transform viewport = scrollView != null ? FindTransformRecursive(scrollView, "Viewport") : null;

                if (erosionSlotContent == null)
                    erosionSlotContent = viewport != null ? FindTransformRecursive(viewport, "Content") : null;

                if (erosionSlotScrollRect == null && scrollView != null)
                    erosionSlotScrollRect = scrollView.GetComponent<ScrollRect>();
            }
        }

        levelItems.Clear();
        ClearAllErosionSlotInstances();

        if (catalogGroup == null)
        {
            Debug.LogWarning("[ErosionDifficultyCatalogUI] CatalogGroup을 찾지 못했습니다.", this);
            return;
        }

        BindCatalog("Catalog01", 1);
        BindCatalog("Catalog02", 2);
        BindCatalog("Catalog03", 3);

        UpdateAllGroupDimStates();
        RecalculateTargetScore(false);
        RefreshAllVisuals();
        ApplyDisplayedScoreImmediately(targetScore);
    }

    public void OnLevelClicked(ErosionDifficultyLevelItemUI item)
    {
        if (item == null || !item.IsSelectable)
            return;

        // _1~_6은 같은 번호끼리 하나의 단계 그룹입니다.
        // 같은 그룹에서는 1/2/3단계 중 하나만 선택할 수 있습니다.
        if (item.GroupIndex >= 1 && item.GroupIndex <= 6)
        {
            if (item.IsSelected)
            {
                item.SetSelected(false);
            }
            else
            {
                List<ErosionDifficultyLevelItemUI> peers = FindGroupPeers(item.GroupIndex);
                for (int i = 0; i < peers.Count; i++)
                {
                    ErosionDifficultyLevelItemUI peer = peers[i];
                    if (peer != null && peer != item && peer.IsSelected)
                        peer.SetSelected(false);
                }

                item.SetSelected(true);
            }

            UpdateGroupDimStates(item.GroupIndex);
        }
        else
        {
            // _7, _8은 특별 난이도로 단계와 관계없이 독립 선택합니다.
            item.SetSelected(!item.IsSelected);
        }

        RecalculateTargetScore(true);
        RefreshErosionSlotInstances();
    }


    private void RefreshErosionSlotInstances()
    {
        if (erosionSlotPrefab == null || erosionSlotContent == null)
            return;

        Dictionary<string, ErosionDifficultyLevelItemUI> desiredSlots = new Dictionary<string, ErosionDifficultyLevelItemUI>();

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item == null || !item.IsSelectable || !item.IsSelected)
                continue;

            string slotKey = GetErosionSlotKey(item);
            if (!string.IsNullOrEmpty(slotKey))
                desiredSlots[slotKey] = item;
        }

        List<string> staleKeys = new List<string>();
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
                // _1~_6에서 난이도 단계만 바뀐 경우 기존 프리팹을 그대로 재사용합니다.
                existingSlot.name = $"ErosionSlot_{item.name}";
                BindErosionSlot(existingSlot.transform, item);
                continue;
            }

            GameObject slotInstance = Instantiate(erosionSlotPrefab, erosionSlotContent);
            slotInstance.name = $"ErosionSlot_{item.name}";
            BindErosionSlot(slotInstance.transform, item);
            erosionSlotInstances[slotKey] = slotInstance;
            createdNewSlot = true;
        }

        if (createdNewSlot)
            ScrollErosionSlotsToBottom();
    }

    private static string GetErosionSlotKey(ErosionDifficultyLevelItemUI item)
    {
        if (item == null)
            return null;

        // _1~_6은 같은 효과의 단계이므로 그룹당 ErosionSlot 하나를 재사용합니다.
        if (item.GroupIndex >= 1 && item.GroupIndex <= 6)
            return $"Group_{item.GroupIndex}";

        // _7, _8은 단계별 독립 선택이므로 각 Level마다 별도 슬롯을 사용합니다.
        return item.name;
    }

    private void ScrollErosionSlotsToBottom()
    {
        if (erosionSlotScrollRect == null)
            return;

        StartCoroutine(ScrollErosionSlotsToBottomNextFrame());
    }

    private IEnumerator ScrollErosionSlotsToBottomNextFrame()
    {
        // 새 슬롯이 레이아웃에 반영된 뒤 맨 아래로 이동해야 방금 선택한 효과가 보입니다.
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (erosionSlotContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();

        if (erosionSlotScrollRect != null)
        {
            erosionSlotScrollRect.StopMovement();
            erosionSlotScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private static void BindErosionSlot(Transform slotRoot, ErosionDifficultyLevelItemUI item)
    {
        if (slotRoot == null || item == null)
            return;

        Transform iconTransform = FindTransformRecursive(slotRoot, "Icon");
        Image slotIcon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (slotIcon != null)
        {
            slotIcon.sprite = item.IconSprite;
            slotIcon.enabled = item.IconSprite != null;
        }

        Transform valueTransform = FindTransformRecursive(slotRoot, "Value_Text");
        TMP_Text valueText = valueTransform != null ? valueTransform.GetComponent<TMP_Text>() : null;
        if (valueText != null)
            valueText.text = item.ScoreValue.ToString();
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

    private List<ErosionDifficultyLevelItemUI> FindGroupPeers(int groupIndex)
    {
        List<ErosionDifficultyLevelItemUI> peers = new List<ErosionDifficultyLevelItemUI>();

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item != null && item.GroupIndex == groupIndex)
                peers.Add(item);
        }

        return peers;
    }

    private void UpdateGroupDimStates(int groupIndex)
    {
        if (groupIndex < 1 || groupIndex > 6)
            return;

        ErosionDifficultyLevelItemUI selectedItem = null;
        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item != null && item.GroupIndex == groupIndex && item.IsSelected)
            {
                selectedItem = item;
                break;
            }
        }

        for (int i = 0; i < levelItems.Count; i++)
        {
            ErosionDifficultyLevelItemUI item = levelItems[i];
            if (item == null || item.GroupIndex != groupIndex)
                continue;

            item.SetGroupDimmed(selectedItem != null && item != selectedItem);
        }
    }

    private void UpdateAllGroupDimStates()
    {
        for (int groupIndex = 1; groupIndex <= 6; groupIndex++)
            UpdateGroupDimStates(groupIndex);
    }

    private void BindCatalog(string catalogName, int scoreValue)
    {
        Transform catalog = FindDirectChild(catalogGroup, catalogName) ?? FindTransformRecursive(catalogGroup, catalogName);
        if (catalog == null)
        {
            Debug.LogWarning($"[ErosionDifficultyCatalogUI] {catalogName}을 찾지 못했습니다.", this);
            return;
        }

        for (int i = 0; i < catalog.childCount; i++)
        {
            Transform child = catalog.GetChild(i);
            if (child == null || !child.name.StartsWith("Level"))
                continue;

            bool isSelectable = !blockedNames.Contains(child.name);
            ErosionDifficultyLevelItemUI item = child.GetComponent<ErosionDifficultyLevelItemUI>();
            if (item == null)
                item = child.gameObject.AddComponent<ErosionDifficultyLevelItemUI>();

            int groupIndex = ParseGroupIndex(child.name);
            item.Initialize(this, scoreValue, groupIndex, isSelectable, selectedBaskColor, groupDimmedColor, hoverIconScale, hoverScaleDuration);
            levelItems.Add(item);
        }
    }

    private static int ParseGroupIndex(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
            return -1;

        int underscoreIndex = levelName.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= levelName.Length - 1)
            return -1;

        return int.TryParse(levelName.Substring(underscoreIndex + 1), out int result) ? result : -1;
    }

    private void RebuildBlockedNameSet()
    {
        blockedNames.Clear();

        if (blockedLevelNames == null)
            return;

        for (int i = 0; i < blockedLevelNames.Length; i++)
        {
            string levelName = blockedLevelNames[i];
            if (!string.IsNullOrWhiteSpace(levelName))
                blockedNames.Add(levelName.Trim());
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
            erosionValueText.text = displayedScore.ToString();

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
            erosionValueText.text = displayedScore.ToString();
    }

    private void RefreshAllVisuals()
    {
        for (int i = 0; i < levelItems.Count; i++)
        {
            if (levelItems[i] != null)
                levelItems[i].RefreshVisuals();
        }
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
/// Level01_1 같은 개별 침식도 슬롯의 포인터 입력과 시각 상태를 담당합니다.
/// 런타임에 ErosionDifficultyCatalogUI가 자동으로 부착/초기화합니다.
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
    private int scoreValue;
    private int groupIndex;
    private float hoverIconScale = 1.1f;
    private float hoverScaleDuration = 0.12f;
    private Coroutine hoverScaleRoutine;

    public bool IsSelected => isSelected;
    public bool IsSelectable => isSelectable;
    public int ScoreValue => scoreValue;
    public int GroupIndex => groupIndex;
    public Sprite IconSprite => iconGraphic is Image image ? image.sprite : null;

    public void Initialize(
        ErosionDifficultyCatalogUI owner,
        int scoreValue,
        int groupIndex,
        bool isSelectable,
        Color selectedBaskColor,
        Color groupDimmedColor,
        float hoverIconScale,
        float hoverScaleDuration)
    {
        this.owner = owner;
        this.scoreValue = scoreValue;
        this.groupIndex = groupIndex;
        this.isSelectable = isSelectable;
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

        if (!isSelectable)
        {
            isSelected = false;
            isHovered = false;
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
        isGroupDimmed = dimmed;
        RefreshVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable)
            return;

        isHovered = true;
        ApplyHoverScale(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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
        // Bask는 그룹 단계 비활성 표시에 포함하지 않습니다.
        // 선택된 슬롯만 선택색을 사용하고, 미선택 슬롯은 원래 Bask 색을 유지합니다.
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
            // 그룹 비선택 표시는 RGB만 바꿉니다.
            // Line/Icon/Value의 현재 Alpha는 절대 건드리지 않습니다.
            ApplyRgbPreserveCurrentAlpha(lineGraphic, groupDimmedColor);
            ApplyRgbPreserveCurrentAlpha(iconGraphic, groupDimmedColor);
            ApplyRgbPreserveCurrentAlpha(valueGraphic, groupDimmedColor);
            return;
        }

        // 선택 슬롯 또는 그룹 선택 해제 상태에서도 RGB만 원래 값으로 돌립니다.
        // 현재 Alpha는 그대로 유지하므로 선택/취소로 투명도가 바뀌지 않습니다.
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
