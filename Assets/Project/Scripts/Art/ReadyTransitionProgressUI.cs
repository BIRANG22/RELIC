using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the Ready shader and the single parent Ready/Panel.
/// LobbyEquipPanelUI controls the Ready state and calls OpenPanel/ClosePanel.
/// </summary>
public sealed class ReadyTransitionProgressUI : MonoBehaviour
{
    [Serializable]
    private sealed class ProgressAnimationSettings
    {
        [Tooltip("Expected start value. Used when Use Current Value As Start is disabled.")]
        public float from;
        public float to;
        [Min(0f)] public float duration = 0.6f;
        [Min(0f)] public float startDelay;
        public bool useCurrentValueAsStart = true;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [Serializable]
    private sealed class PanelAnimationSettings
    {
        [Tooltip("Expected start Anchored X. Used when Use Current Position As Start is disabled.")]
        public float fromX;
        public float toX;
        [Min(0f)] public float duration = 0.35f;
        [Min(0f)] public float startDelay;
        public bool useCurrentPositionAsStart = true;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [Header("References")]
    [SerializeField] private Graphic transitionGraphic;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    [Header("Ready UI Particle")]
    [Tooltip("Assign Ready/Panel/UIParticle. Activated when opening and deactivated when closing.")]
    [SerializeField] private GameObject readyUiParticle;
    [SerializeField] private bool hideParticleOnAwake = true;

    [Header("Standalone Button Binding")]
    [Tooltip("Keep OFF when BattlePlayButton and LobbyEquipPanelUI handle the buttons.")]
    [SerializeField] private bool bindButtonsDirectly = false;

    [Header("Initial State")]
    [SerializeField] private float initialProgress = 0f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Play Button - Transition")]
    [SerializeField] private ProgressAnimationSettings playTransition = new ProgressAnimationSettings
    {
        from = 0f, to = -2f, duration = 0.6f, startDelay = 0f
    };

    [Header("Play Button - Panel Movement")]
    [SerializeField] private PanelAnimationSettings playPanel = new PanelAnimationSettings
    {
        fromX = 1350f, toX = 0f, duration = 0.35f, startDelay = 0.18f
    };

    [Header("Back Button - Panel Movement")]
    [SerializeField] private PanelAnimationSettings backPanel = new PanelAnimationSettings
    {
        fromX = 0f, toX = 1350f, duration = 0.35f, startDelay = 0f
    };

    [Header("Back Button - Transition")]
    [Tooltip("Start Delay is measured from the Back sequence start. 0.35 means after the default slide.")]
    [SerializeField] private ProgressAnimationSettings backTransition = new ProgressAnimationSettings
    {
        from = -2f, to = 0f, duration = 0.6f, startDelay = 0.35f
    };

    private static readonly int ProgressId = Shader.PropertyToID("_progress");
    private Material instanceMaterial;
    private Material originalMaterial;
    private Coroutine panelSequence;
    private float currentProgress;

    public bool IsAnimating => panelSequence != null;

    private void Awake()
    {
        if (transitionGraphic == null)
        {
            Debug.LogError("[ReadyTransitionProgressUI] Assign Ready/Transition Graphic.", this);
            enabled = false;
            return;
        }

        originalMaterial = transitionGraphic.material;
        if (originalMaterial == null || !originalMaterial.HasProperty(ProgressId))
        {
            Debug.LogError("[ReadyTransitionProgressUI] Material does not expose _progress.", this);
            enabled = false;
            return;
        }

        instanceMaterial = new Material(originalMaterial);
        instanceMaterial.name = originalMaterial.name + " (Ready Transition Instance)";
        transitionGraphic.material = instanceMaterial;
        SetProgress(initialProgress);
        if (hideParticleOnAwake) SetReadyParticleActive(false);
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

    // Standalone preview methods; do not bind these alongside LobbyEquipPanelUI Open/Close.
    public void Play() => OpenPanel(panel);
    public void Back() => ClosePanel(panel, null);

    public void OpenPanel(RectTransform panelRect)
    {
        if (!isActiveAndEnabled) return;
        CachePanel(panelRect);
        StopPanelSequence();
        SetReadyParticleActive(true);
        panelSequence = StartCoroutine(RunSequence(playTransition, playPanel, null));
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
        SetReadyParticleActive(false);
        panelSequence = StartCoroutine(RunSequence(backTransition, backPanel, onComplete));
    }

    public bool IsPanelAwayFromClosedPosition(RectTransform panelRect)
    {
        CachePanel(panelRect);
        return panel != null && Mathf.Abs(panel.anchoredPosition.x - backPanel.toX) > 0.5f;
    }

    public bool IsPanelAtOpenPosition(RectTransform panelRect)
    {
        CachePanel(panelRect);
        return panel != null && Mathf.Abs(panel.anchoredPosition.x - playPanel.toX) <= 0.5f;
    }

    public void StopPanelSequence()
    {
        if (panelSequence == null) return;
        StopCoroutine(panelSequence);
        panelSequence = null;
    }

    private void SetReadyParticleActive(bool active)
    {
        if (readyUiParticle != null && readyUiParticle.activeSelf != active)
            readyUiParticle.SetActive(active);
    }

    private void CachePanel(RectTransform panelRect)
    {
        if (panelRect != null) panel = panelRect;
    }

    private IEnumerator RunSequence(ProgressAnimationSettings progress, PanelAnimationSettings movement, Action onComplete)
    {
        if (panel == null)
            Debug.LogWarning("[ReadyTransitionProgressUI] Assign Ready/Panel RectTransform.", this);

        // Both animations use delays relative to the same button click. This supports
        // overlap and strict sequencing without relying on Button.OnClick order.
        float totalDuration = Mathf.Max(progress.startDelay + progress.duration,
                                        movement.startDelay + movement.duration);
        float progressFrom = progress.useCurrentValueAsStart ? currentProgress : progress.from;
        float panelFrom = panel != null
            ? (movement.useCurrentPositionAsStart ? panel.anchoredPosition.x : movement.fromX)
            : movement.fromX;
        float elapsed = 0f;

        if (!progress.useCurrentValueAsStart) SetProgress(progress.from);
        if (panel != null && !movement.useCurrentPositionAsStart) SetAnchoredX(panel, movement.fromX);

        while (elapsed < totalDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (elapsed >= progress.startDelay)
            {
                float t = progress.duration <= 0f ? 1f : Mathf.Clamp01((elapsed - progress.startDelay) / progress.duration);
                float eased = progress.easing != null ? progress.easing.Evaluate(t) : t;
                SetProgress(Mathf.LerpUnclamped(progressFrom, progress.to, eased));
            }

            if (panel != null && elapsed >= movement.startDelay)
            {
                float t = movement.duration <= 0f ? 1f : Mathf.Clamp01((elapsed - movement.startDelay) / movement.duration);
                float eased = movement.easing != null ? movement.easing.Evaluate(t) : t;
                SetAnchoredX(panel, Mathf.LerpUnclamped(panelFrom, movement.toX, eased));
            }

            yield return null;
        }

        SetProgress(progress.to);
        if (panel != null) SetAnchoredX(panel, movement.toX);
        panelSequence = null;
        onComplete?.Invoke();
    }

    private static void SetAnchoredX(RectTransform rect, float x)
    {
        Vector2 pos = rect.anchoredPosition;
        pos.x = x;
        rect.anchoredPosition = pos;
    }

    private void SetProgress(float value)
    {
        currentProgress = value;
        if (instanceMaterial == null) return;
        instanceMaterial.SetFloat(ProgressId, value);
        transitionGraphic.SetMaterialDirty();
    }
}
