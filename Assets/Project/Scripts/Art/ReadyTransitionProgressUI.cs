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

    [Header("Ready Panel BackButton Fade")]
    [Tooltip("CanvasGroup on Ready/Panel/Ready_Panel/BackButton. Automatically found when empty.")]
    [SerializeField] private CanvasGroup readyBackButtonCanvasGroup;

    [Header("Transition Background")]
    [Tooltip("Optional. PositionPanel/BackgroundPanel is found automatically when left empty.")]
    [SerializeField] private GameObject backgroundPanel;

    [Header("Lobby Icon Visibility")]
    [Tooltip("Canvas/Lobby_Icon. Hidden when the Ready opening transition starts and shown after the closing slide finishes.")]
    [SerializeField] private GameObject lobbyIconRoot;
    [Tooltip("Fade duration for Lobby_Icon when the Ready panel opens or closes.")]
    [SerializeField, Min(0f)] private float lobbyIconFadeDuration = 0.2f;
    private CanvasGroup lobbyIconCanvasGroup;

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
    [SerializeField]
    private ProgressAnimationSettings playTransition = new ProgressAnimationSettings
    {
        from = 0f,
        to = -2f,
        duration = 0.6f,
        startDelay = 0f
    };

    [Header("Play Button - Panel Movement")]
    [SerializeField]
    private PanelAnimationSettings playPanel = new PanelAnimationSettings
    {
        fromX = 1350f,
        toX = 0f,
        duration = 0.35f,
        startDelay = 0.18f
    };

    [Header("Back Button - Panel Movement")]
    [SerializeField]
    private PanelAnimationSettings backPanel = new PanelAnimationSettings
    {
        fromX = 0f,
        toX = 1350f,
        duration = 0.35f,
        startDelay = 0f
    };

    [Header("Back Button - Transition")]
    [Tooltip("Start Delay is measured from the Back sequence start. 0.35 means after the default slide.")]
    [SerializeField]
    private ProgressAnimationSettings backTransition = new ProgressAnimationSettings
    {
        from = -2f,
        to = 0f,
        duration = 0.6f,
        startDelay = 0.35f
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
        ResolveReadyBackButtonCanvasGroup();
        SetReadyBackButtonAlpha(0f, false);
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

    public void OpenPanel(RectTransform panelRect, Action onComplete = null)
    {
        if (!isActiveAndEnabled) return;
        CachePanel(panelRect);
        StopPanelSequence();
        ResolveReadyBackButtonCanvasGroup();
        SetReadyBackButtonAlpha(readyBackButtonCanvasGroup != null ? readyBackButtonCanvasGroup.alpha : 0f, false);
        SetReadyParticleActive(true);
        ResolveLobbyIconRoot();
        panelSequence = StartCoroutine(RunSequence(playTransition, playPanel, onComplete));
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
        ResolveReadyBackButtonCanvasGroup();
        SetReadyBackButtonAlpha(readyBackButtonCanvasGroup != null ? readyBackButtonCanvasGroup.alpha : 1f, false);
        SetReadyParticleActive(false);
        ResolveLobbyIconRoot();
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

    private void HideBackgroundPanel()
    {
        if (backgroundPanel == null)
        {
            Transform positionPanel = transform.parent;
            if (positionPanel != null)
            {
                Transform found = positionPanel.Find("BackgroundPanel");
                if (found != null)
                    backgroundPanel = found.gameObject;
            }
        }

        if (backgroundPanel != null && backgroundPanel.activeSelf)
            backgroundPanel.SetActive(false);
    }

    private void ResolveLobbyIconRoot()
    {
        if (lobbyIconRoot != null)
            return;

        // Lobby_Icon is a sibling of PositionPanel under the outer Canvas.
        Transform current = transform;
        while (current != null)
        {
            Transform found = current.Find("Lobby_Icon");
            if (found != null)
            {
                lobbyIconRoot = found.gameObject;
                return;
            }
            current = current.parent;
        }
    }

    private void EnsureLobbyIconCanvasGroup()
    {
        ResolveLobbyIconRoot();
        if (lobbyIconRoot == null) return;
        if (lobbyIconCanvasGroup == null)
        {
            lobbyIconCanvasGroup = lobbyIconRoot.GetComponent<CanvasGroup>();
            if (lobbyIconCanvasGroup == null)
                lobbyIconCanvasGroup = lobbyIconRoot.AddComponent<CanvasGroup>();
        }
    }

    private void SetLobbyIconAlpha(float alpha)
    {
        EnsureLobbyIconCanvasGroup();
        if (lobbyIconCanvasGroup == null) return;
        lobbyIconCanvasGroup.alpha = Mathf.Clamp01(alpha);
        bool visible = alpha > 0.001f;
        lobbyIconCanvasGroup.interactable = visible;
        lobbyIconCanvasGroup.blocksRaycasts = visible;
    }

    private void FinishLobbyIconFade(bool opening)
    {
        if (lobbyIconRoot == null) return;
        SetLobbyIconAlpha(opening ? 0f : 1f);
        if (opening)
            lobbyIconRoot.SetActive(false);
        else if (!lobbyIconRoot.activeSelf)
            lobbyIconRoot.SetActive(true);
    }

    private void SetReadyParticleActive(bool active)
    {
        if (readyUiParticle != null && readyUiParticle.activeSelf != active)
            readyUiParticle.SetActive(active);
    }

    private void ResolveReadyBackButtonCanvasGroup()
    {
        if (readyBackButtonCanvasGroup != null) return;
        Transform target = panel != null ? panel.Find("Ready_Panel/BackButton") : null;
        if (target == null)
        {
            Transform ready = transform.Find("Panel/Ready_Panel/BackButton");
            target = ready;
        }
        if (target != null)
            readyBackButtonCanvasGroup = target.GetComponent<CanvasGroup>();
    }

    private void SetReadyBackButtonAlpha(float alpha, bool interactable)
    {
        if (readyBackButtonCanvasGroup == null) return;
        readyBackButtonCanvasGroup.alpha = Mathf.Clamp01(alpha);
        readyBackButtonCanvasGroup.interactable = interactable;
        readyBackButtonCanvasGroup.blocksRaycasts = interactable;
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
        bool opening = ReferenceEquals(movement, playPanel);
        float iconFadeStart = opening ? progress.startDelay : movement.startDelay + movement.duration;
        float totalDuration = Mathf.Max(Mathf.Max(progress.startDelay + progress.duration,
                                        movement.startDelay + movement.duration),
                                        iconFadeStart + lobbyIconFadeDuration);
        float progressFrom = progress.useCurrentValueAsStart ? currentProgress : progress.from;
        float panelFrom = panel != null
            ? (movement.useCurrentPositionAsStart ? panel.anchoredPosition.x : movement.fromX)
            : movement.fromX;
        float elapsed = 0f;
        bool backgroundHidden = false;
        EnsureLobbyIconCanvasGroup();
        float iconAlphaFrom = lobbyIconCanvasGroup != null ? lobbyIconCanvasGroup.alpha : (opening ? 1f : 0f);
        if (opening && lobbyIconRoot != null && !lobbyIconRoot.activeSelf)
        {
            lobbyIconRoot.SetActive(true);
            SetLobbyIconAlpha(1f);
            iconAlphaFrom = 1f;
        }
        float backButtonFrom = readyBackButtonCanvasGroup != null ? readyBackButtonCanvasGroup.alpha : (opening ? 0f : 1f);
        float backButtonTo = opening ? 1f : 0f;
        SetReadyBackButtonAlpha(backButtonFrom, false);

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

                // Hide the background when the shader transition actually begins.
                if (!backgroundHidden)
                {
                    HideBackgroundPanel();
                    backgroundHidden = true;
                }
            }

            if (panel != null && elapsed >= movement.startDelay)
            {
                float t = movement.duration <= 0f ? 1f : Mathf.Clamp01((elapsed - movement.startDelay) / movement.duration);
                float eased = movement.easing != null ? movement.easing.Evaluate(t) : t;
                SetAnchoredX(panel, Mathf.LerpUnclamped(panelFrom, movement.toX, eased));
                SetReadyBackButtonAlpha(Mathf.LerpUnclamped(backButtonFrom, backButtonTo, eased), false);
            }

            if (lobbyIconRoot != null && elapsed >= iconFadeStart)
            {
                if (!opening && !lobbyIconRoot.activeSelf)
                {
                    lobbyIconRoot.SetActive(true);
                    SetLobbyIconAlpha(0f);
                }
                float fadeT = lobbyIconFadeDuration <= 0f ? 1f :
                    Mathf.Clamp01((elapsed - iconFadeStart) / lobbyIconFadeDuration);
                fadeT = fadeT * fadeT * (3f - 2f * fadeT);
                SetLobbyIconAlpha(Mathf.Lerp(iconAlphaFrom, opening ? 0f : 1f, fadeT));
            }

            yield return null;
        }

        SetProgress(progress.to);
        if (!backgroundHidden) HideBackgroundPanel();
        if (panel != null) SetAnchoredX(panel, movement.toX);
        SetReadyBackButtonAlpha(backButtonTo, opening);
        FinishLobbyIconFade(opening);
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
