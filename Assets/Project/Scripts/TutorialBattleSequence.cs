using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Sheet-driven tutorial presentation. The previous hard-coded battle/event tutorial
/// steps were removed; new phases can be added here as the new worksheet grows.
/// </summary>
public sealed class TutorialBattleSequence : MonoBehaviour
{
    private static bool dialoguePresentationRunning;
    public static bool IsPresentationRunning => dialoguePresentationRunning || TutorialFocusGuideRunner.IsRunning;
    public static bool IsBlockingBattleStart => false;
    public static string ActiveTutorialMapId { get; private set; } = string.Empty;
    public static int ActiveBattleGuideStep => -1;
    public static bool IsFirstTutorialRunning => false;
    public static bool IsFinalTutorialRunning => false;
    public static bool IsContinuationActive => false;
    public static int CurrentContinuationOrder => 0;

    // Compatibility entrypoints for callers that have not yet migrated to the
    // new tutorial step API. No legacy tutorial state machine is retained.
    public static bool CanClickTutorialMonster() => !IsPresentationRunning || TutorialFocusGuideRunner.IsMonsterInspectClickStep;
    public static bool CanClickTutorialPlayer() => !IsPresentationRunning;
    public static bool CanClickFinalTutorialWorldMonster(MonsterUnit monster) => TutorialFocusGuideRunner.AllowsMonsterInfoWorldClick(monster);
    public static bool CanClickFinalTutorialWorldCharacter(string characterId) => false;
    public static void NotifyFinalTutorialWorldCharacterClicked(string characterId) { }
    public static void NotifyFinalTutorialCompoundUsed() { }
    public static bool CanUseBattleSkill(string skillId, string moveId, string firstSkillId) =>
        TutorialFocusGuideRunner.IsRunning ? TutorialFocusGuideRunner.AllowsSkill(skillId, moveId, firstSkillId) : !IsPresentationRunning;
    public static bool IsShortcutAllowed(KeyCode key) => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsShortcut(key) : !IsPresentationRunning || key == KeyCode.Escape;
    // Permit the battle room to initialise its default character before dialogue.
    // Player-driven selections remain blocked once the dialogue overlay begins.
    public static bool CanSelectTutorialCharacter(string characterId) => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsCharacter(characterId) : !IsPresentationRunning || !dialogueInputActive;
    public static bool CanClickTutorialGrid(int gridIndex) => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsGrid(gridIndex) : !IsPresentationRunning;
    public static bool CanAcceptTutorialMove(PlayerReservedCommand command) => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsMove(command) : !IsPresentationRunning;
    public static bool CanAcceptTutorialCommand(PlayerReservedCommand command, BattleTimelineController timeline) => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsCommand(command, timeline) : !IsPresentationRunning;
    public static bool CanClickTutorialEndTurn() => TutorialFocusGuideRunner.IsRunning
        ? TutorialFocusGuideRunner.AllowsEndTurn() : !IsPresentationRunning;
    public static void StartContinuation(string mapId, int firstOrder, int lastOrder) { }
    public static void NotifyTargetSelected(string target) { }

    private static bool dialogueInputActive;
    private static int suppressAdvanceUntilFrame;
    public static void NotifyModalInteraction()
    {
        suppressAdvanceUntilFrame = Mathf.Max(suppressAdvanceUntilFrame, Time.frameCount + 1);
    }

    public static string ResolveDialogueAdvanceSoundId(string soundId) =>
        string.IsNullOrWhiteSpace(soundId) ? AudioIds.Sfx.TutorialTextAdvance : soundId.Trim();

    [SerializeField, Min(1f)] private float dialogueCharactersPerSecond = 35f;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string dialogueAdvanceSoundId = AudioIds.Sfx.TutorialTextAdvance;
    [SerializeField, Range(0f, 1f)] private float dialogueAdvanceSoundVolume = 0.5f;

