using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블러 패널이 열릴 때 요청받아 게임 카메라 화면을 한 번 캡처합니다.
/// 평상시에는 별도의 캡처 루프를 실행하지 않으므로 다른 씬의 UI 입력에 영향을 주지 않습니다.
/// 기본적으로 UI 레이어는 캡처에서 제외하지만, UIBlurInclude가 붙은 UI는 캡처 순간에만 임시로 포함합니다.
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

    [Header("Selective UI Blur")]
    [Tooltip("UIBlurInclude가 붙은 UI를 캡처에 포함합니다.")]
    [SerializeField] private bool includeMarkedUI = true;

    [Tooltip("Screen Space - Overlay Canvas를 캡처할 때 임시 Screen Space - Camera로 전환하는 거리입니다.")]
    [SerializeField, Min(0.01f)] private float overlayCanvasPlaneDistance = 1f;

    private RenderTexture screenCaptureTexture;
    private RenderTexture capturedTexture;
    private int capturedScreenWidth;
    private int capturedScreenHeight;

    private readonly List<GameObject> markedTargets = new List<GameObject>();
    private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private readonly Dictionary<Canvas, CanvasState> originalCanvasStates = new Dictionary<Canvas, CanvasState>();
    private readonly List<UIBlurInclude> hiddenMarkedUI = new List<UIBlurInclude>();
    private int activeBlurPresentationCount;

    private struct CanvasState
    {
        public RenderMode RenderMode;
        public Camera WorldCamera;
        public float PlaneDistance;
    }

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

    /// <summary>
    /// 블러 패널이 화면에 표시되기 시작했음을 알립니다.
    /// 캡처에 포함된 원본 UI는 선명하게 다시 그려지지 않도록 숨깁니다.
    /// </summary>
    public static void BeginBlurPresentation()
    {
        EnsureInstance();

        if (instance == null)
            return;

        instance.activeBlurPresentationCount++;
        instance.HideMarkedUIForBlur();
    }

    /// <summary>
    /// 블러 패널이 닫혔음을 알립니다. 마지막 블러 패널이 닫히면 원본 UI를 복구합니다.
    /// </summary>
    public static void EndBlurPresentation()
    {
        if (instance == null)
            return;

        instance.activeBlurPresentationCount = Mathf.Max(0, instance.activeBlurPresentationCount - 1);

        if (instance.activeBlurPresentationCount == 0)
            instance.RestoreHiddenMarkedUI();
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

        RestoreMarkedUI();
        RestoreHiddenMarkedUI();
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
        bool temporarilyRevealedMarkedUI = activeBlurPresentationCount > 0;

        if (temporarilyRevealedMarkedUI)
            SetHiddenMarkedUICaptureVisibility(true);

        try
        {
            int captureMask = previousCullingMask;

            if (excludeUILayer)
                captureMask = RemoveUILayer(captureMask);

            if (includeMarkedUI)
                PrepareMarkedUIForCapture(sourceCamera, captureMask);

            sourceCamera.cullingMask = captureMask;
            sourceCamera.targetTexture = screenCaptureTexture;

            Canvas.ForceUpdateCanvases();
            sourceCamera.Render();
            Graphics.Blit(screenCaptureTexture, capturedTexture);
        }
        finally
        {
            sourceCamera.targetTexture = previousTargetTexture;
            sourceCamera.cullingMask = previousCullingMask;
            RestoreMarkedUI();

            if (temporarilyRevealedMarkedUI)
                SetHiddenMarkedUICaptureVisibility(false);

            Canvas.ForceUpdateCanvases();
        }
    }

    private void HideMarkedUIForBlur()
    {
        UIBlurInclude[] markers = FindObjectsByType<UIBlurInclude>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < markers.Length; i++)
        {
            UIBlurInclude marker = markers[i];
            if (marker == null || !marker.isActiveAndEnabled || !marker.gameObject.activeInHierarchy)
                continue;

            if (!hiddenMarkedUI.Contains(marker))
                hiddenMarkedUI.Add(marker);

            marker.BeginBlurHide();
        }
    }

    private void RestoreHiddenMarkedUI()
    {
        for (int i = hiddenMarkedUI.Count - 1; i >= 0; i--)
        {
            UIBlurInclude marker = hiddenMarkedUI[i];
            if (marker != null)
                marker.EndBlurHide();
        }

        hiddenMarkedUI.Clear();
    }

    private void SetHiddenMarkedUICaptureVisibility(bool visible)
    {
        for (int i = hiddenMarkedUI.Count - 1; i >= 0; i--)
        {
            UIBlurInclude marker = hiddenMarkedUI[i];

            if (marker == null)
            {
                hiddenMarkedUI.RemoveAt(i);
                continue;
            }

            marker.SetTemporarilyVisibleForCapture(visible);
        }
    }

    private void PrepareMarkedUIForCapture(Camera sourceCamera, int captureMask)
    {
        RestoreMarkedUI();
        markedTargets.Clear();

        UIBlurInclude[] markers = FindObjectsByType<UIBlurInclude>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        if (markers == null || markers.Length == 0)
            return;

        for (int i = 0; i < markers.Length; i++)
        {
            UIBlurInclude marker = markers[i];
            if (marker == null)
                continue;

            marker.CollectCaptureTargets(markedTargets);
        }

        if (markedTargets.Count == 0)
            return;

        int temporaryLayer = FindCaptureIncludedLayer(captureMask);
        if (temporaryLayer < 0)
        {
            Debug.LogWarning(
                "[UIBlurBackgroundCaptureManager] 선택 UI를 캡처할 수 있는 카메라 레이어를 찾지 못했습니다.",
                this);
            return;
        }

        for (int i = 0; i < markedTargets.Count; i++)
        {
            GameObject target = markedTargets[i];
            if (target == null || originalLayers.ContainsKey(target))
                continue;

            originalLayers.Add(target, target.layer);
            target.layer = temporaryLayer;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                continue;

            Canvas rootCanvas = canvas.rootCanvas;
            if (rootCanvas == null || originalCanvasStates.ContainsKey(rootCanvas))
                continue;

            originalCanvasStates.Add(rootCanvas, new CanvasState
            {
                RenderMode = rootCanvas.renderMode,
                WorldCamera = rootCanvas.worldCamera,
                PlaneDistance = rootCanvas.planeDistance
            });

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                rootCanvas.worldCamera = sourceCamera;
                rootCanvas.planeDistance = GetSafePlaneDistance(sourceCamera);
            }
            else if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera && rootCanvas.worldCamera == null)
            {
                rootCanvas.worldCamera = sourceCamera;
            }
        }
    }

    private void RestoreMarkedUI()
    {
        if (originalLayers.Count > 0)
        {
            foreach (KeyValuePair<GameObject, int> pair in originalLayers)
            {
                if (pair.Key != null)
                    pair.Key.layer = pair.Value;
            }

            originalLayers.Clear();
        }

        if (originalCanvasStates.Count > 0)
        {
            foreach (KeyValuePair<Canvas, CanvasState> pair in originalCanvasStates)
            {
                Canvas canvas = pair.Key;
                if (canvas == null)
                    continue;

                CanvasState state = pair.Value;
                canvas.renderMode = state.RenderMode;
                canvas.worldCamera = state.WorldCamera;
                canvas.planeDistance = state.PlaneDistance;
            }

            originalCanvasStates.Clear();
        }

        markedTargets.Clear();
    }

    private float GetSafePlaneDistance(Camera sourceCamera)
    {
        if (sourceCamera == null)
            return Mathf.Max(0.01f, overlayCanvasPlaneDistance);

        return Mathf.Max(
            sourceCamera.nearClipPlane + 0.01f,
            overlayCanvasPlaneDistance);
    }

    private static int FindCaptureIncludedLayer(int captureMask)
    {
        int uiLayer = LayerMask.NameToLayer("UI");

        if ((captureMask & 1) != 0 && uiLayer != 0)
            return 0;

        for (int layer = 0; layer < 32; layer++)
        {
            if (layer == uiLayer)
                continue;

            if ((captureMask & (1 << layer)) != 0)
                return layer;
        }

        return -1;
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
