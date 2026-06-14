using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BattleDiagonalSceneTransition : MonoBehaviour
{
    public enum TransitionDirection
    {
        MapToRoom,
        RoomToMap
    }

    [Header("Root")]
    [SerializeField] private GameObject transitionRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Canvas Sorting")]
    [SerializeField] private bool ensureRootCanvas = false;
    [SerializeField] private int canvasSortingOrder = 6000;

    [Header("Image")]
    [SerializeField] private RectTransform transitionImage;

    [Header("Map To Room Position")]
    [SerializeField] private Vector3 mapToRoomStartPosition = new Vector3(1600f, 900f, 0f);
    [SerializeField] private Vector3 mapToRoomCoverPosition = Vector3.zero;
    [SerializeField] private Vector3 mapToRoomEndPosition = new Vector3(-1600f, -900f, 0f);

    [Header("Room To Map Position")]
    [SerializeField] private Vector3 roomToMapStartPosition = new Vector3(-1600f, -900f, 0f);
    [SerializeField] private Vector3 roomToMapCoverPosition = Vector3.zero;
    [SerializeField] private Vector3 roomToMapEndPosition = new Vector3(1600f, 900f, 0f);

    [Header("Timing")]
    [SerializeField] private float coverDuration = 0.35f;
    [SerializeField] private float uncoverDuration = 0.35f;
    [SerializeField] private float coveredHoldDuration = 0.05f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve coverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve uncoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sound")]
    [SerializeField] private bool playTransitionSound = true;
    [SerializeField] private SfxType transitionSfx = SfxType.SceneTransition;
    [SerializeField] private float transitionSfxVolumeMultiplier = 1f;

    private bool isInitialized;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        InitializeIfNeeded();
        HideImmediate();
    }

    public async Task PlayMapToRoomAsync(Action onCovered)
    {
        await PlayAsync(TransitionDirection.MapToRoom, onCovered);
    }

    public async Task PlayRoomToMapAsync(Action onCovered)
    {
        await PlayAsync(TransitionDirection.RoomToMap, onCovered);
    }


    public async Task PlayRoomToMapAlreadyCoveredAsync(Action onCovered)
    {
        await PlayAlreadyCoveredAsync(TransitionDirection.RoomToMap, onCovered);
    }

    public async Task PlayAlreadyCoveredAsync(TransitionDirection direction, Action onCovered)
    {
        InitializeIfNeeded();

        if (isPlaying)
            return;

        isPlaying = true;

        Vector3 startPosition;
        Vector3 coverPosition;
        Vector3 endPosition;
        GetPositions(direction, out startPosition, out coverPosition, out endPosition);

        Show();
        SetImagePosition(coverPosition);
        PlayTransitionSound();

        onCovered?.Invoke();

        if (coveredHoldDuration > 0f)
            await WaitUnscaledAsync(coveredHoldDuration);

        await AnimatePositionAsync(coverPosition, endPosition, uncoverDuration, uncoverCurve);
        SetImagePosition(endPosition);

        HideImmediate();
        isPlaying = false;
    }

    public async Task PlayAsync(TransitionDirection direction, Action onCovered)
    {
        InitializeIfNeeded();

        if (isPlaying)
            return;

        isPlaying = true;

        Vector3 startPosition;
        Vector3 coverPosition;
        Vector3 endPosition;
        GetPositions(direction, out startPosition, out coverPosition, out endPosition);

        Show();
        SetImagePosition(startPosition);
        PlayTransitionSound();

        await AnimatePositionAsync(startPosition, coverPosition, coverDuration, coverCurve);
        SetImagePosition(coverPosition);

        onCovered?.Invoke();

        if (coveredHoldDuration > 0f)
            await WaitUnscaledAsync(coveredHoldDuration);

        await AnimatePositionAsync(coverPosition, endPosition, uncoverDuration, uncoverCurve);
        SetImagePosition(endPosition);

        HideImmediate();
        isPlaying = false;
    }

    public void HideImmediate()
    {
        InitializeIfNeeded();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (transitionRoot != null)
            transitionRoot.SetActive(false);
    }

    public void SetMapToRoomStartImmediate()
    {
        InitializeIfNeeded();
        SetImagePosition(mapToRoomStartPosition);
    }

    public void SetRoomToMapStartImmediate()
    {
        InitializeIfNeeded();
        SetImagePosition(roomToMapStartPosition);
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
            return;

        if (transitionRoot == null)
            transitionRoot = gameObject;

        if (canvasGroup == null)
            canvasGroup = transitionRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = transitionRoot.AddComponent<CanvasGroup>();

        if (transitionImage == null)
        {
            Image image = transitionRoot.GetComponentInChildren<Image>(true);
            if (image != null)
                transitionImage = image.rectTransform;
        }

        EnsureCanvasIfNeeded();
        isInitialized = true;
    }

    private void EnsureCanvasIfNeeded()
    {
        if (!ensureRootCanvas)
            return;

        Canvas canvas = transitionRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = transitionRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortingOrder;

        if (transitionRoot.GetComponent<GraphicRaycaster>() == null)
            transitionRoot.AddComponent<GraphicRaycaster>();
    }

    private void Show()
    {
        if (transitionRoot != null)
        {
            transitionRoot.SetActive(true);
            transitionRoot.transform.SetAsLastSibling();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        Canvas.ForceUpdateCanvases();
    }

    private void GetPositions(
        TransitionDirection direction,
        out Vector3 startPosition,
        out Vector3 coverPosition,
        out Vector3 endPosition)
    {
        if (direction == TransitionDirection.RoomToMap)
        {
            startPosition = roomToMapStartPosition;
            coverPosition = roomToMapCoverPosition;
            endPosition = roomToMapEndPosition;
            return;
        }

        startPosition = mapToRoomStartPosition;
        coverPosition = mapToRoomCoverPosition;
        endPosition = mapToRoomEndPosition;
    }

    private async Task AnimatePositionAsync(Vector3 startPosition, Vector3 endPosition, float duration, AnimationCurve curve)
    {
        if (transitionImage == null)
            return;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            transitionImage.localPosition = Vector3.LerpUnclamped(startPosition, endPosition, curveValue);
            await Task.Yield();
        }

        transitionImage.localPosition = endPosition;
    }

    private async Task WaitUnscaledAsync(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            await Task.Yield();
        }
    }

    private void SetImagePosition(Vector3 position)
    {
        if (transitionImage != null)
            transitionImage.localPosition = position;
    }

    private void PlayTransitionSound()
    {
        if (!playTransitionSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(transitionSfx, transitionSfxVolumeMultiplier);
    }
}