    private Coroutine routine;
    private bool initialHudReady;
    private GameObject dialogRoot;
    private GameObject tooltipRoot;
    private GameObject focusRoot;
    private GameObject overlayRoot;
    private TMP_Text dialogText;
    private TMP_Text completeText;
    private Image portrait;
    private Transform tutorialRoot;
    // An invisible, front-most raycast shield prevents clicks on unrelated UI.
    // The skip and menu buttons use their own higher-sorting canvases.
    private GameObject dialogueInputShield;
    private CanvasGroup dialogueShieldGroup;
    private Canvas skipCanvas;
    private bool addedSkipCanvas;
    private GraphicRaycaster skipRaycaster;
    private bool addedSkipRaycaster;
    private bool originalSkipOverrideSorting;
    private int originalSkipSortingOrder;
    private Canvas dialogCanvas;
    private bool addedDialogCanvas;
    private GraphicRaycaster dialogRaycaster;
    private bool addedDialogRaycaster;
    private bool originalDialogOverrideSorting;
    private int originalDialogSortingOrder;
    private readonly List<RaycastResult> raycastResults = new();

    // Only the relic reward dialogue uses this temporary screen-space overlay.
    private GameObject relicDialogueOverlay;
    private Transform relicDialogOriginalParent;
    private int relicDialogOriginalSibling;
    private Vector2 relicDialogAnchorsMin, relicDialogAnchorsMax, relicDialogPivot;
    private Vector2 relicDialogSize, relicDialogPosition;
    private Vector3 relicDialogScale;
    private Quaternion relicDialogRotation;

