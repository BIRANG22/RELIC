using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Play() 호출 시 이 오브젝트를 이동시키고, 지정한 여러 UI/스프라이트를 동시에 페이드인합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AppearSlideAnimation : MonoBehaviour
{
    [Header("등장 위치 (원래 위치 기준)")]
    [SerializeField] private Vector3 startOffset = new Vector3(0f, -30f, 0f);

    [Header("애니메이션")]
    [SerializeField, Min(0f)] private float startDelay = 0f;
    [SerializeField, Min(0.01f)] private float duration = 0.3f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("투명도 대상 (여러 개 지정 가능)")]
    [Tooltip("페이드인할 UI CanvasGroup들을 등록하세요. 각 대상에 CanvasGroup 컴포넌트가 필요합니다.")]
    [SerializeField] private CanvasGroup[] targetCanvasGroups = new CanvasGroup[0];
    [Tooltip("페이드인할 SpriteRenderer들을 등록하세요. 하위 캐릭터들을 각각 드래그할 수 있습니다.")]
    [SerializeField] private SpriteRenderer[] targetSpriteRenderers = new SpriteRenderer[0];

    // 이전 버전에서 지정한 단일 대상도 유지합니다. 새로 등록할 때는 위 배열을 사용하세요.
    [SerializeField, HideInInspector] private CanvasGroup targetCanvasGroup;
    [SerializeField, HideInInspector] private SpriteRenderer targetSpriteRenderer;

    [Header("투명도 (0 = 투명, 1 = 불투명)")]
    [SerializeField, Range(0f, 1f)] private float startAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float endAlpha = 1f;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private readonly List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private readonly List<float> originalSpriteAlphas = new List<float>();
    private Coroutine currentAnimation;
    private bool initialized;
    private readonly Dictionary<SpriteRenderer, float> originalAlphaByRenderer = new Dictionary<SpriteRenderer, float>();

    private void Awake() { Initialize(); }

    private void Initialize()
    {
        if (initialized) return;
        rectTransform = transform as RectTransform;
        originalPosition = rectTransform != null ? rectTransform.anchoredPosition3D : transform.localPosition;
        initialized = true;
    }

    private void CollectTargets()
    {
        canvasGroups.Clear();
        spriteRenderers.Clear();
        originalSpriteAlphas.Clear();

        if (targetCanvasGroups != null)
            foreach (CanvasGroup group in targetCanvasGroups)
                if (group != null && !canvasGroups.Contains(group)) canvasGroups.Add(group);

        if (targetSpriteRenderers != null)
            foreach (SpriteRenderer renderer in targetSpriteRenderers)
                if (renderer != null && !spriteRenderers.Contains(renderer)) spriteRenderers.Add(renderer);

        if (targetCanvasGroup != null && !canvasGroups.Contains(targetCanvasGroup))
            canvasGroups.Add(targetCanvasGroup);
        if (targetSpriteRenderer != null && !spriteRenderers.Contains(targetSpriteRenderer))
            spriteRenderers.Add(targetSpriteRenderer);

        // 아무 대상도 지정하지 않은 경우 기존과 동일하게 자기 자신을 사용합니다.
        if (canvasGroups.Count == 0 && spriteRenderers.Count == 0)
        {
            if (rectTransform != null)
            {
                CanvasGroup group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
                canvasGroups.Add(group);
            }
            else
            {
                SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                if (renderer != null) spriteRenderers.Add(renderer);
            }
        }

        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (!originalAlphaByRenderer.TryGetValue(renderer, out float originalAlpha))
            {
                originalAlpha = renderer.color.a;
                originalAlphaByRenderer.Add(renderer, originalAlpha);
            }
            originalSpriteAlphas.Add(originalAlpha);
        }
    }

    // 자동 재생하지 않습니다. TitleManager 등의 외부 스크립트에서 Play()를 호출합니다.
    // 기존 Show() 호출과의 호환성은 유지합니다.
    public void Show()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (gameObject.activeInHierarchy) Play();
    }

    public void RefreshOriginalPosition()
    {
        Initialize();
        originalPosition = rectTransform != null ? rectTransform.anchoredPosition3D : transform.localPosition;
    }

    public void Play()
    {
        if (!gameObject.activeInHierarchy) return;
        Initialize();
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        CollectTargets();
        SetPosition(originalPosition + startOffset);
        SetAlpha(startAlpha);
        currentAnimation = StartCoroutine(Animate());
    }

    public void Hide()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator Animate()
    {
        // 시작 위치와 시작 투명도를 유지한 상태로 딜레이를 기다립니다.
        float delayElapsed = 0f;
        while (delayElapsed < startDelay)
        {
            delayElapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        float elapsed = 0f;
        Vector3 from = originalPosition + startOffset;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easing.Evaluate(t);
            SetPosition(Vector3.LerpUnclamped(from, originalPosition, easedT));
            SetAlpha(Mathf.LerpUnclamped(startAlpha, endAlpha, easedT));
            yield return null;
        }
        SetPosition(originalPosition);
        SetAlpha(endAlpha);
        currentAnimation = null;
    }

    private void OnDisable()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        if (initialized)
        {
            SetPosition(originalPosition);
            SetAlpha(endAlpha);
        }
    }

    private void SetPosition(Vector3 position)
    {
        if (rectTransform != null) rectTransform.anchoredPosition3D = position;
        else transform.localPosition = position;
    }

    private void SetAlpha(float alpha)
    {
        foreach (CanvasGroup group in canvasGroups)
            if (group != null) group.alpha = alpha;

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null) continue;
            Color color = renderer.color;
            color.a = originalSpriteAlphas[i] * alpha;
            renderer.color = color;
        }
    }
}
