using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;

public sealed class ResearchResultPanelUI : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";

    [Header("Presentation Root (separate from Wagon)")]
    [Tooltip("The Canvas/DustiumConversionPresentation object. Only this object is hidden; Wagon remains active.")]
    [SerializeField] private GameObject presentationRoot;

    [Header("Wagon Red Dustium Shader Fade")]
    [Tooltip("Assign Background / Interaction / Wagon / red_dustium.")]
    [SerializeField] private GameObject wagonRedDustium;
    [Tooltip("Renderer of red_dustium using M_dissolve_dustium. Can be found automatically in children.")]
    [SerializeField] private Renderer wagonRedDustiumRenderer;
    [Tooltip("Shader Graph Fade reference is _Fade (capital F).")]
    [SerializeField] private string wagonFadeProperty = "_Fade";
    [SerializeField, Min(0.01f)] private float wagonRedDustiumFadeDuration = 1.1f;

    [Header("Wagon Conversion VFX")]
    [Tooltip("Scene VFX under Background / Interaction / Wagon / Vfx_dustium_change.")]
    [SerializeField] private GameObject wagonConversionVfx;
    [Tooltip("Seconds to smoothly fade existing particles and trails after the conversion transfers finish.")]
    [SerializeField, Min(0.01f)] private float wagonConversionVfxFadeOutDuration = 0.5f;

    private Coroutine wagonConversionVfxFadeCoroutine;
    private ParticleSystem[] wagonVfxFadeParticles;
    private SpriteRenderer[] wagonVfxFadeSprites;
    private Color[] wagonVfxFadeSpriteOriginalColors;
    private TrailRenderer[] wagonVfxFadeTrails;
    private Gradient[] wagonVfxFadeTrailOriginalGradients;

    private void SetWagonConversionVfxActive(bool active)
    {
        CancelWagonConversionVfxFade();
        if (wagonConversionVfx == null || wagonConversionVfx == gameObject) return;
        wagonConversionVfx.SetActive(active);
        if (active)
        {
            // Reusing the scene object must restart its effects from a clean state.
            foreach (ParticleSystem particle in wagonConversionVfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null || !particle.gameObject.activeInHierarchy) continue;
                particle.Clear(true);
                particle.Play(true);
            }
        }
    }

    private void CancelWagonConversionVfxFade()
    {
        if (wagonConversionVfxFadeCoroutine != null)
        {
            StopCoroutine(wagonConversionVfxFadeCoroutine);
            wagonConversionVfxFadeCoroutine = null;
        }
        RestoreWagonConversionVfxVisuals();
    }

    private void RestoreWagonConversionVfxVisuals()
    {
        if (wagonVfxFadeSprites != null)
            for (int i = 0; i < wagonVfxFadeSprites.Length; i++)
                if (wagonVfxFadeSprites[i] != null)
                    wagonVfxFadeSprites[i].color = wagonVfxFadeSpriteOriginalColors[i];
        if (wagonVfxFadeTrails != null)
            for (int i = 0; i < wagonVfxFadeTrails.Length; i++)
                if (wagonVfxFadeTrails[i] != null)
                    wagonVfxFadeTrails[i].colorGradient = wagonVfxFadeTrailOriginalGradients[i];
        wagonVfxFadeSprites = null;
        wagonVfxFadeSpriteOriginalColors = null;
        wagonVfxFadeTrails = null;
        wagonVfxFadeTrailOriginalGradients = null;
        wagonVfxFadeParticles = null;
    }

    private IEnumerator FadeOutWagonConversionVfx()
    {
        if (wagonConversionVfx == null || !wagonConversionVfx.activeInHierarchy)
            yield break;

        wagonVfxFadeParticles = wagonConversionVfx.GetComponentsInChildren<ParticleSystem>(true);
        wagonVfxFadeSprites = wagonConversionVfx.GetComponentsInChildren<SpriteRenderer>(true);
        wagonVfxFadeTrails = wagonConversionVfx.GetComponentsInChildren<TrailRenderer>(true);
        wagonVfxFadeSpriteOriginalColors = new Color[wagonVfxFadeSprites.Length];
        wagonVfxFadeTrailOriginalGradients = new Gradient[wagonVfxFadeTrails.Length];
        for (int i = 0; i < wagonVfxFadeSprites.Length; i++)
            if (wagonVfxFadeSprites[i] != null)
                wagonVfxFadeSpriteOriginalColors[i] = wagonVfxFadeSprites[i].color;
        for (int i = 0; i < wagonVfxFadeTrails.Length; i++)
            if (wagonVfxFadeTrails[i] != null)
                wagonVfxFadeTrailOriginalGradients[i] = wagonVfxFadeTrails[i].colorGradient;

        // Stop spawning NEW particles, but allow already spawned particles to fade.
        foreach (ParticleSystem particle in wagonVfxFadeParticles)
            if (particle != null)
                particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        float duration = Mathf.Max(0.01f, wagonConversionVfxFadeOutDuration);
        float elapsed = 0f;
        float previousAlpha = 1f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            // Relative factor preserves particles' original per-particle alpha and
            // any pre-existing Color over Lifetime alpha changes.
            float multiplier = previousAlpha > 0.00001f ? alpha / previousAlpha : 0f;
            foreach (ParticleSystem particle in wagonVfxFadeParticles)
            {
                if (particle == null) continue;
                int count = particle.particleCount;
                if (count == 0) continue;
                ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[count];
                count = particle.GetParticles(buffer);
                for (int i = 0; i < count; i++)
                {
                    Color32 c = buffer[i].startColor;
                    c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * multiplier), 0, 255);
                    buffer[i].startColor = c;
                }
                particle.SetParticles(buffer, count);
            }
            for (int i = 0; i < wagonVfxFadeSprites.Length; i++)
            {
                if (wagonVfxFadeSprites[i] == null) continue;
                Color c = wagonVfxFadeSpriteOriginalColors[i];
                c.a *= alpha;
                wagonVfxFadeSprites[i].color = c;
            }
            for (int i = 0; i < wagonVfxFadeTrails.Length; i++)
            {
                if (wagonVfxFadeTrails[i] == null) continue;
                Gradient original = wagonVfxFadeTrailOriginalGradients[i];
                GradientAlphaKey[] keys = original.alphaKeys;
                for (int k = 0; k < keys.Length; k++) keys[k].alpha *= alpha;
                Gradient faded = new Gradient();
                faded.SetKeys(original.colorKeys, keys);
                wagonVfxFadeTrails[i].colorGradient = faded;
            }
            previousAlpha = alpha;
            yield return null;
        }

        if (wagonConversionVfx != null) wagonConversionVfx.SetActive(false);
        RestoreWagonConversionVfxVisuals();
        wagonConversionVfxFadeCoroutine = null;
    }

    [Header("Dustium Conversion Presentation")]
    [SerializeField] private CanvasGroup presentationCanvasGroup;
    [SerializeField] private TMP_Text redDustiumValueText;
    [SerializeField] private RectTransform fixedRedDustiumIcon;
    [SerializeField] private RectTransform transferBlur;
    [SerializeField] private Graphic transferBlurGraphic;
    [SerializeField] private LobbyBlueDustiumHudUI blueDustiumHud;

    [Header("Conversion VFX")]
    [Tooltip("Assign a regular VFX prefab (GameObject, Particle System / Trails). No transfer controller is required on the prefab.")]
    [FormerlySerializedAs("acquisitionTransferVfxPrefab")]
    [SerializeField] private GameObject conversionVfxPrefab;
    [Tooltip("Seconds each conversion VFX takes to reach the Blue Dustium HUD.")]
    [SerializeField, Min(0.01f)] private float conversionVfxDuration = 0.6f;
    [Tooltip("Sideways screen-space arc deviation in pixels (random left/right per transfer).")]
    [SerializeField] private Vector2 conversionVfxSideSpread = new Vector2(240f, 460f);
    [Tooltip("Vertical screen-space arc deviation in pixels.")]
    [SerializeField] private Vector2 conversionVfxVerticalSpread = new Vector2(-170f, 210f);
    [Tooltip("Random travel duration multiplier range.")]
    [SerializeField] private Vector2 conversionVfxDurationMultiplier = new Vector2(0.9f, 1.08f);
    [Header("Conversion VFX Color over Lifetime")]
    [Tooltip("Applied to every Particle System Color over Lifetime module in the conversion VFX. Time 0 is particle birth; time 1 is particle death. Start Color is set to white to avoid multiplying the gradient by a second tint.")]
    [SerializeField] private Gradient conversionColorOverLifetime = CreateDefaultConversionLifetimeGradient();

    private static Gradient CreateDefaultConversionLifetimeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(0x9F, 0x2B, 0x2B, 0xFF), 0f),
                new GradientColorKey(new Color32(0x3F, 0x6F, 0xBB, 0xFF), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }

    // One deterministic gradient shared by the moving VFX and the legacy fallback.
    // The transition spans the entire flight: 0% = red, 100% = blue.
    private static readonly Color ConversionStartColor = new Color32(0x9F, 0x2B, 0x2B, 0xFF);
    private static readonly Color ConversionEndColor = new Color32(0x3F, 0x6F, 0xBB, 0xFF);

    [Header("Repeated Conversion Transfers")]
    [Tooltip("Amount at or below this threshold uses the small transfer count. Exactly 100 uses 5 transfers by default.")]
    [SerializeField, Min(1)] private int smallConversionThreshold = 100;
    [Tooltip("Number of transfers when the converted amount is at or below the threshold.")]
    [SerializeField, Min(1)] private int smallConversionTransferCount = 5;
    [Tooltip("Number of transfers when the converted amount is above the threshold.")]
    [SerializeField, Min(1)] private int largeConversionTransferCount = 8;
    [Tooltip("Seconds between launches. Smaller values create a rapid, overlapping stream of VFX.")]
    [SerializeField, Min(0.005f)] private float acquisitionLaunchInterval = 0.045f;

    [Header("Fixed Red Dustium Icon Pulse")]
    [Tooltip("Optional red glow Image over the source icon. Pulses with each orb launch.")]
    [SerializeField] private Image redLaunchGlow;
    [SerializeField, Min(1f)] private float redLaunchGlowScale = 1.25f;
    [Tooltip("Scale multiplier reached once for each conversion VFX launch.")]
    [SerializeField, Min(1f)] private float fixedRedIconPulseScale = 1.1f;
    [Tooltip("Minimum seconds for one complete scale-up and scale-down per VFX. The launch interval is extended if needed to complete a pulse.")]
    [SerializeField, Min(0.01f)] private float fixedRedIconPulsePeriod = 0.12f;

    [Header("Arrival Feedback")]
    [Tooltip("HUD scale multiplier for each arriving orb. Each arrival receives its own pulse.")]
    [SerializeField, Min(1f)] private float perArrivalHudPulseScale = 1.05f;
    [SerializeField, Min(0.01f)] private float perArrivalHudPulseDuration = 0.12f;
    [Header("Final Completion Feedback")]
    [Tooltip("Optional ring Image already positioned over the blue Dustium HUD. Assign a ring sprite in the Inspector. Shown once after all transfers arrive.")]
    [SerializeField] private Image finalCompletionRing;
    [SerializeField, Min(1f)] private float finalCompletionRingScale = 1.6f;
    [SerializeField, Min(0.01f)] private float finalCompletionRingDuration = 0.3f;

    [Header("Presentation Timing")]
    [SerializeField, Min(0f)] private float introDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float transferDuration = 1.1f;
    [SerializeField, Min(0f)] private float transferCurveHeight = 180f;
    [SerializeField, Min(1f)] private float arrivalPulseScale = 1.25f;
    [SerializeField, Min(0f)] private float arrivalPulseDuration = 0.18f;

    private readonly List<GameObject> activeConversionVfx = new List<GameObject>();
    private Coroutine presentationCoroutine;
    private Coroutine wagonFadeCoroutine;
    private MaterialPropertyBlock wagonPropertyBlock;
    private int wagonFadePropertyId;

    private bool isOpening;
    private bool ownsHudPresentationState;
    private bool hudWasEnabled;
    private Vector3 hudOriginalScale = Vector3.one;
    private int conversionSession;
    private Vector3 fixedRedIconBaseScale = Vector3.one;
    private int queuedArrivalPulses;
    private Coroutine arrivalPulseCoroutine;
    private Vector3 finalRingBaseScale = Vector3.one;
    private Color finalRingBaseColor = Color.white;
    private Vector3 redGlowBaseScale = Vector3.one;
    private Color redGlowBaseColor = Color.white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterPendingResultAutoOpen()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryOpenPendingResultOnLoadedScene();
    }

    private void Start()
    {
        OpenIfPending();
    }

    private GameObject GetPresentationRoot()
    {
        return presentationRoot != null ? presentationRoot :
            (presentationCanvasGroup != null ? presentationCanvasGroup.gameObject : null);
    }

    private void SetPresentationVisible(bool visible)
    {
        GameObject root = GetPresentationRoot();
        // Never turn Wagon (the script host) off when hiding the presentation.
        if (root != null && root != gameObject)
            root.SetActive(visible);
    }

    private void SetWagonRedDustiumVisible(bool visible)
    {
        if (wagonRedDustium == null)
            return;
        if (visible)
        {
            wagonRedDustium.SetActive(true);
            SetWagonShaderFade(1f);
        }
        else
        {
            SetWagonShaderFade(0f);
            wagonRedDustium.SetActive(false);
        }
    }

    private void SetWagonShaderFade(float value)
    {
        if (wagonRedDustiumRenderer == null && wagonRedDustium != null)
            wagonRedDustiumRenderer = wagonRedDustium.GetComponentInChildren<Renderer>(true);
        if (wagonRedDustiumRenderer == null)
            return;
        if (wagonFadePropertyId == 0)
            wagonFadePropertyId = Shader.PropertyToID(
                string.IsNullOrEmpty(wagonFadeProperty) ? "_Fade" : wagonFadeProperty);
        if (wagonPropertyBlock == null)
            wagonPropertyBlock = new MaterialPropertyBlock();
        wagonRedDustiumRenderer.GetPropertyBlock(wagonPropertyBlock);
        wagonPropertyBlock.SetFloat(wagonFadePropertyId, Mathf.Clamp01(value));
        wagonRedDustiumRenderer.SetPropertyBlock(wagonPropertyBlock);
    }

    private void BeginWagonRedDustiumFade()
    {
        if (wagonRedDustium == null)
            return;
        if (wagonFadeCoroutine != null)
            StopCoroutine(wagonFadeCoroutine);
        SetWagonRedDustiumVisible(true);
        wagonFadeCoroutine = StartCoroutine(FadeWagonRedDustium());
    }

    private IEnumerator FadeWagonRedDustium()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, wagonRedDustiumFadeDuration);
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            SetWagonShaderFade(1f - elapsed / duration);
            yield return null;
        }
        SetWagonRedDustiumVisible(false);
        wagonFadeCoroutine = null;
    }

    public static bool TryOpenPendingResultOnLoadedScene()
    {
        ResearchResultPanelUI[] panels = Resources.FindObjectsOfTypeAll<ResearchResultPanelUI>();
        for (int i = 0; i < panels.Length; i++)
        {
            ResearchResultPanelUI panel = panels[i];
            if (panel == null || panel.gameObject == null ||
                !panel.gameObject.scene.IsValid() ||
                panel.gameObject.scene.name != LobbySceneName)
            {
                continue;
            }

            if (panel.OpenIfPending())
                return true;
        }

        return false;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.IsValid() && scene.name == LobbySceneName)
            TryOpenPendingResultOnLoadedScene();
    }

    private bool OpenIfPending()
    {
        if (isOpening || presentationCoroutine != null)
            return true;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!PendingResearchSettlementService.HasPending(lobby))
        {
            SetWagonConversionVfxActive(false);
            SetWagonRedDustiumVisible(false);
            SetPresentationVisible(false);
            return false;
        }

        if (!CanLocalPlayerMutateHostOnlyState())
        {
            SetPresentationVisible(false);
            return false;
        }

        isOpening = true;
        PendingResearchResultData pending = lobby.PendingResearchResult;
        if (pending.IsApplied)
        {
            ClearPendingAndSave(lobby);
            SetPresentationVisible(false);
            isOpening = false;
            return false;
        }

        PendingResearchSettlementResult settlement =
            PendingResearchSettlementService.ApplyOnceWithResult(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);
        SaveSystem.Instance?.SaveCurrentProgress();
        PublishHostSnapshotAfterLocalMutation();

        if (!settlement.Applied || settlement.ConvertedRedDustium <= 0 ||
            !HasPresentationReferences())
        {
            ClearPendingAndSave(lobby);
            LobbyBlueDustiumHudUI.RefreshAll();
            SetPresentationVisible(false);
            isOpening = false;
            return false;
        }

        SetPresentationVisible(true);
        PrepareHud(settlement.BlueBefore);
        PreparePresentation(settlement.ConvertedRedDustium);
        presentationCoroutine = StartCoroutine(PlayConversionPresentation(lobby, settlement));
        isOpening = false;
        return true;
    }

    // Editor / Development Build only. Replays the real conversion presentation
    // without creating a pending exploration result or changing saved currency.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool TryPlayDebugConversion(int amount = 500)
    {
        if (!Application.isPlaying ||
            SceneManager.GetActiveScene().name != LobbySceneName)
            return false;

        ResearchResultPanelUI[] panels = Resources.FindObjectsOfTypeAll<ResearchResultPanelUI>();
        for (int i = 0; i < panels.Length; i++)
        {
            ResearchResultPanelUI panel = panels[i];
            if (panel == null || !panel.gameObject.scene.IsValid() ||
                panel.gameObject.scene.name != LobbySceneName ||
                panel.presentationCoroutine != null || panel.isOpening ||
                !panel.HasPresentationReferences())
                continue;

            LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
            if (lobby == null || PendingResearchSettlementService.HasPending(lobby))
                return false;

            return panel.PlayDebugConversion(lobby, amount);
        }
        return false;
    }

    private bool PlayDebugConversion(LobbyRuntimeData lobby, int amount)
    {
        if (amount <= 0 || presentationCoroutine != null || isOpening)
            return false;

        isOpening = true;
        SetPresentationVisible(true);
        int blueBefore = Mathf.Max(0, lobby.BlueDustium);
        PendingResearchSettlementResult preview = new PendingResearchSettlementResult(
            true, blueBefore, blueBefore + amount, amount);
        PrepareHud(blueBefore);
        PreparePresentation(amount);
        presentationCoroutine = StartCoroutine(
            PlayConversionPresentation(lobby, preview, true));
        isOpening = false;
        return true;
    }
