using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    private const string ResolutionIndexPrefsKey = "Relic.ResolutionIndex";
    private const int DefaultResolutionIndex = 3;
    private const int LetterboxSortingOrder = 32000;

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
    private static Color letterboxColor = Color.white;//레터박스 색
    private ResolutionLetterboxOverlay letterboxOverlay;
    private Coroutine resolutionRefreshCoroutine;
    private readonly List<ResolutionCanvasViewportFitter> canvasViewportFitters = new();
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private FullScreenMode lastFullScreenMode = (FullScreenMode)(-1);

    public static int CurrentResolutionIndex { get; private set; } = DefaultResolutionIndex;
    public static ResolutionOption CurrentResolution => SupportedResolutions[CurrentResolutionIndex];
    public static Color LetterboxColor => letterboxColor;

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

        ApplySavedResolution();
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
        bool screenSizeChanged = lastScreenWidth != Screen.width || lastScreenHeight != Screen.height;
        bool fullScreenModeChanged = lastFullScreenMode != Screen.fullScreenMode;

        if (!screenSizeChanged && !fullScreenModeChanged)
            return;

        ApplyLetterbox();

        // 창모드와 전체화면 전환은 실제 화면 크기 적용이 여러 프레임 늦을 수 있습니다.
        // 전환이 감지되면 해상도 변경과 동일하게 화면 배치를 반복 갱신합니다.
        if (fullScreenModeChanged)
            StartResolutionRefresh();
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
        ApplyResolution(GetSavedResolutionIndex(), false);
    }

    public static void ApplyResolution(int index, bool saveSelection)
    {
        if (!IsValidResolutionIndex(index))
            index = GetDefaultResolutionIndex();

        CurrentResolutionIndex = index;
        ResolutionOption resolution = SupportedResolutions[index];

        if (saveSelection)
        {
            PlayerPrefs.SetInt(ResolutionIndexPrefsKey, index);
            PlayerPrefs.Save();
        }

        Screen.SetResolution(resolution.Width, resolution.Height, FullScreenMode.Windowed);

        if (instance != null)
            instance.StartResolutionRefresh();
    }

    private void StartResolutionRefresh()
    {
        if (resolutionRefreshCoroutine != null)
            StopCoroutine(resolutionRefreshCoroutine);

        // Screen.SetResolution은 실제 창 크기를 다음 프레임부터 반영합니다.
        // 창과 Canvas 크기가 완전히 갱신될 때까지 몇 프레임 동안 화면 배치를 다시 맞춥니다.
        resolutionRefreshCoroutine = StartCoroutine(RefreshResolutionLayoutRoutine());
    }

    private IEnumerator RefreshResolutionLayoutRoutine()
    {
        const int refreshFrameCount = 5;

        for (int i = 0; i < refreshFrameCount; i++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyLetterbox();
        }

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
        if (canvasSize.x <= 0f || canvasSize.y <= 0f || targetSize.x <= 0f || targetSize.y <= 0f)
            return new ResolutionCanvasViewportLayout(Vector2.zero, Vector2.zero, 1f);

        Vector2 viewportSize = new(
            Mathf.Max(0f, viewport.width * canvasSize.x),
            Mathf.Max(0f, viewport.height * canvasSize.y));

        float scale = Mathf.Min(viewportSize.x / targetSize.x, viewportSize.y / targetSize.y);
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            scale = 1f;

        Vector2 position = new(
            (viewport.xMin + viewport.width * 0.5f - 0.5f) * canvasSize.x,
            (viewport.yMin + viewport.height * 0.5f - 0.5f) * canvasSize.y);

        return new ResolutionCanvasViewportLayout(
            position,
            targetSize,
            scale);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (instance != null)
            return;

        ResolutionManager existing = FindFirstObjectByType<ResolutionManager>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var managerObject = new GameObject(nameof(ResolutionManager));
        managerObject.AddComponent<ResolutionManager>();
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
        ApplyLetterbox();
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
        ApplyCanvasViewports(viewport);
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

            if (targetCamera.targetTexture != null)
                continue;

            if (targetCamera.targetDisplay != 0)
                continue;

            targetCamera.rect = viewport;
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

    private void ApplyCanvasViewports(Rect viewport)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        canvasViewportFitters.Clear();

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!ShouldFitCanvas(canvas))
                continue;

            ResolutionCanvasViewportFitter fitter = canvas.GetComponent<ResolutionCanvasViewportFitter>();
            if (fitter == null)
                fitter = canvas.gameObject.AddComponent<ResolutionCanvasViewportFitter>();

            canvasViewportFitters.Add(fitter);
        }

        for (int i = 0; i < canvasViewportFitters.Count; i++)
            canvasViewportFitters[i].Apply(viewport, CurrentResolution.Width, CurrentResolution.Height);
    }

    private static bool ShouldFitCanvas(Canvas canvas)
    {
        if (canvas == null)
            return false;

        if (!canvas.isRootCanvas)
            return false;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return false;

        if (canvas.GetComponent<ResolutionLetterboxOverlay>() != null)
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

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

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
        image.raycastTarget = true;

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

public sealed class ResolutionCanvasViewportFitter : MonoBehaviour
{
    private const string ViewportObjectName = "Resolution Viewport";

    private RectTransform viewportRoot;

    public void Apply(Rect viewport, int targetWidth, int targetHeight)
    {
        EnsureViewportRoot();
        MoveDirectChildrenIntoViewport();

        RectTransform canvasRect = transform as RectTransform;
        Vector2 canvasSize = canvasRect != null
            ? canvasRect.rect.size
            : new Vector2(Screen.width, Screen.height);
        Vector2 targetSize = GetTargetContentSize(targetWidth, targetHeight);
        ResolutionCanvasViewportLayout layout = ResolutionManager.CalculateCanvasViewportLayout(
            canvasSize,
            viewport,
            targetSize);

        viewportRoot.anchorMin = new Vector2(0.5f, 0.5f);
        viewportRoot.anchorMax = new Vector2(0.5f, 0.5f);
        viewportRoot.pivot = new Vector2(0.5f, 0.5f);
        viewportRoot.anchoredPosition = layout.Position;
        viewportRoot.sizeDelta = layout.Size;
        viewportRoot.localScale = Vector3.one * layout.Scale;
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

        return new Vector2(fallbackWidth, fallbackHeight);
    }

    private void EnsureViewportRoot()
    {
        if (viewportRoot != null)
            return;

        Transform existing = transform.Find(ViewportObjectName);
        if (existing != null)
            viewportRoot = existing as RectTransform;

        if (viewportRoot == null)
        {
            var viewportObject = new GameObject(ViewportObjectName, typeof(RectTransform));
            viewportObject.transform.SetParent(transform, false);
            viewportRoot = viewportObject.GetComponent<RectTransform>();
        }

        viewportRoot.SetAsFirstSibling();
        viewportRoot.localRotation = Quaternion.identity;
        viewportRoot.anchoredPosition3D = Vector3.zero;
    }

    private void MoveDirectChildrenIntoViewport()
    {
        List<Transform> childrenToMove = new();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child == viewportRoot)
                continue;

            childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
        {
            Transform child = childrenToMove[i];
            child.SetParent(viewportRoot, false);
            child.SetSiblingIndex(viewportRoot.childCount - 1);
        }
    }
}
