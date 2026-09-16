using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    private const string ResolutionIndexPrefsKey = "Relic.ResolutionIndex";
    private const string FullscreenPrefsKey = "Relic.Fullscreen";
    private const int DefaultResolutionIndex = 3;
    private const int LetterboxSortingOrder = 32000;
    private const int RequiredStableRefreshFrames = 3;
    private const int MaxResolutionRefreshFrames = 45;

    private static readonly ResolutionOption[] SupportedResolutions =
    {
        new(1280, 720),
        new(1366, 768),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160)
    };

    private static ResolutionManager instance;
    private static Color letterboxColor = new Color32(0x00, 0x02, 0x22, 0xFF);

    private ResolutionLetterboxOverlay letterboxOverlay;
    private Coroutine resolutionRefreshCoroutine;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private FullScreenMode lastFullScreenMode = (FullScreenMode)(-1);
    private bool initialized;

    public static int CurrentResolutionIndex { get; private set; } = DefaultResolutionIndex;
    public static ResolutionOption CurrentResolution => SupportedResolutions[CurrentResolutionIndex];
    public static bool IsFullScreen { get; private set; }
    public static Color LetterboxColor => letterboxColor;

    /// <summary>
    /// Bootstrap에서 명시적으로 한 번 호출합니다.
    /// RuntimeInitializeOnLoadMethod로 게임 시작 전에 강제 생성하지 않습니다.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (instance == null)
        {
            ResolutionManager existing = FindFirstObjectByType<ResolutionManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
            }
            else
            {
                var managerObject = new GameObject(nameof(ResolutionManager));
                instance = managerObject.AddComponent<ResolutionManager>();
            }
        }

        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool screenSizeChanged = lastScreenWidth != Screen.width || lastScreenHeight != Screen.height;
        bool fullScreenModeChanged = lastFullScreenMode != Screen.fullScreenMode;

        if (!screenSizeChanged && !fullScreenModeChanged)
            return;

        StartResolutionRefresh();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        CurrentResolutionIndex = GetSavedResolutionIndex();
        IsFullScreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 0) != 0;

        ApplyCurrentResolution(false);
    }

    public static IReadOnlyList<ResolutionOption> GetSupportedResolutions()
    {
        return SupportedResolutions;
    }

    public static List<string> GetSupportedResolutionLabels()
    {
        var labels = new List<string>(SupportedResolutions.Length);

        for (int i = 0; i < SupportedResolutions.Length; i++)
            labels.Add(SupportedResolutions[i].Label);

        return labels;
    }

    public static void SetLetterboxColor(Color color)
    {
        letterboxColor = color;

        if (instance != null && instance.letterboxOverlay != null)
            instance.letterboxOverlay.SetColor(letterboxColor);
    }

    public static void ApplySavedResolution()
    {
        CurrentResolutionIndex = GetSavedResolutionIndex();
        IsFullScreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 0) != 0;
        ApplyCurrentResolution(false);
    }

    public static void ApplyResolution(int index, bool saveSelection)
    {
        if (!IsValidResolutionIndex(index))
            index = GetDefaultResolutionIndex();

        CurrentResolutionIndex = index;

        if (saveSelection)
        {
            PlayerPrefs.SetInt(ResolutionIndexPrefsKey, index);
            PlayerPrefs.Save();
        }

        ApplyCurrentResolution(false);
    }

    public static void SetFullScreen(bool isFullScreen, bool saveSelection)
    {
        IsFullScreen = isFullScreen;

        if (saveSelection)
        {
            PlayerPrefs.SetInt(FullscreenPrefsKey, isFullScreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        ApplyCurrentResolution(false);
    }

    private static void ApplyCurrentResolution(bool forceRefreshOnly)
    {
        if (!forceRefreshOnly)
        {
            ResolutionOption resolution = CurrentResolution;
            Screen.SetResolution(
                resolution.Width,
                resolution.Height,
                IsFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        if (instance != null)
            instance.StartResolutionRefresh();
    }

    private void StartResolutionRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (resolutionRefreshCoroutine != null)
            StopCoroutine(resolutionRefreshCoroutine);

        resolutionRefreshCoroutine = StartCoroutine(RefreshResolutionLayoutRoutine());
    }

    private IEnumerator RefreshResolutionLayoutRoutine()
    {
        ResolutionRefreshStability stability = default;

        for (int i = 0; i < MaxResolutionRefreshFrames; i++)
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            ApplyLetterbox();

            Vector2Int screenSize = new(Screen.width, Screen.height);
            Vector2 layoutSize = new(Screen.width, Screen.height);
            if (stability.Observe(screenSize, layoutSize, RequiredStableRefreshFrames))
                break;
        }

        Canvas.ForceUpdateCanvases();
        ApplyLetterbox();
        resolutionRefreshCoroutine = null;
    }

    public static Rect CalculateLetterboxRect(
        int screenWidth,
        int screenHeight,
        int targetWidth,
        int targetHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            return new Rect(0f, 0f, 1f, 1f);

        float screenAspect = screenWidth / (float)screenHeight;
        float targetAspect = targetWidth / (float)targetHeight;

        if (Mathf.Approximately(screenAspect, targetAspect))
            return new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > targetAspect)
        {
            float width = targetAspect / screenAspect;
            float x = (1f - width) * 0.5f;
            return new Rect(x, 0f, width, 1f);
        }

        float height = screenAspect / targetAspect;
        float y = (1f - height) * 0.5f;
        return new Rect(0f, y, 1f, height);
    }

    // 기존 테스트/호출부 호환용 계산 API는 유지합니다.
    public static ResolutionCanvasViewportLayout CalculateCanvasViewportLayout(
        Vector2 canvasSize,
        Rect viewport,
        int targetWidth,
        int targetHeight)
    {
        return CalculateCanvasViewportLayout(canvasSize, viewport, new Vector2(targetWidth, targetHeight));
    }

    public static ResolutionCanvasViewportLayout CalculateCanvasViewportLayout(
        Vector2 canvasSize,
        Rect viewport,
        Vector2 targetSize)
    {
        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return new ResolutionCanvasViewportLayout(Vector2.zero, Vector2.zero, 1f);

        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            return new ResolutionCanvasViewportLayout(Vector2.zero, targetSize, 1f);

        Vector2 viewportSize = new(
            Mathf.Max(0f, viewport.width * canvasSize.x),
            Mathf.Max(0f, viewport.height * canvasSize.y));

        float scale = Mathf.Min(viewportSize.x / targetSize.x, viewportSize.y / targetSize.y);
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            scale = 1f;

        Vector2 position = new(
            (viewport.xMin + viewport.width * 0.5f - 0.5f) * canvasSize.x,
            (viewport.yMin + viewport.height * 0.5f - 0.5f) * canvasSize.y);

        return new ResolutionCanvasViewportLayout(position, targetSize, scale);
    }

    private static bool IsValidResolutionIndex(int index)
    {
        return index >= 0 && index < SupportedResolutions.Length;
    }

    private static int GetSavedResolutionIndex()
    {
        int savedIndex = PlayerPrefs.GetInt(ResolutionIndexPrefsKey, GetDefaultResolutionIndex());
        return IsValidResolutionIndex(savedIndex) ? savedIndex : GetDefaultResolutionIndex();
    }

    private static int GetDefaultResolutionIndex()
    {
        int displayWidth = Screen.currentResolution.width;
        int displayHeight = Screen.currentResolution.height;

        if (displayWidth <= 0 || displayHeight <= 0)
            return DefaultResolutionIndex;

        int bestIndex = 0;
        for (int i = 0; i < SupportedResolutions.Length; i++)
        {
            ResolutionOption resolution = SupportedResolutions[i];
            if (resolution.Width <= displayWidth && resolution.Height <= displayHeight)
                bestIndex = i;
        }

        return bestIndex;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (initialized)
            StartResolutionRefresh();
    }

    private void ApplyLetterbox()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastFullScreenMode = Screen.fullScreenMode;

        ResolutionOption resolution = CurrentResolution;
        Rect viewport = CalculateLetterboxRect(
            Screen.width,
            Screen.height,
            resolution.Width,
            resolution.Height);

        ApplyCameraViewport(viewport);
        ApplyExplicitCanvasFitters(viewport);
        EnsureLetterboxOverlay().Apply(viewport, letterboxColor);
    }

    private static void ApplyCameraViewport(Rect viewport)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera targetCamera = cameras[i];
            if (targetCamera == null)
                continue;

            // RenderTexture/VFX 카메라는 해상도 보정 대상에서 제외합니다.
            if (targetCamera.targetTexture != null)
                continue;

            if (targetCamera.targetDisplay != 0)
                continue;

            targetCamera.rect = viewport;
        }
    }

    /// <summary>
    /// ResolutionCanvasViewportFitter를 개발자가 직접 붙여 둔 Canvas만 보정합니다.
    /// 모든 Canvas에 런타임 AddComponent를 하지 않습니다.
    /// </summary>
    private static void ApplyExplicitCanvasFitters(Rect viewport)
    {
        ResolutionCanvasViewportFitter[] fitters =
            FindObjectsByType<ResolutionCanvasViewportFitter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        ResolutionOption resolution = CurrentResolution;
        for (int i = 0; i < fitters.Length; i++)
        {
            ResolutionCanvasViewportFitter fitter = fitters[i];
            if (fitter == null)
                continue;

            fitter.Apply(viewport, resolution.Width, resolution.Height);
        }
    }

    private ResolutionLetterboxOverlay EnsureLetterboxOverlay()
    {
        if (letterboxOverlay != null)
            return letterboxOverlay;

        letterboxOverlay = FindFirstObjectByType<ResolutionLetterboxOverlay>(FindObjectsInactive.Include);
        if (letterboxOverlay != null)
            return letterboxOverlay;

        var overlayObject = new GameObject("Resolution Letterbox Overlay");
        DontDestroyOnLoad(overlayObject);

        letterboxOverlay = overlayObject.AddComponent<ResolutionLetterboxOverlay>();
        letterboxOverlay.Configure(LetterboxSortingOrder);

        return letterboxOverlay;
    }

    // 기존 테스트/외부 참조 호환용입니다. 자동 적용에는 사용하지 않습니다.
    public static bool ShouldFitCanvasForResolution(Canvas canvas)
    {
        if (canvas == null)
            return false;

        if (!canvas.isRootCanvas)
            return false;

        // World Space Canvas는 해상도 UI 보정 대상이 아닙니다.
        if (canvas.renderMode == RenderMode.WorldSpace)
            return false;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
            canvas.renderMode != RenderMode.ScreenSpaceCamera)
            return false;

        if (canvas.targetDisplay != 0)
            return false;

        if (canvas.GetComponent<ResolutionLetterboxOverlay>() != null)
            return false;

        if (canvas.GetComponentInParent<ResolutionCanvasFitOptOut>() != null)
            return false;

        return true;
    }
}

