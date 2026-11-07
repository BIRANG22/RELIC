using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ResearchResultPanelUI : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";

    [Header("Dustium Conversion Presentation")]
    [SerializeField] private CanvasGroup presentationCanvasGroup;
    [SerializeField] private TMP_Text redDustiumValueText;
    [SerializeField] private RectTransform fixedRedDustiumIcon;
    [SerializeField] private RectTransform transferBlur;
    [SerializeField] private Graphic transferBlurGraphic;
    [SerializeField] private LobbyBlueDustiumHudUI blueDustiumHud;

    [Header("Shared Lobby Acquisition Transfer VFX")]
    [Tooltip("Use the same ScreenSpaceTransferOrbEffect prefab as the lobby artifact/compound acquisition effect. If unassigned, uses the original transfer blur.")]
    [SerializeField] private ScreenSpaceTransferOrbEffect acquisitionTransferEffectPrefab;
    [Header("Conversion Orb Color Transition")]
    [Tooltip("Color at launch from the Red Dustium icon.")]
    [SerializeField] private Color conversionOrbStartColor = new Color32(0x9F, 0x2B, 0x2B, 0xFF);
    [Tooltip("Color on arrival at the Blue Dustium HUD.")]
    [SerializeField] private Color conversionOrbEndColor = new Color32(0x3F, 0x6F, 0xBB, 0xFF);
    [Tooltip("Normalized progress at which the orb begins changing color.")]
    [SerializeField, Range(0f, 1f)] private float conversionColorChangeStart = 0.35f;
    [Tooltip("Normalized progress at which the orb has fully changed to blue.")]
    [SerializeField, Range(0f, 1f)] private float conversionColorChangeEnd = 0.65f;

    [Header("Repeated Acquisition Transfers")]
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
    [SerializeField, Range(0f, 1f)] private float colorTransitionStart = 0.3f;
    [SerializeField, Min(1f)] private float arrivalPulseScale = 1.25f;
    [SerializeField, Min(0f)] private float arrivalPulseDuration = 0.18f;
    [SerializeField] private Color redDustiumColor = new Color(1f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color blueDustiumColor = new Color(0.27f, 0.49f, 0.9f, 1f);

    private Coroutine presentationCoroutine;
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
            gameObject.SetActive(false);
            return false;
        }

        if (!CanLocalPlayerMutateHostOnlyState())
        {
            gameObject.SetActive(false);
            return false;
        }

        isOpening = true;
        PendingResearchResultData pending = lobby.PendingResearchResult;
        if (pending.IsApplied)
        {
            ClearPendingAndSave(lobby);
            gameObject.SetActive(false);
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
            gameObject.SetActive(false);
            isOpening = false;
            return false;
        }

        gameObject.SetActive(true);
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
        gameObject.SetActive(true);
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
        return presentationCanvasGroup != null && redDustiumValueText != null &&
               fixedRedDustiumIcon != null && blueDustiumHud != null &&
               ((acquisitionTransferEffectPrefab != null &&
                 acquisitionTransferEffectPrefab.HasMovingVfxPrefab) ||
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
        redDustiumValueText.color = redDustiumColor;
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
            bool useLegacyBlur = (acquisitionTransferEffectPrefab == null ||
                                  !acquisitionTransferEffectPrefab.HasMovingVfxPrefab) &&
                                 transferBlurGraphic != null;
            transferBlur.gameObject.SetActive(useLegacyBlur);
            if (useLegacyBlur)
            {
                transferBlurGraphic.color = redDustiumColor;
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

        Vector3 startPosition = fixedRedDustiumIcon.position;
        Vector3 endPosition = blueDustiumHud.transform.position;
        bool useSharedTransfer = acquisitionTransferEffectPrefab != null &&
                                 acquisitionTransferEffectPrefab.HasMovingVfxPrefab;
        if (useSharedTransfer)
        {
            // Arrival-driven updates keep the balance synchronized with the actual VFX,
            // including the last transfer (which can be smaller than dustiumPerTransfer).
            yield return PlayRepeatedAcquisitionTransfers(settlement);
        }
        else
        {
            yield return PlayLegacyConversionTransfer(settlement, startPosition, endPosition);
        }

        ResetFixedRedIconScale();
        fixedRedDustiumIcon.gameObject.SetActive(false);
        redDustiumValueText.gameObject.SetActive(false);
        blueDustiumHud.SetValueImmediate(settlement.BlueAfter);
        if (!useSharedTransfer && transferBlur != null && transferBlurGraphic != null)
        {
            transferBlur.position = endPosition;
            transferBlurGraphic.color = blueDustiumColor;
        }
        if (arrivalPulseDuration > 0f)
            yield return PlayArrivalPulse();
        if (useSharedTransfer)
            yield return PlayFinalCompletionRing();

        presentationCoroutine = null;
        RestoreHud();
        if (!debugPreview)
            ClearPendingAndSave(lobby);
        presentationCanvasGroup.alpha = 0f;
        if (transferBlur != null)
            transferBlur.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private IEnumerator PlayRepeatedAcquisitionTransfers(PendingResearchSettlementResult settlement)
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
        Camera sourceCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(fixedRedDustiumIcon, Camera.main);
        Camera targetCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(targetRect, Camera.main);
        Vector2 startScreen = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(fixedRedDustiumIcon, sourceCamera);
        Vector2 endScreen = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(targetRect, targetCamera);

        for (int i = 0; i < numberOfTransfers; i++)
        {
            // Evenly distribute the exact converted total over the fixed number of VFX.
            // E.g. 500 -> 8 transfers: 63,63,63,63,62,62,62,62.
            int transferAmount = total / numberOfTransfers +
                (i < total % numberOfTransfers ? 1 : 0);
            launchedAmount += transferAmount;
            ScreenSpaceTransferOrbEffect effect = Instantiate(acquisitionTransferEffectPrefab);
            effect.ReachedDestination += () =>
            {
                // An interrupted/closed presentation must not affect a future session.
                if (thisSession != conversionSession || !isActiveAndEnabled)
                    return;
                arrivedAmount += transferAmount;
                arrivedCount++;
                blueDustiumHud.SetValueImmediate(settlement.BlueBefore + arrivedAmount);
                QueueArrivalPulse();
            };
            effect.PlayDetachedConversionGradient(startScreen, endScreen,
                conversionOrbStartColor, conversionOrbEndColor,
                conversionColorChangeStart, conversionColorChangeEnd);
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
                float colorProgress = Mathf.InverseLerp(
                    Mathf.Clamp01(colorTransitionStart), 1f, progress);
                transferBlurGraphic.color = EvaluateTransferColor(
                    redDustiumColor, blueDustiumColor, colorProgress);
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
