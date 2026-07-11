using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널이 활성화되거나 비활성화될 때
/// 알파값과 크기를 부드럽게 전환합니다.
/// </summary>
[DisallowMultipleComponent]
public class OptionPanelTransition : MonoBehaviour
{
    [Header("열기 효과")]
    [SerializeField, Min(0.01f)] private float openDuration = 0.22f;
    [SerializeField, Range(0.5f, 1f)] private float openStartScale = 0.96f;

    [Header("닫기 효과")]
    [SerializeField, Min(0.01f)] private float closeDuration = 0.18f;
    [SerializeField, Range(0.5f, 1f)] private float closeEndScale = 0.96f;


    [Header("전환 중 검정 처리")]
    [Tooltip("열고 닫히는 동안만 검정색으로 보일 배경 이미지를 등록합니다. 전환이 끝나면 원래 색상으로 돌아갑니다.")]
    [SerializeField] private Graphic[] transitionBackgroundGraphics;

    [Header("곡선")]
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine transitionCoroutine;
    private bool isClosing;
    private Color[] originalTransitionColors;

    public bool IsClosing => isClosing;

    private void Awake()
    {
        CacheReferences();
        FindTransitionBackgroundAutomatically();
        CacheTransitionBackgroundColors();
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        isClosing = false;
    }

    /// <summary>
    /// 패널을 활성화한 직후 호출합니다.
    /// </summary>
    public void PlayOpen()
    {
        CacheReferences();
        FindTransitionBackgroundAutomatically();
        StopCurrentTransition();

        isClosing = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = Vector3.one * openStartScale;

        transitionCoroutine = StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 닫기 효과가 끝난 뒤 onClosed를 호출합니다.
    /// </summary>
    public void PlayClose(Action onClosed)
    {
        if (!gameObject.activeInHierarchy)
        {
            onClosed?.Invoke();
            return;
        }

        if (isClosing)
            return;

        CacheReferences();
        FindTransitionBackgroundAutomatically();
        StopCurrentTransition();

        isClosing = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        transitionCoroutine = StartCoroutine(CloseRoutine(onClosed));
    }

    /// <summary>
    /// 애니메이션 없이 완전히 열린 상태로 맞춥니다.
    /// </summary>
    public void SetOpenedImmediately()
    {
        CacheReferences();
        StopCurrentTransition();

        isClosing = false;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = Vector3.one;
        RestoreTransitionBackgroundColors();
    }

    private IEnumerator OpenRoutine()
    {
        Color[] transitionStartColors = CaptureCurrentTransitionColors();
        ApplyTransitionColorProgress(0f, true, transitionStartColors);
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / openDuration);
            float progress = openCurve.Evaluate(normalizedTime);

            canvasGroup.alpha = progress;
            rectTransform.localScale = Vector3.one * Mathf.Lerp(openStartScale, 1f, progress);
            ApplyTransitionColorProgress(progress, true, transitionStartColors);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = Vector3.one;
        RestoreTransitionBackgroundColors();
        transitionCoroutine = null;
    }

    private IEnumerator CloseRoutine(Action onClosed)
    {
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = rectTransform.localScale;
        Color[] transitionStartColors = CaptureCurrentTransitionColors();
        Vector3 targetScale = Vector3.one * closeEndScale;
        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / closeDuration);
            float progress = closeCurve.Evaluate(normalizedTime);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            ApplyTransitionColorProgress(progress, false, transitionStartColors);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = targetScale;
        ApplyTransitionColorProgress(1f, false, transitionStartColors);
        RestoreTransitionBackgroundColors();
        transitionCoroutine = null;
        isClosing = false;
        onClosed?.Invoke();
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void StopCurrentTransition()
    {
        if (transitionCoroutine == null)
            return;

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }

    /// <summary>
    /// 런타임에 자동으로 추가되는 설정 프리팹에서는
    /// BackGround_Back 오브젝트의 Graphic을 자동으로 찾아 연결합니다.
    /// </summary>
    private void FindTransitionBackgroundAutomatically()
    {
        if (transitionBackgroundGraphics != null && transitionBackgroundGraphics.Length > 0)
            return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null && graphic.gameObject.name == "BackGround_Back")
            {
                transitionBackgroundGraphics = new[] { graphic };
                return;
            }
        }

        transitionBackgroundGraphics = Array.Empty<Graphic>();
        Debug.LogWarning("[OptionPanelTransition] BackGround_Back 오브젝트의 Graphic을 찾지 못했습니다.", this);
    }

    private void CacheTransitionBackgroundColors()
    {
        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        originalTransitionColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic != null)
            {
                originalTransitionColors[i] = graphic.color;
            }
        }
    }

    private Color[] CaptureCurrentTransitionColors()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        Color[] colors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            colors[i] = graphic != null ? graphic.color : Color.white;
        }

        return colors;
    }

    /// <summary>
    /// 열릴 때는 검정에서 원래 색상으로, 닫힐 때는 현재 색상에서 검정으로 자연스럽게 보간합니다.
    /// </summary>
    private void ApplyTransitionColorProgress(float progress, bool opening, Color[] startColors)
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color original = originalTransitionColors[i];
            Color black = new Color(0f, 0f, 0f, original.a);

            if (opening)
            {
                graphic.color = Color.Lerp(black, original, progress);
            }
            else
            {
                Color start = startColors != null && i < startColors.Length ? startColors[i] : original;
                Color targetBlack = new Color(0f, 0f, 0f, start.a);
                graphic.color = Color.Lerp(start, targetBlack, progress);
            }
        }
    }

    private void SetTransitionBackgroundsBlack()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color current = graphic.color;
            graphic.color = new Color(0f, 0f, 0f, current.a);
        }
    }

    private void RestoreTransitionBackgroundColors()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic != null)
            {
                graphic.color = originalTransitionColors[i];
            }
        }
    }

    private void CacheTransitionBackgroundColorsIfNeeded()
    {
        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        if (originalTransitionColors == null || originalTransitionColors.Length != count)
        {
            CacheTransitionBackgroundColors();
        }
    }

}
