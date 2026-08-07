using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 팝업이 열리기 전의 화면을 저해상도 RenderTexture로 보관합니다.
/// 블러 패널이 하나라도 활성화되어 있는 동안에는 캡처를 멈춰
/// 팝업 자체가 블러 배경에 다시 찍히는 것을 방지합니다.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class UIBlurBackgroundCaptureManager : MonoBehaviour
{
    private const string RuntimeObjectName = "UIBlurBackgroundCaptureManager";

    private static UIBlurBackgroundCaptureManager instance;
    private static int activeBlurPanelCount;

    [Header("Capture")]
    [SerializeField, Range(2, 8)] private int downsample = 4;
    [SerializeField, Min(0.05f)] private float captureInterval = 0.10f;

    private RenderTexture screenCaptureTexture;
    private RenderTexture capturedTexture;
    private Coroutine captureRoutine;
    private int capturedScreenWidth;
    private int capturedScreenHeight;

    public static Texture CapturedTexture
    {
        get
        {
            EnsureInstance();
            return instance != null ? instance.capturedTexture : null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitialize()
    {
        EnsureInstance();
    }

    public static void RegisterBlurPanel()
    {
        EnsureInstance();
        activeBlurPanelCount++;
    }

    public static void UnregisterBlurPanel()
    {
        activeBlurPanelCount = Mathf.Max(0, activeBlurPanelCount - 1);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        instance = FindFirstObjectByType<UIBlurBackgroundCaptureManager>(FindObjectsInactive.Include);
        if (instance != null)
            return;

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        instance = runtimeObject.AddComponent<UIBlurBackgroundCaptureManager>();
        DontDestroyOnLoad(runtimeObject);
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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (captureRoutine == null)
            captureRoutine = StartCoroutine(CaptureLoop());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        ReleaseCaptureTextures();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeBlurPanelCount = 0;
        ReleaseCaptureTextures();
    }

    private IEnumerator CaptureLoop()
    {
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        float nextCaptureTime = 0f;

        while (true)
        {
            yield return waitForEndOfFrame;

            if (activeBlurPanelCount > 0)
                continue;

            if (Time.unscaledTime < nextCaptureTime)
                continue;

            nextCaptureTime = Time.unscaledTime + captureInterval;
            CaptureCurrentScreen();
        }
    }

    private void CaptureCurrentScreen()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        EnsureCaptureTextures();
        if (screenCaptureTexture == null || capturedTexture == null)
            return;

        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenCaptureTexture);
        Graphics.Blit(screenCaptureTexture, capturedTexture);
    }

    private void EnsureCaptureTextures()
    {
        int safeDownsample = Mathf.Max(1, downsample);
        int blurWidth = Mathf.Max(1, Screen.width / safeDownsample);
        int blurHeight = Mathf.Max(1, Screen.height / safeDownsample);

        bool sameSize =
            screenCaptureTexture != null &&
            capturedTexture != null &&
            capturedScreenWidth == Screen.width &&
            capturedScreenHeight == Screen.height &&
            screenCaptureTexture.width == Screen.width &&
            screenCaptureTexture.height == Screen.height &&
            capturedTexture.width == blurWidth &&
            capturedTexture.height == blurHeight;

        if (sameSize)
            return;

        ReleaseCaptureTextures();

        screenCaptureTexture = CreateRenderTexture(
            Screen.width,
            Screen.height,
            "UI_Blur_Screen_Capture",
            FilterMode.Bilinear);

        capturedTexture = CreateRenderTexture(
            blurWidth,
            blurHeight,
            "UI_Blur_Background_Capture",
            FilterMode.Bilinear);

        capturedScreenWidth = Screen.width;
        capturedScreenHeight = Screen.height;
    }

    private static RenderTexture CreateRenderTexture(
        int width,
        int height,
        string textureName,
        FilterMode filterMode)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = textureName,
            filterMode = filterMode,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        texture.Create();
        return texture;
    }

    private void ReleaseCaptureTextures()
    {
        ReleaseRenderTexture(ref screenCaptureTexture);
        ReleaseRenderTexture(ref capturedTexture);
        capturedScreenWidth = 0;
        capturedScreenHeight = 0;
    }

    private static void ReleaseRenderTexture(ref RenderTexture texture)
    {
        if (texture == null)
            return;

        if (texture.IsCreated())
            texture.Release();

        Destroy(texture);
        texture = null;
    }
}
