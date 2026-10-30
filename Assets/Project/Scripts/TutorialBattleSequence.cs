using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

/// <summary>
/// Executes the pre-battle presentation authored in the Tutorial worksheet.
/// Normal turn planning and input remain owned by BattleRoomLoader.
/// </summary>
public sealed class TutorialBattleSequence : MonoBehaviour
{
    public static bool IsPresentationRunning { get; private set; }

    // 튜토리얼 안내 중에는 해당 단계가 요구하는 단축키만 허용합니다.
    public static bool IsShortcutAllowed(KeyCode key)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = FindFirstObjectByType<TutorialBattleSequence>(FindObjectsInactive.Include);
        if (sequence == null) return false;
        switch (sequence.tutorialStageIndex)
        {
            case 24: return key == KeyCode.A;
            case 26:
            case 28: return key == KeyCode.Q;
            case 27: return key == KeyCode.Tab;
            case 29: return key == KeyCode.Space;
            default: return false;
        }
    }

    // 27번에서 카야를 선택한 뒤 28번 기억 예약 완료까지 다른 캐릭터로 전환하지 않습니다.
    // 마우스/HUD/Tab/타임라인 내부 선택 모두 동일한 규칙을 사용합니다.
    public static bool CanSelectTutorialCharacter(string characterId)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = FindFirstObjectByType<TutorialBattleSequence>(FindObjectsInactive.Include);
        if (sequence == null || sequence.tutorialStageIndex < 24 || sequence.tutorialStageIndex > 28)
            return true;
        if (sequence.tutorialStageIndex == 27 || sequence.tutorialStageIndex == 28)
            return characterId == "Char_02";
        return characterId == "Char_01";
    }

    // 튜토리얼 이동 목적지는 25번의 Grid_16만 허용합니다.
    // 버튼을 누른 마우스 입력이 바로 다음 단계의 그리드 클릭으로 전달되지 않도록 보호합니다.
    private bool grid25InputArmed;
    public static bool CanClickTutorialGrid(int gridIndex)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = FindFirstObjectByType<TutorialBattleSequence>(FindObjectsInactive.Include);
        return sequence != null && sequence.tutorialStageIndex == 25 &&
               sequence.grid25InputArmed && gridIndex == 16;
    }

    public static bool CanAcceptTutorialMove(PlayerReservedCommand command)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = FindFirstObjectByType<TutorialBattleSequence>(FindObjectsInactive.Include);
        return sequence != null && sequence.tutorialStageIndex == 25 &&
               sequence.grid25InputArmed && command != null &&
               command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == 16;
    }

    public static bool IsBlockingBattleStart { get; private set; }
    public bool IsInitialBattleHudReady { get; private set; }

    public void NotifyInitialBattleHudReady() => IsInitialBattleHudReady = true;
    // 대화 중 전투 UI의 클릭 및 호버 이벤트를 차단합니다.
    private readonly Dictionary<GraphicRaycaster, bool> blockedBattleRaycasters = new();
    private readonly Dictionary<Collider, bool> blockedGridColliders = new();
    private readonly Dictionary<Collider2D, bool> blockedGridColliders2D = new();
    private GameObject dialogueInputBlocker;
    private Canvas tutorialSortingCanvas;
    private bool previousOverrideSorting;
    private int previousSortingOrder;
    private BattleRoomLoader loader;
    private Coroutine routine;
    private GameObject dialogRoot;
    private GameObject tooltipRoot;
    private GameObject focusRoot;
    private RectTransform overlayRoot;
    private RectTransform worldGridFocusProxy;
    [SerializeField, Min(0f)] private float focusSlideDuration = 0.45f;
    private Coroutine focusSlideRoutine;
    private TMP_Text dialogText;
    private TMP_Text guideText;
    private GameObject dialogTextComplete;
    private GameObject tooltipTextComplete;
    private TMP_Text dialogCompleteLabel;
    private TMP_Text tooltipCompleteLabel;
    private Image portrait;
    private GameObject tutorialModeRoot;
    [SerializeField, Min(0f)] private float textCompleteFadeDuration = 0.2f;
    private readonly Dictionary<GameObject, Coroutine> completeFades = new();
    [SerializeField, Min(1f)] private float dialogueCharactersPerSecond = 35f;
    private readonly Dictionary<Renderer, bool> gridRenderers = new();
    private BattleTurnExecutor tutorialTurnExecutor;
    private bool tutorialControlInputOpened;
    private bool tutorialEndTurnClicked;
    private UnityEngine.UI.Button tutorialEndTurnButton;
    private int tutorialStageIndex;
    private int skill26ReservationBaseline = -1;
    private int skill28ReservationBaseline = -1;

    // 실제 타임라인 등록 시점에 튜토리얼이 허용하는 횟수만 받습니다.
    public static bool CanAcceptTutorialCommand(PlayerReservedCommand command, BattleTimelineController timeline)
    {
        if (!IsPresentationRunning || command == null || timeline == null) return true;
        TutorialBattleSequence sequence = FindFirstObjectByType<TutorialBattleSequence>(FindObjectsInactive.Include);
        if (sequence == null) return true;
        // 다른 그리드/다른 캐릭터의 이동 예약은 단계 진행 여부와 무관하게 차단합니다.
        if (command.ReservedMoveGridIndex >= 0)
            return CanAcceptTutorialMove(command);
        if (sequence.tutorialStageIndex == 25)
            return false;
        if (sequence.tutorialStageIndex == 26)
            return command.CharacterId == "Char_01" && sequence.skill26ReservationBaseline >= 0 &&
                CountCharacterCommands(timeline, "Char_01") < sequence.skill26ReservationBaseline + 1;
        if (sequence.tutorialStageIndex == 28)
            return command.CharacterId == "Char_02" && sequence.skill28ReservationBaseline >= 0 &&
                CountCharacterCommands(timeline, "Char_02") < sequence.skill28ReservationBaseline + 2;
        return true;
    }



    public void Begin(BattleRoomLoader battleLoader, string mapId)
    {
        if (routine != null) StopCoroutine(routine);
        loader = battleLoader;
        IsPresentationRunning = true;
        IsBlockingBattleStart = true;
        IsInitialBattleHudReady = false;
        var steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || steps.Count == 0)
        {
            IsPresentationRunning = false;
            IsBlockingBattleStart = false;
            return;
        }
        routine = StartCoroutine(Run(steps));
    }

    private IEnumerator Run(IReadOnlyList<TutorialData> steps)
    {
        // Wait for the standard room entry and its character movement to finish.
        while (BattleSceneController.IsBattleRoomIntroPlaying)
            yield return null;
        yield return null;
        ResolveUi();
        HideGridVisuals();
        HideUi();

        string previousGuideTarget = null;
        foreach (TutorialData step in steps)
        {
            if (step == null) continue;
            string phase = step.Phase ?? string.Empty;
            string output = step.TextOutput ?? string.Empty;
            bool sameGuideTarget = output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previousGuideTarget)
                && string.Equals(previousGuideTarget, step.HighlightTarget, StringComparison.OrdinalIgnoreCase);
            previousGuideTarget = output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase)
                ? step.HighlightTarget : null;
            // HUD guides run after battle-start presentation and timeline slots are visible.
            if (IsCharacterHudGuide(step) && IsBlockingBattleStart)
            {
                HideUi();
                IsBlockingBattleStart = false;
                while (!IsInitialBattleHudReady) yield return null;
                // 이전 단계에서 숨겨둔 그리드를 먼저 복원해야 표시 완료를 기다릴 수 있습니다.
                RestoreGridVisuals();
                GridManager preparedGrid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
                if (preparedGrid != null)
                    preparedGrid.SetGridVisible(true);
                yield return WaitUntilGridRendered();
            }
            if (phase.Equals("SpawnMonsters", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                RestoreGridVisuals();
                loader.CompleteDeferredMonsterSpawn();
                yield return loader.PlayTutorialMonsterEntrance();
                continue;
            }

            if (output.Equals("BattleMapIntroText", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                yield return BattleMapIntroText.ShowMessageAndWait(Localize(step));
            }
            else if (output.Equals("Dialogbox", StringComparison.OrdinalIgnoreCase))
            {
                SetBattleUiPointerBlocked(true);
                ShowDialogue(step);
                yield return RevealDialogueAndWaitForClick();
                yield return FadeOutComplete(dialogTextComplete);
                SetBattleUiPointerBlocked(false);
            }
            else if (output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase))
            {
                tutorialStageIndex = step.Order;
                grid25InputArmed = false;
                if (step.Order == 25)
                {
                    // 24번 이동 버튼 클릭/키 누름의 같은 입력을 25번 그리드 입력으로 처리하지 않습니다.
                    while (Input.GetMouseButton(0) || Input.touchCount > 0)
                        yield return null;
                    yield return null;
                    grid25InputArmed = true;
                }
                // Only the first hands-on control step opens normal reservation input.
                // BattleRoomLoader still owns the later turn / input lifecycle.
                if (step.Order == 24 && !tutorialControlInputOpened)
                {
                    tutorialTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
                    if (tutorialTurnExecutor != null)
                    {
                        tutorialTurnExecutor.SetBattleInputReady(true);
                        tutorialControlInputOpened = true;
                    }
                }
                // 타임라인/행동 소개(19~22번)는 설명만 넘길 수 있고 전투 UI에는 입력할 수 없습니다.
                bool blockGuideInput = IsBattleUiExplanationGuide(step);
                ShowGuide(step, sameGuideTarget);
                if (blockGuideInput)
                {
                    SetBattleUiPointerBlocked(true);
                    if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(true);
                }
                // ClickAny만 화면 클릭으로 진행합니다. 조작 단계는 실제 선택/예약 상태로 판정합니다.
                string advance = step.Advance ?? string.Empty;
                if (advance.Equals("ClickTarget", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveMove", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveAction", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveActionTwice", StringComparison.OrdinalIgnoreCase))
                    yield return WaitForRequiredBattleAction(step);
                else
                    yield return WaitForClick();
                yield return FadeOutComplete(tooltipTextComplete);
                if (blockGuideInput)
                {
                    SetBattleUiPointerBlocked(false);
                    if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(false);
                }
            }

            if (phase.Equals("StartBattle", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                // The existing loader is already waiting on deferred monster spawn.
                // It owns monster planning, battle intro, turn 1 and HUD activation.
                if (loader != null) loader.CompleteDeferredMonsterSpawn();
                break;
            }
        }

        HideUi();
        routine = null;
        SetBattleUiPointerBlocked(false);
        ReleaseDialogueInputBlocker();
        IsPresentationRunning = false;
        IsBlockingBattleStart = false;
        enabled = false;
    }

    private static bool IsBattleUiExplanationGuide(TutorialData step)
    {
        if (step == null || !string.Equals(step.Advance, "ClickAny", StringComparison.OrdinalIgnoreCase))
            return false;

        string key = step.TextKey ?? string.Empty;
        return string.Equals(key, "tutorial.t01.battleslot.01", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.battleslot.02", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.action.01", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.action.02", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBattleSlotGuide(TutorialData step)
    {
        string target = step?.HighlightTarget?.Replace('\\', '/').Trim('/');
        return !string.IsNullOrEmpty(target) &&
            (target.Equals("BattleSlot/Back", StringComparison.OrdinalIgnoreCase) ||
             target.EndsWith("/BattleSlot/Back", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCharacterHudGuide(TutorialData step)
    {
        string target = step?.HighlightTarget?.Replace('\\', '/').Trim('/');
        return !string.IsNullOrEmpty(target) &&
            (target.Equals("Char01/Back", StringComparison.OrdinalIgnoreCase) ||
             target.Equals("CharHUD/Char01/Back", StringComparison.OrdinalIgnoreCase));
    }

    private void HideGridVisuals()
    {
        gridRenderers.Clear();
        foreach (GridCell cell in FindObjectsByType<GridCell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || gridRenderers.ContainsKey(renderer)) continue;
                gridRenderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }
    }

    private void RestoreGridVisuals()
    {
        foreach (var entry in gridRenderers)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        gridRenderers.Clear();
    }

    private static string Localize(TutorialData step)
    {
        if (string.IsNullOrWhiteSpace(step.TextKey)) return string.Empty;
        string result = GameLocalization.Get(step.TextKey, step.TextKO ?? string.Empty);
        return NormalizeLineBreaks(result);
    }

    private static string NormalizeLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Text may originate from spreadsheet cells or localization tables.
        return value.Replace("<br />", "\n").Replace("<br/>", "\n")
            .Replace("<br>", "\n").Replace("<BR>", "\n")
            .Replace("\\r\\n", "\n").Replace("\\n", "\n")
            .Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private void ResolveUi()
    {
        Transform tutorialRoot = null;
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.name == "TutorialMode" && candidate.gameObject.scene.IsValid())
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
        tutorialModeRoot = tutorialRoot.gameObject;
        // Scene designers may leave the entire TutorialMode hierarchy inactive.
        if (!tutorialModeRoot.activeSelf) tutorialModeRoot.SetActive(true);
        Transform dialog = Find(tutorialRoot, "Dialogbox");
        Transform tooltip = Find(tutorialRoot, "TutorialTooltip");
        Transform focus = Find(tutorialRoot, "HighlightFrame");
        overlayRoot = Find(tutorialRoot, "FocusOverlay") as RectTransform;
        dialogRoot = dialog != null ? dialog.gameObject : null;
        tooltipRoot = tooltip != null ? tooltip.gameObject : null;
        focusRoot = focus != null ? focus.gameObject : null;
        dialogText = dialog != null ? Find(dialog, "Dialog")?.GetComponent<TMP_Text>() : null;
        guideText = tooltip != null ? Find(tooltip, "Text")?.GetComponent<TMP_Text>() : null;
        if (guideText == null && tooltip != null) guideText = tooltip.GetComponentInChildren<TMP_Text>(true);
        ProtectTutorialText(dialogText);
        ProtectTutorialText(guideText);
        dialogTextComplete = dialog != null ? Find(dialog, "TextComplete")?.gameObject : null;
        tooltipTextComplete = tooltip != null ? Find(tooltip, "TextComplete")?.gameObject : null;
        dialogCompleteLabel = dialogTextComplete != null ? dialogTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        tooltipCompleteLabel = tooltipTextComplete != null ? tooltipTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        // TextComplete 또한 다른 로컬라이징 컴포넌트가 값을 덮어쓰지 못하게 합니다.
        ProtectTutorialText(dialogCompleteLabel);
        ProtectTutorialText(tooltipCompleteLabel);
        portrait = dialog != null ? Find(dialog, "Icon")?.GetComponent<Image>() : null;
        CreateDialogueInputBlocker(tutorialRoot);
    }

    // TutorialBattleSequence resolves TextKey itself, so no second component may
    // bind or overwrite the text while the dialogue typewriter is running.
    private static void ProtectTutorialText(TMP_Text target)
    {
        if (target == null) return;
        if (target.GetComponent<LocalizationAutoBindingIgnore>() == null)
            target.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        var staticWriter = target.GetComponent<LocalizedTMPText>();
        if (staticWriter != null) staticWriter.enabled = false;
        var dynamicWriter = target.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null) dynamicWriter.enabled = false;
        var legacyWriter = target.GetComponent<LocalizeStringEvent>();
        if (legacyWriter != null) legacyWriter.enabled = false;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform result = Find(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private void HideUi()
    {
        if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(false);
        if (dialogRoot != null) dialogRoot.SetActive(false);
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        if (focusRoot != null) focusRoot.SetActive(false);
        if (focusSlideRoutine != null)
        {
            StopCoroutine(focusSlideRoutine);
            focusSlideRoutine = null;
        }
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(false);
        ResetComplete(dialogTextComplete);
        ResetComplete(tooltipTextComplete);
    }

    private static CanvasGroup EnsureCompleteCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private void ResetComplete(GameObject target)
    {
        if (target == null) return;
        if (completeFades.TryGetValue(target, out Coroutine fade) && fade != null)
            StopCoroutine(fade);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (group != null) group.alpha = 0f;
        target.SetActive(false);
    }

    private void SetComplete(GameObject target, bool visible)
    {
        if (target == null) return;
        if (completeFades.TryGetValue(target, out Coroutine previous) && previous != null)
            StopCoroutine(previous);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (visible)
        {
            target.SetActive(true);
            group.alpha = 0f;
            if (isActiveAndEnabled)
                completeFades[target] = StartCoroutine(FadeComplete(target, 0f, 1f));
            else group.alpha = 1f;
        }
        else
        {
            if (group != null) group.alpha = 0f;
            target.SetActive(false);
        }
    }

    private IEnumerator FadeComplete(GameObject target, float from, float to)
    {
        if (target == null) yield break;
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (group == null) yield break;
        float duration = Mathf.Max(0.001f, textCompleteFadeDuration);
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
        if (to <= 0f) target.SetActive(false);
        completeFades.Remove(target);
    }

    private IEnumerator FadeOutComplete(GameObject target)
    {
        if (target == null || !target.activeInHierarchy) yield break;
        if (completeFades.TryGetValue(target, out Coroutine previous) && previous != null)
            StopCoroutine(previous);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        yield return FadeComplete(target, group.alpha, 0f);
    }

    // A step can override the completion prompt without changing the prefab text.
    // Translation uses the same Text table as other tutorial dialogue.
    private void SetCompletionPrompt(TutorialData step, TMP_Text label)
    {
        if (step == null || label == null) return;
        string key = step.CompleteTextKey;
        string korean = step.CompleteTextKO;
        if (string.IsNullOrWhiteSpace(key))
        {
            string advance = step.Advance ?? string.Empty;
            key = advance.Equals("ClickAny", StringComparison.OrdinalIgnoreCase)
                ? "tutorial.prompt.click_any"
                : "tutorial.prompt.press_button";
        }
        // If no explicit worksheet fallback exists, preserve the configured prefab
        // phrase for ClickAny. Button prompts should be defined by worksheet key.
        string fallback = !string.IsNullOrWhiteSpace(korean) ? korean :
            (step.Advance != null && step.Advance.Equals("ClickAny", StringComparison.OrdinalIgnoreCase)
                ? label.text : string.Empty);
        label.text = NormalizeLineBreaks(GameLocalization.Get(key, fallback));
    }

    private void ShowDialogue(TutorialData step)
    {
        HideUi();
        if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(true);
        if (dialogText != null)
        {
            dialogText.maxVisibleCharacters = int.MaxValue;
            dialogText.text = Localize(step);
            dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;
        }
        if (portrait != null)
        {
            Sprite sprite = null;
            bool has = DataManager.Instance?.CharacterIconDatabase?.TryGetIcon(step.SpeakerId, out sprite) == true;
            portrait.sprite = has ? sprite : null;
            portrait.enabled = has;
        }
        if (dialogRoot != null) dialogRoot.SetActive(true);
        SetCompletionPrompt(step, dialogCompleteLabel);
        SetComplete(dialogTextComplete, false);
    }

    private void ShowGuide(TutorialData step, bool keepExistingFocus)
    {
        // Consecutive guides for the same UI target should keep the existing
        // FocusOverlay and HighlightFrame instead of reopening their animation.
        if (keepExistingFocus && overlayRoot != null && overlayRoot.gameObject.activeSelf)
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            ResetComplete(tooltipTextComplete);
        }
        else
        {
            HideUi();
            keepExistingFocus = false;
        }
        if (guideText != null) guideText.text = Localize(step);
        if (tooltipRoot != null) tooltipRoot.SetActive(true);
        // Tooltip displays the whole message immediately.
        SetComplete(tooltipTextComplete, true);
        SetCompletionPrompt(step, tooltipCompleteLabel);
        if (focusRoot == null || string.IsNullOrWhiteSpace(step.HighlightTarget)) return;
        Transform target = null;
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy &&
                MatchesHighlightTarget(item, step.HighlightTarget))
            {
                target = item;
                break;
            }
        }
        RectTransform source = target as RectTransform;
        if (source == null && target != null)
            source = CreateWorldGridFocusProxy(target);
        if (source != null && focusRoot.transform is RectTransform frame)
        {
            RectTransform parent = frame.parent as RectTransform;
            if (parent != null)
            {
                Vector3[] corners = new Vector3[4];
                source.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                foreach (Vector3 corner in corners)
                {
                    Vector2 local = parent.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
                frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
                frame.anchoredPosition = (min + max) * .5f - parent.rect.center;
                frame.sizeDelta = max - min + Vector2.one * 16f;
                UpdateHighlightCorners(frame);
                focusRoot.SetActive(true);
                if (!keepExistingFocus) ShowFocusOverlay(source);
                // Excel TooltipPosition controls guide placement; blank defaults to Below.
                bool isAbove = string.Equals(step.TooltipPosition?.Trim(), "Above", StringComparison.OrdinalIgnoreCase);
                if (isAbove)
                    PositionTooltipAbove(source);
                else
                    PositionTooltipBelow(source);
                KeepTooltipInsideScreen();
            }
        }
    }

    // MeshRenderer 기반 월드 그리드의 화면 영역을 UI RectTransform으로 변환합니다.
    private RectTransform CreateWorldGridFocusProxy(Transform worldTarget)
    {
        RectTransform frame = focusRoot != null ? focusRoot.transform as RectTransform : null;
        RectTransform parent = frame != null ? frame.parent as RectTransform : null;
        Renderer renderer = worldTarget != null ? worldTarget.GetComponent<Renderer>() : null;
        Camera camera = Camera.main;
        if (parent == null || renderer == null || camera == null)
        {
            Debug.LogWarning("[TutorialBattleSequence] Grid focus requires a renderer, camera and UI parent.", this);
            return null;
        }

        Bounds bounds = renderer.bounds;
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    Vector3 screen = camera.WorldToScreenPoint(point);
                    if (screen.z <= 0f) continue;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 local))
                        continue;
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
        if (min.x == float.MaxValue) return null;
        if (worldGridFocusProxy == null)
        {
            GameObject proxy = new GameObject("TutorialWorldGridFocusTarget", typeof(RectTransform));
            worldGridFocusProxy = proxy.GetComponent<RectTransform>();
            worldGridFocusProxy.SetParent(parent, false);
        }
        worldGridFocusProxy.anchorMin = worldGridFocusProxy.anchorMax = new Vector2(.5f, .5f);
        worldGridFocusProxy.pivot = new Vector2(.5f, .5f);
        worldGridFocusProxy.anchoredPosition = (min + max) * .5f - parent.rect.center;
        worldGridFocusProxy.sizeDelta = new Vector2(Mathf.Max(12f, max.x - min.x), Mathf.Max(12f, max.y - min.y));
        return worldGridFocusProxy;
    }

    private static bool MatchesHighlightTarget(Transform item, string key)
    {
        if (item == null || string.IsNullOrWhiteSpace(key)) return false;
        string normalized = key.Replace('\\', '/').Trim('/');
        if (!normalized.Contains("/")) return item.name.Equals(normalized, StringComparison.OrdinalIgnoreCase);
        string[] segments = normalized.Split('/');
        Transform current = item;
        for (int i = segments.Length - 1; i >= 0; --i)
        {
            if (current == null || !current.name.Equals(segments[i], StringComparison.OrdinalIgnoreCase)) return false;
            current = current.parent;
        }
        return true;
    }

    private void PositionTooltipAbove(RectTransform source)
    {
        if (tooltipRoot == null || source == null) return;
        RectTransform tooltip = tooltipRoot.transform as RectTransform;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (parent == null) return;
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera : null;
        Canvas tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
        Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tooltipCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 topMiddle = (corners[1] + corners[2]) * .5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, topMiddle);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, tooltipCamera, out Vector2 local)) return;
        const float gap = 12f;
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(.5f, .5f);
        tooltip.pivot = new Vector2(.5f, 0f);
        tooltip.anchoredPosition = local - parent.rect.center + Vector2.up * gap;
    }

    private void LateUpdate()
    {
        // 텍스트 레이아웃이 다음 프레임에 갱신되는 경우에도 툴팁 화면 이탈을 방지합니다.
        if (IsPresentationRunning && tooltipRoot != null && tooltipRoot.activeInHierarchy)
            KeepTooltipInsideScreen();
    }

    // 실제 화면 좌표로 크기와 모서리를 계산하여 Canvas Scaler/피벗에 관계없이 클램프합니다.
    private void KeepTooltipInsideScreen()
    {
        RectTransform tooltip = tooltipRoot != null ? tooltipRoot.transform as RectTransform : null;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (tooltip == null || parent == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
        Canvas.ForceUpdateCanvases();
        Canvas canvas = tooltip.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        tooltip.GetWorldCorners(corners);
        float left = float.MaxValue, right = float.MinValue;
        float bottom = float.MaxValue, top = float.MinValue;
        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
            left = Mathf.Min(left, screenPoint.x);
            right = Mathf.Max(right, screenPoint.x);
            bottom = Mathf.Min(bottom, screenPoint.y);
            top = Mathf.Max(top, screenPoint.y);
        }
        const float margin = 12f;
        float availableWidth = Mathf.Max(0f, Screen.width - margin * 2f);
        float availableHeight = Mathf.Max(0f, Screen.height - margin * 2f);
        float dx = right - left > availableWidth
            ? Screen.width * 0.5f - (left + right) * 0.5f
            : left < margin ? margin - left : right > Screen.width - margin ? Screen.width - margin - right : 0f;
        float dy = top - bottom > availableHeight
            ? Screen.height * 0.5f - (bottom + top) * 0.5f
            : bottom < margin ? margin - bottom : top > Screen.height - margin ? Screen.height - margin - top : 0f;
        if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) return;
        Vector2 originScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, tooltip.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, originScreen, uiCamera, out Vector2 originLocal) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, originScreen + new Vector2(dx, dy), uiCamera, out Vector2 shiftedLocal))
            tooltip.anchoredPosition += shiftedLocal - originLocal;
    }

    // The Line sprite contains four fixed-size corner marks. Do not stretch it:
    // crop each original corner into a separate UI mask and place the masks on
    // the corresponding HighlightFrame corners. This preserves sprite pixels.
    private static void UpdateHighlightCorners(RectTransform frame)
    {
        RectTransform line = Find(frame, "Line") as RectTransform;
        if (line == null) return;
        Image original = line.GetComponent<Image>();
        if (original == null || original.sprite == null) return;

        // Use the authored dimensions, not the (possibly stretched) dimensions.
        // The default Line image is 100 x 100.
        Vector2 originalSize = line.sizeDelta;
        if (line.anchorMin != line.anchorMax || originalSize.x <= 0f || originalSize.y <= 0f)
            originalSize = original.sprite.rect.size / Mathf.Max(1f, original.sprite.pixelsPerUnit);
        if (originalSize.x <= 0f || originalSize.y <= 0f)
            originalSize = new Vector2(100f, 100f);

        // Restore the original image's authored size; it serves only as a
        // template for the four cropped corner images below.
        line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
        line.pivot = new Vector2(.5f, .5f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = originalSize;
        line.localScale = Vector3.one;
        original.enabled = false;

        string[] names = { "TutorialCorner_TL", "TutorialCorner_TR", "TutorialCorner_BL", "TutorialCorner_BR" };
        Vector2[] pivots = { new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(0f, 0f), new Vector2(1f, 0f) };
        for (int i = 0; i < names.Length; i++)
        {
            Vector2 corner = pivots[i];
            Transform existing = frame.Find(names[i]);
            RectTransform mask;
            if (existing == null)
            {
                var maskObject = new GameObject(names[i], typeof(RectTransform), typeof(RectMask2D));
                mask = maskObject.GetComponent<RectTransform>();
                mask.SetParent(frame, false);
                var graphicObject = new GameObject("CornerGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform graphic = graphicObject.GetComponent<RectTransform>();
                graphic.SetParent(mask, false);
            }
            else mask = existing as RectTransform;
            if (mask == null) continue;

            mask.anchorMin = mask.anchorMax = corner;
            mask.pivot = corner;
            mask.anchoredPosition = Vector2.zero;
            mask.sizeDelta = originalSize * .5f;
            mask.localScale = Vector3.one;
            RectTransform cropped = mask.Find("CornerGraphic") as RectTransform;
            if (cropped == null) continue;
            cropped.anchorMin = cropped.anchorMax = corner;
            cropped.pivot = corner;
            cropped.anchoredPosition = Vector2.zero;
            cropped.sizeDelta = originalSize;
            cropped.localScale = Vector3.one;
            Image graphicImage = cropped.GetComponent<Image>();
            graphicImage.sprite = original.sprite;
            graphicImage.color = original.color;
            graphicImage.material = original.material;
            graphicImage.type = Image.Type.Simple;
            graphicImage.preserveAspect = original.preserveAspect;
            graphicImage.raycastTarget = false;
            mask.gameObject.SetActive(true);
        }
    }

    // The guide is centered beneath the highlighted HUD element (not beneath the screen).
    private void PositionTooltipBelow(RectTransform source)
    {
        if (tooltipRoot == null || source == null) return;
        RectTransform tooltip = tooltipRoot.transform as RectTransform;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (parent == null) return;

        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera : null;
        Canvas tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
        Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tooltipCanvas.worldCamera : null;

        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 bottomMiddle = (corners[0] + corners[3]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, bottomMiddle);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, tooltipCamera, out Vector2 local))
            return;

        const float gap = 12f;
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.pivot = new Vector2(0.5f, 1f);
        tooltip.anchoredPosition = local - parent.rect.center + Vector2.down * gap;
    }

    // Match the four-panel FocusOverlay used by the existing battle tutorial.
    // The target remains unobscured while the surrounding HUD is dimmed.
    private void ShowFocusOverlay(RectTransform source)
    {
        if (overlayRoot == null || source == null) return;
        RectTransform top = Find(overlayRoot, "Top") as RectTransform;
        RectTransform bottom = Find(overlayRoot, "Bottom") as RectTransform;
        RectTransform left = Find(overlayRoot, "Left") as RectTransform;
        RectTransform right = Find(overlayRoot, "Right") as RectTransform;
        if (top == null || bottom == null || left == null || right == null)
        {
            Debug.LogWarning("[TutorialBattleSequence] FocusOverlay needs Top, Bottom, Left and Right panels.", this);
            return;
        }

        // Convert through screen coordinates to support canvases with different scaling.
        Canvas targetCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera : null;
        Canvas overlayCanvas = overlayRoot.GetComponentInParent<Canvas>();
        Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? overlayCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corner);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, overlayCamera, out Vector2 point))
                return;
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        const float padding = 8f;
        Rect r = overlayRoot.rect;
        float x0 = Mathf.Clamp(min.x - padding, r.xMin, r.xMax);
        float x1 = Mathf.Clamp(max.x + padding, r.xMin, r.xMax);
        float y0 = Mathf.Clamp(min.y - padding, r.yMin, r.yMax);
        float y1 = Mathf.Clamp(max.y + padding, r.yMin, r.yMax);
        bool wasVisible = overlayRoot.gameObject.activeSelf;
        if (focusSlideRoutine != null)
        {
            StopCoroutine(focusSlideRoutine);
            focusSlideRoutine = null;
        }
        SetOverlayPanel(top, Rect.MinMaxRect(r.xMin, y1, r.xMax, r.yMax));
        SetOverlayPanel(bottom, Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, y0));
        SetOverlayPanel(left, Rect.MinMaxRect(r.xMin, y0, x0, y1));
        SetOverlayPanel(right, Rect.MinMaxRect(x1, y0, r.xMax, y1));
        overlayRoot.gameObject.SetActive(true);
        top.gameObject.SetActive(true);
        bottom.gameObject.SetActive(true);
        left.gameObject.SetActive(true);
        right.gameObject.SetActive(true);

        // Animate only the initial reveal, not each page of the same guide.
        if (!wasVisible && focusSlideDuration > 0f)
        {
            RectTransform[] panels = { top, bottom, left, right };
            Vector2[] finals = { top.anchoredPosition, bottom.anchoredPosition,
                left.anchoredPosition, right.anchoredPosition };
            Vector2[] starts = {
                finals[0] + Vector2.up * r.height,
                finals[1] + Vector2.down * r.height,
                finals[2] + Vector2.left * r.width,
                finals[3] + Vector2.right * r.width
            };
            for (int i = 0; i < panels.Length; i++)
                panels[i].anchoredPosition = starts[i];
            focusSlideRoutine = StartCoroutine(AnimateFocusSlide(panels, starts, finals));
        }
    }

    private IEnumerator AnimateFocusSlide(RectTransform[] panels, Vector2[] starts, Vector2[] finals)
    {
        float elapsed = 0f;
        while (elapsed < focusSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, focusSlideDuration));
            t = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null)
                    panels[i].anchoredPosition = Vector2.LerpUnclamped(starts[i], finals[i], t);
            yield return null;
        }
        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null) panels[i].anchoredPosition = finals[i];
        focusSlideRoutine = null;
    }

    private static void SetOverlayPanel(RectTransform panel, Rect rect)
    {
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = rect.center;
        panel.sizeDelta = rect.size;
    }

    // First click during typing reveals the whole message; the next click advances.
    private IEnumerator RevealDialogueAndWaitForClick()
    {
        if (dialogText == null)
        {
            SetComplete(dialogTextComplete, true);
            yield return WaitForClick();
            yield break;
        }

        dialogText.ForceMeshUpdate();
        int totalCharacters = dialogText.textInfo.characterCount;
        int shown = 0;
        float progress = 0f;
        // Prevent the click that opened the dialogue from immediately skipping it.
        yield return null;

        while (shown < totalCharacters)
        {
            if (IsAdvancePressed())
            {
                dialogText.maxVisibleCharacters = int.MaxValue;
                SetComplete(dialogTextComplete, true);
                // Consume this press; require a separate click to advance.
                yield return null;
                yield return WaitForClick();
                yield break;
            }
            progress += Time.unscaledDeltaTime * dialogueCharactersPerSecond;
            int next = Mathf.Min(totalCharacters, Mathf.FloorToInt(progress));
            if (next > shown)
            {
                shown = next;
                dialogText.maxVisibleCharacters = shown;
            }
            yield return null;
        }
        dialogText.maxVisibleCharacters = int.MaxValue;
        SetComplete(dialogTextComplete, true);
        yield return WaitForClick();
    }

    // Advance values are validated against the actual selection/reservation state,
    // not against an arbitrary click or a tooltip-dismiss button.
    private IEnumerator WaitForRequiredBattleAction(TutorialData step)
    {
        BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        PlayerSkillReservationController reservation = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);
        string advance = step.Advance ?? string.Empty;
        int baseline = CountReservedPlayerCommands(timeline);
        int kayaBaseline = CountCharacterCommands(timeline, "Char_02");
        int hiltBaseline = CountCharacterCommands(timeline, "Char_01");
        if (step.Order == 26) skill26ReservationBaseline = hiltBaseline;
        if (step.Order == 28) skill28ReservationBaseline = kayaBaseline;
        bool moveButton = step.Order == 24;
        bool kayaSelect = step.Order == 27;
        bool endTurn = step.Order == 29;
        tutorialEndTurnClicked = false;
        if (endTurn)
        {
            if (tutorialTurnExecutor == null)
                tutorialTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
            tutorialEndTurnButton = tutorialTurnExecutor != null ? tutorialTurnExecutor.EndTurnButton : null;
            if (tutorialEndTurnButton != null)
                tutorialEndTurnButton.onClick.AddListener(OnTutorialEndTurnClicked);
        }
        yield return null;
        while (true)
        {
            if (endTurn)
            {
                if (tutorialEndTurnClicked || Input.GetKeyDown(KeyCode.Space)) break;
            }
            else if (kayaSelect)
            {
                if (timeline != null && timeline.SelectedCharacter != null &&
                    timeline.SelectedCharacter.CharacterId == "Char_02") break;
            }
            else if (advance.Equals("ReserveActionTwice", StringComparison.OrdinalIgnoreCase))
            {
                if (timeline != null && timeline.SelectedCharacter != null &&
                    timeline.SelectedCharacter.CharacterId == "Char_02" &&
                    CountCharacterCommands(timeline, "Char_02") >= kayaBaseline + 2) break;
            }
            else if (advance.Equals("ClickTarget", StringComparison.OrdinalIgnoreCase))
            {
                if (moveButton && reservation != null && reservation.IsMoveSkillSelectionActive()) break;
            }
            else if (advance.Equals("ReserveMove", StringComparison.OrdinalIgnoreCase))
            {
                if (HasNewMoveToGrid(timeline, baseline, 16) &&
                    (reservation == null || !reservation.IsMoveSkillSelectionActive())) break;
            }
            else if (advance.Equals("ReserveAction", StringComparison.OrdinalIgnoreCase))
            {
                if (CountCharacterCommands(timeline, "Char_01") > hiltBaseline) break;
            }
            yield return null;
        }
        if (tutorialEndTurnButton != null)
        {
            tutorialEndTurnButton.onClick.RemoveListener(OnTutorialEndTurnClicked);
            tutorialEndTurnButton = null;
        }
    }

    private void OnTutorialEndTurnClicked() => tutorialEndTurnClicked = true;

    private static bool HasNewMoveToGrid(BattleTimelineController timeline, int baseline, int gridIndex)
    {
        if (timeline == null || CountReservedPlayerCommands(timeline) <= baseline) return false;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands == null) continue;
            foreach (var command in commands)
                if (command != null && command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == gridIndex)
                    return true;
        }
        return false;
    }

    private static int CountCharacterCommands(BattleTimelineController timeline, string characterId)
    {
        if (timeline == null) return 0;
        int count = 0;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands == null) continue;
            foreach (var command in commands)
                if (command != null && command.CharacterId == characterId) count++;
        }
        return count;
    }

    private static int CountReservedPlayerCommands(BattleTimelineController timeline)
    {
        if (timeline == null) return 0;
        int count = 0;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands != null) count += commands.Count;
        }
        return count;
    }

    private static bool IsAdvancePressed()
    {
        return Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private static IEnumerator WaitForClick()
    {
        // Avoid advancing on the same click that opened this step.
        yield return null;
        while (!IsAdvancePressed())
            yield return null;
    }

    // HUD 준비 신호만으로는 실제 전투 그리드 표시가 보장되지 않습니다.
    // GridManager 상태와 GridCell의 렌더러를 모두 확인합니다.
    private IEnumerator WaitUntilGridRendered()
    {
        while (true)
        {
            GridManager grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
            bool ready = grid != null && grid.IsGridVisible;
            if (ready)
            {
                GridCell[] cells = grid.GetComponentsInChildren<GridCell>(false);
                ready = cells.Length > 0;
                int visibleCount = 0;
                foreach (GridCell cell in cells)
                {
                    if (cell == null || !cell.gameObject.activeInHierarchy) continue;
                    Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(false);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                        {
                            visibleCount++;
                            break;
                        }
                    }
                }
                ready = ready && visibleCount > 0;
            }
            if (ready)
            {
                // 실제 렌더 프레임을 한 번 보낸 후 설명을 시작합니다.
                yield return new WaitForEndOfFrame();
                yield break;
            }
            yield return null;
        }
    }

    // 전투 HUD의 입력을 직접 차단하므로 캔버스 정렬 순서와 관계없이 동작합니다.
    // 해당 구성요소의 기존 활성 상태는 대화 종료 시 그대로 복구합니다.
    private void SetBattleUiPointerBlocked(bool blocked)
    {
        if (!blocked)
        {
            foreach (var pair in blockedBattleRaycasters)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedBattleRaycasters.Clear();
            foreach (var pair in blockedGridColliders)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedGridColliders.Clear();
            foreach (var pair in blockedGridColliders2D)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedGridColliders2D.Clear();
            return;
        }

        if (blockedBattleRaycasters.Count > 0) return;
        foreach (GraphicRaycaster raycaster in FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (raycaster == null || !raycaster.enabled) continue;
            Transform t = raycaster.transform;
            bool battleUi = false;
            while (t != null)
            {
                if (t.name == "BattleHUDCanvas" || t.name == "BattleCharacterPanel" || t.name == "BattleSlot")
                {
                    battleUi = true;
                    break;
                }
                t = t.parent;
            }
            if (!battleUi) continue;
            blockedBattleRaycasters[raycaster] = raycaster.enabled;
            raycaster.enabled = false;
        }
        GridManager grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
        if (grid == null) return;
        foreach (Collider collider in grid.GetComponentsInChildren<Collider>(false))
        {
            blockedGridColliders[collider] = collider.enabled;
            collider.enabled = false;
        }
        foreach (Collider2D collider in grid.GetComponentsInChildren<Collider2D>(false))
        {
            blockedGridColliders2D[collider] = collider.enabled;
            collider.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (tutorialEndTurnButton != null)
        {
            tutorialEndTurnButton.onClick.RemoveListener(OnTutorialEndTurnClicked);
            tutorialEndTurnButton = null;
        }
        IsPresentationRunning = false;
        RestoreGridVisuals();
        HideUi();
        SetBattleUiPointerBlocked(false);
        ReleaseDialogueInputBlocker();
    }

    private void CreateDialogueInputBlocker(Transform tutorialRoot)
    {
        if (dialogueInputBlocker != null || tutorialRoot == null) return;
        // The transparent screen sits over the battle UI but below the tutorial dialogue.
        dialogueInputBlocker = new GameObject("TutorialDialogueInputBlocker", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        // 루트 Canvas를 유지하여 화면 전체를 정확하게 덮습니다.
        // BattleRoom의 Transform 아래에 두면 RectTransform 크기가 틀어질 수 있습니다.
        dialogueInputBlocker.transform.SetParent(null, false);
        Canvas blockerCanvas = dialogueInputBlocker.GetComponent<Canvas>();
        blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blockerCanvas.overrideSorting = true;
        blockerCanvas.sortingOrder = 30000;
        RectTransform rect = dialogueInputBlocker.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image blockerImage = dialogueInputBlocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;
        // Keep the existing tutorial artwork above the blocker.
        tutorialSortingCanvas = tutorialRoot.GetComponent<Canvas>();
        if (tutorialSortingCanvas == null) tutorialSortingCanvas = tutorialRoot.gameObject.AddComponent<Canvas>();
        previousOverrideSorting = tutorialSortingCanvas.overrideSorting;
        previousSortingOrder = tutorialSortingCanvas.sortingOrder;
        tutorialSortingCanvas.overrideSorting = true;
        tutorialSortingCanvas.sortingOrder = 30001;
        if (tutorialRoot.GetComponent<GraphicRaycaster>() == null)
            tutorialRoot.gameObject.AddComponent<GraphicRaycaster>();
        dialogueInputBlocker.SetActive(false);
    }

    private void ReleaseDialogueInputBlocker()
    {
        if (dialogueInputBlocker != null)
        {
            Destroy(dialogueInputBlocker);
            dialogueInputBlocker = null;
        }
        if (tutorialSortingCanvas != null)
        {
            tutorialSortingCanvas.overrideSorting = previousOverrideSorting;
            tutorialSortingCanvas.sortingOrder = previousSortingOrder;
            tutorialSortingCanvas = null;
        }
    }
}