#endif



    private bool HasPresentationReferences()
    {
        return GetPresentationRoot() != null && GetPresentationRoot() != gameObject &&
               presentationCanvasGroup != null && redDustiumValueText != null &&
               fixedRedDustiumIcon != null && blueDustiumHud != null &&
               (conversionVfxPrefab != null ||
                (transferBlur != null && transferBlurGraphic != null));
    }

    private void PrepareHud(int blueBefore)
    {
        hudWasEnabled = blueDustiumHud.enabled;
        hudOriginalScale = blueDustiumHud.transform.localScale;
        ownsHudPresentationState = true;
        blueDustiumHud.enabled = false;
        blueDustiumHud.SetValueImmediate(blueBefore);
    }

    private void PreparePresentation(int convertedRedDustium)
    {
        presentationCanvasGroup.alpha = 1f;
        presentationCanvasGroup.interactable = false;
        presentationCanvasGroup.blocksRaycasts = false;
        redDustiumValueText.gameObject.SetActive(true);
        redDustiumValueText.text = convertedRedDustium.ToString();
        redDustiumValueText.color = ConversionStartColor;
        fixedRedDustiumIcon.gameObject.SetActive(true);
        // Respect the RectTransform position authored in the Inspector.
        // Transfer effects originate from the icon's actual screen-space center.
        fixedRedIconBaseScale = fixedRedDustiumIcon.localScale;
        ResetFixedRedIconScale();
        if (redLaunchGlow != null)
        {
            redGlowBaseScale = redLaunchGlow.rectTransform.localScale;
            redGlowBaseColor = redLaunchGlow.color;
            redLaunchGlow.gameObject.SetActive(false);
        }
        queuedArrivalPulses = 0;
        if (arrivalPulseCoroutine != null)
        {
            StopCoroutine(arrivalPulseCoroutine);
            arrivalPulseCoroutine = null;
        }
        if (finalCompletionRing != null)
        {
            finalRingBaseScale = finalCompletionRing.rectTransform.localScale;
            finalRingBaseColor = finalCompletionRing.color;
            finalCompletionRing.gameObject.SetActive(false);
        }
        if (transferBlur != null)
        {
            bool useLegacyBlur = conversionVfxPrefab == null &&
                                 transferBlurGraphic != null;
            transferBlur.gameObject.SetActive(useLegacyBlur);
            if (useLegacyBlur)
            {
                transferBlurGraphic.color = ConversionStartColor;
                transferBlur.position = fixedRedDustiumIcon.position;
                transferBlur.localScale = Vector3.one;
            }
        }
    }

    private IEnumerator PlayConversionPresentation(
        LobbyRuntimeData lobby,
        PendingResearchSettlementResult settlement,
        bool debugPreview = false)
    {
        if (introDuration > 0f)
            yield return new WaitForSecondsRealtime(introDuration);

        BeginWagonRedDustiumFade();
        SetWagonConversionVfxActive(true);

        Vector3 startPosition = fixedRedDustiumIcon.position;
        Vector3 endPosition = blueDustiumHud.transform.position;
        bool useConversionVfx = conversionVfxPrefab != null;
        if (useConversionVfx)
        {
            // Arrival-driven updates keep the balance synchronized with the actual VFX,
            // including the last transfer (which can be smaller than dustiumPerTransfer).
            yield return PlayRepeatedConversionTransfers(settlement);
        }
        else
        {
            yield return PlayLegacyConversionTransfer(settlement, startPosition, endPosition);
        }

        // All conversion transfers have reached their destination (or timed out).
        // Fade the Wagon scene VFX out after the last conversion transfer arrives.
        if (wagonConversionVfx != null && wagonConversionVfx.activeInHierarchy)
            wagonConversionVfxFadeCoroutine = StartCoroutine(FadeOutWagonConversionVfx());

        if (wagonFadeCoroutine != null)
        {
            StopCoroutine(wagonFadeCoroutine);
            wagonFadeCoroutine = null;
        }
        SetWagonRedDustiumVisible(false);
        ResetFixedRedIconScale();
        fixedRedDustiumIcon.gameObject.SetActive(false);
        redDustiumValueText.gameObject.SetActive(false);
        blueDustiumHud.SetValueImmediate(settlement.BlueAfter);
        if (!useConversionVfx && transferBlur != null && transferBlurGraphic != null)
        {
            transferBlur.position = endPosition;
            transferBlurGraphic.color = ConversionEndColor;
        }
        if (arrivalPulseDuration > 0f)
            yield return PlayArrivalPulse();
        if (useConversionVfx)
            yield return PlayFinalCompletionRing();

        presentationCoroutine = null;
        RestoreHud();
        if (!debugPreview)
            ClearPendingAndSave(lobby);
        presentationCanvasGroup.alpha = 0f;
        if (transferBlur != null)
            transferBlur.gameObject.SetActive(false);
        SetPresentationVisible(false);
    }

    private IEnumerator PlayRepeatedConversionTransfers(PendingResearchSettlementResult settlement)
    {
        int total = Mathf.Max(0, settlement.ConvertedRedDustium);
        float interval = Mathf.Max(0.005f, acquisitionLaunchInterval);
        int desiredTransfers = total <= Mathf.Max(1, smallConversionThreshold)
            ? Mathf.Max(1, smallConversionTransferCount)
            : Mathf.Max(1, largeConversionTransferCount);
        // Every launched VFX must carry at least one unit, even for tiny conversions.
        int numberOfTransfers = Mathf.Min(total, desiredTransfers);
        int launchedAmount = 0;
        int arrivedAmount = 0;
        int arrivedCount = 0;
        int thisSession = ++conversionSession;

        RectTransform targetRect = blueDustiumHud.transform as RectTransform;
        Camera sourceCamera = ResolveConversionUiCamera(fixedRedDustiumIcon);
        Camera targetCamera = ResolveConversionUiCamera(targetRect);
        Vector2 startScreen = GetConversionScreenCenter(fixedRedDustiumIcon, sourceCamera);
        Vector2 endScreen = GetConversionScreenCenter(targetRect, targetCamera);

        for (int i = 0; i < numberOfTransfers; i++)
        {
            // Evenly distribute the exact converted total over the fixed number of VFX.
            // E.g. 500 -> 8 transfers: 63,63,63,63,62,62,62,62.
            int transferAmount = total / numberOfTransfers +
                (i < total % numberOfTransfers ? 1 : 0);
            launchedAmount += transferAmount;
            // Spawn the selected VFX directly. Do not instantiate or attach
            // The acquisition orb controller; this animation is fully self-contained.
            int thisTransferAmount = transferAmount;
            StartCoroutine(PlayDirectConversionVfx(startScreen, endScreen, () =>
            {
                if (thisSession != conversionSession || !isActiveAndEnabled)
                    return;
                arrivedAmount += thisTransferAmount;
                arrivedCount++;
                blueDustiumHud.SetValueImmediate(settlement.BlueBefore + arrivedAmount);
                QueueArrivalPulse();
            }));
            redDustiumValueText.text = (total - launchedAmount).ToString();
            // One and only one complete icon pulse for each VFX launch.
            // Waiting for the pulse prevents rapid launches from merging into a
            // single visual pulse. There are exactly numberOfTransfers cycles.
            float pulseDuration = Mathf.Max(interval, fixedRedIconPulsePeriod, 0.01f);
            yield return PulseFixedRedIconOnce(pulseDuration);
            if (launchedAmount >= total)
            {
                fixedRedDustiumIcon.gameObject.SetActive(false);
                redDustiumValueText.gameObject.SetActive(false);
            }
        }

        // The effect can last beyond the last launch. Wait for the final arrival
        // before ending the conversion panel and its HUD presentation state.
        // A bounded fallback prevents a broken or deleted VFX from hanging the panel.
        float waitElapsed = 0f;
        const float arrivalTimeout = 10f;
        while (arrivedCount < numberOfTransfers && waitElapsed < arrivalTimeout)
        {
            waitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        blueDustiumHud.SetValueImmediate(settlement.BlueAfter);
        // Let every arrival finish its feedback before the final completion cue.
        float pulseWaitElapsed = 0f;
        while ((queuedArrivalPulses > 0 || arrivalPulseCoroutine != null) &&
               pulseWaitElapsed < 5f)
        {
            pulseWaitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        blueDustiumHud.transform.localScale = hudOriginalScale;
    }

    // Only conversion uses this direct prefab path; the shared acquisition orb
    // component is neither required nor created.
    private static Camera ResolveConversionUiCamera(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);
    }

    private static Vector2 GetConversionScreenCenter(RectTransform rect, Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * 0.5f);
    }

    private IEnumerator PlayDirectConversionVfx(Vector2 startScreen, Vector2 endScreen,
        System.Action reachedDestination)
    {
        if (conversionVfxPrefab == null)
            yield break;

        // Screen-space overlay host keeps the source/destination aligned with HUD.
        GameObject host = new GameObject("DustiumConversionVFX", typeof(RectTransform), typeof(Canvas));
        activeConversionVfx.Add(host);
        Canvas canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20000;
        RectTransform root = host.GetComponent<RectTransform>();
        root.localScale = Vector3.one;

        // Inactive staging prevents prefab playOnAwake and renderer caches from
        // showing an uncolored first frame.
        GameObject stage = new GameObject("VFXPosition", typeof(RectTransform), typeof(CanvasGroup));
        stage.SetActive(false);
        RectTransform mover = stage.GetComponent<RectTransform>();
        mover.SetParent(root, false);
        mover.anchorMin = mover.anchorMax = new Vector2(0.5f, 0.5f);
        mover.pivot = new Vector2(0.5f, 0.5f);
        mover.sizeDelta = Vector2.zero;
        CanvasGroup visibility = stage.GetComponent<CanvasGroup>();
        visibility.alpha = 0f;
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        MoveConversionToScreen(mover, root, startScreen);

        GameObject visual = Instantiate(conversionVfxPrefab, mover, false);
        visual.name = conversionVfxPrefab.name + "_Conversion";
        visual.transform.localPosition = Vector3.zero;
        ParticleSystem[] particles = visual.GetComponentsInChildren<ParticleSystem>(true);
        TrailRenderer[] trails = visual.GetComponentsInChildren<TrailRenderer>(true);
        SpriteRenderer[] sprites = visual.GetComponentsInChildren<SpriteRenderer>(true);
        Graphic[] graphics = visual.GetComponentsInChildren<Graphic>(true);
        // Color over Lifetime is responsible for the VFX color.
        // Do not change particle Start Color every frame.
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = Color.white;
        for (int i = 0; i < graphics.Length; i++)
            if (graphics[i] != null) graphics[i].color = Color.white;

        // Neutralize baked material tints on this instance only. Never modify
        // the source prefab or shared acquisition-effect materials.
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", Color.white);
            }
        }
        foreach (TrailRenderer trail in trails)
        {
            if (trail == null) continue;
            trail.Clear();
            // Trails inherit their particle tint (where supported), rather than
            // adding a second red/blue color gradient.
            trail.colorGradient = CreateWhiteGradient();
        }
        foreach (ParticleSystem particle in particles)
        {
            if (particle == null) continue;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
            var lifeColor = particle.colorOverLifetime;
            lifeColor.enabled = true;
            lifeColor.color = new ParticleSystem.MinMaxGradient(conversionColorOverLifetime);
            var speedColor = particle.colorBySpeed;
            speedColor.enabled = false;
            var trailModule = particle.trails;
            if (trailModule.enabled)
            {
                trailModule.inheritParticleColor = true;
                trailModule.colorOverLifetime = new ParticleSystem.MinMaxGradient(Color.white);
                trailModule.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.white);
            }
            var main = particle.main;
            main.playOnAwake = false;
            main.prewarm = false;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        }
        stage.SetActive(true);
        foreach (ParticleSystem particle in particles)
        {
            if (particle != null && particle.gameObject.activeInHierarchy)
                particle.Play(true);
        }
        // Initial particle/UI mesh update happens before revealing the effect.
        yield return null;
        if (visibility != null) visibility.alpha = 1f;

        float durationMin = Mathf.Min(conversionVfxDurationMultiplier.x, conversionVfxDurationMultiplier.y);
        float durationMax = Mathf.Max(conversionVfxDurationMultiplier.x, conversionVfxDurationMultiplier.y);
        float duration = Mathf.Max(0.01f, conversionVfxDuration * Random.Range(durationMin, durationMax));
        float side = Random.value < 0.5f ? -1f : 1f;
        float deviation = Random.Range(Mathf.Max(0f, Mathf.Min(conversionVfxSideSpread.x, conversionVfxSideSpread.y)),
            Mathf.Max(0f, Mathf.Max(conversionVfxSideSpread.x, conversionVfxSideSpread.y)));
        float vertical = Random.Range(Mathf.Min(conversionVfxVerticalSpread.x, conversionVfxVerticalSpread.y),
            Mathf.Max(conversionVfxVerticalSpread.x, conversionVfxVerticalSpread.y));
        Vector2 control = (startScreen + endScreen) * 0.5f + new Vector2(side * deviation, vertical) * 2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            Vector2 point = (1f - t) * (1f - t) * startScreen +
                2f * (1f - t) * t * control + t * t * endScreen;
            MoveConversionToScreen(mover, root, point);
            yield return null;
        }
        MoveConversionToScreen(mover, root, endScreen);
        reachedDestination?.Invoke();
        activeConversionVfx.Remove(host);
        Destroy(host);
    }

    private static void MoveConversionToScreen(RectTransform mover, RectTransform root, Vector2 screenPosition)
    {
        if (mover == null || root == null) return;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out local))
            mover.anchoredPosition = local;
    }

    private static Gradient CreateWhiteGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }

    private void QueueArrivalPulse()
    {
        if (blueDustiumHud == null)
            return;
        queuedArrivalPulses++;
        if (arrivalPulseCoroutine == null)
            arrivalPulseCoroutine = StartCoroutine(PlayQueuedArrivalPulses());
    }

    private IEnumerator PlayQueuedArrivalPulses()
    {
        while (queuedArrivalPulses > 0)
        {
            queuedArrivalPulses--;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, perArrivalHudPulseDuration);
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                float wave = Mathf.Sin(Mathf.PI * elapsed / duration);
                if (blueDustiumHud != null)
                    blueDustiumHud.transform.localScale = hudOriginalScale *
                        Mathf.Lerp(1f, Mathf.Max(1f, perArrivalHudPulseScale), wave);
                yield return null;
            }
        }
        if (blueDustiumHud != null)
            blueDustiumHud.transform.localScale = hudOriginalScale;
        arrivalPulseCoroutine = null;
    }

    private IEnumerator PlayFinalCompletionRing()
    {
        if (finalCompletionRing == null)
            yield break;

        RectTransform ringRect = finalCompletionRing.rectTransform;
        ringRect.localScale = finalRingBaseScale;
        Color color = finalRingBaseColor;
        color.a = 0f;
        finalCompletionRing.color = color;
        finalCompletionRing.gameObject.SetActive(true);
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, finalCompletionRingDuration);
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float t = Mathf.Clamp01(elapsed / duration);
            ringRect.localScale = finalRingBaseScale *
                Mathf.Lerp(1f, Mathf.Max(1f, finalCompletionRingScale), t);
            color.a = finalRingBaseColor.a * (1f - t);
            finalCompletionRing.color = color;
            yield return null;
        }
        finalCompletionRing.gameObject.SetActive(false);
        ringRect.localScale = finalRingBaseScale;
        finalCompletionRing.color = finalRingBaseColor;
    }

    private IEnumerator PlayLegacyConversionTransfer(
        PendingResearchSettlementResult settlement, Vector3 startPosition, Vector3 endPosition)
    {
        float duration = Mathf.Max(0.01f, transferDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position += Vector3.up * (4f * transferCurveHeight * progress * (1f - progress));
            if (transferBlur != null && transferBlurGraphic != null)
            {
                transferBlur.position = position;
                float colorProgress = progress;
                transferBlurGraphic.color = EvaluateTransferColor(
                    ConversionStartColor, ConversionEndColor, colorProgress);
            }
            int remainingRedDustium = CalculateRemainingRedDustium(
                settlement.ConvertedRedDustium, progress);
            redDustiumValueText.text = remainingRedDustium.ToString();
            bool showFixedRedDustium = ShouldShowFixedRedDustium(
                settlement.ConvertedRedDustium, progress);
            // The legacy one-shot transfer has one single pulse, synchronized
            // with that movement rather than an independent repeating timer.
            if (showFixedRedDustium)
                fixedRedDustiumIcon.localScale = fixedRedIconBaseScale *
                    (1f + (Mathf.Max(1f, fixedRedIconPulseScale) - 1f) *
                     Mathf.Sin(Mathf.PI * progress));
            else
                ResetFixedRedIconScale();
            fixedRedDustiumIcon.gameObject.SetActive(showFixedRedDustium);
            redDustiumValueText.gameObject.SetActive(showFixedRedDustium);
            blueDustiumHud.SetValueImmediate(CalculateDisplayedBlueDustium(
                settlement.BlueBefore, settlement.ConvertedRedDustium, progress));
            yield return null;
        }
    }

    private IEnumerator PulseFixedRedIconOnce(float duration)
    {
        if (fixedRedDustiumIcon == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        // The first frame starts at the unscaled, original size.
        ResetFixedRedIconScale();
        if (redLaunchGlow != null)
            redLaunchGlow.gameObject.SetActive(true);
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(Mathf.PI * progress);
            fixedRedDustiumIcon.localScale = fixedRedIconBaseScale *
                Mathf.Lerp(1f, Mathf.Max(1f, fixedRedIconPulseScale), pulse);
            if (redLaunchGlow != null)
            {
                redLaunchGlow.rectTransform.localScale = redGlowBaseScale *
                    Mathf.Lerp(1f, Mathf.Max(1f, redLaunchGlowScale), pulse);
                Color glowColor = redGlowBaseColor;
                glowColor.a *= pulse;
                redLaunchGlow.color = glowColor;
            }
            yield return null;
        }
        ResetFixedRedIconScale();
        if (redLaunchGlow != null)
        {
            redLaunchGlow.rectTransform.localScale = redGlowBaseScale;
            redLaunchGlow.color = redGlowBaseColor;
            redLaunchGlow.gameObject.SetActive(false);
        }
    }

    private void ResetFixedRedIconScale()
    {
        if (fixedRedDustiumIcon != null)
            fixedRedDustiumIcon.localScale = fixedRedIconBaseScale;
    }

    private IEnumerator PlayArrivalPulse()
    {
        float duration = Mathf.Max(0.01f, arrivalPulseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            blueDustiumHud.transform.localScale = Vector3.LerpUnclamped(
                hudOriginalScale,
                hudOriginalScale * Mathf.Max(1f, arrivalPulseScale),
                pulse);
            yield return null;
        }

        blueDustiumHud.transform.localScale = hudOriginalScale;
    }

    private void RestoreHud()
    {
        if (!ownsHudPresentationState || blueDustiumHud == null)
            return;

        blueDustiumHud.transform.localScale = hudOriginalScale;
        blueDustiumHud.enabled = hudWasEnabled;
        blueDustiumHud.Refresh();
        ownsHudPresentationState = false;
    }

    private static void ClearPendingAndSave(LobbyRuntimeData lobby)
    {
        PendingResearchSettlementService.Clear(lobby);
        DataManager.Instance?.LobbyRuntimeStore?.Set(lobby);
        SaveSystem.Instance?.SaveCurrentProgress();
        PublishHostSnapshotAfterLocalMutation();
    }

    private void OnDisable()
    {
        SetWagonConversionVfxActive(false);
        if (wagonFadeCoroutine != null)
        {
            StopCoroutine(wagonFadeCoroutine);
            wagonFadeCoroutine = null;
        }
        SetWagonRedDustiumVisible(false);
        ResetFixedRedIconScale();
        if (redLaunchGlow != null)
        {
            redLaunchGlow.rectTransform.localScale = redGlowBaseScale;
            redLaunchGlow.color = redGlowBaseColor;
            redLaunchGlow.gameObject.SetActive(false);
        }
        queuedArrivalPulses = 0;
        if (arrivalPulseCoroutine != null)
        {
            StopCoroutine(arrivalPulseCoroutine);
            arrivalPulseCoroutine = null;
        }
        if (finalCompletionRing != null)
        {
            finalCompletionRing.rectTransform.localScale = finalRingBaseScale;
            finalCompletionRing.color = finalRingBaseColor;
            finalCompletionRing.gameObject.SetActive(false);
        }
        conversionSession++;
        foreach (GameObject vfx in activeConversionVfx)
            if (vfx != null) Destroy(vfx);
        activeConversionVfx.Clear();
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }

        RestoreHud();
        if (presentationCanvasGroup != null)
            presentationCanvasGroup.alpha = 0f;
        if (fixedRedDustiumIcon != null)
            fixedRedDustiumIcon.gameObject.SetActive(false);
        if (transferBlur != null)
            transferBlur.gameObject.SetActive(false);
        if (redDustiumValueText != null)
            redDustiumValueText.gameObject.SetActive(false);
    }

    public static int CalculateRemainingRedDustium(int amount, float progress)
    {
        return Mathf.RoundToInt(Mathf.Lerp(
            Mathf.Max(0, amount), 0f, Mathf.Clamp01(progress)));
    }

    public static int CalculateDisplayedBlueDustium(
        int blueBefore,
        int convertedRedDustium,
        float progress)
    {
        return Mathf.RoundToInt(Mathf.Lerp(
            Mathf.Max(0, blueBefore),
            Mathf.Max(0, blueBefore) + Mathf.Max(0, convertedRedDustium),
            Mathf.Clamp01(progress)));
    }

    public static Color EvaluateTransferColor(Color red, Color blue, float progress)
    {
        return Color.Lerp(red, blue, Mathf.Clamp01(progress));
    }

    public static bool ShouldShowFixedRedDustium(int amount, float progress)
    {
        return CalculateRemainingRedDustium(amount, progress) > 0;
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null || synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }
}
