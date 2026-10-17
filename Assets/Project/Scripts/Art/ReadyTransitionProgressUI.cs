using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls Ready/Transition shader progress and Info/Ready panel movement.
/// LobbyEquipPanelUI owns the panel state; this component owns the animation.
/// </summary>
public sealed class ReadyTransitionProgressUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Graphic transitionGraphic;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;
    [Header("Standalone Button Binding")]
    [Tooltip("Leave false while LobbyEquipPanelUI controls Ready open/close.")]
    [SerializeField] private bool bindButtonsDirectly = false;

    [Header("Progress")]
    [SerializeField] private float initialProgress = 0f;
    [SerializeField] private float playProgress = -2f;
    [SerializeField] private float backProgress = 0f;
    [Min(0f)][SerializeField] private float duration = 0.6f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Panel Movement Target")]
    [Tooltip("Assign Ready/Panel. The Info_Panel and Ready_Panel children move together.")]
    [SerializeField] private RectTransform panel;

    [Header("Panel Movement Positions (Anchored X)")]
    [SerializeField] private float panelStartX = 1350f;
    [SerializeField] private float panelEndX = 0f;

    [Header("Panel Movement Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;
    [SerializeField]
    private AnimationCurve slideCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));
    [Tooltip("Delay from the start of opening Progress until Panel starts moving.")]
    [SerializeField, Min(0f)] private float slideEnterDelay = 0.18f;

    private static readonly int ProgressId = Shader.PropertyToID("_progress");
    private Material instanceMaterial;
    private Material originalMaterial;
    private Coroutine progressAnimation;
    private Coroutine panelSequence;
    private float currentProgress;

    public bool IsAnimating => progressAnimation != null || panelSequence != null;

    private void Awake()
    {
        if (transitionGraphic == null)
        {
            Debug.LogError("ReadyTransitionProgressUI: Assign Ready/Transition Graphic.", this);
            enabled = false;
            return;
        }

        originalMaterial = transitionGraphic.material;
        if (originalMaterial == null || !originalMaterial.HasProperty(ProgressId))
        {
            Debug.LogError("ReadyTransitionProgressUI: Material has no _progress property.", this);
            enabled = false;
            return;
        }

        instanceMaterial = new Material(originalMaterial);
        instanceMaterial.name = originalMaterial.name + " (Ready Transition Instance)";
        transitionGraphic.material = instanceMaterial;
        SetProgress(initialProgress);
    }

    private void OnEnable()
    {
        if (!bindButtonsDirectly) return;
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (backButton != null) backButton.onClick.AddListener(Back);
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        StopPanelSequence();
    }

    private void OnDestroy()
    {
        if (transitionGraphic != null && instanceMaterial != null && transitionGraphic.material == instanceMaterial)
            transitionGraphic.material = originalMaterial;
        if (instanceMaterial != null) Destroy(instanceMaterial);
    }

    public void Play() => AnimateTo(playProgress);
    public void Back() => AnimateTo(backProgress);

    public void OpenPanel(RectTransform panelRect)
    {
        if (!isActiveAndEnabled) return;
        CachePanel(panelRect);
        StopPanelSequence();
        panelSequence = StartCoroutine(OpenSequence());
    }

    public void ClosePanel(RectTransform panelRect, Action onComplete)
    {
        if (!isActiveAndEnabled)
        {
            onComplete?.Invoke();
            return;
        }
        CachePanel(panelRect);
        StopPanelSequence();
        panelSequence = StartCoroutine(CloseSequence(onComplete));
    }

    public bool IsPanelAwayFromClosedPosition(RectTransform panelRect)
    {
        CachePanel(panelRect);
        return panel != null && Mathf.Abs(panel.anchoredPosition.x - panelStartX) > 0.5f;
    }

    public bool IsPanelAtOpenPosition(RectTransform panelRect)
    {
        CachePanel(panelRect);
        return panel != null && Mathf.Abs(panel.anchoredPosition.x - panelEndX) <= 0.5f;
    }

    public void StopPanelSequence()
    {
        if (panelSequence != null)
        {
            StopCoroutine(panelSequence);
            panelSequence = null;
        }
        if (progressAnimation != null)
        {
            StopCoroutine(progressAnimation);
            progressAnimation = null;
        }
    }

    private void CachePanel(RectTransform panelRect)
    {
        if (panelRect != null) panel = panelRect;
    }

    private IEnumerator OpenSequence()
    {
        AnimateTo(playProgress);
        if (slideEnterDelay > 0f)
            yield return WaitSeconds(slideEnterDelay);
        yield return MovePanel(true);
        while (progressAnimation != null)
            yield return null;
        panelSequence = null;
    }

    private IEnumerator CloseSequence(Action onComplete)
    {
        yield return MovePanel(false);
        AnimateTo(backProgress);
        while (progressAnimation != null)
            yield return null;
        panelSequence = null;
        onComplete?.Invoke();
    }

    private IEnumerator MovePanel(bool opening)
    {
        if (panel == null)
        {
            Debug.LogWarning("[ReadyTransitionProgressUI] Assign Ready/Panel RectTransform.", this);
            yield break;
        }

        float fromX = panel.anchoredPosition.x;
        float toX = opening ? panelEndX : panelStartX;

        if (slideDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float eased = slideCurve != null ? slideCurve.Evaluate(t) : t;
                SetAnchoredX(panel, Mathf.LerpUnclamped(fromX, toX, eased));
                yield return null;
            }
        }
        SetAnchoredX(panel, toX);
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private static void SetAnchoredX(RectTransform rect, float x)
    {
        if (rect == null) return;
        Vector2 position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }

    private void AnimateTo(float target)
    {
        if (instanceMaterial == null || !isActiveAndEnabled) return;
        if (progressAnimation != null) StopCoroutine(progressAnimation);
        progressAnimation = StartCoroutine(AnimateProgress(target));
    }

    private IEnumerator AnimateProgress(float target)
    {
        float start = currentProgress;
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetProgress(Mathf.LerpUnclamped(start, target, easing != null ? easing.Evaluate(t) : t));
                yield return null;
            }
        }
        SetProgress(target);
        progressAnimation = null;
    }

    private void SetProgress(float value)
    {
        currentProgress = value;
        if (instanceMaterial == null) return;
        instanceMaterial.SetFloat(ProgressId, value);
        transitionGraphic.SetMaterialDirty();
    }
}