    private void PutRelicDialogueInFront()
    {
        if (dialogRoot == null || relicDialogueOverlay != null) return;
        RectTransform dialog = dialogRoot.transform as RectTransform;
        if (dialog == null) return;

        // Capture its current on-screen rectangle before changing canvas hierarchy.
        Canvas sourceCanvas = dialog.GetComponentInParent<Canvas>();
        Camera camera = sourceCanvas != null && sourceCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.rootCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        dialog.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        Vector2 pivotScreen = RectTransformUtility.WorldToScreenPoint(camera, dialog.position);

        Vector2 originalRectSize = dialog.rect.size;
        relicDialogOriginalParent = dialog.parent;
        relicDialogOriginalSibling = dialog.GetSiblingIndex();
        relicDialogAnchorsMin = dialog.anchorMin;
        relicDialogAnchorsMax = dialog.anchorMax;
        relicDialogPivot = dialog.pivot;
        relicDialogSize = dialog.sizeDelta;
        relicDialogPosition = dialog.anchoredPosition;
        relicDialogScale = dialog.localScale;
        relicDialogRotation = dialog.localRotation;

        relicDialogueOverlay = new GameObject("__RelicTutorialDialogueOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        RectTransform overlayRect = relicDialogueOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Canvas overlayCanvas = relicDialogueOverlay.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 32760;

        dialog.SetParent(overlayRect, false);
        dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
        dialog.pivot = relicDialogPivot;
        dialog.localRotation = Quaternion.identity;
        // Preserve the original RectTransform layout; compensate for the source
        // canvas scale on this pixel-sized overlay instead of enlarging sizeDelta.
        float screenWidth = Mathf.Abs(topRight.x - bottomLeft.x);
        float screenHeight = Mathf.Abs(topRight.y - bottomLeft.y);
        dialog.sizeDelta = originalRectSize;
        dialog.localScale = new Vector3(
            originalRectSize.x > 0.01f ? screenWidth / originalRectSize.x : 1f,
            originalRectSize.y > 0.01f ? screenHeight / originalRectSize.y : 1f,
            1f);
        dialog.anchoredPosition = pivotScreen - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void RestoreRelicDialogueParent()
    {
        if (relicDialogueOverlay == null) return;
        if (dialogRoot != null && relicDialogOriginalParent != null)
        {
            RectTransform rect = dialogRoot.transform as RectTransform;
            if (rect != null)
            {
                rect.SetParent(relicDialogOriginalParent, false);
                rect.anchorMin = relicDialogAnchorsMin;
                rect.anchorMax = relicDialogAnchorsMax;
                rect.pivot = relicDialogPivot;
                rect.sizeDelta = relicDialogSize;
                rect.anchoredPosition = relicDialogPosition;
                rect.localScale = relicDialogScale;
                rect.localRotation = relicDialogRotation;
                rect.SetSiblingIndex(Mathf.Min(relicDialogOriginalSibling, relicDialogOriginalParent.childCount - 1));
            }
        }
        Destroy(relicDialogueOverlay);
        relicDialogueOverlay = null;
        relicDialogOriginalParent = null;
    }


    public void NotifyInitialBattleHudReady() => initialHudReady = true;

    public void Begin(BattleRoomLoader loader, string mapId)
    {
        if (routine != null) StopCoroutine(routine);
        Cleanup();
        IReadOnlyList<TutorialData> steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || steps.Count == 0) return;
        ActiveTutorialMapId = mapId ?? string.Empty;
        initialHudReady = false;
        dialogueInputActive = false;
        dialoguePresentationRunning = true;
        // Block inventory, equipment and other unrelated UI from the moment
        // the tutorial begins, not only after the opening dialogue appears.
        // The battle room may still complete its automatic character selection.
        ResolveUi();
        HideUi();
        CreateDialogueShield();
        routine = StartCoroutine(Run(steps));
    }

    private IEnumerator Run(IReadOnlyList<TutorialData> steps)
    {
        // BattleRoomLoader owns monster creation, enemy intent planning and the
        // normal battle-start presentation. It signals this runner only after
        // its initial HUD/timeline presentation has completed.
        while (!initialHudReady) yield return null;
        while (BattleSceneController.IsBattleRoomIntroPlaying || BattleMapIntroText.IsAnyPlayingOrVisible())
            yield return null;
        // The initial HUD-ready event occurs while BattleTurnExecutor is still
        // holding player input locked, so its reservation grid is hidden.
        // Show it explicitly and wait for a visible grid AND character selection.
        GridManager grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
        if (grid != null) grid.SetGridVisible(true);
        while (true)
        {
            grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
            BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Exclude);
            bool selected = timeline != null && timeline.SelectedCharacter != null;
            bool visible = grid != null && grid.IsGridVisible && HasVisibleGridCells(grid);
            if (selected && visible &&
                !BattleSceneController.IsBattleRoomIntroPlaying &&
                !BattleMapIntroText.IsAnyPlayingOrVisible())
                break;
            yield return null;
        }
        // Selecting a character in the battle loader is allowed while player input is
        // locked, but its timeline and character portrait feedback can be deferred.
        // Force both visuals now; never start the dialogue before they are visible.
        BattleTimelineController selectedTimeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Exclude);
        BattleCharacterPanelUI characterPanel = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Exclude);
        if (selectedTimeline != null && selectedTimeline.SelectedCharacter != null)
        {
            if (selectedTimeline.ActiveSlotIndex < 0)
                selectedTimeline.SelectTimelineSlotFromNetwork(0, false, true);
            characterPanel?.RefreshTutorialInitialSelectionHighlight();
        }

        string selectedCharacterId = selectedTimeline != null && selectedTimeline.SelectedCharacter != null
            ? selectedTimeline.SelectedCharacter.CharacterId : null;
        bool loggedMissingHighlight = false;
        float highlightWait = 0f;
        while (characterPanel != null &&
               !characterPanel.IsCharacterSelectionHighlightVisible(selectedCharacterId))
        {
            highlightWait += Time.unscaledDeltaTime;
            if (!loggedMissingHighlight && highlightWait >= 5f)
            {
                loggedMissingHighlight = true;
                Debug.LogWarning("[TutorialBattleSequence] Waiting for initial character selection highlight; dialogue remains blocked.", this);
                characterPanel.RefreshTutorialInitialSelectionHighlight();
            }
            yield return null;
        }
        // The animation must have finished before we reveal the first dialogue.
        yield return new WaitForEndOfFrame();
        float visibleHold = 0f;
        while (visibleHold < 0.2f)
        {
            visibleHold += Time.unscaledDeltaTime;
            yield return null;
        }

        // The shield has been active since Begin(), so players could not
        // open auxiliary panels during the intro or selection/highlight wait.
        dialogueInputActive = true;
        foreach (TutorialData step in steps)
        {
            if (step == null || step.Order > 4 ||
                !string.Equals(step.TextOutput, "Dialogbox", StringComparison.OrdinalIgnoreCase))
                continue;
            ShowDialogue(step);
            yield return RevealAndAdvance();
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }
        // Preserve the current map before Cleanup resets ActiveTutorialMapId.
        string guideIndex = string.Equals(ActiveTutorialMapId, "Map_28", StringComparison.OrdinalIgnoreCase)
            ? "Index04" : "Index01";
        Cleanup();
        // Release the dialogue raycast shield before displaying the guide.
        // Spawn next frame so Destroy(shield) completes first.
        yield return null;
        TutorialGuideController.UnlockIndex(guideIndex);
        BattleSceneController scene = FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);
        if (scene != null) scene.SpawnTutorialGuideAfterDialogue(guideIndex);
        else Debug.LogError("[TutorialBattleSequence] BattleSceneController missing; guide not opened.", this);
        enabled = false;
    }

    // Reuse the opening dialogue presentation for the first battle's victory lines.
    // Called after the battle-end text and before the reward panel is opened.
    public IEnumerator PlayVictoryDialogueAndGuide(string mapId)
    {
        IReadOnlyList<TutorialData> steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null) yield break;

        var victoryLines = new List<TutorialData>();
        foreach (TutorialData item in steps)
            if (item != null && item.Order >= 13 && item.Order <= 15 &&
                string.Equals(item.TextOutput, "Dialogbox", StringComparison.OrdinalIgnoreCase))
                victoryLines.Add(item);
        if (victoryLines.Count == 0) yield break;
        victoryLines.Sort((a, b) => a.Order.CompareTo(b.Order));

        dialoguePresentationRunning = true;
        dialogueInputActive = true;
        ActiveTutorialMapId = mapId;
        ResolveUi();
        HideUi();
        CreateDialogueShield();
        try
        {
            foreach (TutorialData line in victoryLines)
            {
                ShowDialogue(line);
                yield return RevealAndAdvance();
                if (dialogRoot != null) dialogRoot.SetActive(false);
            }
        }
        finally
        {
            Cleanup();
        }

        // The shield must be destroyed before the modal guide takes input.
        yield return null;
        BattleSceneController scene = FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);
        if (scene == null)
        {
            Debug.LogError("[TutorialBattleSequence] BattleSceneController missing; Index02 not opened.", this);
            yield break;
        }
        scene.SpawnTutorialGuideAfterDialogue("Index02");
        yield return null;
        while (TutorialGuideController.IsModalOpen)
            yield return null;
    }

    // Event tutorial: play the two opening lines after the title and room intro,
    // before the event choices enter the screen.
    public IEnumerator PlayFirstEventEntranceDialogue()
    {
        const string mapId = TutorialBattleEntrySetup.FirstTutorialEventMapId;
        IReadOnlyList<TutorialData> steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || steps.Count == 0)
        {
            Debug.LogError("[TutorialBattleSequence] Tutorial_02 Map_27 rows are not loaded in TutorialDatabase. Reimport the Tutorial sheet from GameData.xlsx into Unity game data.", this);
            yield break;
        }

        var lines = new List<TutorialData>();
        foreach (TutorialData step in steps)
        {
            if (step != null && step.TutorialId == "Tutorial_02" &&
                step.Order >= 1 && step.Order <= 2 &&
                string.Equals(step.TextOutput, "Dialogbox", StringComparison.OrdinalIgnoreCase))
                lines.Add(step);
        }
        if (lines.Count == 0)
        {
            Debug.LogWarning("[TutorialBattleSequence] Tutorial_02 dialogue Order 1-2 not found for Map_27. Check the loaded GameData Tutorial sheet and TextOutput=Dialogbox.", this);
            yield break;
        }
        lines.Sort((a, b) => a.Order.CompareTo(b.Order));
        Debug.Log($"[TutorialBattleSequence] Tutorial_02 event dialogue ready: {lines.Count} line(s).", this);

        dialoguePresentationRunning = true;
        dialogueInputActive = true;
        ActiveTutorialMapId = mapId;
        ResolveUi();
        if (dialogRoot == null || dialogText == null)
        {
            Debug.LogError("[TutorialBattleSequence] Event dialogue UI not found under TutorialMode/Dialogbox.", this);
            Cleanup();
            yield break;
        }
        HideUi();
        CreateDialogueShield();
        // EventRoom uses a separate foreground UI layer. Reuse the existing
        // overlay path (including its original on-screen size compensation)
        // rather than creating another dialog presentation mechanism.
        PutRelicDialogueInFront();
        if (dialogCanvas != null) dialogCanvas.sortingOrder = 32765;
        try
        {
            foreach (TutorialData line in lines)
            {
                ShowDialogue(line);
                yield return RevealAndAdvance();
                if (dialogRoot != null) dialogRoot.SetActive(false);
            }
        }
        finally
        {
            Cleanup();
        }
    }

    // Triggered only after the first tutorial relic panel has finished its entrance animation.
    // Dialogue orders 16+ are intentionally data-driven; an empty range is a no-op.
    public IEnumerator PlayFirstRelicRewardDialogue()
    {
        IReadOnlyList<TutorialData> steps = DataManager.Instance?.TutorialDatabase?.GetForMap("Map_26");
        if (steps == null) yield break;

        var lines = new List<TutorialData>();
        foreach (TutorialData step in steps)
        {
            if (step != null && step.Order >= 16 && step.Order <= 17 &&
                string.Equals(step.TextOutput, "Dialogbox", StringComparison.OrdinalIgnoreCase))
                lines.Add(step);
        }
        if (lines.Count == 0) yield break;
        lines.Sort((a, b) => a.Order.CompareTo(b.Order));

        dialoguePresentationRunning = true;
        dialogueInputActive = true;
        ActiveTutorialMapId = "Map_26";
        ResolveUi();
        HideUi();
        CreateDialogueShield();
        PutRelicDialogueInFront();
        if (dialogCanvas != null) dialogCanvas.sortingOrder = 32765;
        try
        {
            foreach (TutorialData line in lines)
            {
                ShowDialogue(line);
                yield return RevealAndAdvance();
                if (dialogRoot != null) dialogRoot.SetActive(false);
            }
        }
        finally
        {
            RestoreRelicDialogueParent();
            Cleanup();
        }
    }

    // Tutorial_02: chest reward dialogue, after Index03 and the reward panel intro.
    public IEnumerator PlaySecondTutorialRelicRewardDialogue()
    {
        const string mapId = "Map_27";
        var steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null) yield break;
        var lines = new List<TutorialData>();
        foreach (TutorialData item in steps)
            if (item != null && item.TutorialId == "Tutorial_02" &&
                item.Order >= 3 && item.Order <= 4 &&
                string.Equals(item.TextOutput, "Dialogbox", StringComparison.OrdinalIgnoreCase))
                lines.Add(item);
        if (lines.Count == 0) yield break;
        lines.Sort((a, b) => a.Order.CompareTo(b.Order));
        dialoguePresentationRunning = true;
        dialogueInputActive = true;
        ActiveTutorialMapId = mapId;
        ResolveUi();
        HideUi();
        CreateDialogueShield();
        PutRelicDialogueInFront();
        if (dialogCanvas != null) dialogCanvas.sortingOrder = 32765;
        try
        {
            foreach (TutorialData line in lines)
            {
                ShowDialogue(line);
                yield return RevealAndAdvance();
                if (dialogRoot != null) dialogRoot.SetActive(false);
            }
        }
        finally
        {
            RestoreRelicDialogueParent();
            Cleanup();
        }
    }

    private static bool HasVisibleGridCells(GridManager grid)
    {
        GridCell[] cells = grid.GetComponentsInChildren<GridCell>(false);
        foreach (GridCell cell in cells)
        {
            if (cell == null || !cell.gameObject.activeInHierarchy) continue;
            Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(false);
            foreach (Renderer renderer in renderers)
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    return true;
        }
        return false;
    }

    private static Canvas SetupFrontCanvas(GameObject target, int sortingOrder,
        out bool addedCanvas, out GraphicRaycaster raycaster, out bool addedRaycaster,
        out bool originalOverride, out int originalSortingOrder)
    {
        Canvas canvas = target.GetComponent<Canvas>();
        addedCanvas = canvas == null;
        if (canvas == null) canvas = target.AddComponent<Canvas>();
        originalOverride = canvas.overrideSorting;
        originalSortingOrder = canvas.sortingOrder;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        raycaster = target.GetComponent<GraphicRaycaster>();
        addedRaycaster = raycaster == null;
        if (raycaster == null) raycaster = target.AddComponent<GraphicRaycaster>();
        raycaster.enabled = true;
        return canvas;
    }

    private void CreateDialogueShield()
    {
        if (tutorialRoot == null) return;
        dialogueInputShield = new GameObject("__TutorialDialogueInputShield", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
        dialogueInputShield.transform.SetParent(tutorialRoot, false);
        RectTransform rect = dialogueInputShield.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Canvas canvas = dialogueInputShield.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32740;
        Image image = dialogueInputShield.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        dialogueShieldGroup = dialogueInputShield.GetComponent<CanvasGroup>();
        dialogueShieldGroup.blocksRaycasts = true;

        if (dialogRoot != null)
            dialogCanvas = SetupFrontCanvas(dialogRoot, 32741,
                out addedDialogCanvas, out dialogRaycaster, out addedDialogRaycaster,
                out originalDialogOverrideSorting, out originalDialogSortingOrder);
        Transform skip = FindChild(tutorialRoot, "SkipButton");
        if (skip == null) skip = FindChild(tutorialRoot, "TutorialSkipButton");
        if (skip != null)
            skipCanvas = SetupFrontCanvas(skip.gameObject, 32742,
                out addedSkipCanvas, out skipRaycaster, out addedSkipRaycaster,
                out originalSkipOverrideSorting, out originalSkipSortingOrder);
    }

    private void Update()
    {
        if (dialogueShieldGroup == null) return;
        var menu = FindFirstObjectByType<BattleMenuEscapeInputController>(FindObjectsInactive.Include);
        bool menuVisible = menu != null && menu.IsMenuPanelVisible;
        bool confirmationVisible = UIManager.Instance != null && UIManager.Instance.IsConfirmDialogOpen;
        // Menus and their confirmation dialogs handle their own input blocking.
        dialogueShieldGroup.blocksRaycasts = !menuVisible && !confirmationVisible;
    }

    private void RestoreFrontCanvas(Canvas canvas, bool addedCanvas, bool originalOverride, int originalOrder,
        GraphicRaycaster raycaster, bool addedRaycaster)
    {
        if (raycaster != null && addedRaycaster) Destroy(raycaster);
        if (canvas == null) return;
        if (addedCanvas) Destroy(canvas);
        else
        {
            canvas.overrideSorting = originalOverride;
            canvas.sortingOrder = originalOrder;
        }
    }

    private void ResolveUi()
    {
        tutorialRoot = null;
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.name == "TutorialMode" && candidate.gameObject.scene.IsValid() &&
                candidate.gameObject.scene == gameObject.scene)
            {
                tutorialRoot = candidate;
                break;
            }
        }
        if (tutorialRoot == null)
        {
            Debug.LogError("[TutorialBattleSequence] TutorialMode not found.", this);
            return;
        }
        tutorialRoot.gameObject.SetActive(true);
        Transform dialog = FindChild(tutorialRoot, "Dialogbox");
        dialogRoot = dialog != null ? dialog.gameObject : null;
        tooltipRoot = FindChild(tutorialRoot, "TutorialTooltip")?.gameObject;
        focusRoot = FindChild(tutorialRoot, "HighlightFrame")?.gameObject;
        overlayRoot = FindChild(tutorialRoot, "FocusOverlay")?.gameObject;
        dialogText = FindChild(dialog, "Dialog")?.GetComponent<TMP_Text>();
        completeText = FindChild(dialog, "TextComplete")?.GetComponentInChildren<TMP_Text>(true);
        portrait = FindChild(dialog, "Icon")?.GetComponent<Image>();
        ProtectText(dialogText);
        ProtectText(completeText);
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;
        foreach (Transform child in root)
        {
            Transform match = FindChild(child, childName);
            if (match != null) return match;
        }
        return null;
    }

    private static void ProtectText(TMP_Text text)
    {
        if (text == null) return;
        if (text.GetComponent<LocalizationAutoBindingIgnore>() == null)
            text.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        var original = text.GetComponent<LocalizedTMPText>();
        if (original != null) original.enabled = false;
        var dynamicText = text.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicText != null) dynamicText.enabled = false;
        var eventText = text.GetComponent<LocalizeStringEvent>();
        if (eventText != null) eventText.enabled = false;
    }

    private void HideUi()
    {
        if (dialogRoot != null) dialogRoot.SetActive(false);
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        if (focusRoot != null) focusRoot.SetActive(false);
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void ShowDialogue(TutorialData step)
    {
        if (dialogText != null)
        {
            string fallback = step.TextKO ?? string.Empty;
            string localized = string.IsNullOrWhiteSpace(step.TextKey) ? fallback :
                GameLocalization.Get(step.TextKey, fallback);
            dialogText.text = (localized ?? string.Empty).Replace("\\n", "\n");
            LocaleTMPFontGuard.Prepare(dialogText);
            dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;
        }
        if (portrait != null)
        {
            Sprite sprite = null;
            bool found = DataManager.Instance?.CharacterIconDatabase?.TryGetIcon(step.SpeakerId, out sprite) == true;
            portrait.sprite = found ? sprite : null;
            portrait.enabled = found;
        }
        if (completeText != null)
        {
            string key = string.IsNullOrWhiteSpace(step.CompleteTextKey) ? "tutorial.prompt.click_any" : step.CompleteTextKey;
            completeText.text = GameLocalization.Get(key, step.CompleteTextKO ?? "아무 곳이나 클릭하여 계속");
            completeText.gameObject.SetActive(false);
        }
        if (dialogRoot != null) dialogRoot.SetActive(true);
    }

    private IEnumerator RevealAndAdvance()
    {
        yield return null;
        if (dialogText != null)
        {
            int shown = 0;
            float progress = 0f;
            int total = dialogText.textInfo.characterCount;
            while (shown < total)
            {
                if (WasAdvancePressed()) break;
                progress += Time.unscaledDeltaTime * dialogueCharactersPerSecond;
                shown = Mathf.Min(total, Mathf.FloorToInt(progress));
                dialogText.maxVisibleCharacters = shown;
                yield return null;
            }
            dialogText.maxVisibleCharacters = int.MaxValue;
        }
        if (completeText != null) completeText.gameObject.SetActive(true);
        // Do not count the click that finished typing as the next dialogue click.
        while (Input.GetMouseButton(0)) yield return null;
        yield return null;
        while (!WasAdvancePressed()) yield return null;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(ResolveDialogueAdvanceSoundId(dialogueAdvanceSoundId), dialogueAdvanceSoundVolume);
    }

    private bool WasAdvancePressed()
    {
        if (Time.frameCount <= suppressAdvanceUntilFrame) return false;
        if (UIManager.Instance != null && UIManager.Instance.IsConfirmDialogOpen) return false;
        var menu = FindFirstObjectByType<BattleMenuEscapeInputController>(FindObjectsInactive.Include);
        if (menu != null && menu.IsMenuPanelVisible) return false;
        if (!Input.GetMouseButtonDown(0)) return false;
        if (EventSystem.current != null)
        {
            raycastResults.Clear();
            PointerEventData pointer = new(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(pointer, raycastResults);
            foreach (RaycastResult hit in raycastResults)
            {
                Transform current = hit.gameObject != null ? hit.gameObject.transform : null;
                while (current != null)
                {
                    if (current.name == "SkipButton" || current.name == "TutorialSkipButton" ||
                        current.name == "MenuButton" || current.name == "MenuPanel" ||
                        current.name == "MenuRoot" || current.name == "Check(Clone)")
                    {
                        NotifyModalInteraction();
                        return false;
                    }
                    current = current.parent;
                }
            }
        }
        return true;
    }

    private void Cleanup()
    {
        RestoreRelicDialogueParent();
        if (dialogueInputShield != null) Destroy(dialogueInputShield);
        dialogueInputShield = null;
        dialogueShieldGroup = null;
        RestoreFrontCanvas(dialogCanvas, addedDialogCanvas, originalDialogOverrideSorting,
            originalDialogSortingOrder, dialogRaycaster, addedDialogRaycaster);
        RestoreFrontCanvas(skipCanvas, addedSkipCanvas, originalSkipOverrideSorting,
            originalSkipSortingOrder, skipRaycaster, addedSkipRaycaster);
        dialogCanvas = null;
        skipCanvas = null;
        dialogRaycaster = null;
        skipRaycaster = null;
        addedDialogCanvas = false;
        addedSkipCanvas = false;
        addedDialogRaycaster = false;
        addedSkipRaycaster = false;
        HideUi();
        routine = null;
        dialogueInputActive = false;
        dialoguePresentationRunning = false;
        ActiveTutorialMapId = string.Empty;
    }

    private void OnDisable()
    {
        Cleanup();
    }
}