public readonly struct ResolutionOption
{
    public readonly int Width;
    public readonly int Height;

    public string Label => $"{Width} \u00D7 {Height}";

    public ResolutionOption(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

public readonly struct ResolutionCanvasViewportLayout
{
    public readonly Vector2 Position;
    public readonly Vector2 Size;
    public readonly float Scale;

    public ResolutionCanvasViewportLayout(Vector2 position, Vector2 size, float scale)
    {
        Position = position;
        Size = size;
        Scale = scale;
    }
}

public struct ResolutionRefreshStability
{
    private Vector2Int lastScreenSize;
    private Vector2 lastCanvasSize;
    private int stableFrameCount;
    private bool hasLastSample;

    public bool Observe(Vector2Int screenSize, Vector2 canvasSize, int requiredStableFrames)
    {
        if (screenSize.x <= 0 || screenSize.y <= 0 || canvasSize.x <= 0f || canvasSize.y <= 0f)
        {
            Reset();
            return false;
        }

        if (!hasLastSample || lastScreenSize != screenSize || !Approximately(lastCanvasSize, canvasSize))
        {
            lastScreenSize = screenSize;
            lastCanvasSize = canvasSize;
            stableFrameCount = 0;
            hasLastSample = true;
            return false;
        }

        stableFrameCount++;
        return stableFrameCount >= Mathf.Max(1, requiredStableFrames);
    }

    private void Reset()
    {
        lastScreenSize = default;
        lastCanvasSize = default;
        stableFrameCount = 0;
        hasLastSample = false;
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }
}

public sealed class ResolutionLetterboxOverlay : MonoBehaviour
{
    private const int DefaultSortingOrder = 32000;

    private RectTransform topBar;
    private RectTransform bottomBar;
    private RectTransform leftBar;
    private RectTransform rightBar;

    public void Configure(int sortingOrder)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        topBar = EnsureBar("Top");
        bottomBar = EnsureBar("Bottom");
        leftBar = EnsureBar("Left");
        rightBar = EnsureBar("Right");
    }

    public void Apply(Rect viewport, Color color)
    {
        if (topBar == null)
            Configure(DefaultSortingOrder);

        SetColor(color);

        ApplyAnchors(topBar, new Vector2(0f, viewport.yMax), Vector2.one);
        ApplyAnchors(bottomBar, Vector2.zero, new Vector2(1f, viewport.yMin));
        ApplyAnchors(leftBar, new Vector2(0f, viewport.yMin), new Vector2(viewport.xMin, viewport.yMax));
        ApplyAnchors(rightBar, new Vector2(viewport.xMax, viewport.yMin), new Vector2(1f, viewport.yMax));
    }

    public void SetColor(Color color)
    {
        SetBarColor(topBar, color);
        SetBarColor(bottomBar, color);
        SetBarColor(leftBar, color);
        SetBarColor(rightBar, color);
    }

    private RectTransform EnsureBar(string barName)
    {
        Transform existing = transform.Find(barName);
        if (existing != null && existing is RectTransform existingRect)
            return existingRect;

        var barObject = new GameObject(barName);
        barObject.transform.SetParent(transform, false);

        var rect = barObject.AddComponent<RectTransform>();
        var image = barObject.AddComponent<Image>();

        image.color = ResolutionManager.LetterboxColor;
        image.raycastTarget = false;

        return rect;
    }

    private static void ApplyAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetBarColor(RectTransform rect, Color color)
    {
        if (rect == null)
            return;

        Image image = rect.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }
}

/// <summary>
/// 필요한 Canvas에만 수동으로 붙이는 선택적 보정 컴포넌트입니다.
/// 기존처럼 Canvas 자식들을 런타임에 강제로 재부모화하지 않습니다.
/// </summary>
public sealed class ResolutionCanvasViewportFitter : MonoBehaviour
{
    private const string ViewportObjectName = "Resolution Viewport";
    private static readonly Vector2 DefaultContentSize = new(1920f, 1080f);

    [SerializeField] private RectTransform contentRoot;

    public RectTransform ContentRoot
    {
        get
        {
            ResolveExistingContentRoot();
            return contentRoot;
        }
    }

    public static RectTransform ResolveContentRoot(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        ResolutionCanvasViewportFitter fitter =
            canvasTransform.GetComponent<ResolutionCanvasViewportFitter>();
        if (fitter != null && fitter.ContentRoot != null)
            return fitter.ContentRoot;

        Transform existing = canvasTransform.Find(ViewportObjectName);
        return existing as RectTransform;
    }

    public void Apply(Rect viewport, int targetWidth, int targetHeight)
    {
        ResolveExistingContentRoot();
        if (contentRoot == null)
            return;

        RectTransform canvasRect = transform as RectTransform;
        Vector2 canvasSize = canvasRect != null
            ? canvasRect.rect.size
            : new Vector2(Screen.width, Screen.height);

        Vector2 targetSize = GetTargetContentSize(targetWidth, targetHeight);
        ResolutionCanvasViewportLayout layout = ResolutionManager.CalculateCanvasViewportLayout(
            canvasSize,
            viewport,
            targetSize);

        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.anchoredPosition = layout.Position;
        contentRoot.sizeDelta = layout.Size;
        contentRoot.localScale = Vector3.one * layout.Scale;
    }

    private void ResolveExistingContentRoot()
    {
        if (contentRoot != null)
            return;

        Transform existing = transform.Find(ViewportObjectName);
        if (existing != null)
            contentRoot = existing as RectTransform;
    }

    private Vector2 GetTargetContentSize(int fallbackWidth, int fallbackHeight)
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null
            && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution.x > 0f
            && scaler.referenceResolution.y > 0f)
        {
            return scaler.referenceResolution;
        }

        if (fallbackWidth > 0 && fallbackHeight > 0)
            return new Vector2(fallbackWidth, fallbackHeight);

        return DefaultContentSize;
    }
}
