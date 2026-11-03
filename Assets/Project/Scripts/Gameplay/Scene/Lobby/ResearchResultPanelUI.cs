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

    [Header("Presentation Timing")]
    [SerializeField, Min(0f)] private float introDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float transferDuration = 1.1f;
    [SerializeField, Min(0f)] private float transferCurveHeight = 180f;
    [SerializeField, Range(0f, 1f)] private float colorTransitionStart = 0.3f;
    [SerializeField, Min(1f)] private float arrivalPulseScale = 1.25f;
    [SerializeField, Min(0f)] private float arrivalPulseDuration = 0.18f;
    [SerializeField] private Vector2 transferStartOffset = new Vector2(-90f, 0f);
    [SerializeField] private Color redDustiumColor = new Color(1f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color blueDustiumColor = new Color(0.27f, 0.49f, 0.9f, 1f);

    private Coroutine presentationCoroutine;
    private bool isOpening;
    private bool ownsHudPresentationState;
    private bool hudWasEnabled;
    private Vector3 hudOriginalScale = Vector3.one;

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

    private bool HasPresentationReferences()
    {
        return presentationCanvasGroup != null && redDustiumValueText != null &&
               fixedRedDustiumIcon != null && transferBlur != null &&
               transferBlurGraphic != null &&
               blueDustiumHud != null;
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
        fixedRedDustiumIcon.position = redDustiumValueText.rectTransform.position +
                                        (Vector3)transferStartOffset;
        fixedRedDustiumIcon.localScale = Vector3.one;
        transferBlur.gameObject.SetActive(true);
        transferBlurGraphic.color = redDustiumColor;
        transferBlur.position = fixedRedDustiumIcon.position;
        transferBlur.localScale = Vector3.one;
    }

    private IEnumerator PlayConversionPresentation(
        LobbyRuntimeData lobby,
        PendingResearchSettlementResult settlement)
    {
        if (introDuration > 0f)
            yield return new WaitForSecondsRealtime(introDuration);

        Vector3 startPosition = fixedRedDustiumIcon.position;
        Vector3 endPosition = blueDustiumHud.transform.position;
        float duration = Mathf.Max(0.01f, transferDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position += Vector3.up * (4f * transferCurveHeight * progress * (1f - progress));
            transferBlur.position = position;
            float colorProgress = Mathf.InverseLerp(
                Mathf.Clamp01(colorTransitionStart), 1f, progress);
            transferBlurGraphic.color = EvaluateTransferColor(
                redDustiumColor, blueDustiumColor, colorProgress);
            int remainingRedDustium = CalculateRemainingRedDustium(
                settlement.ConvertedRedDustium, progress);
            redDustiumValueText.text = remainingRedDustium.ToString();
            bool showFixedRedDustium = ShouldShowFixedRedDustium(
                settlement.ConvertedRedDustium, progress);
            fixedRedDustiumIcon.gameObject.SetActive(showFixedRedDustium);
            redDustiumValueText.gameObject.SetActive(showFixedRedDustium);
            blueDustiumHud.SetValueImmediate(CalculateDisplayedBlueDustium(
                settlement.BlueBefore, settlement.ConvertedRedDustium, progress));
            yield return null;
        }

        fixedRedDustiumIcon.gameObject.SetActive(false);
        redDustiumValueText.gameObject.SetActive(false);
        blueDustiumHud.SetValueImmediate(settlement.BlueAfter);
        transferBlur.position = endPosition;
        transferBlurGraphic.color = blueDustiumColor;
        if (arrivalPulseDuration > 0f)
            yield return PlayArrivalPulse();

        presentationCoroutine = null;
        RestoreHud();
        ClearPendingAndSave(lobby);
        presentationCanvasGroup.alpha = 0f;
        transferBlur.gameObject.SetActive(false);
        gameObject.SetActive(false);
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
