using System.Collections;
using System.Text;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExplorationResultPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text elapsedTimeCountText;
    [SerializeField] private TMP_Text erosionLevelText;
    [SerializeField] private Image stagePreviewImage;
    [SerializeField] private TMP_Text redDustiumText;
    [SerializeField] private TMP_Text normalBattleCountText;
    [SerializeField] private TMP_Text eliteBattleCountText;
    [SerializeField] private TMP_Text eventCountText;
    [SerializeField] private ExplorationResultCharacterRowUI[] characterRows;
    [Header("Result Count-Up")]
    [SerializeField, Min(0f)] private float countUpDuration = 1f;
    [Header("Report Layout")]
    [SerializeField] private BattleBagItemSlotUI iconSlotPrefab;
    [SerializeField] private Image hardModeBlurPrefab;
    [SerializeField] private Image hardModeBackdropBlurPrefab;
    [SerializeField] private RectTransform hardModeSlotRoot;
    [Header("Erosion Icon Source")]
    [SerializeField] private ErosionIconDatabase erosionIconDatabase;
    [SerializeField] private RectTransform acquiredItemSlotRoot;
    [SerializeField] private GameObject statisticsOverlayRoot;
    [SerializeField] private RectTransform statisticsOverlayRect;
    [SerializeField] private TMP_Text statisticsOverlayText;
    [SerializeField] private RectTransform statisticsOverlayBoundary;
    [SerializeField, Min(1)] private int maxHardModeSlots = 12;
    private const int AcquiredItemSlotCount = 8;
    [SerializeField] private Button returnToBaseButton;
    [SerializeField] private Canvas sortingCanvas;
    [SerializeField] private int resultPanelSortingOrder = 25000;
    [SerializeField] private List<StagePresentationEntry> stagePresentations = new();

    private ExplorationResultData result;
    private bool isTransitioning;
    private bool isDefeatResult;
    private bool stageClearExperienceApplied;
    private float researchRewardMultiplier = 1f;
    private BattleStageClearExperienceContext experienceContext = BattleStageClearExperienceContext.Empty;
    private readonly List<BattleBagItemSlotUI> hardModeSlots = new();
    private readonly List<Image> hardModeBackdropBlurs = new();
    private readonly List<BattleBagItemSlotUI> acquiredItemSlots = new();
    private readonly Dictionary<Image, Color> acquiredSlotOriginalColors = new();
    private static readonly Color EmptyAcquiredSlotBackColor = new Color32(0x11, 0x11, 0x11, 0xFF);
    private static readonly Color EmptyAcquiredSlotLineColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    private ExplorationResultCharacterRowUI statisticsOverlayOwner;
    private bool statisticsOverlayPinned;
    private Coroutine resultCountUpCoroutine;
    private float targetElapsedSeconds;
    private int targetNormalBattleCount;
    private int targetEliteBattleCount;
    private int targetEventCount;
    private bool hasResultCountUpTargets;

    private void Awake()
    {
        AutoBindSceneReferences();

        if (returnToBaseButton != null)
            returnToBaseButton.onClick.AddListener(ReturnToBase);
    }

    public void Open()
    {
        OpenInternal(false, 1f);
    }

    public void OpenDefeat(float rewardMultiplier = 0.5f)
    {
        OpenInternal(true, rewardMultiplier);
    }

    // 이어하기에서는 저장된 결과 화면만 복원하고 새로운 체크포인트를 생성하지 않습니다.
    public void OpenSavedResult(bool defeat, float rewardMultiplier)
    {
        OpenInternal(defeat, rewardMultiplier, false);
    }

    private void OpenInternal(bool defeat, float rewardMultiplier, bool saveResultCheckpoint = true)
    {
        StopResultCountUp(false);

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.Get();
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);

        result = ExplorationResultBuilder.Build(runtime);
        if (runtime?.IsTutorialBattle == true)
            IncludeTutorialPartyInResult(result);
        isDefeatResult = defeat;
        experienceContext =
            BattleStageClearExperienceService.BuildContext(mapRuntime, currentNode, defeat);
        researchRewardMultiplier = Mathf.Max(0f, rewardMultiplier);
        isTransitioning = false;
        stageClearExperienceApplied = false;
        statisticsOverlayOwner = null;
        statisticsOverlayPinned = false;
        if (statisticsOverlayRoot != null)
            statisticsOverlayRoot.SetActive(false);

        if (returnToBaseButton != null)
            returnToBaseButton.interactable = true;

        gameObject.SetActive(true);

        if (HasStructuredBindings())
            ApplyStructuredResult(
                result,
                isDefeatResult,
                researchRewardMultiplier,
                currentNode,
                experienceContext,
                mapRuntime);
        else if (resultText != null)
            resultText.text = BuildText(result, isDefeatResult, researchRewardMultiplier);

        BringResultPanelToFront();

        if (saveResultCheckpoint && SaveSystem.Instance != null)
        {
            // 전투방 입장 스냅샷보다 결과 보고 상태를 우선 저장합니다.
            SaveSystem.Instance.ClearBattleRoomEntryCheckpoint();
            SaveSystem.Instance.SaveCheckpoint(new ResumeData
            {
                Phase = ResumePhase.ExplorationResult,
                NodeIndex = mapRuntime != null ? mapRuntime.CurrentNodeIndex : -1,
                MapId = mapRuntime?.CurrentMapId,
                ExplorationResultDefeat = defeat,
                ExplorationResultRewardMultiplier = rewardMultiplier
            });
        }
    }

    // 전투 행동 기록이 없어도 튜토리얼에 합류한 모든 파티원을 결과에 포함합니다.
    private static void IncludeTutorialPartyInResult(ExplorationResultData explorationResult)
    {
        if (explorationResult == null) return;

        DataManager data = DataManager.Instance;
        if (data?.PartyRuntimeStore == null) return;
        explorationResult.CharacterStatistics ??= new List<BattleRunCharacterStatisticsData>();

        for (int slot = 0; slot < 3; slot++)
        {
            string characterId = data.PartyRuntimeStore.GetCharacterId(slot);
            if (string.IsNullOrWhiteSpace(characterId)) continue;

            bool exists = false;
            for (int i = 0; i < explorationResult.CharacterStatistics.Count; i++)
            {
                BattleRunCharacterStatisticsData stats = explorationResult.CharacterStatistics[i];
                if (stats != null && string.Equals(stats.CharacterId, characterId, System.StringComparison.Ordinal))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                explorationResult.CharacterStatistics.Add(new BattleRunCharacterStatisticsData { CharacterId = characterId });
        }
    }

    private void ApplyStructuredResult(
        ExplorationResultData value,
        bool defeat,
        float rewardMultiplier,
        GeneratedMapNodeData currentNode,
        BattleStageClearExperienceContext clearContext,
        MapRuntimeData mapRuntime)
    {
        if (resultText != null)
            resultText.gameObject.SetActive(false);

        MapData currentMap = ResolveCurrentMap(currentNode);
        StagePresentationEntry stagePresentation =
            ResolveStagePresentation(currentMap);

        SetText(titleText, GameLocalization.Get("ui.exploration.result.title", "탐사 보고"));
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.Get();
        SetText(stageNameText, GameLocalization.Get(
            "ui.exploration.result.elapsed_time",
            "소요 시간"));
        SetText(erosionLevelText, FormatErosionLevel(CalculateSelectedErosionScore(
            runtime?.SelectedErosionDifficultyIds,
            DataManager.Instance?.ErosionDatabase)));
        SetText(redDustiumText, FormatRedDustium(value.Remnant, rewardMultiplier));

        ExplorationNodeVisitSummary summary = ExplorationNodeVisitSummary.Count(mapRuntime);
        StartResultCountUp(runtime?.ExplorationElapsedSeconds ?? 0f, summary);

        ApplyStagePreviewImage(stagePresentation?.PreviewSprite);
        ApplyHardModeSlots(runtime);
        ApplyAcquiredItemSlots(value);

        ApplyCharacterRows(
            value,
            clearContext);
    }

    public static string FormatElapsedSummary(float seconds, string label)
    {
        return $"{label?.Trim()}  {BattleExplorationElapsedTime.Format(seconds)}";
    }

    public static int CalculateCountUpSeconds(float targetSeconds, float progress)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, targetSeconds) * Mathf.Clamp01(progress));
    }

    public static int CalculateCountUpCount(int targetCount, float progress)
    {
        return Mathf.FloorToInt(Mathf.Max(0, targetCount) * Mathf.Clamp01(progress));
    }

    private void StartResultCountUp(
        float elapsedSeconds,
        ExplorationNodeVisitSummary summary)
    {
        StopResultCountUp(false);

        targetElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        targetNormalBattleCount = Mathf.Max(0, summary.NormalBattleCount);
        targetEliteBattleCount = Mathf.Max(0, summary.EliteBattleCount);
        targetEventCount = Mathf.Max(0, summary.EventCount);
        hasResultCountUpTargets = true;

        if (!Application.isPlaying || countUpDuration <= 0f)
        {
            ApplyResultCountUpProgress(1f);
            return;
        }

        ApplyResultCountUpProgress(0f);
        resultCountUpCoroutine = StartCoroutine(AnimateResultCountUp());
    }

    private IEnumerator AnimateResultCountUp()
    {
        float elapsed = 0f;
        while (elapsed < countUpDuration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            ApplyResultCountUpProgress(elapsed / countUpDuration);
        }

        ApplyResultCountUpProgress(1f);
        resultCountUpCoroutine = null;
    }

    private void ApplyResultCountUpProgress(float progress)
    {
        int displayedSeconds = CalculateCountUpSeconds(targetElapsedSeconds, progress);
        int displayedNormalCount = CalculateCountUpCount(targetNormalBattleCount, progress);
        int displayedEliteCount = CalculateCountUpCount(targetEliteBattleCount, progress);
        int displayedEventCount = CalculateCountUpCount(targetEventCount, progress);

        SetText(elapsedTimeCountText, BattleExplorationElapsedTime.Format(displayedSeconds));
        SetText(normalBattleCountText, displayedNormalCount.ToString());
        SetText(eliteBattleCountText, displayedEliteCount.ToString());
        SetText(eventCountText, displayedEventCount.ToString());
    }

    private void StopResultCountUp(bool applyFinalValues)
    {
        if (resultCountUpCoroutine != null)
        {
            StopCoroutine(resultCountUpCoroutine);
            resultCountUpCoroutine = null;
        }

        if (applyFinalValues && hasResultCountUpTargets)
            ApplyResultCountUpProgress(1f);
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= SyncHardModeBackdropBlurs;
        StopResultCountUp(true);
    }

    public static string FormatErosionLevel(int score)
    {
        return Mathf.Max(0, score).ToString();
    }

    private void ApplyCharacterRows(
        ExplorationResultData value,
        BattleStageClearExperienceContext clearContext)
    {
        if (characterRows == null)
            return;

        IReadOnlyDictionary<string, BattleStageClearExperiencePreview> experiencePreviews =
            BattleStageClearExperienceService.Preview(
                DataManager.Instance?.CharacterRuntimeStore,
                value?.CharacterStatistics,
                clearContext);

        int statsCount = value?.CharacterStatistics?.Count ?? 0;
        for (int i = 0; i < characterRows.Length; i++)
        {
            ExplorationResultCharacterRowUI row = characterRows[i];
            if (row == null)
                continue;

            if (i >= statsCount)
            {
                row.Clear();
                continue;
            }

            BattleRunCharacterStatisticsData statistics = value.CharacterStatistics[i];
            int gainedExperience = 0;
            bool leveledUp = false;
            float experienceProgress = 0f;
            int displayLevel = 1;
            IReadOnlyList<string> unlockTexts = null;

            if (TryGetExperiencePreview(
                    experiencePreviews,
                    statistics?.CharacterId,
                    out BattleStageClearExperiencePreview preview))
            {
                gainedExperience = preview.ExperienceGained;
                leveledUp = preview.LeveledUp;
                experienceProgress = preview.ProgressAfter01;
                displayLevel = preview.LevelAfter;
                unlockTexts = ResolveCharacterUnlockTexts(statistics?.CharacterId, preview);
            }

            CharacterRuntimeData character = ResolveCharacterRuntime(statistics?.CharacterId);
            if (displayLevel <= 1 && character != null)
                displayLevel = Mathf.Max(1, character.Level);

            row.Bind(
                statistics,
                ResolveCharacterResultImage(statistics?.CharacterId),
                character,
                displayLevel,
                gainedExperience,
                leveledUp,
                experienceProgress,
                unlockTexts,
                this);
        }
    }

    private bool HasStructuredBindings()
    {
        AutoBindSceneReferences();

        return titleText != null &&
               stageNameText != null &&
               elapsedTimeCountText != null &&
               redDustiumText != null &&
               characterRows != null &&
               characterRows.Length > 0;
    }

    private void AutoBindSceneReferences()
    {
        titleText ??= FindText("ExplorationReportTitle");
        stageNameText ??= FindText("ExplorationReportElapsedTimeLabel");
        elapsedTimeCountText ??= FindText("ExplorationReportElapsedTimeCount");
        erosionLevelText ??= FindText("ExplorationReportErosionLevel");
        stagePreviewImage ??= FindImage("ExplorationReportStagePreview");
        redDustiumText ??= FindText("ExplorationReportRedDustium");
        normalBattleCountText ??= FindText("ExplorationReportNormalBattleCount");
        eliteBattleCountText ??= FindText("ExplorationReportEliteBattleCount");
        eventCountText ??= FindText("ExplorationReportEventCount");
        hardModeSlotRoot ??= FindChildRecursive(transform, "ExplorationReportSelectedErosionSlots") as RectTransform;
        statisticsOverlayRoot ??= FindChildRecursive(transform, "ExplorationReportStatisticsOverlay")?.gameObject;
        statisticsOverlayRect ??= statisticsOverlayRoot != null
            ? statisticsOverlayRoot.transform as RectTransform
            : null;
        statisticsOverlayText ??= statisticsOverlayRoot != null
            ? statisticsOverlayRoot.GetComponentInChildren<TMP_Text>(true)
            : null;
        statisticsOverlayBoundary ??= FindChildRecursive(transform, "ExplorationReportFrame") as RectTransform;
        if (acquiredItemSlotRoot == null)
        {
            Transform itemRoot = FindChildRecursive(transform, "ExplorationReportAcquiredItems");
            acquiredItemSlotRoot = itemRoot as RectTransform;
        }

        if (characterRows == null || characterRows.Length == 0)
            characterRows = GetComponentsInChildren<ExplorationResultCharacterRowUI>(true);

        sortingCanvas ??= GetComponent<Canvas>();
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static MapData ResolveCurrentMap(GeneratedMapNodeData currentNode)
    {
        if (currentNode == null ||
            string.IsNullOrWhiteSpace(currentNode.MapId) ||
            DataManager.Instance?.MapDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.MapDatabase.TryGet(currentNode.MapId, out MapData map)
            ? map
            : null;
    }

    private static string ResolveStageName(
        GeneratedMapNodeData currentNode,
        MapData currentMap)
    {
        if (currentMap != null && !string.IsNullOrWhiteSpace(currentMap.Name))
            return currentMap.Name;

        if (currentMap != null && !string.IsNullOrWhiteSpace(currentMap.Stage))
            return currentMap.Stage;

        if (currentNode != null && !string.IsNullOrWhiteSpace(currentNode.MapId))
            return currentNode.MapId;

        return GameLocalization.Get("ui.exploration.record");
    }

    private static string ResolveStageName(
        GeneratedMapNodeData currentNode,
        MapData currentMap,
        StagePresentationEntry stagePresentation)
    {
        if (stagePresentation != null && !string.IsNullOrWhiteSpace(stagePresentation.DisplayName))
            return stagePresentation.DisplayName.Trim();

        return ResolveStageName(currentNode, currentMap);
    }

    private StagePresentationEntry ResolveStagePresentation(MapData currentMap)
    {
        if (stagePresentations == null || stagePresentations.Count == 0)
            return null;

        string stage = NormalizeId(currentMap?.Stage);
        if (string.IsNullOrEmpty(stage))
            return null;

        for (int i = 0; i < stagePresentations.Count; i++)
        {
            StagePresentationEntry entry = stagePresentations[i];
            if (entry == null)
                continue;

            string entryStage = NormalizeId(entry.Stage);
            if (!string.IsNullOrEmpty(entryStage) &&
                string.Equals(entryStage, stage, System.StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void ApplyStagePreviewImage(Sprite previewSprite)
    {
        if (stagePreviewImage == null)
            return;

        stagePreviewImage.sprite = null;
        stagePreviewImage.enabled = false;
    }

    private void ApplyHardModeSlots(BattleRuntimeData runtime)
    {
        // Continue skips the lobby transition, so restore the catalog sprite cache explicitly.
        ErosionDifficultyCatalogUI.PrepareLoadedCatalogIconCacheForBattleStart();
        EnsureSlotCount(hardModeSlots, hardModeSlotRoot, maxHardModeSlots, "HardModeSlot");
        EnsureHardModeBackdropBlurCount(maxHardModeSlots);
        IReadOnlyList<string> ids = runtime?.SelectedErosionDifficultyIds;
        int visibleCount = Mathf.Min(ids?.Count ?? 0, maxHardModeSlots);
        for (int i = 0; i < hardModeSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = hardModeSlots[i];
            // 결과 화면의 침식 슬롯은 표시 전용입니다. 프리팹의 호버 연출과 SFX가 실행되지 않도록 레이캐스트를 차단합니다.
            CanvasGroup displayOnlyCanvasGroup = slot.GetComponent<CanvasGroup>();
            if (displayOnlyCanvasGroup == null)
                displayOnlyCanvasGroup = slot.gameObject.AddComponent<CanvasGroup>();
            displayOnlyCanvasGroup.interactable = false;
            displayOnlyCanvasGroup.blocksRaycasts = false;
            Image backdropBlur = i < hardModeBackdropBlurs.Count
                ? hardModeBackdropBlurs[i]
                : null;
            EnsureHardModeBlur(slot);
            // 선택 침식 슬롯의 위치와 크기는 루트의 Layout Group에서 관리합니다.
            bool visible = i < visibleCount && !string.IsNullOrWhiteSpace(ids[i]);
            if (backdropBlur != null)
            {
                LayoutElement backdropLayout = backdropBlur.GetComponent<LayoutElement>();
                if (backdropLayout == null)
                    backdropLayout = backdropBlur.gameObject.AddComponent<LayoutElement>();
                backdropLayout.ignoreLayout = true;
                backdropBlur.gameObject.SetActive(visible);
                PlaceBackdropImmediatelyBeforeSlot(backdropBlur.transform, slot.transform);
            }

            slot.gameObject.SetActive(visible);
            if (!visible)
                continue;

            string id = ids[i].Trim();
            ErosionData data = ResolveErosionData(id);
            Sprite icon = ResolveErosionIcon(data);
            slot.SetupDisplayIcon(icon, FormatErosionTier(data?.Tier ?? 0));
            if (icon == null)
                Debug.LogWarning($"[ExplorationResultPanelUI] Missing erosion icon: {id} (database: {erosionIconDatabase?.name ?? DataManager.Instance?.ErosionIconDatabase?.name ?? "null"}). Assign Erosion Icon Database in the result panel Inspector.", this);
        }
    }

    private void EnsureHardModeBackdropBlurCount(int count)
    {
        if (hardModeSlotRoot == null || hardModeBackdropBlurPrefab == null)
            return;

        while (hardModeBackdropBlurs.Count < count)
        {
            Image backdropBlur = Instantiate(hardModeBackdropBlurPrefab, hardModeSlotRoot, false);
            backdropBlur.name = $"HardModeSlotBackdropBlur_{hardModeBackdropBlurs.Count}";
            backdropBlur.raycastTarget = false;
            // 배경 블러는 별도 형제 오브젝트이므로 Layout Group의 셀 계산에서 제외합니다.
            LayoutElement layoutElement = backdropBlur.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = backdropBlur.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            hardModeBackdropBlurs.Add(backdropBlur);
        }
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += SyncHardModeBackdropBlurs;
    }

    private void SyncHardModeBackdropBlurs()
    {
        // Layout Group이 슬롯을 배치한 뒤 블러를 슬롯 위치에 맞춥니다.
        int count = Mathf.Min(hardModeSlots.Count, hardModeBackdropBlurs.Count);
        for (int i = 0; i < count; i++)
        {
            BattleBagItemSlotUI slot = hardModeSlots[i];
            Image backdrop = hardModeBackdropBlurs[i];
            if (slot == null || backdrop == null || !slot.gameObject.activeInHierarchy || !backdrop.gameObject.activeInHierarchy)
                continue;

            SyncHardModeBackdropTransform(backdrop.rectTransform, slot.RectTransform);
        }
    }

    private static void SyncHardModeBackdropTransform(RectTransform backdrop, RectTransform slot)
    {
        if (backdrop == null || slot == null)
            return;

        backdrop.anchorMin = slot.anchorMin;
        backdrop.anchorMax = slot.anchorMax;
        backdrop.pivot = new Vector2(0.5f, 0.5f);
        backdrop.localRotation = slot.localRotation;
        backdrop.localScale = slot.localScale;
        backdrop.position = slot.TransformPoint(slot.rect.center);
    }

    private static void PlaceBackdropImmediatelyBeforeSlot(Transform backdrop, Transform slot)
    {
        if (backdrop == null || slot == null || backdrop.parent != slot.parent)
            return;

        int slotIndex = slot.GetSiblingIndex();
        if (backdrop.GetSiblingIndex() < slotIndex)
            slotIndex--;

        backdrop.SetSiblingIndex(slotIndex);
    }

    private void EnsureHardModeBlur(BattleBagItemSlotUI slot)
    {
        if (slot == null ||
            hardModeBlurPrefab == null ||
            slot.transform.Find("HardModeRedBlur") != null)
        {
            return;
        }

        Image blur = Instantiate(hardModeBlurPrefab, slot.transform, false);
        blur.name = "HardModeRedBlur";
        blur.raycastTarget = false;
        Transform icon = slot.transform.Find("Icon");
        if (icon != null)
            blur.transform.SetSiblingIndex(icon.GetSiblingIndex());
    }

    private void ApplyAcquiredItemSlots(ExplorationResultData value)
    {
        // Always show exactly eight bag slots. Their layout is controlled by the parent Layout Group.
        EnsureSlotCount(acquiredItemSlots, acquiredItemSlotRoot, AcquiredItemSlotCount, "AcquiredItemSlot");
        List<BagItemStack> stacks = BagItemStackUtility.BuildStacks(value?.BagItemIds);
        for (int i = 0; i < acquiredItemSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = acquiredItemSlots[i];
            if (slot == null)
                continue;

            // Result slots are display-only; block hover/click and their SFX without changing the prefab.
            CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = slot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            slot.gameObject.SetActive(true);
            if (i >= stacks.Count)
            {
                slot.SetupDisplayIcon(null);
                slot.SetQuantityVisible(false);
                ApplyAcquiredSlotEmptyColors(slot, true);
                continue;
            }

            BagItemStack stack = stacks[i];
            slot.Setup(stack.ItemId, stack.Count, null, null, null);
            slot.SetQuantityVisible(true);
            ApplyAcquiredSlotEmptyColors(slot, false);
        }
    }

    // 결과 패널에 생성한 슬롯의 Back/Line만 변경하며 프리팹 원래 색은 보존합니다.
    private void ApplyAcquiredSlotEmptyColors(BattleBagItemSlotUI slot, bool empty)
    {
        if (slot == null)
            return;

        ApplyAcquiredSlotImageColor(slot.transform, "Back", empty, EmptyAcquiredSlotBackColor);
        ApplyAcquiredSlotImageColor(slot.transform, "Line", empty, EmptyAcquiredSlotLineColor);
    }

    private void ApplyAcquiredSlotImageColor(Transform root, string childName, bool empty, Color emptyColor)
    {
        Transform child = FindChildRecursive(root, childName);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image == null)
            return;

        if (!acquiredSlotOriginalColors.TryGetValue(image, out Color original))
        {
            // 슬롯의 기본 Back 색을 Setup()이 변경할 수 있으므로 최초 생성된
            // 프리팹의 색을 가져올 수 있도록 아래의 캐시 메서드도 사용합니다.
            original = image.color;
            acquiredSlotOriginalColors[image] = original;
        }

        image.color = empty ? emptyColor : original;
    }

    private void EnsureSlotCount(
        List<BattleBagItemSlotUI> slots,
        RectTransform root,
        int count,
        string slotName)
    {
        if (root == null || iconSlotPrefab == null)
            return;

        while (slots.Count < count)
        {
            BattleBagItemSlotUI slot = Instantiate(iconSlotPrefab, root, false);
            slot.name = $"{slotName}_{slots.Count}";
            RectTransform rect = slot.RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
            }
            slots.Add(slot);
            if (ReferenceEquals(slots, acquiredItemSlots))
            {
                CacheAcquiredSlotImageColor(slot.transform, "Back");
                CacheAcquiredSlotImageColor(slot.transform, "Line");
            }
        }
    }

    private void CacheAcquiredSlotImageColor(Transform root, string childName)
    {
        Transform child = FindChildRecursive(root, childName);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image != null && !acquiredSlotOriginalColors.ContainsKey(image))
            acquiredSlotOriginalColors.Add(image, image.color);
    }

    private static void ApplySlotLayout(
        RectTransform rect,
        int index,
        int columns,
        float horizontalSpacing,
        float verticalSpacing,
        float scale)
    {
        if (rect == null)
            return;

        int safeColumns = Mathf.Max(1, columns);
        rect.anchoredPosition = new Vector2(
            (index % safeColumns) * Mathf.Max(0f, horizontalSpacing),
            -(index / safeColumns) * Mathf.Max(0f, verticalSpacing));
        rect.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private static ErosionData ResolveErosionData(string id)
    {
        DataManager manager = DataManager.Instance;
        return manager?.ErosionDatabase != null &&
               manager.ErosionDatabase.TryGet(id, out ErosionData data)
            ? data
            : null;
    }

    private Sprite ResolveErosionIcon(ErosionData data)
    {
        if (data == null)
            return null;

        if (ErosionDifficultyCatalogUI.TryGetDisplayedErosionIcon(data.DifficultyId, out Sprite cached) &&
            cached != null)
        {
            return cached;
        }

        // 배틀 씬의 ErosionSelect는 자체 아이콘 DB를 가지고 있으므로
        // 결과 패널에 별도 DB가 연결되지 않았을 때 해당 조회 경로를 재사용합니다.
        BattleErosionSelectedLevelsUI[] selectors =
            Object.FindObjectsByType<BattleErosionSelectedLevelsUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BattleErosionSelectedLevelsUI selector in selectors)
        {
            if (selector == null || selector.gameObject.scene != gameObject.scene)
                continue;
            Sprite sceneIcon = selector.GetSelectedIconForResult(data);
            if (sceneIcon != null)
                return sceneIcon;
        }

        // 결과 패널은 로비 씬을 거치지 않고 복원될 수도 있으므로
        // 인스펙터의 DB 참조를 우선 사용합니다.
        ErosionIconDatabase database = erosionIconDatabase;
        if (database == null)
            database = DataManager.Instance?.ErosionIconDatabase;

        if (database == null)
        {
            // Resources에 DB가 하나만 있다면 자동으로 복구합니다.
            ErosionIconDatabase[] available = Resources.LoadAll<ErosionIconDatabase>(string.Empty);
            if (available != null && available.Length == 1)
                database = available[0];
        }

        if (database == null)
            return null;

        erosionIconDatabase = database;
        if (database.TryGetIcon(data, out Sprite icon) && icon != null)
            return icon;

        return database.UnavailableIcon;
    }

    public static int CalculateSelectedErosionScore(
        IReadOnlyList<string> selectedDifficultyIds,
        ErosionDatabase database)
    {
        if (selectedDifficultyIds == null || database == null)
            return 0;

        int score = 0;
        for (int i = 0; i < selectedDifficultyIds.Count; i++)
        {
            string id = selectedDifficultyIds[i];
            if (!string.IsNullOrWhiteSpace(id) &&
                database.TryGet(id, out ErosionData data) &&
                data != null)
            {
                score += data.Score;
            }
        }

        return score;
    }

    public static string FormatErosionTier(int tier)
    {
        return tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => string.Empty
        };
    }

    public void ShowStatisticsOverlay(
        ExplorationResultCharacterRowUI owner,
        string text,
        RectTransform source)
    {
        if (statisticsOverlayPinned && statisticsOverlayOwner != owner)
            return;

        ShowStatisticsOverlayInternal(owner, text, source);
    }

    public void HideStatisticsOverlay(ExplorationResultCharacterRowUI owner)
    {
        if (statisticsOverlayPinned || statisticsOverlayOwner != owner)
            return;

        statisticsOverlayOwner = null;
        if (statisticsOverlayRoot != null)
            statisticsOverlayRoot.SetActive(false);
    }

    public void ToggleStatisticsOverlay(
        ExplorationResultCharacterRowUI owner,
        string text,
        RectTransform source)
    {
        if (statisticsOverlayPinned && statisticsOverlayOwner == owner)
        {
            statisticsOverlayPinned = false;
            statisticsOverlayOwner = null;
            if (statisticsOverlayRoot != null)
                statisticsOverlayRoot.SetActive(false);
            return;
        }

        statisticsOverlayPinned = true;
        ShowStatisticsOverlayInternal(owner, text, source);
    }

    private void ShowStatisticsOverlayInternal(
        ExplorationResultCharacterRowUI owner,
        string text,
        RectTransform source)
    {
        if (statisticsOverlayRoot == null || statisticsOverlayRect == null || source == null)
            return;

        statisticsOverlayOwner = owner;
        SetText(statisticsOverlayText, text);
        PositionStatisticsOverlay(source);
        statisticsOverlayRoot.SetActive(true);
        statisticsOverlayRoot.transform.SetAsLastSibling();
    }

    private void PositionStatisticsOverlay(RectTransform source)
    {
        RectTransform boundary = statisticsOverlayBoundary != null
            ? statisticsOverlayBoundary
            : statisticsOverlayRect.parent as RectTransform;
        if (boundary == null)
            return;

        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 localBottomRight = boundary.InverseTransformPoint(corners[3]);
        Vector2 boundaryLocalPosition = CalculateStatisticsOverlayPosition(
            localBottomRight,
            boundary.rect,
            statisticsOverlayRect.rect.size,
            statisticsOverlayRect.pivot,
            new Vector2(12f, 12f));
        Vector2 overlayAnchor = (statisticsOverlayRect.anchorMin + statisticsOverlayRect.anchorMax) * 0.5f;
        statisticsOverlayRect.anchoredPosition = ConvertBoundaryLocalToAnchoredPosition(
            boundaryLocalPosition,
            boundary.rect,
            overlayAnchor);
    }

    public static Vector2 CalculateStatisticsOverlayPosition(
        Vector2 sourceBottomRight,
        Rect boundary,
        Vector2 overlaySize,
        Vector2 pivot,
        Vector2 gap)
    {
        Vector2 safeGap = new(Mathf.Max(0f, gap.x), Mathf.Max(0f, gap.y));
        Vector2 desired = sourceBottomRight + new Vector2(
            safeGap.x + overlaySize.x * pivot.x,
            -safeGap.y - overlaySize.y * (1f - pivot.y));
        desired.x = Mathf.Clamp(
            desired.x,
            boundary.xMin + overlaySize.x * pivot.x,
            boundary.xMax - overlaySize.x * (1f - pivot.x));
        desired.y = Mathf.Clamp(
            desired.y,
            boundary.yMin + overlaySize.y * pivot.y,
            boundary.yMax - overlaySize.y * (1f - pivot.y));
        return desired;
    }

    public static Vector2 ConvertBoundaryLocalToAnchoredPosition(
        Vector2 boundaryLocalPosition,
        Rect boundary,
        Vector2 normalizedAnchor)
    {
        Vector2 anchorReference = new(
            Mathf.Lerp(boundary.xMin, boundary.xMax, normalizedAnchor.x),
            Mathf.Lerp(boundary.yMin, boundary.yMax, normalizedAnchor.y));
        return boundaryLocalPosition - anchorReference;
    }

    private static CharacterRuntimeData ResolveCharacterRuntime(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || DataManager.Instance?.CharacterRuntimeStore == null)
            return null;

        return DataManager.Instance.CharacterRuntimeStore.TryGet(characterId.Trim(), out CharacterRuntimeData character)
            ? character
            : null;
    }

    private static Sprite ResolveCharacterResultImage(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || DataManager.Instance == null)
            return null;

        string id = characterId.Trim();
        if (DataManager.Instance.CharacterIconDatabase != null)
        {
            if (DataManager.Instance.CharacterIconDatabase.TryGetSideImage(id, out Sprite sideImage))
                return sideImage;

            if (DataManager.Instance.CharacterIconDatabase.TryGetPortrait(id, out Sprite portrait))
                return portrait;
        }

        if (DataManager.Instance.CharacterDatabase != null &&
            DataManager.Instance.CharacterDatabase.TryGet(id, out CharacterMasterData character))
        {
            return character.Icon;
        }

        return null;
    }

    private static bool TryGetExperiencePreview(
        IReadOnlyDictionary<string, BattleStageClearExperiencePreview> previews,
        string characterId,
        out BattleStageClearExperiencePreview preview)
    {
        preview = default;
        if (previews == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        return previews.TryGetValue(characterId.Trim(), out preview);
    }

    private static IReadOnlyList<string> ResolveCharacterUnlockTexts(
        string characterId,
        BattleStageClearExperiencePreview preview)
    {
        if (!preview.LeveledUp ||
            string.IsNullOrWhiteSpace(characterId) ||
            DataManager.Instance?.CharacterDatabase == null)
        {
            return null;
        }

        string id = characterId.Trim();
        if (!DataManager.Instance.CharacterDatabase.TryGet(id, out CharacterMasterData character))
            return null;

        return CharacterLevelUnlockService.GetUnlockTexts(
            character,
            DataManager.Instance.RuneDatabase,
            DataManager.Instance.SkillDatabase,
            preview.LevelBefore,
            preview.LevelAfter);
    }

    private static BattleNodeSummary CountClearedNodeSummary(
        BattleStageClearExperienceContext context)
    {
        return new BattleNodeSummary
        {
            NormalBattleCount = context.NormalBattleClearCount,
            EliteBattleCount = context.EliteBattleClearCount,
            EventCount = context.EventClearCount
        };
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    public static string FormatRedDustium(int amount, float rewardMultiplier)
    {
        int rewardedAmount = Mathf.FloorToInt(Mathf.Max(0, amount) * Mathf.Max(0f, rewardMultiplier));
        return $"+ {rewardedAmount}";
    }

    private string BuildText(ExplorationResultData value, bool defeat, float rewardMultiplier)
    {
        StringBuilder text = new();
        if (defeat)
        {
            text.AppendLine("\uD328\uBC30");
            text.AppendLine($"\uBCF4\uC0C1 {Mathf.FloorToInt(Mathf.Max(0f, rewardMultiplier) * 100f)}%");
            text.AppendLine();
        }

        text.AppendLine("━━━━━━━━━━━━━━━━━");
        text.AppendLine("탐사 결과");
        text.AppendLine("━━━━━━━━━━━━━━━━━\n");
        text.AppendLine($"레드 더스티움\n{value.Remnant}\n");
        text.AppendLine(GameLocalization.Get("common.relic"));
        AppendRelics(text, value);
        text.AppendLine("\n전투 기술 데이터");
        AppendSkills(text, value);
        text.AppendLine("\n캐릭터 전투 통계");
        AppendStatistics(text, value);
        return text.ToString();
    }

    private static void AppendRelics(StringBuilder text, ExplorationResultData value)
    {
        if (value.RelicIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.RelicIds.Count; i++)
        {
            string id = value.RelicIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.RelicDatabase != null &&
                DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic))
            {
                name = string.IsNullOrWhiteSpace(relic.Name) ? id : relic.Name;
                rarity = relic.Rarity;
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendSkills(StringBuilder text, ExplorationResultData value)
    {
        if (value.NewSkillIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.NewSkillIds.Count; i++)
        {
            string id = value.NewSkillIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.SkillDatabase != null &&
                DataManager.Instance.SkillDatabase.TryGet(id, out SkillMasterData skill))
            {
                name = string.IsNullOrWhiteSpace(skill.Name) ? id : GameDataLocalization.SkillName(skill);
                rarity = SkillRarityUtility.GetDisplayName(skill);
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendStatistics(StringBuilder text, ExplorationResultData value)
    {
        if (value.CharacterStatistics.Count == 0) { text.AppendLine("기록 없음"); return; }
        for (int i = 0; i < value.CharacterStatistics.Count; i++)
        {
            BattleRunCharacterStatisticsData stats = value.CharacterStatistics[i];
            string name = stats.CharacterId;
            if (DataManager.Instance?.CharacterDatabase != null &&
                DataManager.Instance.CharacterDatabase.TryGet(stats.CharacterId, out CharacterMasterData character) &&
                !string.IsNullOrWhiteSpace(character.Name))
            {
                name = character.Name;
            }
            text.AppendLine($"[{name}]");
            text.AppendLine($"입힌 피해 {stats.DamageDealt} / 받은 피해 {stats.DamageTaken}");
            text.AppendLine($"사망 {stats.DeathCount} / 처치 {stats.KillCount} / 버프 {stats.BuffApplied}");
        }
    }

    private async void ReturnToBase()
    {
        if (isTransitioning || result == null || DataManager.Instance == null || GameManager.Instance == null)
            return;

        isTransitioning = true;
        if (returnToBaseButton != null)
            returnToBaseButton.interactable = false;

        ApplyStageClearExperienceOnce();

        bool isTutorial = DataManager.Instance.BattleRuntimeStore?.Get()?.IsTutorialBattle == true;
        if (isTutorial)
        {
            if (!TutorialBattleEntrySetup.TryPrepareLobbyPartyAfterTutorial(DataManager.Instance))
            {
                Debug.LogError("[ExplorationResultPanelUI] Failed to prepare tutorial lobby party.");
                isTransitioning = false;
                if (returnToBaseButton != null) returnToBaseButton.interactable = true;
                return;
            }
            TutorialSettings.MarkTutorialSeen();
        }

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        if (isTutorial)
        {
            // 튜토리얼은 탐사 보고만 표시하고 로비 연구 정산 UI를 열지 않습니다.
            PendingResearchSettlementService.Clear(lobby);
        }
        else
        {
            lobby.PendingResearchResult = ExplorationResearchService.CreatePending(
                result,
                DataManager.Instance,
                researchRewardMultiplier);
            lobby.HasPendingResearchResult = true;
        }
        lobby.OwnedRelicIds.Clear();
        ClearLobbyEquippedRelics(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);

        BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);

        // Tutorial HP is only for the introductory battles. The normal lobby run
        // must begin with the party fully healed, after snapshot restoration.
        if (isTutorial)
        {
            for (int slot = 0; slot < DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue; slot++)
            {
                string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(slot);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                if (DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData character) &&
                    character != null)
                {
                    character.CurrentHP = Mathf.Max(1, character.MaxHP);
                }
            }
        }

        SaveSystem.Instance?.SaveCurrentProgress();

        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        cleaner?.Clean();

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private void ApplyStageClearExperienceOnce()
    {
        if (stageClearExperienceApplied ||
            result?.CharacterStatistics == null ||
            DataManager.Instance?.CharacterRuntimeStore == null)
        {
            return;
        }

        stageClearExperienceApplied = true;
        BattleStageClearExperienceService.Apply(
            DataManager.Instance.CharacterRuntimeStore,
            result.CharacterStatistics,
            experienceContext);
    }

    private void BringResultPanelToFront()
    {
        transform.SetAsLastSibling();

        sortingCanvas ??= GetComponent<Canvas>();
        if (sortingCanvas == null)
            return;

        Canvas parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (parentCanvas != null && parentCanvas != sortingCanvas)
            sortingCanvas.sortingLayerID = parentCanvas.sortingLayerID;

        sortingCanvas.overrideSorting = true;
        sortingCanvas.sortingOrder = Mathf.Max(0, resultPanelSortingOrder);
    }

    private static void ClearLobbyEquippedRelics(LobbyRuntimeData lobby)
    {
        if (lobby.CharacterLoadouts == null)
            return;
        for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            lobby.CharacterLoadouts[i].EquippedRelicIds = new string[7];
    }

    private struct BattleNodeSummary
    {
        public int NormalBattleCount;
        public int EliteBattleCount;
        public int EventCount;
    }

    [System.Serializable]
    private sealed class StagePresentationEntry
    {
        [SerializeField] private string stage;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite previewSprite;

        public string Stage => stage;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
