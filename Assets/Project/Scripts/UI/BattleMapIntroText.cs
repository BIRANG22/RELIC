using System.Collections;
using TMPro;
using UnityEngine;

public class BattleMapIntroText : MonoBehaviour
{
    private static BattleMapIntroText instance;

    [Header("Text")]
    [SerializeField] private TMP_Text introText;
    [SerializeField, TextArea(2, 5)] private string message = "전투 지역 진입";

    [Header("Image")]
    [SerializeField] private GameObject introImage;
    [SerializeField] private bool autoBindIntroImage = true;
    [SerializeField] private string introImageObjectName = "Image";

    [Header("Timing")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool ignorePlayOnStart = true;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float stayDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Motion")]
    [SerializeField] private bool useMoveEffect = true;
    [SerializeField] private Vector2 startOffset = new Vector2(0f, -20f);
    [SerializeField] private Vector2 endOffset = Vector2.zero;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private Coroutine playRoutine;
    private int playVersion;

    private void Awake()
    {
        instance = this;

        if (introText == null)
            introText = GetComponentInChildren<TMP_Text>(true);

        BindIntroImageIfNeeded();

        rectTransform = introText != null ? introText.rectTransform : null;

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        HideImmediate();
    }

    private void OnEnable()
    {
        if (instance == null)
            instance = this;
    }

    private void OnDestroy()
    {
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

    public void Play()
    {
        Play(message);
    }

    public void Play(string text)
    {
        if (introText == null)
        {
            Debug.LogWarning("[BattleMapIntroText] Intro Text가 연결되어 있지 않습니다.", this);
            return;
        }

        StopCurrentRoutine();

        int version = ++playVersion;
        playRoutine = StartCoroutine(PlayRoutine(text, version));
    }

    public IEnumerator PlayAndWait(string text)
    {
        if (introText == null)
        {
            Debug.LogWarning("[BattleMapIntroText] Intro Text가 연결되어 있지 않습니다.", this);
            yield break;
        }

        StopCurrentRoutine();

        int version = ++playVersion;
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
        return Mathf.Max(0f, startDelay) +
               Mathf.Max(0f, fadeInDuration) +
               Mathf.Max(0f, stayDuration) +
               Mathf.Max(0f, fadeOutDuration);
    }

    public void HideImmediate()
    {
        if (introText != null)
        {
            introText.alpha = 0f;
            introText.gameObject.SetActive(false);
        }

        if (introImage != null)
            introImage.SetActive(false);

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
        introText.text = string.IsNullOrEmpty(text) ? message : text;
        introText.alpha = 0f;
        introText.gameObject.SetActive(true);

        if (introImage != null)
            introImage.SetActive(true);

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + startOffset;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return FadeAndMove(0f, 1f, fadeInDuration, startOffset, endOffset);

        if (stayDuration > 0f)
            yield return new WaitForSeconds(stayDuration);

        yield return FadeAndMove(1f, 0f, fadeOutDuration, endOffset, endOffset);

        if (version == playVersion)
        {
            introText.gameObject.SetActive(false);

            if (introImage != null)
                introImage.SetActive(false);

            playRoutine = null;
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

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t -= 1f;
        return t * t * t + 1f;
    }
}
