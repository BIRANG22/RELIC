using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turn intro with a RectMask2D wipe. The Mask clips back, line and IntroText together.
/// Keep this component active on BattleTurnIntroText; the Mask child may start inactive.
/// </summary>
public class BattleTurnIntroText : MonoBehaviour
{
    private static BattleTurnIntroText instance;

    public static int CurrentPlayCounter { get; private set; }
    public static event Action IntroStarted;
    public static event Action IntroCompleted;

    [Header("References")]
    [SerializeField] private TMP_Text introText;
    [Tooltip("The Mask RectTransform containing back, line and IntroText.")]
    [SerializeField] private RectTransform mask;
    [Tooltip("CanvasGroup on Mask; used to fade all children in together.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Turn Intro SFX")]
    [Tooltip("턴 시작 인트로가 등장할 때 SFX를 재생합니다.")]
    [SerializeField] private bool playTurnIntroSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string turnIntroSfxId;
    [SerializeField, Range(0f, 1f)] private float turnIntroSfxVolume = 1f;
    [Tooltip("턴 인트로 등장 시작 시점을 기준으로 SFX 재생을 늦추는 시간(초).")]
    [SerializeField, Min(0f)] private float turnIntroSfxDelay;

    [Header("Play On Start")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool ignorePlayOnStart = true;
    [SerializeField, Min(1)] private int previewTurn = 1;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0f)] private float enterDuration = 0.45f;
    [SerializeField, Min(0f)] private float holdDuration = 0.65f;
    [SerializeField, Min(0f)] private float exitDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Mask Width Wipe")]
    [Tooltip("0 = use the width set on the Mask RectTransform in the editor.")]
    [SerializeField, Min(0f)] private float fullMaskWidth;
    [Tooltip("Reveal the contents from RIGHT to LEFT by expanding the Mask while keeping its right edge fixed.")]
    [SerializeField] private bool revealByWidthOnEnter = true;
    [Tooltip("At exit, narrow the Mask from LEFT to RIGHT while the whole banner moves right.")]
    [SerializeField] private bool wipeLeftToRightOnExit = true;
    [SerializeField] private AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Banner Slide (whole Mask)")]
    [Tooltip("Move the whole Mask left -> center -> right while clipping its children.")]
    [SerializeField] private bool moveBanner = true;
    [SerializeField] private Vector2 enterStartOffset = new Vector2(-200f, 0f);
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [Tooltip("Distance the entire banner drifts to the right during the middle hold phase. X=30 means 30 UI units to the right.")]
    [SerializeField] private Vector2 holdMoveOffset = new Vector2(30f, 0f);
    [SerializeField] private Vector2 exitEndOffset = new Vector2(200f, 0f);

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField, Range(0f, 1f)] private float startAlpha;
    [SerializeField, Range(0f, 1f)] private float middleAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float endAlpha;
    [Tooltip("Fade out the entire banner while it moves right and the Mask narrows.")]
    [SerializeField] private bool fadeOutDuringWipe = true;

    private Coroutine playRoutine;
    private Coroutine delayedSfxRoutine;
    private int playVersion;
    private Vector2 originalMaskPosition;
    private float originalMaskWidth;
    private bool originalRectCaptured;

    private void Awake()
    {
        InitializeReferences();
        HideImmediate();
    }

    private void OnEnable() => InitializeReferences();

    private void Start()
    {
        if (playOnStart && !ignorePlayOnStart)
            PlayTurn(previewTurn);
    }

    private void OnDestroy()
    {
        CancelDelayedSfx();
        if (instance == this) instance = null;
    }

    public static bool IsAnyPlayingOrVisible()
    {
        BattleTurnIntroText target = FindTarget();
        return target != null && (target.playRoutine != null ||
                                  (target.mask != null && target.mask.gameObject.activeInHierarchy));
    }

    public static void ShowMessage(string message)
    {
        BattleTurnIntroText target = FindTarget();
        if (target != null) target.Play(message);
        else Debug.LogWarning("[BattleTurnIntroText] Turn intro component was not found.");
    }

    public static IEnumerator ShowMessageAndWait(string message)
    {
        BattleTurnIntroText target = FindTarget();
        if (target != null) yield return target.PlayAndWait(message);
    }

    public static void ShowTurnStart(int turnNumber) => ShowMessage(BuildTurnMessage(turnNumber));

    public static IEnumerator ShowTurnStartAndWait(int turnNumber)
    {
        yield return ShowMessageAndWait(BuildTurnMessage(turnNumber));
    }

    public static string BuildTurnMessage(int turnNumber)
    {
        int number = Mathf.Max(1, turnNumber);
        return GameLocalization.FormatWithFallback(
            "battle.effect_info.turn_start_format",
            "{0}턴",
            number);
    }

    public void PlayTurn(int turnNumber) => Play(BuildTurnMessage(turnNumber));

    public IEnumerator PlayTurnAndWait(int turnNumber)
    {
        yield return PlayAndWait(BuildTurnMessage(turnNumber));
    }

    public void Play(string message)
    {
        if (!PreparePlay()) return;
        int version = ++playVersion;
        CurrentPlayCounter++;
        IntroStarted?.Invoke();
        playRoutine = StartCoroutine(PlayRoutine(message, version));
    }

    public IEnumerator PlayAndWait(string message)
    {
        if (!PreparePlay()) yield break;
        int version = ++playVersion;
        CurrentPlayCounter++;
        IntroStarted?.Invoke();
        playRoutine = StartCoroutine(PlayRoutine(message, version));
        yield return playRoutine;
    }

    public void StopAndHide()
    {
        ++playVersion;
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        HideImmediate();
    }

    public float GetTotalPlayDuration() =>
        Mathf.Max(0f, startDelay) + Mathf.Max(0f, enterDuration) +
        Mathf.Max(0f, holdDuration) + Mathf.Max(0f, exitDuration);

    public void HideImmediate()
    {
        CancelDelayedSfx();
        InitializeReferences();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        RestoreOriginalMaskRect();
        if (mask != null) mask.gameObject.SetActive(false);
    }

    private bool PreparePlay()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        InitializeReferences();
        if (introText == null || mask == null || canvasGroup == null)
        {
            Debug.LogWarning("[BattleTurnIntroText] Mask / IntroText reference is missing.", this);
            return false;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        CancelDelayedSfx();
        return true;
    }

    private void ScheduleTurnIntroSfx()
    {
        CancelDelayedSfx();
        if (!playTurnIntroSfx || string.IsNullOrWhiteSpace(turnIntroSfxId)) return;
        if (turnIntroSfxDelay <= 0f)
            PlayConfiguredTurnIntroSfx();
        else
            delayedSfxRoutine = StartCoroutine(PlayTurnSfxAfterDelay());
    }

    private IEnumerator PlayTurnSfxAfterDelay()
    {
        yield return WaitDuration(turnIntroSfxDelay);
        delayedSfxRoutine = null;
        PlayConfiguredTurnIntroSfx();
    }

    private void CancelDelayedSfx()
    {
        if (delayedSfxRoutine == null) return;
        StopCoroutine(delayedSfxRoutine);
        delayedSfxRoutine = null;
    }

    private void PlayConfiguredTurnIntroSfx()
    {
        if (!playTurnIntroSfx || string.IsNullOrWhiteSpace(turnIntroSfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
            return;

        audioManager.PlaySfx(turnIntroSfxId, Mathf.Clamp01(turnIntroSfxVolume));
    }

    private IEnumerator PlayRoutine(string message, int version)
    {
        RestoreOriginalMaskRect();
        mask.gameObject.SetActive(true);
        introText.gameObject.SetActive(true);
        // Localize components may assign their own text on enable.
        introText.text = string.IsNullOrEmpty(message) ? BuildTurnMessage(1) : message;
        introText.alpha = 1f;
        introText.ForceMeshUpdate();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = useFade ? startAlpha : middleAlpha;

        float fullWidth = GetFullWidth();
        float enterWidth = revealByWidthOnEnter ? 0f : fullWidth;
        Vector2 start = moveBanner ? enterStartOffset : Vector2.zero;
        Vector2 center = moveBanner ? centerOffset : Vector2.zero;
        Vector2 end = moveBanner ? exitEndOffset : Vector2.zero;
        Vector2 holdEnd = center + (moveBanner && holdDuration > 0f ? holdMoveOffset : Vector2.zero);

        // Fix the RIGHT edge while expanding: visible area opens right -> left.
        SetMaskAt(fullWidth, enterWidth, true, start);
        if (startDelay > 0f) yield return WaitDuration(startDelay);

        ScheduleTurnIntroSfx();

        yield return Animate(enterDuration, fullWidth,
            enterWidth, fullWidth, start, center,
            useFade ? startAlpha : middleAlpha, middleAlpha,
            enterCurve, true);

        // Keep drifting slowly right while the banner remains fully visible.
        // Full mask width and opacity stay unchanged during this interval.
        if (holdDuration > 0f)
            yield return Animate(holdDuration, fullWidth,
                fullWidth, fullWidth, center, holdEnd,
                middleAlpha, middleAlpha, AnimationCurve.Linear(0f, 0f, 1f, 1f), true);

        // Keep right edge fixed when narrowing to erase from left -> right.
        yield return Animate(exitDuration, fullWidth,
            fullWidth, wipeLeftToRightOnExit ? 0f : fullWidth,
            holdEnd, end, middleAlpha,
            useFade && fadeOutDuringWipe ? endAlpha : middleAlpha,
            exitCurve, wipeLeftToRightOnExit);

        if (version == playVersion)
        {
            playRoutine = null;
            HideImmediate();
            IntroCompleted?.Invoke();
        }
    }

    private IEnumerator Animate(float duration, float fullWidth,
        float fromWidth, float toWidth, Vector2 fromOffset, Vector2 toOffset,
        float fromAlpha, float toAlpha, AnimationCurve curve, bool anchorRight)
    {
        if (duration <= 0f)
        {
            SetMaskAt(fullWidth, toWidth, anchorRight, toOffset);
            canvasGroup.alpha = toAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t;
            SetMaskAt(fullWidth, Mathf.LerpUnclamped(fromWidth, toWidth, eased),
                anchorRight, Vector2.LerpUnclamped(fromOffset, toOffset, eased));
            canvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, eased);
            yield return null;
        }
        SetMaskAt(fullWidth, toWidth, anchorRight, toOffset);
        canvasGroup.alpha = toAlpha;
    }

    /// <summary>
    /// Keep the right edge fixed during right-to-left reveal and left-to-right erase.
    /// Movement offset is applied to the ENTIRE mask concurrently with the width wipe.
    /// </summary>
    private void SetMaskAt(float fullWidth, float newWidth, bool anchorRight, Vector2 offset)
    {
        if (mask == null) return;
        float width = Mathf.Clamp(newWidth, 0f, fullWidth);
        float pivot = mask.pivot.x;
        float edgeOffset = anchorRight
            ? (fullWidth - width) * (1f - pivot)
            : -(fullWidth - width) * pivot;
        mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        mask.anchoredPosition = originalMaskPosition + offset + new Vector2(edgeOffset, 0f);
    }

    private IEnumerator WaitDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private float GetFullWidth()
    {
        return Mathf.Max(0f, fullMaskWidth > 0f ? fullMaskWidth : originalMaskWidth);
    }

    private void RestoreOriginalMaskRect()
    {
        if (mask == null || !originalRectCaptured) return;
        mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalMaskWidth);
        mask.anchoredPosition = originalMaskPosition;
    }

    private void InitializeReferences()
    {
        if (instance == null) instance = this;
        if (mask == null)
        {
            Transform found = transform.Find("Mask");
            if (found != null) mask = found as RectTransform;
        }
        if (introText == null && mask != null)
            introText = mask.GetComponentInChildren<TMP_Text>(true);
        if (introText == null)
            introText = GetComponentInChildren<TMP_Text>(true);

        if (mask == null) return;

        if (mask.GetComponent<RectMask2D>() == null)
            mask.gameObject.AddComponent<RectMask2D>();

        if (canvasGroup == null || canvasGroup.transform != mask)
            canvasGroup = mask.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = mask.gameObject.AddComponent<CanvasGroup>();

        if (!originalRectCaptured)
        {
            originalMaskPosition = mask.anchoredPosition;
            originalMaskWidth = mask.rect.width;
            originalRectCaptured = true;
        }
    }

    private static BattleTurnIntroText FindTarget()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<BattleTurnIntroText>(FindObjectsInactive.Include);
        return instance;
    }
}
