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

    [Header("Image / Material")]
    [Tooltip("비워두면 자식 Image들 중 Progress/Direction 프로퍼티를 가진 Material의 Image를 자동으로 찾습니다.")]
    [SerializeField] private Image transitionImage;

    [Header("Material Properties")]
    [Tooltip("Shader에서 Display Name/Reference Name을 기준으로 progress 프로퍼티를 자동 탐색합니다.")]
    [SerializeField] private string progressDisplayName = "progress";
    [Tooltip("Shader에서 Display Name/Reference Name을 기준으로 direction 프로퍼티를 자동 탐색합니다.")]
    [SerializeField] private string directionDisplayName = "direction";

    [Header("Progress")]
    [SerializeField] private float startProgress = 0.5f;
    [SerializeField] private float coveredProgress = -2f;
    [SerializeField] private float endProgress = 0.5f;

    [Header("Direction")]
    [SerializeField] private float coverDirection = 1f;
    [SerializeField] private float uncoverDirection = 0f;

    [Header("Timing")]
    [SerializeField] private float coverDuration = 0.35f;
    [SerializeField] private float uncoverDuration = 0.35f;
    [SerializeField] private float coveredHoldDuration = 0.05f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve coverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve uncoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sound")]
    [SerializeField] private bool playTransitionSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string transitionSfx = AudioIds.Sfx.SceneTransition;
    [SerializeField] private float transitionSfxVolumeMultiplier = 1f;

    private bool isInitialized;
    private bool isPlaying;

    private Material transitionMaterial;
    private string resolvedProgressProperty;
    private string resolvedDirectionProperty;

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

        Show();
        PlayTransitionSound();

        SetDirection(coverDirection);
        SetProgress(coveredProgress);

        onCovered?.Invoke();

        SetDirection(uncoverDirection);

        if (coveredHoldDuration > 0f)
            await WaitUnscaledAsync(coveredHoldDuration);

        await AnimateProgressAsync(
            coveredProgress,
            endProgress,
            uncoverDuration,
            uncoverCurve);

        SetProgress(endProgress);

        HideImmediate();
        isPlaying = false;
    }

    public async Task PlayAsync(TransitionDirection direction, Action onCovered)
    {
        InitializeIfNeeded();

        if (isPlaying)
            return;

        isPlaying = true;

        Show();
        PlayTransitionSound();

        SetDirection(coverDirection);
        SetProgress(startProgress);

        await AnimateProgressAsync(
            startProgress,
            coveredProgress,
            coverDuration,
            coverCurve);

        SetProgress(coveredProgress);

        onCovered?.Invoke();

        SetDirection(uncoverDirection);

        if (coveredHoldDuration > 0f)
            await WaitUnscaledAsync(coveredHoldDuration);

        await AnimateProgressAsync(
            coveredProgress,
            endProgress,
            uncoverDuration,
            uncoverCurve);

        SetProgress(endProgress);

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
        SetDirection(coverDirection);
        SetProgress(startProgress);
    }

    public void SetRoomToMapStartImmediate()
    {
        InitializeIfNeeded();
        SetDirection(coverDirection);
        SetProgress(startProgress);
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
            transitionImage = FindTransitionImage();

        if (transitionImage == null)
        {
            Debug.LogWarning(
                $"[BattleDiagonalSceneTransition] 자식 Image들에서 '{progressDisplayName}' / '{directionDisplayName}' 프로퍼티를 가진 Material을 찾지 못했습니다.",
                this);
        }

        ResolveTransitionMaterial();
        EnsureCanvasIfNeeded();

        isInitialized = true;
    }

    private Image FindTransitionImage()
    {
        if (transitionRoot == null)
            return null;

        Image[] images = transitionRoot.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null || image.material == null)
                continue;

            if (TryResolveMaterialProperties(
                    image.material,
                    out _,
                    out _))
            {
                return image;
            }
        }

        return null;
    }

    private void ResolveTransitionMaterial()
    {
        transitionMaterial = null;
        resolvedProgressProperty = null;
        resolvedDirectionProperty = null;

        if (transitionImage == null)
            return;

        transitionMaterial = transitionImage.material;

        if (transitionMaterial == null)
        {
            Debug.LogWarning(
                "[BattleDiagonalSceneTransition] Transition Image에 Material이 없습니다.",
                this);
            return;
        }

        if (!TryResolveMaterialProperties(
                transitionMaterial,
                out resolvedProgressProperty,
                out resolvedDirectionProperty))
        {
            Debug.LogWarning(
                $"[BattleDiagonalSceneTransition] Shader '{transitionMaterial.shader?.name}'에서 " +
                $"'{progressDisplayName}' / '{directionDisplayName}' 프로퍼티를 찾지 못했습니다.",
                this);
            return;
        }

        Debug.Log(
            $"[BattleDiagonalSceneTransition] Material property resolved - " +
            $"Progress: '{resolvedProgressProperty}', Direction: '{resolvedDirectionProperty}'",
            this);
    }

    private bool TryResolveMaterialProperties(
        Material material,
        out string progressProperty,
        out string directionProperty)
    {
        progressProperty = ResolveShaderPropertyName(material, progressDisplayName);
        directionProperty = ResolveShaderPropertyName(material, directionDisplayName);

        return !string.IsNullOrWhiteSpace(progressProperty) &&
               !string.IsNullOrWhiteSpace(directionProperty);
    }

    private static string ResolveShaderPropertyName(Material material, string requestedName)
    {
        if (material == null || material.shader == null || string.IsNullOrWhiteSpace(requestedName))
            return null;

        Shader shader = material.shader;
        string normalizedRequested = NormalizePropertyName(requestedName);
        int propertyCount = shader.GetPropertyCount();

        // 1. 실제 Reference Name이 정확히 일치하는 경우를 우선합니다.
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = shader.GetPropertyName(i);

            if (string.Equals(
                    propertyName,
                    requestedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return propertyName;
            }
        }

        // 2. 앞의 '_' 등을 무시한 Reference Name 비교.
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = shader.GetPropertyName(i);

            if (string.Equals(
                    NormalizePropertyName(propertyName),
                    normalizedRequested,
                    StringComparison.OrdinalIgnoreCase))
            {
                return propertyName;
            }
        }

        // 3. Shader Graph Inspector에 보이는 Display Name(Description)으로 비교.
        for (int i = 0; i < propertyCount; i++)
        {
            string description = shader.GetPropertyDescription(i);

            if (!string.IsNullOrWhiteSpace(description) &&
                string.Equals(
                    NormalizePropertyName(description),
                    normalizedRequested,
                    StringComparison.OrdinalIgnoreCase))
            {
                return shader.GetPropertyName(i);
            }
        }

        return null;
    }

    private static string NormalizePropertyName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimStart('_').Replace(" ", string.Empty);
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

    private async Task AnimateProgressAsync(
        float from,
        float to,
        float duration,
        AnimationCurve curve)
    {
        if (transitionMaterial == null)
            return;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float curveValue = curve != null
                ? curve.Evaluate(normalizedTime)
                : normalizedTime;

            float progress = Mathf.LerpUnclamped(from, to, curveValue);
            SetProgress(progress);

            await Task.Yield();
        }

        SetProgress(to);
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

    private void SetProgress(float value)
    {
        if (transitionMaterial == null || string.IsNullOrWhiteSpace(resolvedProgressProperty))
            return;

        transitionMaterial.SetFloat(resolvedProgressProperty, value);
    }

    private void SetDirection(float value)
    {
        if (transitionMaterial == null || string.IsNullOrWhiteSpace(resolvedDirectionProperty))
            return;

        transitionMaterial.SetFloat(resolvedDirectionProperty, value);
    }

    private void PlayTransitionSound()
    {
        if (!playTransitionSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(
            transitionSfx,
            transitionSfxVolumeMultiplier);
    }
}
