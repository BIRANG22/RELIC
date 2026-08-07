using UnityEngine;

/// <summary>
/// 블러 패널이 열릴 때 요청받아 게임 카메라 화면을 한 번 캡처합니다.
/// 평상시에는 별도의 캡처 루프를 실행하지 않으므로 다른 씬의 UI 입력에 영향을 주지 않습니다.
/// 캡처 시에는 카메라의 Culling Mask에서 UI 레이어만 제외합니다.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class UIBlurBackgroundCaptureManager : MonoBehaviour
{
    private const string RuntimeObjectName = "UIBlurBackgroundCaptureManager";

    private static UIBlurBackgroundCaptureManager instance;

    [Header("Capture")]
    [SerializeField, Range(2, 8)] private int downsample = 4;

    [Header("UI Exclusion")]
    [Tooltip("프로젝트의 UI 레이어를 캡처 카메라의 Culling Mask에서 제외합니다.")]
    [SerializeField] private bool excludeUILayer = true;

    private RenderTexture screenCaptureTexture;
    private RenderTexture capturedTexture;
    private int capturedScreenWidth;
    private int capturedScreenHeight;

    public static Texture CapturedTexture
    {
        get
        {
            return instance != null ? instance.capturedTexture : null;
        }
    }

    /// <summary>
    /// 블러가 필요한 순간에만 배경을 캡처하고 결과 텍스처를 반환합니다.
    /// </summary>
    public static Texture CaptureBackgroundNow()
    {
        EnsureInstance();

        if (instance == null)
            return null;

        instance.CaptureCurrentScreen();
        return instance.capturedTexture;
    }

    // 기존 코드와의 참조 호환성을 위해 유지합니다.
    // 더 이상 전역 캡처 루프나 패널 카운트를 사용하지 않습니다.
    public static void RegisterBlurPanel()
    {
        EnsureInstance();
    }

    public static void UnregisterBlurPanel()
    {
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
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        ReleaseCaptureTextures();
    }

    private void CaptureCurrentScreen()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Camera sourceCamera = FindCaptureCamera();
        if (sourceCamera == null)
            return;

        EnsureCaptureTextures();
        if (screenCaptureTexture == null || capturedTexture == null)
            return;

        RenderTexture previousTargetTexture = sourceCamera.targetTexture;
        int previousCullingMask = sourceCamera.cullingMask;

        try
        {
            if (excludeUILayer)
                sourceCamera.cullingMask = RemoveUILayer(previousCullingMask);

            sourceCamera.targetTexture = screenCaptureTexture;
            sourceCamera.Render();
            Graphics.Blit(screenCaptureTexture, capturedTexture);
        }
        finally
        {
            sourceCamera.targetTexture = previousTargetTexture;
            sourceCamera.cullingMask = previousCullingMask;
        }
    }

    private static Camera FindCaptureCamera()
    {
        Camera mainCamera = Camera.main;
        if (IsUsableCaptureCamera(mainCamera))
            return mainCamera;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Camera bestCamera = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (!IsUsableCaptureCamera(camera))
                continue;

            if (bestCamera == null)
            {
                bestCamera = camera;
                continue;
            }

            bool cameraRendersUI = CameraRendersUILayer(camera);
            bool bestRendersUI = CameraRendersUILayer(bestCamera);

            if (bestRendersUI && !cameraRendersUI)
            {
                bestCamera = camera;
                continue;
            }

            if (cameraRendersUI == bestRendersUI && camera.depth < bestCamera.depth)
                bestCamera = camera;
        }

        return bestCamera;
    }

    private static bool IsUsableCaptureCamera(Camera camera)
    {
        return camera != null &&
               camera.enabled &&
               camera.gameObject.activeInHierarchy &&
               camera.targetTexture == null;
    }

    private static bool CameraRendersUILayer(Camera camera)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return false;

        return (camera.cullingMask & (1 << uiLayer)) != 0;
    }

    private static int RemoveUILayer(int cullingMask)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return cullingMask;

        return cullingMask & ~(1 << uiLayer);
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
            "UI_Blur_Camera_Capture",
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
        RenderTexture texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
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
