using System.Collections;
using TMPro;
using UnityEngine;

public class BattleMapIntroText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text introText;
    [SerializeField, TextArea(2, 5)] private string message = "전투 지역 진입";

    [Header("Timing")]
    [SerializeField] private bool playOnStart;
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

    private void Awake()
    {
        if (introText == null)
            introText = GetComponentInChildren<TMP_Text>(true);

        rectTransform = introText != null ? introText.rectTransform : null;

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        HideImmediate();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
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

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine(text));
    }

    public void HideImmediate()
    {
        if (introText == null)
            return;

        introText.alpha = 0f;
        introText.gameObject.SetActive(false);

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + startOffset;
    }

    private IEnumerator PlayRoutine(string text)
    {
        introText.text = string.IsNullOrEmpty(text) ? message : text;
        introText.alpha = 0f;
        introText.gameObject.SetActive(true);

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + startOffset;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return FadeAndMove(0f, 1f, fadeInDuration, startOffset, endOffset);

        if (stayDuration > 0f)
            yield return new WaitForSeconds(stayDuration);

        yield return FadeAndMove(1f, 0f, fadeOutDuration, endOffset, endOffset);

        introText.gameObject.SetActive(false);
        playRoutine = null;
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

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t -= 1f;
        return t * t * t + 1f;
    }
}
