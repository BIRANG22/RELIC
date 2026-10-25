using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Executes the pre-battle presentation authored in the Tutorial worksheet.
/// Normal turn planning and input remain owned by BattleRoomLoader.
/// </summary>
public sealed class TutorialBattleSequence : MonoBehaviour
{
    public static bool IsPresentationRunning { get; private set; }
    private BattleRoomLoader loader;
    private Coroutine routine;
    private GameObject dialogRoot;
    private GameObject tooltipRoot;
    private GameObject focusRoot;
    private RectTransform overlayRoot;
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

    public void Begin(BattleRoomLoader battleLoader, string mapId)
    {
        if (routine != null) StopCoroutine(routine);
        loader = battleLoader;
        IsPresentationRunning = true;
        var steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || steps.Count == 0) return;
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
                ShowDialogue(step);
                yield return RevealDialogueAndWaitForClick();
                yield return FadeOutComplete(dialogTextComplete);
            }
            else if (output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase))
            {
                ShowGuide(step, sameGuideTarget);
                yield return WaitForClick();
                yield return FadeOutComplete(tooltipTextComplete);
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
        IsPresentationRunning = false;
        enabled = false;
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
        dialogTextComplete = dialog != null ? Find(dialog, "TextComplete")?.gameObject : null;
        tooltipTextComplete = tooltip != null ? Find(tooltip, "TextComplete")?.gameObject : null;
        dialogCompleteLabel = dialogTextComplete != null ? dialogTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        tooltipCompleteLabel = tooltipTextComplete != null ? tooltipTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        portrait = dialog != null ? Find(dialog, "Icon")?.GetComponent<Image>() : null;
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
        SetCompletionPrompt(step, tooltipCompleteLabel);
        // Tooltip displays the whole message immediately.
        SetComplete(tooltipTextComplete, true);
        if (focusRoot == null || string.IsNullOrWhiteSpace(step.HighlightTarget)) return;
        Transform target = null;
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy &&
                string.Equals(item.name, step.HighlightTarget, StringComparison.OrdinalIgnoreCase))
            {
                target = item;
                break;
            }
        }
        if (target is RectTransform source && focusRoot.transform is RectTransform frame)
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
                PositionTooltipBelow(source);
            }
        }
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

    private void OnDisable()
    {
        IsPresentationRunning = false;
        RestoreGridVisuals();
        HideUi();
    }
}
