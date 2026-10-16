using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Turn banner: inactive Image (with child IntroText) is enabled on play,
/// moves left-to-center-to-right, then fades out and becomes inactive again.
/// Keep this component on the active BattleTurnIntroText parent, not on Image.
/// </summary>
public class BattleTurnIntroText : MonoBehaviour
{
    private static BattleTurnIntroText instance;

    public static int CurrentPlayCounter { get; private set; }
    public static event Action IntroStarted;
    public static event Action IntroCompleted;

    [Header("References")]
    [SerializeField] private TMP_Text introText;
    [SerializeField, HideInInspector] private RectTransform animatedRoot;
    [SerializeField, HideInInspector] private CanvasGroup canvasGroup;
    [Header("VFX Prefab (Project Asset)")]
    [Tooltip("Project 창의 VFX 프리팹을 연결합니다. 미연결 시 텍스트만 표시합니다.")]
    [SerializeField] private GameObject textAppearVfxPrefab;
    [Tooltip("VFX 생성 부모. 비워두면 BattleTurnIntroText 밑에 생성합니다.")]
    [SerializeField] private Transform vfxSpawnParent;
    [SerializeField] private Vector3 vfxLocalPositionOffset;
    [Tooltip("켜면 VFX가 텍스트와 함께 이동합니다. VFX 자체에 이동 애니메이션이 있으면 끄세요.")]
    [SerializeField] private bool moveVfxWithText = true;
    [SerializeField] private bool restartParticleSystems = true;
    private GameObject spawnedVfx;

