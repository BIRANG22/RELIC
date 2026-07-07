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
    private ResolutionLetterboxOverlay letterboxOverlay;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    public static int CurrentResolutionIndex { get; private set; } = DefaultResolutionIndex;
    public static ResolutionOption CurrentResolution => SupportedResolutions[CurrentResolutionIndex];

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
        if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            return;

        ApplyLetterbox();
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
            instance.ApplyLetterbox();
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

        ResolutionOption resolution = CurrentResolution;
        Rect viewport = CalculateLetterboxRect(
            Screen.width,
            Screen.height,
            resolution.Width,
            resolution.Height);

        ApplyCameraViewport(viewport);
        EnsureLetterboxOverlay().Apply(viewport);
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

    public void Apply(Rect viewport)
    {
        if (topBar == null)
            Configure(DefaultSortingOrder);

        ApplyAnchors(topBar, new Vector2(0f, viewport.yMax), Vector2.one);
        ApplyAnchors(bottomBar, Vector2.zero, new Vector2(1f, viewport.yMin));
        ApplyAnchors(leftBar, new Vector2(0f, viewport.yMin), new Vector2(viewport.xMin, viewport.yMax));
        ApplyAnchors(rightBar, new Vector2(viewport.xMax, viewport.yMin), new Vector2(1f, viewport.yMax));
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

        image.color = Color.black;
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
}
