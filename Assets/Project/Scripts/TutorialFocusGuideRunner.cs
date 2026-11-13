using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the new interactive Tutorial sheet rows AFTER the index guide is closed.
/// Tooltip positioning follows HighlightTarget; TooltipPosition 0 means screen center.
/// Advancement is driven by selected character/slot and actual reserved commands, not button click counts.
/// </summary>
public sealed class TutorialFocusGuideRunner : MonoBehaviour
{
    public static TutorialFocusGuideRunner Active { get; private set; }
    public static bool IsRunning => Active != null && Active.running;

    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 165f);
    [SerializeField, Min(0f)] private float tooltipSideGap = 18f;
    [SerializeField] private Vector2 highlightPadding = new Vector2(14f, 14f);
    [SerializeField] private string tooltipObjectName = "TutorialTooltip";
    [SerializeField] private string highlightObjectName = "HighlightFrame";

    private Coroutine routine;
    private bool running;
    private bool moveWasSelected;
    private TutorialData step;
    private BattleTimelineController timeline;
    private RectTransform tooltip;
    private RectTransform highlight;
    private RectTransform highlightLine;
    [Header("Highlight border (fixed-size corners; no 9-slice)")]
    [SerializeField, Min(1f)] private float highlightCornerBorderPixels = 24f;
    private Image originalLineImage;
    private bool originalLineImageEnabled;
    private readonly RawImage[] linePieces = new RawImage[4];
    private float cornerDisplaySize;
    private bool tooltipPositionLocked;
    private readonly List<GameObject> generatedLinePieces = new();

    private RectTransform uiTarget;
    private Transform worldTarget;
    private TMP_Text tooltipText;
    private TMP_Text tooltipCompleteText;
    private int initialCommandCount;
    private int initialMatchingCount;
    // Reuse TutorialMode/FocusOverlay (Top, Bottom, Left, Right).
    // Each element receives its own explicit canvas order so the battle HUD cannot cover it.
    [SerializeField] private int overlaySortingOrder = 30000;
    [Header("Focus overlay entrance")]
    [SerializeField, Min(0f)] private float focusOverlayEnterDuration = 0.35f;
    private float focusOverlayEnterStart;
    private bool focusOverlayEntering;
    private RectTransform dimRoot;
    private Transform presentationRoot;
    private RectTransform presentationViewport;
    private RectTransform skipButton;
    // An independent overlay canvas prevents the focus mask / viewport from
    // covering or clipping the tutorial explanation.
    private GameObject tooltipForegroundRoot;
    private Transform originalTooltipParent;
    private int originalTooltipSibling;

    private bool skipOriginallyActive;
    private bool skipStateSaved;
    private bool rootOriginallyActive;
    private bool viewportOriginallyActive;
    private bool hierarchyStateSaved;
    private readonly Image[] dimParts = new Image[4];
    private readonly List<CanvasState> canvasStates = new();
    private readonly HashSet<GameObject> raisedTooltipRoots = new();
    private float nextTooltipLayerCheck;
    private readonly List<GraphicRaycaster> addedRaycasters = new();

    private sealed class CanvasState
    {
        public Canvas Canvas;
        public bool Added;
        public bool OriginalOverride;
        public int OriginalOrder;
        public int OriginalLayer;
    }

    private void Awake()
    {
        // The scene may serialize HighlightFrame as active. Hide it before the
        // first rendered frame; DisplayStep enables it for an actual focus.
        Transform tutorialMode = FindTutorialMode();
        Transform frame = tutorialMode != null ? FindDescendant(tutorialMode, highlightObjectName) : null;
        if (frame != null) frame.gameObject.SetActive(false);
    }

    public void BeginAfterPanel(string mapId)
    {
        if (routine != null) StopCoroutine(routine);
        running = false;
        routine = StartCoroutine(Run(mapId));
    }

    private IEnumerator Run(string mapId)
    {
        // The panel is spawned synchronously; wait for it to close, not just for its Close() request.
        yield return null;
        while (TutorialGuideController.IsModalOpen)
            yield return null;
        yield return null;

        var all = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (all == null) yield break;
        var guideSteps = new List<TutorialData>();
        foreach (var item in all)
            if (item != null && item.Order > 4 &&
                string.Equals(item.TextOutput, "TutorialTooltip", StringComparison.OrdinalIgnoreCase))
                guideSteps.Add(item);
        if (guideSteps.Count == 0) yield break;
        guideSteps.Sort((a, b) => a.Order.CompareTo(b.Order));

        Active = this;
        running = true;
        timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Exclude);
        ResolvePresentation();
        if (tooltip == null || highlight == null)
            Debug.LogWarning("[TutorialFocusGuideRunner] TutorialTooltip/HighlightFrame not found. Progression remains active, but the presentation is missing.", this);

        foreach (var item in guideSteps)
        {
            step = item;
            moveWasSelected = false;
            initialCommandCount = CountCommands(SlotForStep(item.Order));
            initialMatchingCount = CountMatchingCommands(item.Order);
            DisplayStep(item);
            // State-based completion. Do not advance on merely clicking a skill button.
            while (running && !IsStepComplete(item))
            {
                UpdatePresentation();
                yield return null;
            }
            if (!running) break;
            // Prevent the action that completed this step from being processed as the next step.
            yield return null;
        }
        HidePresentation();
        running = false;
        if (Active == this) Active = null;
        step = null;
        routine = null;
    }

    private void Update()
    {
        if (!running) return;
        UpdatePresentation();
        // TooltipPanel and TooltipUI may be instantiated after this guide begins.
        if (Time.unscaledTime >= nextTooltipLayerCheck)
        {
            nextTooltipLayerCheck = Time.unscaledTime + 0.25f;
            RaiseTooltipLayers();
        }
    }

    public static void NotifyMoveSelected()
    {
        if (IsRunning && Active.step != null && Active.step.Order == 5 && Active.IsSelected("Char_01", 0))
            Active.moveWasSelected = true;
    }

    public static void NotifySkillSelected() { /* Skill selection alone never completes a reservation. */ }

    private bool IsStepComplete(TutorialData current)
    {
        if (timeline == null) return false;
        switch (current.Order)
        {
            case 5: return moveWasSelected;
            case 6: return IsSelected("Char_01", 0) && HasNewMoveAtGrid(0, "Char_01", 16);
            case 7: return IsSelected("Char_01", 0) && CountMatchingCommands(7) > initialMatchingCount;
            case 8: return timeline.ActiveSlotIndex == 1;
            case 9: return IsSelected("Char_02", 1);
            case 10:
            case 11:
                return IsSelected("Char_02", 1) &&
                CountMatchingCommands(current.Order) > initialMatchingCount;
            // Turn end completes via CanClickTutorialEndTurn, immediately before executing the turn.
            case 12: return false;
            default: return true;
        }
    }

    private bool IsSelected(string characterId, int slot) =>
        timeline != null && timeline.ActiveSlotIndex == slot &&
        string.Equals(timeline.SelectedCharacter?.CharacterId, characterId, StringComparison.OrdinalIgnoreCase);

    private int SlotForStep(int order) => order <= 7 ? 0 : 1;

    private int CountCommands(int slot) => timeline?.GetPlayerCommands(slot)?.Count ?? 0;

    private int CountMatchingCommands(int order)
    {
        int slot = order <= 7 ? 0 : 1;
        string character = order <= 7 ? "Char_01" : "Char_02";
        var commands = timeline?.GetPlayerCommands(slot);
        if (commands == null) return 0;
        string expectedSkill = timeline?.SelectedCharacter?.AbilitySkillId;
        int count = 0;
        foreach (var cmd in commands)
            if (cmd != null && string.Equals(cmd.CharacterId, character, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(expectedSkill) &&
                string.Equals(cmd.SkillId, expectedSkill, StringComparison.OrdinalIgnoreCase) &&
                !cmd.IsFacingFlipCommand)
                count++;
        return count;
    }

    private bool HasNewMoveAtGrid(int slot, string character, int gridIndex)
    {
        var commands = timeline?.GetPlayerCommands(slot);
        if (commands == null || commands.Count <= initialCommandCount) return false;
        foreach (var cmd in commands)
            if (cmd != null && string.Equals(cmd.CharacterId, character, StringComparison.OrdinalIgnoreCase) &&
                cmd.ReservedMoveGridIndex == gridIndex)
                return true;
        return false;
    }

    public static bool AllowsSkill(string skillId, string moveId, string firstSkillId)
    {
        if (!IsRunning) return true;
        int order = Active.step?.Order ?? -1;
        bool isMove = !string.IsNullOrEmpty(moveId) && string.Equals(skillId, moveId, StringComparison.OrdinalIgnoreCase);
        bool first = !string.IsNullOrEmpty(firstSkillId) && string.Equals(skillId, firstSkillId, StringComparison.OrdinalIgnoreCase);
        return (order == 5 || order == 6) ? isMove : (order == 7 || order == 10 || order == 11) && first;
    }

    public static bool AllowsShortcut(KeyCode key)
    {
        if (!IsRunning) return true;
        if (key == KeyCode.Escape) return true;
        switch (Active.step?.Order ?? -1)
        {
            case 5: return key == BattleKeyboardShortcutBindings.Move;
            case 7:
            case 10:
            case 11: return key == BattleKeyboardShortcutBindings.Skill01;
            case 8: return key == KeyCode.Alpha2;
            case 9: return key == KeyCode.Tab;
            case 12: return key == KeyCode.Space;
            default: return false;
        }
    }

    public static bool AllowsSlot(int slot)
    {
        if (!IsRunning) return true;
        int order = Active.step?.Order ?? -1;
        return order == 8 ? slot == 1 : (order <= 7 ? slot == 0 : slot == 1);
    }

    public static bool AllowsCharacter(string id)
    {
        if (!IsRunning) return true;
        int order = Active.step?.Order ?? -1;
        return string.Equals(id, order >= 9 ? "Char_02" : "Char_01", StringComparison.OrdinalIgnoreCase);
    }

    public static bool AllowsGrid(int index)
    {
        if (!IsRunning) return true;
        int order = Active.step?.Order ?? -1;
        return order == 6 ? index == 16 : order == 7 || order == 10 || order == 11;
    }

    public static bool AllowsCommand(PlayerReservedCommand command, BattleTimelineController controller)
    {
        if (!IsRunning) return true;
        if (command == null || controller == null) return false;
        int order = Active.step?.Order ?? -1;
        if (order == 6)
            return controller.ActiveSlotIndex == 0 && command.CharacterId == "Char_01" &&
                command.ReservedMoveGridIndex == 16;
        if (order == 7 || order == 10 || order == 11)
        {
            string expectedCharacter = order == 7 ? "Char_01" : "Char_02";
            int expectedSlot = order == 7 ? 0 : 1;
            string expectedSkill = controller.SelectedCharacter?.AbilitySkillId;
            return controller.ActiveSlotIndex == expectedSlot &&
                command.CharacterId == expectedCharacter &&
                !string.IsNullOrEmpty(expectedSkill) &&
                string.Equals(command.SkillId, expectedSkill, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public static bool AllowsMove(PlayerReservedCommand command) =>
        !IsRunning || (Active.step?.Order == 6 && command != null &&
            command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == 16);

    public static bool AllowsEndTurn()
    {
        if (!IsRunning) return true;
        if (Active.step?.Order != 12) return false;
        // Reset restriction before the executor runs the actual turn.
        Active.HidePresentation();
        Active.running = false;
        return true;
    }

    private void ResolvePresentation()
    {
        // The actual hierarchy is TutorialMode / Resolution Viewport / (FocusOverlay,
        // HighlightFrame, TutorialTooltip). The viewport or its children can be inactive.
        presentationRoot = FindTutorialMode();
        if (presentationRoot != null)
        {
            presentationViewport = FindDescendant(presentationRoot, "Resolution Viewport") as RectTransform;
            if (!hierarchyStateSaved)
            {
                rootOriginallyActive = presentationRoot.gameObject.activeSelf;
                viewportOriginallyActive = presentationViewport != null && presentationViewport.gameObject.activeSelf;
                hierarchyStateSaved = true;
            }
            presentationRoot.gameObject.SetActive(true);
            if (presentationViewport != null) presentationViewport.gameObject.SetActive(true);
            Transform searchRoot = presentationViewport != null ? presentationViewport : presentationRoot;
            tooltip = FindDescendant(searchRoot, tooltipObjectName) as RectTransform;
            highlight = FindDescendant(searchRoot, highlightObjectName) as RectTransform;
            skipButton = FindDescendant(searchRoot, "SkipButton") as RectTransform;
            if (skipButton != null && !skipStateSaved)
            {
                skipOriginallyActive = skipButton.gameObject.activeSelf;
                skipStateSaved = true;
            }
        }
        if (tooltip == null || highlight == null)
        {
            foreach (var rect in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (!rect.gameObject.scene.IsValid() || rect.gameObject.scene != gameObject.scene) continue;
                if (tooltip == null && rect.name == tooltipObjectName) tooltip = rect;
                if (highlight == null && rect.name == highlightObjectName) highlight = rect;
            }
        }
        if (highlight != null)
        {
            highlightLine = FindDescendant(highlight, "Line") as RectTransform;
            if (highlightLine == null)
                Debug.LogWarning("[TutorialFocusGuideRunner] HighlightFrame/Line missing.", this);
        }
        if (highlight != null)
            foreach (var graphic in highlight.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        if (tooltip != null)
        {
            foreach (var graphic in tooltip.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            TMP_Text[] labels = tooltip.GetComponentsInChildren<TMP_Text>(true);
            foreach (var candidate in labels)
                if (candidate != null && (candidate.name == "Guide" || candidate.name == "Text" || candidate.name == "GuideText"))
                { tooltipText = candidate; break; }
            if (tooltipText == null && labels.Length > 0) tooltipText = labels[0];
            foreach (var candidate in labels)
                if (candidate != null && candidate.name == "TextComplete")
                { tooltipCompleteText = candidate; break; }
            if (tooltipText != null)
            {
                var local = tooltipText.GetComponent<LocalizedTMPText>();
                if (local != null) local.enabled = false;
                var dynamic = tooltipText.GetComponent<DynamicLocalizedTMPText>();
                if (dynamic != null) dynamic.enabled = false;
            }
        }
        BindExistingFocusOverlay();
        SetupHighlightLinePieces();
        CreateTooltipForegroundCanvas();
        SetupFocusLayerOrder();
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindTutorialMode()
    {
        // Resources.FindObjectsOfTypeAll includes inactive GameObjects, as used by
        // the original TutorialBattleSequence. Restrict results to this scene.
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.name != "TutorialMode" || !candidate.gameObject.scene.IsValid() ||
                candidate.gameObject.scene != gameObject.scene) continue;
            if (FindDescendant(candidate, "FocusOverlay") != null) return candidate;
        }
        return null;
    }

    private void CreateTooltipForegroundCanvas()
    {
        if (tooltip == null || tooltipForegroundRoot != null || presentationRoot == null) return;
        originalTooltipParent = tooltip.parent;
        originalTooltipSibling = tooltip.GetSiblingIndex();

        tooltipForegroundRoot = new GameObject("__TutorialFocusTooltipForeground",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        tooltipForegroundRoot.transform.SetParent(presentationRoot.parent, false);
        RectTransform rootRect = tooltipForegroundRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Canvas rootCanvas = tooltipForegroundRoot.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingLayerID = SortingLayer.NameToID("Default");
        rootCanvas.sortingOrder = overlaySortingOrder + 100;

        CanvasScaler scaler = tooltipForegroundRoot.GetComponent<CanvasScaler>();
        CanvasScaler originalScaler = originalTooltipParent != null
            ? originalTooltipParent.GetComponentInParent<CanvasScaler>() : null;
        if (originalScaler != null)
        {
            scaler.uiScaleMode = originalScaler.uiScaleMode;
            scaler.referenceResolution = originalScaler.referenceResolution;
            scaler.screenMatchMode = originalScaler.screenMatchMode;
            scaler.matchWidthOrHeight = originalScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = originalScaler.referencePixelsPerUnit;
            scaler.scaleFactor = originalScaler.scaleFactor;
        }
        // Position is updated with screen coordinates every frame, so changing
        // the parent does not change where the bubble is anchored.
        tooltip.SetParent(rootRect, false);
        tooltip.SetAsLastSibling();
    }

    private void RestoreTooltipForegroundCanvas()
    {
        if (tooltip != null && originalTooltipParent != null)
        {
            tooltip.SetParent(originalTooltipParent, false);
            tooltip.SetSiblingIndex(Mathf.Clamp(originalTooltipSibling, 0,
                Mathf.Max(0, originalTooltipParent.childCount - 1)));
        }
        originalTooltipParent = null;
        if (tooltipForegroundRoot != null) Destroy(tooltipForegroundRoot);
        tooltipForegroundRoot = null;
    }

    // The Line art contains four corner ornaments, not a scalable rectangular frame.
    // Draw only the four untouched corner regions at fixed size; move them to the
    // highlighted bounds without stretching the original sprite or its ornaments.
    private void SetupHighlightLinePieces()
    {
        if (highlightLine == null || originalLineImage != null) return;
        Image source = highlightLine.GetComponent<Image>();
        if (source == null || source.sprite == null) return;
        Sprite sprite = source.sprite;
        Rect tr = sprite.textureRect;
        float pixels = Mathf.Min(highlightCornerBorderPixels, tr.width * 0.5f, tr.height * 0.5f);
        if (pixels < 1f) return;

        // Convert source texture pixels into the actual size of the original UI artwork.
        // RawImage UV cropping alone does not preserve the original visual scale.
        // Capture the prefab size BEFORE the highlight frame is resized.
        Rect originalRect = highlightLine.rect;
        float sourceScaleX = originalRect.width / Mathf.Max(1f, sprite.rect.width);
        float sourceScaleY = originalRect.height / Mathf.Max(1f, sprite.rect.height);
        cornerDisplaySize = pixels * Mathf.Min(sourceScaleX, sourceScaleY);
        if (cornerDisplaySize <= 0f)
        {
            Debug.LogWarning("[TutorialFocusGuideRunner] HighlightFrame/Line has no initial size; cannot preserve the corner artwork.", this);
            return;
        }

        originalLineImage = source;
        originalLineImageEnabled = source.enabled;
        // Bottom-left, bottom-right, top-left, top-right; independent of sprite border.
        for (int i = 0; i < 4; i++)
        {
            bool right = (i % 2) == 1;
            bool top = i >= 2;
            var go = new GameObject("__TutorialFixedCorner_" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(highlightLine, false);
            var image = go.GetComponent<RawImage>();
            image.texture = sprite.texture;
            image.color = source.color;
            image.raycastTarget = false;
            float px = tr.x + (right ? tr.width - pixels : 0f);
            float py = tr.y + (top ? tr.height - pixels : 0f);
            image.uvRect = new Rect(px / sprite.texture.width,
                py / sprite.texture.height,
                pixels / sprite.texture.width, pixels / sprite.texture.height);
            linePieces[i] = image;
            generatedLinePieces.Add(go);
        }
        source.enabled = false;
        UpdateHighlightLinePieces();
    }

    private void UpdateHighlightLinePieces()
    {
        if (highlightLine == null || linePieces[0] == null) return;
        float halfW = highlightLine.rect.width * 0.5f;
        float halfH = highlightLine.rect.height * 0.5f;
        // Fixed pixel size regardless of the highlight dimensions.
        float size = cornerDisplaySize;
        for (int i = 0; i < 4; i++)
        {
            RawImage image = linePieces[i];
            if (image == null) continue;
            bool right = (i % 2) == 1;
            bool top = i >= 2;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(
                (right ? 1f : -1f) * (halfW - size * 0.5f),
                (top ? 1f : -1f) * (halfH - size * 0.5f));
        }
    }

    private void RestoreHighlightLinePieces()
    {
        foreach (GameObject go in generatedLinePieces)
            if (go != null) Destroy(go);
        generatedLinePieces.Clear();
        for (int i = 0; i < linePieces.Length; i++) linePieces[i] = null;
        if (originalLineImage != null)
            originalLineImage.enabled = originalLineImageEnabled;
        originalLineImage = null;
        cornerDisplaySize = 0f;
    }

    private void SetFrontCanvas(GameObject target, int order, bool needsRaycaster = false)
    {
        if (target == null) return;
        Canvas canvas = target.GetComponent<Canvas>();
        bool added = canvas == null;
        if (added) canvas = target.AddComponent<Canvas>();
        canvasStates.Add(new CanvasState
        {
            Canvas = canvas,
            Added = added,
            OriginalOverride = canvas.overrideSorting,
            OriginalOrder = canvas.sortingOrder,
            OriginalLayer = canvas.sortingLayerID
        });
        canvas.overrideSorting = true;
        // A sorting order is only comparable within the same sorting layer.
        // All tutorial canvases must share one layer, even if the HUD uses another.
        // Match the actual parent canvas, not the separate viewport branch.
        // TutorialTooltip is deliberately moved to an independent overlay canvas.
        Canvas rootCanvas = target.transform.parent != null
            ? target.transform.parent.GetComponentInParent<Canvas>() : null;
        if (rootCanvas != null && rootCanvas != canvas)
            canvas.sortingLayerID = rootCanvas.sortingLayerID;
        canvas.sortingOrder = order;
        if (needsRaycaster && target.GetComponent<GraphicRaycaster>() == null)
            addedRaycasters.Add(target.AddComponent<GraphicRaycaster>());
    }

    private void SetupFocusLayerOrder()
    {
        // The old TutorialMode presentation is in a different canvas branch from
        // BattleHUDCanvas. Give each presentation element a deterministic order,
        // including SkipButton. Merely changing sibling order is insufficient.
        if (presentationViewport != null)
            SetFrontCanvas(presentationViewport.gameObject, overlaySortingOrder - 10);
        if (dimRoot != null)
            SetFrontCanvas(dimRoot.gameObject, overlaySortingOrder, true);
        if (highlight != null)
            SetFrontCanvas(highlight.gameObject, overlaySortingOrder + 10);
        if (tooltip != null)
        {
            SetFrontCanvas(tooltip.gameObject, overlaySortingOrder + 120);
            // Any nested canvases in the tooltip can otherwise render in their
            // original sort order, behind the dimming mask.
            foreach (Canvas nested in tooltip.GetComponentsInChildren<Canvas>(true))
                if (nested != null && nested.gameObject != tooltip.gameObject)
                    SetFrontCanvas(nested.gameObject, overlaySortingOrder + 121);
        }
        if (skipButton != null)
        {
            SetFrontCanvas(skipButton.gameObject, overlaySortingOrder + 110, true);
            foreach (Graphic graphic in skipButton.GetComponentsInChildren<Graphic>(true))
                if (graphic != null && (graphic.GetComponent<Selectable>() != null ||
                    graphic.GetComponentInParent<Selectable>() != null))
                    graphic.raycastTarget = true;
        }

        // Force correct order even when there are other canvases under TutorialMode.
        if (dimRoot != null) dimRoot.SetAsFirstSibling();
        if (highlight != null) highlight.SetAsLastSibling();
        if (tooltip != null) tooltip.SetAsLastSibling();
        if (skipButton != null) skipButton.SetAsLastSibling();
        // Keep the tutorial explanation above dimming and skip overlays.
        if (tooltip != null) tooltip.SetAsLastSibling();
        RaiseTooltipLayers();
    }

    private void RaiseTooltipLayers()
    {
        foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!rect.gameObject.scene.IsValid()) continue;
            if (rect.name != "TooltipPanel" && rect.name != "TooltipUI") continue;
            if (tooltip != null && rect.IsChildOf(tooltip)) continue;
            if (!raisedTooltipRoots.Add(rect.gameObject)) continue;
            SetFrontCanvas(rect.gameObject, overlaySortingOrder + 130);
        }
    }

    private void RestoreFocusLayerOrder()
    {
        foreach (var raycaster in addedRaycasters)
            if (raycaster != null) Destroy(raycaster);
        addedRaycasters.Clear();
        for (int i = canvasStates.Count - 1; i >= 0; i--)
        {
            CanvasState state = canvasStates[i];
            if (state.Canvas == null) continue;
            if (state.Added) Destroy(state.Canvas);
            else
            {
                state.Canvas.overrideSorting = state.OriginalOverride;
                state.Canvas.sortingOrder = state.OriginalOrder;
                state.Canvas.sortingLayerID = state.OriginalLayer;
            }
        }
        canvasStates.Clear();
        raisedTooltipRoots.Clear();
    }

    private void DisplayStep(TutorialData current)
    {
        tooltipPositionLocked = false;
        if (tooltip != null) tooltip.gameObject.SetActive(true);
        if (highlight != null) highlight.gameObject.SetActive(true);
        if (dimRoot != null) dimRoot.gameObject.SetActive(true);
        focusOverlayEnterStart = Time.unscaledTime;
        focusOverlayEntering = focusOverlayEnterDuration > 0f;
        if (skipButton != null) skipButton.gameObject.SetActive(true);
        // The timeline slots are nested under BattleSlot/TimelineBar in the live HUD.
        // Prefer that concrete hierarchy, even when the Excel path omits TimelineBar.
        uiTarget = FindTimelineSlotTarget(current.HighlightTarget);
        if (uiTarget == null) uiTarget = FindUi(current.HighlightTarget);
        worldTarget = uiTarget == null ? FindGrid(current.HighlightTarget) : null;
        if (uiTarget == null && worldTarget == null)
            Debug.LogWarning($"[TutorialFocusGuideRunner] Highlight target missing: {current.HighlightTarget} (order {current.Order})", this);
        if (tooltipText != null)
        {
            string fallback = current.TextKO ?? string.Empty;
            tooltipText.text = string.IsNullOrWhiteSpace(current.TextKey)
                ? fallback : GameLocalization.Get(current.TextKey, fallback);
            LocaleTMPFontGuard.Prepare(tooltipText);
        }
        if (tooltipCompleteText != null)
        {
            string fallback = current.CompleteTextKO ?? string.Empty;
            tooltipCompleteText.text = string.IsNullOrWhiteSpace(current.CompleteTextKey)
                ? fallback : GameLocalization.Get(current.CompleteTextKey, fallback);
            LocaleTMPFontGuard.Prepare(tooltipCompleteText);
        }
        // Resolve the text layout before taking the one-time tooltip position.
        Canvas.ForceUpdateCanvases();
        UpdatePresentation();
    }

    private static RectTransform FindUi(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        string[] names = path.Split('/');
        if (names.Length == 0) return null;
        string leaf = names[names.Length - 1];

        foreach (var rect in FindObjectsByType<RectTransform>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (rect.name != leaf || !rect.gameObject.scene.IsValid()) continue;

            // Match EVERY consecutive parent, not an ordered subsequence.
            // For example, Active/Move must not match Active/Move/Move.
            Transform node = rect;
            int cursor = names.Length - 1;
            while (node != null && cursor >= 0 && node.name == names[cursor])
            {
                node = node.parent;
                cursor--;
            }
            if (cursor < 0) return rect;
        }
        return null;
    }

    // Excel refers to BattleSlot/TimelineSlot02, but the actual hierarchy is
    // BattleSlot/TimelineBar/TimelineSlot02/Order01..Order05.
    private RectTransform FindTimelineSlotTarget(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !path.EndsWith("TimelineSlot02", StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (RectTransform rect in FindObjectsByType<RectTransform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rect.name != "TimelineSlot02" || rect.gameObject.scene != gameObject.scene)
                continue;
            Transform parent = rect.parent;
            if (parent == null || parent.name != "TimelineBar") continue;
            if (parent.parent == null || parent.parent.name != "BattleSlot") continue;
            return rect;
        }

        Debug.LogWarning("[TutorialFocusGuideRunner] BattleSlot/TimelineBar/TimelineSlot02 not found.", this);
        return null;
    }

    // The slot's own RectTransform may represent only the selection header.
    // Include all five Order cells so the frame and focus opening cover the
    // complete timeline column, without including adjacent timeline slots.
    private static void ExpandTimelineSlotScreenBounds(
        RectTransform slot, ref Vector2 screenMin, ref Vector2 screenMax)
    {
        if (slot == null || slot.name != "TimelineSlot02") return;
        bool foundOrder = false;
        for (int i = 1; i <= 5; i++)
        {
            Transform order = slot.Find($"Order{i:00}");
            if (!(order is RectTransform orderRect)) continue;
            AddRectScreenBounds(orderRect, ref screenMin, ref screenMax);
            foundOrder = true;
        }
        if (!foundOrder)
            Debug.LogWarning("[TutorialFocusGuideRunner] TimelineSlot02 has no Order01-Order05 RectTransforms.", slot);
    }

    private static void AddRectScreenBounds(
        RectTransform rect, ref Vector2 screenMin, ref Vector2 screenMax)
    {
        if (rect == null) return;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        foreach (Vector3 corner in corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            screenMin = Vector2.Min(screenMin, point);
            screenMax = Vector2.Max(screenMax, point);
        }
    }

    private static Transform FindGrid(string target)
    {
        if (string.IsNullOrEmpty(target) || !target.StartsWith("Grid_", StringComparison.OrdinalIgnoreCase)) return null;
        if (!int.TryParse(target.Substring(5), out int index)) return null;
        foreach (var cell in FindObjectsByType<GridCell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (cell.Index == index) return cell.transform;
        return null;
    }

    private void UpdatePresentation()
    {
        if (highlight == null || (uiTarget == null && worldTarget == null))
        {
            if (tooltip != null && !tooltipPositionLocked && step?.TooltipPosition?.Trim() == "0")
            {
                SetScreenCenter(tooltip, new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
                tooltipPositionLocked = true;
            }
            return;
        }

        // Compute the target bounds in screen coordinates first. RectTransform local units
        // cannot be used directly when the target and highlight have different canvases.
        Vector2 screenMin;
        Vector2 screenMax;
        if (uiTarget != null)
        {
            screenMin = new Vector2(float.MaxValue, float.MaxValue);
            screenMax = new Vector2(float.MinValue, float.MinValue);
            AddRectScreenBounds(uiTarget, ref screenMin, ref screenMax);
            ExpandTimelineSlotScreenBounds(uiTarget, ref screenMin, ref screenMax);
        }
        else
        {
            // Grid cells live in world space. Derive the focus opening from the
            // actual tile bounds, not a fixed 76-pixel square. This stays aligned
            // when the battle camera moves, zooms, or changes resolution.
            if (!TryGetGridScreenBounds(worldTarget, out screenMin, out screenMax)) return;
        }

        // Button hover animations may scale their RectTransform. Position the
        // tooltip only once per tutorial step so it does not chase that scaling.
        if (tooltip != null && !tooltipPositionLocked)
        {
            SetScreenCenter(tooltip, GetTooltipScreenPosition(screenMin, screenMax));
            tooltipPositionLocked = true;
        }

        RectTransform parent = highlight.parent as RectTransform;
        if (parent == null) return;
        Canvas canvas = highlight.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenMin, cam, out Vector2 localMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenMax, cam, out Vector2 localMax)) return;

        // Parent canvas units can differ from target canvas units. Fit the rect in
        // its own parent's local space, ignoring the prefab's old size/rotation.
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = Vector3.one;
        highlight.anchorMin = new Vector2(0.5f, 0.5f);
        highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        Vector2 center = (localMin + localMax) * 0.5f;
        Vector2 dimensions = new Vector2(Mathf.Abs(localMax.x - localMin.x),
                                         Mathf.Abs(localMax.y - localMin.y));
        highlight.anchoredPosition = center;
        highlight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.Max(1f, dimensions.x + highlightPadding.x));
        highlight.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(1f, dimensions.y + highlightPadding.y));
        // The visible border belongs to HighlightFrame/Line, not HighlightFrame.
        // Keep the parent as the positioned frame and fit its Line to exactly
        // that frame, regardless of the prefab's previous anchored size.
        if (highlightLine != null)
        {
            highlightLine.localRotation = Quaternion.identity;
            highlightLine.localScale = Vector3.one;
            highlightLine.anchorMin = new Vector2(0.5f, 0.5f);
            highlightLine.anchorMax = new Vector2(0.5f, 0.5f);
            highlightLine.pivot = new Vector2(0.5f, 0.5f);
            highlightLine.anchoredPosition = Vector2.zero;
            highlightLine.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, highlight.rect.width);
            highlightLine.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, highlight.rect.height);
            UpdateHighlightLinePieces();
        }
        UpdateDimmingMask((screenMin + screenMax) * 0.5f);
    }

    // Project the eight corners of a cell's world-space bounds. Using the cell's
    // own Renderer first avoids including its much larger hover/preview children.
    // Collider is a fallback for cells without a usable renderer.
    private static bool TryGetGridScreenBounds(Transform gridTransform,
        out Vector2 screenMin, out Vector2 screenMax)
    {
        screenMin = Vector2.zero;
        screenMax = Vector2.zero;
        Camera camera = Camera.main;
        if (gridTransform == null || camera == null) return false;

        Bounds bounds;
        Renderer cellRenderer = gridTransform.GetComponent<Renderer>();
        Collider cellCollider = gridTransform.GetComponent<Collider>();
        if (cellRenderer != null && cellRenderer.enabled &&
            cellRenderer.bounds.size.sqrMagnitude > 0.000001f)
            bounds = cellRenderer.bounds;
        else if (cellCollider != null && cellCollider.bounds.size.sqrMagnitude > 0.000001f)
            bounds = cellCollider.bounds;
        else
        {
            Vector3 centerPoint = camera.WorldToScreenPoint(gridTransform.position);
            if (centerPoint.z <= 0f) return false;
            Vector2 center = new Vector2(centerPoint.x, centerPoint.y);
            screenMin = center - Vector2.one * 38f;
            screenMax = center + Vector2.one * 38f;
            return true;
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector2 low = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 high = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 worldCorner = new Vector3(x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                    Vector3 projected = camera.WorldToScreenPoint(worldCorner);
                    if (projected.z <= 0f) return false;
                    Vector2 point = new Vector2(projected.x, projected.y);
                    low = Vector2.Min(low, point);
                    high = Vector2.Max(high, point);
                }
        screenMin = low;
        screenMax = high;
        return true;
    }

    private Vector3 GetTooltipScreenPosition(Vector2 targetMin, Vector2 targetMax)
    {
        string placement = step?.TooltipPosition?.Trim();
        if (placement == "0")
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Vector2 center = (targetMin + targetMax) * 0.5f;
        float gap = tooltipSideGap;
        float tooltipHalfWidth = GetScreenWidth(tooltip) * 0.5f;
        float tooltipHalfHeight = GetScreenHeight(tooltip) * 0.5f;
        if (string.Equals(placement, "Left", StringComparison.OrdinalIgnoreCase))
            return new Vector3(targetMin.x - tooltipHalfWidth - gap, center.y, 0f);
        if (string.Equals(placement, "Right", StringComparison.OrdinalIgnoreCase))
            return new Vector3(targetMax.x + tooltipHalfWidth + gap, center.y, 0f);
        if (string.Equals(placement, "Below", StringComparison.OrdinalIgnoreCase))
            return new Vector3(center.x + tooltipOffset.x,
                targetMin.y - tooltipHalfHeight - gap, 0f);
        // Above, including a blank value, maintains a small gap from the target edge.
        return new Vector3(center.x + tooltipOffset.x,
            targetMax.y + tooltipHalfHeight + gap, 0f);
    }

    private static float GetScreenHeight(RectTransform rect)
    {
        if (rect == null) return 0f;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 top = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
        return Vector2.Distance(bottom, top);
    }

    private static float GetScreenWidth(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 left = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 right = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
        return Vector2.Distance(left, right);
    }

    private static void SetScreenCenter(RectTransform rect, Vector3 position)
    {
        var parent = rect.parent as RectTransform;
        if (parent == null) return;
        Canvas c = rect.GetComponentInParent<Canvas>();
        Camera cam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, position, cam, out var world))
            rect.position = world;
    }

    private void BindExistingFocusOverlay()
    {
        if (dimRoot != null) return;
        Transform tutorialRoot = FindTutorialMode();
        if (tutorialRoot == null)
        {
            Debug.LogWarning("[TutorialFocusGuideRunner] Scene TutorialMode/FocusOverlay not found (including inactive objects).", this);
            return;
        }
        Transform overlay = FindDescendant(
            presentationViewport != null ? (Transform)presentationViewport : tutorialRoot,
            "FocusOverlay");
        dimRoot = overlay as RectTransform;
        if (dimRoot == null)
        {
            Debug.LogWarning("[TutorialFocusGuideRunner] FocusOverlay needs a RectTransform.", this);
            return;
        }
        if (dimRoot.TryGetComponent(out Graphic rootGraphic)) rootGraphic.raycastTarget = false;
        string[] names = { "Top", "Bottom", "Left", "Right" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform part = overlay.Find(names[i]);
            if (part == null || !part.TryGetComponent(out Image graphic))
            {
                Debug.LogWarning($"[TutorialFocusGuideRunner] FocusOverlay/{names[i]} Image missing.", this);
                continue;
            }
            dimParts[i] = graphic;
            graphic.raycastTarget = true;
            // Position the four masks in FocusOverlay-local coordinates.
            var rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }
        dimRoot.anchorMin = Vector2.zero;
        dimRoot.anchorMax = Vector2.one;
        dimRoot.offsetMin = Vector2.zero;
        dimRoot.offsetMax = Vector2.zero;
        dimRoot.gameObject.SetActive(true);
    }

    private void UpdateDimmingMask(Vector3 screenFocus)
    {
        if (dimRoot == null || highlight == null) return;
        Canvas canvas = dimRoot.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dimRoot, screenFocus, cam, out var focus)) return;
        Rect dimBounds = dimRoot.rect;
        float left = dimBounds.xMin, right = dimBounds.xMax;
        float bottom = dimBounds.yMin, top = dimBounds.yMax;
        // The highlight is sized in local units of its parent. Use its screen-space corners
        // for the mask so it also works when the tooltip and focus have different ancestors.
        Vector3[] corners = new Vector3[4];
        highlight.GetWorldCorners(corners);
        Canvas highlightCanvas = highlight.GetComponentInParent<Canvas>();
        Camera highlightCam = highlightCanvas != null && highlightCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? highlightCanvas.worldCamera : null;
        Vector2 low, high;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimRoot,
            RectTransformUtility.WorldToScreenPoint(highlightCam, corners[0]), cam, out low);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimRoot,
            RectTransformUtility.WorldToScreenPoint(highlightCam, corners[2]), cam, out high);
        float x0 = Mathf.Clamp(Mathf.Min(low.x, high.x), left, right);
        float x1 = Mathf.Clamp(Mathf.Max(low.x, high.x), left, right);
        float y0 = Mathf.Clamp(Mathf.Min(low.y, high.y), bottom, top);
        float y1 = Mathf.Clamp(Mathf.Max(low.y, high.y), bottom, top);
        // Slide each of the four mask panels inward from the outer screen edges.
        // Unlike fading the entire overlay, this preserves a clear focus opening.
        float progress = 1f;
        if (focusOverlayEntering)
        {
            progress = focusOverlayEnterDuration <= 0f ? 1f :
                Mathf.Clamp01((Time.unscaledTime - focusOverlayEnterStart) / focusOverlayEnterDuration);
            progress = progress * progress * (3f - 2f * progress);
            if (progress >= 1f) focusOverlayEntering = false;
        }
        float movingTop = Mathf.Lerp(top, y1, progress);
        float movingBottom = Mathf.Lerp(bottom, y0, progress);
        float movingLeft = Mathf.Lerp(left, x0, progress);
        float movingRight = Mathf.Lerp(right, x1, progress);
        SetDimPart(0, left, right, movingTop, top);
        SetDimPart(1, left, right, bottom, movingBottom);
        SetDimPart(2, left, movingLeft, y0, y1);
        SetDimPart(3, movingRight, right, y0, y1);
    }

    private void SetDimPart(int i, float left, float right, float bottom, float top)
    {
        if (dimParts[i] == null) return;
        RectTransform rect = dimParts[i].rectTransform;
        rect.anchoredPosition = new Vector2((left + right) / 2f, (bottom + top) / 2f);
        rect.sizeDelta = new Vector2(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom));
    }

    private void HidePresentation()
    {
        focusOverlayEntering = false;
        tooltipPositionLocked = false;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (dimRoot != null) dimRoot.gameObject.SetActive(false);
        if (skipButton != null && skipStateSaved) skipButton.gameObject.SetActive(skipOriginallyActive);
        RestoreFocusLayerOrder();
        RestoreTooltipForegroundCanvas();
        RestoreHighlightLinePieces();
        if (hierarchyStateSaved)
        {
            if (presentationViewport != null) presentationViewport.gameObject.SetActive(viewportOriginallyActive);
            if (presentationRoot != null) presentationRoot.gameObject.SetActive(rootOriginallyActive);
            hierarchyStateSaved = false;
        }
    }

    private void OnDisable()
    {
        HidePresentation();
        running = false;
        if (Active == this) Active = null;
        // FocusOverlay belongs to the scene; never destroy it.
    }
}