    [Header("Play On Start")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool ignorePlayOnStart = true;
    [SerializeField, Min(1)] private int previewTurn = 1;

    [Header("Motion")]
    [SerializeField] private bool useMoveEffect = true;
    [SerializeField] private Vector2 startOffset = new Vector2(-200f, 0f);
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [SerializeField] private Vector2 exitOffset = new Vector2(200f, 0f);
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0f)] private float enterDuration = 0.45f;
    [SerializeField, Min(0f)] private float holdDuration = 0.65f;
    [SerializeField, Min(0f)] private float exitDuration = 0.45f;
    [SerializeField] private AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Fade (Image + Child Text)")]
    [SerializeField] private bool useFade = true;
    [SerializeField, Range(0f, 1f)] private float startAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float middleAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float endAlpha = 0f;

    private Coroutine playRoutine;
    private int playVersion;
    private Vector2 basePosition;
    private bool positionCaptured;

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
        CleanupVfx();
        if (instance == this)
            instance = null;
    }

    public static bool IsAnyPlayingOrVisible()
    {
        BattleTurnIntroText target = FindTarget();
        return target != null && (target.playRoutine != null ||
                                  (target.animatedRoot != null && target.animatedRoot.gameObject.activeInHierarchy));
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
        return $"{number}{GameLocalization.Get(LocalizationKeys.Battle.Progress)}";
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
        InitializeReferences();
        CleanupVfx();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (animatedRoot != null)
        {
            animatedRoot.anchoredPosition = basePosition + startOffset;
            animatedRoot.gameObject.SetActive(false);
        }
    }

    private bool PreparePlay()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        InitializeReferences();
        if (introText == null || animatedRoot == null || canvasGroup == null)
        {
            Debug.LogWarning("[BattleTurnIntroText] IntroText (TMP) 설정을 확인하세요.", this);
            return false;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        return true;
    }

    private IEnumerator PlayRoutine(string message, int version)
    {
        // Activate the text before assigning the message. Localizer OnEnable may overwrite it,
        // so assign the requested message after activation.
        animatedRoot.gameObject.SetActive(true);
        introText.gameObject.SetActive(true);
        introText.text = string.IsNullOrEmpty(message) ? BuildTurnMessage(1) : message;
        // A previous implementation left TMP_Text.alpha at zero. Restore it explicitly;
        // CanvasGroup alone must control the opacity of both Image and text.
        introText.alpha = 1f;
        introText.ForceMeshUpdate();

        animatedRoot.anchoredPosition = basePosition + (useMoveEffect ? startOffset : centerOffset);
        canvasGroup.alpha = useFade ? startAlpha : middleAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (startDelay > 0f) yield return WaitDuration(startDelay);
        SpawnVfx();
        yield return Animate(enterDuration, startOffset, centerOffset,
            useFade ? startAlpha : middleAlpha, middleAlpha, enterCurve);
        if (holdDuration > 0f) yield return WaitDuration(holdDuration);
        yield return Animate(exitDuration, centerOffset, exitOffset,
            middleAlpha, useFade ? endAlpha : middleAlpha, exitCurve);

        if (version == playVersion)
        {
            playRoutine = null;
            HideImmediate();
            IntroCompleted?.Invoke();
        }
    }

    private IEnumerator Animate(float duration, Vector2 from, Vector2 to,
        float alphaFrom, float alphaTo, AnimationCurve curve)
    {
        float elapsed = 0f;
        if (duration <= 0f)
        {
            if (useMoveEffect) animatedRoot.anchoredPosition = basePosition + to;
            canvasGroup.alpha = alphaTo;
            yield break;
        }
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t;
            if (useMoveEffect)
                animatedRoot.anchoredPosition = basePosition + Vector2.LerpUnclamped(from, to, eased);
            canvasGroup.alpha = Mathf.LerpUnclamped(alphaFrom, alphaTo, eased);
            yield return null;
        }
        if (useMoveEffect) animatedRoot.anchoredPosition = basePosition + to;
        canvasGroup.alpha = alphaTo;
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

    private void SpawnVfx()
    {
        CleanupVfx();
        if (textAppearVfxPrefab == null) return;

        Transform parent = vfxSpawnParent != null ? vfxSpawnParent : transform;
        spawnedVfx = Instantiate(textAppearVfxPrefab, parent, false);
        Transform vfxTransform = spawnedVfx.transform;

        // Follow mode uses the TMP RectTransform as parent, matching its motion.
        if (moveVfxWithText && animatedRoot != null)
        {
            vfxTransform.SetParent(animatedRoot, false);
            vfxTransform.localPosition = vfxLocalPositionOffset;
        }
        else if (animatedRoot != null)
        {
            // Position independent VFX at the text's initial location.
            vfxTransform.position = animatedRoot.TransformPoint(vfxLocalPositionOffset);
        }
        else
        {
            vfxTransform.localPosition = vfxLocalPositionOffset;
        }

        spawnedVfx.SetActive(true);
        if (restartParticleSystems)
        {
            ParticleSystem[] systems = spawnedVfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                systems[i].Clear(true);
                systems[i].Play(true);
            }
        }
    }

    private void CleanupVfx()
    {
        if (spawnedVfx == null) return;
        GameObject target = spawnedVfx;
        spawnedVfx = null;
        // Ensure a stopped animation cannot remain visible during the frame before Destroy.
        target.SetActive(false);
        Destroy(target);
    }

    private void InitializeReferences()
    {
        if (instance == null) instance = this;
        if (introText == null)
            introText = GetComponentInChildren<TMP_Text>(true);

        // Never retain an old serialized Image as the Animated Root.
        // The text is the only animated UI now.
        if (introText != null)
            animatedRoot = introText.rectTransform;

        if (animatedRoot == null) return;

        if (canvasGroup == null || canvasGroup.transform != animatedRoot)
            canvasGroup = animatedRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = animatedRoot.gameObject.AddComponent<CanvasGroup>();

        if (!positionCaptured)
        {
            basePosition = animatedRoot.anchoredPosition;
            positionCaptured = true;
        }
    }

    private static BattleTurnIntroText FindTarget()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<BattleTurnIntroText>(FindObjectsInactive.Include);
        return instance;
    }
}
