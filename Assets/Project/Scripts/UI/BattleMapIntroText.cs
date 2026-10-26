using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapIntroText : MonoBehaviour
{
    private const string DefaultMessageKey = "battle.intro.enter_battle_area";
    private static BattleMapIntroText instance;

    public static int CurrentPlayCounter { get; private set; }
    public static event Action IntroStarted;
    public static event Action IntroCompleted;

    public static bool IsAnyPlayingOrVisible()
    {
        BattleMapIntroText target = FindTarget();

        if (target == null)
            return false;

        if (target.playRoutine != null)
            return true;

        if (target.introText != null && target.introText.gameObject.activeInHierarchy && target.introText.alpha > 0.001f)
            return true;

        if (target.maskImage != null && target.useSkillMask && target.maskImage.gameObject.activeInHierarchy)
            return true;

        if (target.introImage != null && target.introImage.activeInHierarchy)
            return true;

        return false;
    }

    [Header("Text")]
    [SerializeField] private TMP_Text introText;

    [Header("Image")]
    [SerializeField] private GameObject introImage;
    [SerializeField] private bool autoBindIntroImage = true;
    [SerializeField] private string introImageObjectName = "Image";

    [Header("Room Intro SFX")]
    [Tooltip("전투방/이벤트방에 처음 진입할 때 방 인트로 텍스트와 함께 재생합니다.")]
    [SerializeField] private bool playRoomIntroSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string roomIntroSfxId;
    [SerializeField, Range(0f, 1f)] private float roomIntroSfxVolume = 1f;
    [Tooltip("PlayRoomIntroSfx 호출 시점을 기준으로 SFX 재생을 늦추는 시간(초).")]
    [SerializeField, Min(0f)] private float roomIntroSfxDelay;

    [Header("Battle End SFX")]
    [Tooltip("전투 종료 인트로 텍스트를 표시할 때 별도로 재생하는 SFX입니다.")]
    [SerializeField] private bool playBattleEndSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string battleEndSfxId;
    [SerializeField, Range(0f, 1f)] private float battleEndSfxVolume = 1f;
    [SerializeField, Min(0f)] private float battleEndSfxDelay;

    [Header("Timing")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool ignorePlayOnStart = true;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float stayDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Motion")]
    [SerializeField] private bool useMoveEffect = true;
    [SerializeField] private Vector2 startOffset = new Vector2(0f, -20f);
    [SerializeField] private Vector2 endOffset = Vector2.zero;

    [Header("Skill Mask (MaskImage > IntroText)")]
    [SerializeField] private bool useSkillMask = true;
    [SerializeField] private RectTransform maskImage;
    [SerializeField] private Graphic maskGraphic;
    [SerializeField] private Material skillMaskMaterial;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve maskCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Mask Position A / B / C")]
    [SerializeField] private Vector2 maskPositionA;
    [SerializeField] private Vector2 maskPositionB;
    [SerializeField] private Vector2 maskPositionC;
    [SerializeField, Min(0f)] private float positionAToBDelay;
    [SerializeField, Min(0f)] private float positionAToBDuration = 0.5f;
    [SerializeField, Min(0f)] private float positionBHoldDelay = 0.5f;
    [SerializeField, Min(0f)] private float positionBToCDelay;
    [SerializeField, Min(0f)] private float positionBToCDuration = 0.5f;

    [Header("Mask Height A / B / C")]
    [SerializeField, Min(0f)] private float maskHeightA;
    [SerializeField, Min(0f)] private float maskHeightB = 120f;
    [SerializeField, Min(0f)] private float maskHeightC;
    [SerializeField, Min(0f)] private float heightAToBDelay;
    [SerializeField, Min(0f)] private float heightAToBDuration = 0.5f;
    [SerializeField, Min(0f)] private float heightBHoldDelay = 0.5f;
    [SerializeField, Min(0f)] private float heightBToCDelay;
    [SerializeField, Min(0f)] private float heightBToCDuration = 0.5f;
    [SerializeField, Min(0f)] private float maskDeactivateDelay;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private Coroutine playRoutine;
    private Coroutine delayedRoomSfxRoutine;
    private Coroutine delayedBattleEndSfxRoutine;
    private int playVersion;
    private bool isInitialized;

    private void Awake()
    {
        InitializeReferencesIfNeeded();
        HideImmediate();
    }

    private void OnEnable()
    {
        InitializeReferencesIfNeeded();
    }

    private void OnDestroy()
    {
        CancelDelayedRoomSfx();
        CancelDelayedBattleEndSfx();
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (playOnStart && !ignorePlayOnStart)
            Play();
    }

    public static void ShowMessage(string text)
    {
        BattleMapIntroText target = FindTarget();

        if (target == null)
        {
            Debug.LogWarning($"[BattleMapIntroText] BattleMapIntroText를 찾을 수 없습니다. Message: {text}");
            return;
        }

        target.Play(text);
    }

    public static IEnumerator ShowMessageAndWait(string text)
    {
        BattleMapIntroText target = FindTarget();

        if (target == null)
        {
            Debug.LogWarning($"[BattleMapIntroText] BattleMapIntroText를 찾을 수 없습니다. Message: {text}");
            yield break;
        }

        yield return target.PlayAndWait(text);
    }

    public static void PlayRoomIntroSfx()
    {
        BattleMapIntroText target = FindTarget();
        target?.ScheduleRoomIntroSfx();
    }

    public static void PlayBattleEndSfx()
    {
        BattleMapIntroText target = FindTarget();
        target?.ScheduleBattleEndSfx();
    }

    private void ScheduleBattleEndSfx()
    {
        CancelDelayedBattleEndSfx();
        if (!playBattleEndSfx || string.IsNullOrWhiteSpace(battleEndSfxId)) return;
        if (battleEndSfxDelay <= 0f)
            PlayConfiguredBattleEndSfx();
        else
            delayedBattleEndSfxRoutine = StartCoroutine(PlayBattleEndSfxAfterDelay());
    }

    private IEnumerator PlayBattleEndSfxAfterDelay()
    {
        yield return WaitDuration(battleEndSfxDelay, useUnscaledTime);
        delayedBattleEndSfxRoutine = null;
        PlayConfiguredBattleEndSfx();
    }

    private void CancelDelayedBattleEndSfx()
    {
        if (delayedBattleEndSfxRoutine == null) return;
        StopCoroutine(delayedBattleEndSfxRoutine);
        delayedBattleEndSfxRoutine = null;
    }

    private void PlayConfiguredBattleEndSfx()
    {
        if (!playBattleEndSfx || string.IsNullOrWhiteSpace(battleEndSfxId)) return;
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null) return;
        audioManager.PlaySfx(battleEndSfxId, Mathf.Clamp01(battleEndSfxVolume));
    }

    private void ScheduleRoomIntroSfx()
    {
        CancelDelayedRoomSfx();
        if (!playRoomIntroSfx || string.IsNullOrWhiteSpace(roomIntroSfxId)) return;
        if (roomIntroSfxDelay <= 0f)
            PlayConfiguredRoomIntroSfx();
        else
            delayedRoomSfxRoutine = StartCoroutine(PlayRoomSfxAfterDelay());
    }

    private IEnumerator PlayRoomSfxAfterDelay()
    {
        yield return WaitDuration(roomIntroSfxDelay, useUnscaledTime);
        delayedRoomSfxRoutine = null;
        PlayConfiguredRoomIntroSfx();
    }

    private void CancelDelayedRoomSfx()
    {
        if (delayedRoomSfxRoutine == null) return;
        StopCoroutine(delayedRoomSfxRoutine);
        delayedRoomSfxRoutine = null;
    }

    private void PlayConfiguredRoomIntroSfx()
    {
        if (!playRoomIntroSfx || string.IsNullOrWhiteSpace(roomIntroSfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
            return;

        audioManager.PlaySfx(roomIntroSfxId, Mathf.Clamp01(roomIntroSfxVolume));
    }

    public void Play()
    {
        Play(GameLocalization.Get(DefaultMessageKey));
    }

    public void Play(string text)
    {
        EnsureRootActiveForPlay();
        InitializeReferencesIfNeeded();

        if (introText == null)
        {
            Debug.LogWarning("[BattleMapIntroText] Intro Text가 연결되어 있지 않습니다.", this);
            return;
        }

        StopCurrentRoutine();

        CurrentPlayCounter++;
        int version = ++playVersion;
        IntroStarted?.Invoke();
        playRoutine = StartCoroutine(PlayRoutine(text, version));
    }

    public IEnumerator PlayAndWait(string text)
    {
        EnsureRootActiveForPlay();
        InitializeReferencesIfNeeded();

        if (introText == null)
        {
            Debug.LogWarning("[BattleMapIntroText] Intro Text가 연결되어 있지 않습니다.", this);
            yield break;
        }

        StopCurrentRoutine();

        CurrentPlayCounter++;
        int version = ++playVersion;
        IntroStarted?.Invoke();
        playRoutine = StartCoroutine(PlayRoutine(text, version));
        yield return playRoutine;
    }

    public void StopAndHide()
    {
        StopCurrentRoutine();
        playVersion++;
        HideImmediate();
    }

    public float GetTotalPlayDuration()
    {
        if (useSkillMask && maskImage != null)
        {
            float positionLength = positionAToBDelay + positionAToBDuration + positionBHoldDelay + positionBToCDelay + positionBToCDuration;
            float heightLength = heightAToBDelay + heightAToBDuration + heightBHoldDelay + heightBToCDelay + heightBToCDuration;
            return Mathf.Max(0f, startDelay) + Mathf.Max(positionLength, heightLength) + Mathf.Max(0f, maskDeactivateDelay);
        }
        return Mathf.Max(0f, startDelay) +
               Mathf.Max(0f, fadeInDuration) +
               Mathf.Max(0f, stayDuration) +
               Mathf.Max(0f, fadeOutDuration);
    }

    public void HideImmediate()
    {
        InitializeReferencesIfNeeded();

        if (introText != null)
        {
            introText.alpha = 0f;
            introText.gameObject.SetActive(false);
        }

        if (introImage != null)
            introImage.SetActive(false);

        if (maskImage != null)
        {
            maskImage.gameObject.SetActive(false);
            SetMaskPosition(maskPositionA);
            SetMaskHeight(maskHeightA);
        }

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + startOffset;
    }

    private void StopCurrentRoutine()
    {
        if (playRoutine == null)
            return;

        StopCoroutine(playRoutine);
        playRoutine = null;
    }

    private IEnumerator PlayRoutine(string text, int version)
    {
        string message = string.IsNullOrEmpty(text)
            ? GameLocalization.Get(DefaultMessageKey)
            : text;

        // IntroText를 활성화할 때 같은 오브젝트의 로컬라이징 컴포넌트가
        // OnEnable에서 인스펙터 기본 문구를 다시 넣을 수 있습니다.
        // 먼저 오브젝트를 활성화한 뒤 이번에 표시할 문구를 마지막으로 적용합니다.
        bool maskMode = useSkillMask && maskImage != null && introText.transform.IsChildOf(maskImage);
        if (useSkillMask && !maskMode)
            Debug.LogWarning("[BattleMapIntroText] MaskImage must be an ancestor of IntroText. Using legacy fade.", this);

        if (maskMode)
        {
            SetMaskPosition(maskPositionA);
            SetMaskHeight(maskHeightA);
            maskImage.gameObject.SetActive(true);
            if (maskGraphic != null && skillMaskMaterial != null)
                maskGraphic.material = skillMaskMaterial;
        }

        introText.alpha = 0f;
        introText.gameObject.SetActive(true);
        introText.text = message;
        introText.ForceMeshUpdate();

        if (introImage != null && (!maskMode || introImage != maskImage.gameObject))
            introImage.SetActive(true);

        if (rectTransform != null && !maskMode)
            rectTransform.anchoredPosition = baseAnchoredPosition + startOffset;

        if (startDelay > 0f)
            yield return WaitDuration(startDelay, maskMode && useUnscaledTime);

        if (maskMode)
        {
            introText.alpha = 1f;
            // Position and height advance independently, matching the skill cutscene animation.
            float duration = Mathf.Max(
                positionAToBDelay + positionAToBDuration + positionBHoldDelay + positionBToCDelay + positionBToCDuration,
                heightAToBDelay + heightAToBDuration + heightBHoldDelay + heightBToCDelay + heightBToCDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                ApplyMaskAtTime(elapsed);
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            ApplyMaskAtTime(duration);
            if (maskDeactivateDelay > 0f)
                yield return WaitDuration(maskDeactivateDelay, useUnscaledTime);
        }
        else
        {
            yield return FadeAndMove(0f, 1f, fadeInDuration, startOffset, endOffset);
            if (stayDuration > 0f)
                yield return new WaitForSeconds(stayDuration);
            yield return FadeAndMove(1f, 0f, fadeOutDuration, endOffset, endOffset);
        }

        if (version == playVersion)
        {
            introText.gameObject.SetActive(false);

            if (introImage != null)
                introImage.SetActive(false);
            if (maskMode)
                maskImage.gameObject.SetActive(false);

            playRoutine = null;
            IntroCompleted?.Invoke();
        }
    }

    private IEnumerator FadeAndMove(float fromAlpha, float toAlpha, float duration, Vector2 fromOffset, Vector2 toOffset)
    {
        if (duration <= 0f)
        {
            introText.alpha = toAlpha;

            if (useMoveEffect && rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPosition + toOffset;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            introText.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);

            if (useMoveEffect && rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPosition + Vector2.Lerp(fromOffset, toOffset, eased);

            yield return null;
        }

        introText.alpha = toAlpha;

        if (useMoveEffect && rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + toOffset;
    }

    private void InitializeReferencesIfNeeded()
    {
        if (instance == null)
            instance = this;

        if (introText == null)
            introText = GetComponentInChildren<TMP_Text>(true);

        BindIntroImageIfNeeded();

        if (maskImage == null)
        {
            Transform found = transform.Find("MaskImage");
            if (found != null)
                maskImage = found as RectTransform;
        }
        if (maskImage != null && maskGraphic == null)
            maskGraphic = maskImage.GetComponent<Graphic>();
        if (maskImage != null && maskImage.GetComponent<RectMask2D>() == null)
        {
            // RectMask2D is required for children to be clipped. Add via Inspector.
        }

        rectTransform = introText != null ? introText.rectTransform : null;

        if (!isInitialized && rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        isInitialized = true;
    }

    private void EnsureRootActiveForPlay()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (instance == null)
            instance = this;
    }

    private void BindIntroImageIfNeeded()
    {
        if (!autoBindIntroImage || introImage != null || string.IsNullOrWhiteSpace(introImageObjectName))
            return;

        Transform found = transform.Find(introImageObjectName);

        if (found != null)
            introImage = found.gameObject;
    }

    private static BattleMapIntroText FindTarget()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<BattleMapIntroText>(FindObjectsInactive.Include);
        return instance;
    }

    private IEnumerator WaitDuration(float duration, bool unscaled)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void ApplyMaskAtTime(float time)
    {
        float posBStart = positionAToBDelay;
        float posBEnd = posBStart + positionAToBDuration;
        float posCStart = posBEnd + positionBHoldDelay + positionBToCDelay;
        Vector2 position = time < posBStart ? maskPositionA
            : time < posBEnd ? Vector2.LerpUnclamped(maskPositionA, maskPositionB, Curve01(time - posBStart, positionAToBDuration))
            : time < posCStart ? maskPositionB
            : Vector2.LerpUnclamped(maskPositionB, maskPositionC, Curve01(time - posCStart, positionBToCDuration));

        float heightBStart = heightAToBDelay;
        float heightBEnd = heightBStart + heightAToBDuration;
        float heightCStart = heightBEnd + heightBHoldDelay + heightBToCDelay;
        float height = time < heightBStart ? maskHeightA
            : time < heightBEnd ? Mathf.LerpUnclamped(maskHeightA, maskHeightB, Curve01(time - heightBStart, heightAToBDuration))
            : time < heightCStart ? maskHeightB
            : Mathf.LerpUnclamped(maskHeightB, maskHeightC, Curve01(time - heightCStart, heightBToCDuration));
        SetMaskPosition(position);
        SetMaskHeight(height);
    }

    private float Curve01(float elapsed, float duration)
    {
        float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        return maskCurve != null ? maskCurve.Evaluate(t) : t;
    }

    private void SetMaskPosition(Vector2 value)
    {
        if (maskImage == null) return;
        Vector3 pos = maskImage.localPosition;
        pos.x = value.x;
        pos.y = value.y;
        maskImage.localPosition = pos;
    }

    private void SetMaskHeight(float value)
    {
        if (maskImage != null)
            maskImage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, value));
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t -= 1f;
        return t * t * t + 1f;
    }
}
