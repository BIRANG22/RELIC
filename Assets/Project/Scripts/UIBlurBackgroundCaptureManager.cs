using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블러 패널이 열릴 때 요청받아 게임 카메라 화면을 한 번 캡처합니다.
/// 평상시에는 별도의 캡처 루프를 실행하지 않으므로 다른 씬의 UI 입력에 영향을 주지 않습니다.
/// 기본적으로 모든 UI 렌더러는 캡처에서 제외하고, 호출자가 직접 전달한 UI 루트만 임시로 포함합니다.
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

    [Tooltip("Screen Space - Overlay Canvas를 캡처할 때 임시 Screen Space - Camera로 전환하는 거리입니다.")]
    [SerializeField, Min(0.01f)] private float overlayCanvasPlaneDistance = 1f;

    private RenderTexture screenCaptureTexture;
    private RenderTexture capturedTexture;
    private int capturedScreenWidth;
    private int capturedScreenHeight;

    private readonly List<GameObject> captureIncludedTargets = new List<GameObject>();
    private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private readonly Dictionary<Canvas, CanvasState> originalCanvasStates = new Dictionary<Canvas, CanvasState>();
    private readonly Dictionary<CanvasRenderer, CanvasRendererState> originalCanvasRendererStates =
        new Dictionary<CanvasRenderer, CanvasRendererState>();

    private struct CanvasState
    {
        public RenderMode RenderMode;
        public Camera WorldCamera;
        public float PlaneDistance;
    }

    private struct CanvasRendererState
    {
        public bool Cull;
        public float Alpha;
    }

    public static Texture CapturedTexture
    {
        get
        {
            return instance != null ? instance.capturedTexture : null;
        }
    }

    /// <summary>
    /// 기존 호출부 호환용입니다. 예외 UI 없이 모든 UI를 제외하고 캡처합니다.
    /// </summary>
    public static Texture CaptureBackgroundNow()
    {
        return CaptureBackgroundNow(null);
    }

    /// <summary>
    /// 블러가 필요한 순간에만 배경을 캡처하고 결과 텍스처를 반환합니다.
    /// </summary>
    public static Texture CaptureBackgroundNow(IReadOnlyList<GameObject> blurredUiRoots)
    {
        EnsureInstance();

        if (instance == null)
            return null;

        instance.CaptureCurrentScreen(blurredUiRoots);
        return instance.capturedTexture;
    }

    public static List<GameObject> GetValidBlurredUiRoots(IEnumerable<GameObject> roots)
    {
        List<GameObject> validRoots = new List<GameObject>();
        if (roots == null)
            return validRoots;

        foreach (GameObject root in roots)
        {
            if (root == null || validRoots.Contains(root))
                continue;

            validRoots.Add(root);
        }

        return validRoots;
    }

    public static bool IsTransformUnderAnyRoot(Transform target, IReadOnlyList<GameObject> roots)
    {
        if (target == null || roots == null)
            return false;

        for (int i = 0; i < roots.Count; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            Transform rootTransform = root.transform;
            if (target == rootTransform || target.IsChildOf(rootTransform))
                return true;
        }

        return false;
    }

    // 기존 코드와의 참조 호환성을 위해 유지합니다.
    public static void RegisterBlurPanel()
    {
        EnsureInstance();
    }

    public static void UnregisterBlurPanel()
    {
    }

    // 기존 자동 마커 표시/숨김 구조는 더 이상 사용하지 않습니다.
    public static void BeginBlurPresentation()
    {
    }

    public static void EndBlurPresentation()
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

        RestoreCaptureIncludedUI();
        RestoreHiddenCanvasRenderers();
        ReleaseCaptureTextures();
    }

    private void CaptureCurrentScreen(IReadOnlyList<GameObject> blurredUiRoots)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Camera sourceCamera = FindCaptureCamera();
        if (sourceCamera == null)
            return;

        int captureWidth = Mathf.Max(1, sourceCamera.pixelWidth);
        int captureHeight = Mathf.Max(1, sourceCamera.pixelHeight);

        EnsureCaptureTextures(captureWidth, captureHeight);
        if (screenCaptureTexture == null || capturedTexture == null)
            return;

        List<GameObject> validBlurredUiRoots = GetValidBlurredUiRoots(blurredUiRoots);
        List<CameraMouseParallaxController> pausedParallaxControllers =
            BeginCameraMotionPauseForCapture(sourceCamera);
        RenderTexture previousTargetTexture = sourceCamera.targetTexture;
        int previousCullingMask = sourceCamera.cullingMask;
        Matrix4x4 previousProjectionMatrix = sourceCamera.projectionMatrix;

        try
        {
            int captureMask = previousCullingMask;

            if (excludeUILayer)
                captureMask = RemoveUILayer(captureMask);

            HideUnselectedUICanvasRenderers(validBlurredUiRoots);
            PrepareExplicitUIForCapture(sourceCamera, captureMask, validBlurredUiRoots);

            sourceCamera.cullingMask = captureMask;
            sourceCamera.targetTexture = screenCaptureTexture;

            // targetTexture를 지정하면 RenderTexture 크기를 기준으로 카메라의 투영 비율이
            // 다시 계산될 수 있습니다. 실제 게임 화면에서 사용하던 투영 행렬을 그대로
            // 적용하여 블러 캡처가 확대/축소되어 보이지 않도록 합니다.
            sourceCamera.projectionMatrix = previousProjectionMatrix;

            Canvas.ForceUpdateCanvases();
            sourceCamera.Render();
            Graphics.Blit(screenCaptureTexture, capturedTexture);
        }
        finally
        {
            sourceCamera.targetTexture = previousTargetTexture;
            sourceCamera.cullingMask = previousCullingMask;
            sourceCamera.projectionMatrix = previousProjectionMatrix;
            RestoreCaptureIncludedUI();
            RestoreHiddenCanvasRenderers();
            Canvas.ForceUpdateCanvases();
            EndCameraMotionPauseForCapture(pausedParallaxControllers);
        }
    }

    private static List<CameraMouseParallaxController> BeginCameraMotionPauseForCapture(Camera sourceCamera)
    {
        List<CameraMouseParallaxController> pausedControllers =
            new List<CameraMouseParallaxController>();

        if (sourceCamera == null)
            return pausedControllers;

        CameraMouseParallaxController[] controllers =
            FindObjectsByType<CameraMouseParallaxController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            CameraMouseParallaxController controller = controllers[i];
            if (controller == null || !controller.isActiveAndEnabled || !controller.UsesCamera(sourceCamera))
                continue;

            controller.BeginBlurCapturePause();
            pausedControllers.Add(controller);
        }

        return pausedControllers;
    }

    private static void EndCameraMotionPauseForCapture(
        List<CameraMouseParallaxController> pausedControllers)
    {
        if (pausedControllers == null)
            return;

        for (int i = pausedControllers.Count - 1; i >= 0; i--)
        {
            CameraMouseParallaxController controller = pausedControllers[i];
            if (controller != null)
                controller.EndBlurCapturePause();
        }
    }

    private void HideUnselectedUICanvasRenderers(IReadOnlyList<GameObject> includedRoots)
    {
        RestoreHiddenCanvasRenderers();

        CanvasRenderer[] renderers = FindObjectsByType<CanvasRenderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            CanvasRenderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.activeInHierarchy)
                continue;

            if (IsTransformUnderAnyRoot(renderer.transform, includedRoots))
                continue;

            if (originalCanvasRendererStates.ContainsKey(renderer))
                continue;

            originalCanvasRendererStates.Add(renderer, new CanvasRendererState
            {
                Cull = renderer.cull,
                Alpha = renderer.GetAlpha()
            });

            renderer.SetAlpha(0f);
            renderer.cull = true;
        }
    }

    private void RestoreHiddenCanvasRenderers()
    {
        if (originalCanvasRendererStates.Count == 0)
            return;

        foreach (KeyValuePair<CanvasRenderer, CanvasRendererState> pair in originalCanvasRendererStates)
        {
            CanvasRenderer renderer = pair.Key;
            if (renderer == null)
                continue;

            CanvasRendererState state = pair.Value;
            renderer.cull = state.Cull;
            renderer.SetAlpha(state.Alpha);
        }

        originalCanvasRendererStates.Clear();
    }

    private void PrepareExplicitUIForCapture(
        Camera sourceCamera,
        int captureMask,
        IReadOnlyList<GameObject> blurredUiRoots)
    {
        RestoreCaptureIncludedUI();
        captureIncludedTargets.Clear();

        if (blurredUiRoots == null || blurredUiRoots.Count == 0)
            return;

        for (int i = 0; i < blurredUiRoots.Count; i++)
        {
            GameObject root = blurredUiRoots[i];
            if (root == null || !root.activeInHierarchy)
                continue;

            AddRootCanvasToCaptureTargets(root);

            Transform[] targets = root.GetComponentsInChildren<Transform>(true);
            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                Transform target = targets[targetIndex];
                AddCaptureIncludedTarget(target != null ? target.gameObject : null);
            }
        }

        if (captureIncludedTargets.Count == 0)
            return;

        int temporaryLayer = FindCaptureIncludedLayer(captureMask);
        if (temporaryLayer < 0)
        {
            Debug.LogWarning(
                "[UIBlurBackgroundCaptureManager] 선택 UI를 캡처할 수 있는 카메라 레이어를 찾지 못했습니다.",
                this);
            return;
        }

        for (int i = 0; i < captureIncludedTargets.Count; i++)
        {
            GameObject target = captureIncludedTargets[i];
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

    private void AddRootCanvasToCaptureTargets(GameObject includedRoot)
    {
        if (includedRoot == null)
            return;

        Canvas canvas = includedRoot.GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        if (rootCanvas == null || !rootCanvas.gameObject.activeInHierarchy)
            return;

        AddCaptureIncludedTarget(rootCanvas.gameObject);
    }

    private void AddCaptureIncludedTarget(GameObject target)
    {
        if (target == null || captureIncludedTargets.Contains(target))
            return;

        captureIncludedTargets.Add(target);
    }

    private void RestoreCaptureIncludedUI()
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

        captureIncludedTargets.Clear();
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

    private void EnsureCaptureTextures(int captureWidth, int captureHeight)
    {
        captureWidth = Mathf.Max(1, captureWidth);
        captureHeight = Mathf.Max(1, captureHeight);

        int safeDownsample = Mathf.Max(1, downsample);
        int blurWidth = Mathf.Max(1, captureWidth / safeDownsample);
        int blurHeight = Mathf.Max(1, captureHeight / safeDownsample);

        bool sameSize =
            screenCaptureTexture != null &&
            capturedTexture != null &&
            capturedScreenWidth == captureWidth &&
            capturedScreenHeight == captureHeight &&
            screenCaptureTexture.width == captureWidth &&
            screenCaptureTexture.height == captureHeight &&
            capturedTexture.width == blurWidth &&
            capturedTexture.height == blurHeight;

        if (sameSize)
            return;

        ReleaseCaptureTextures();

        screenCaptureTexture = CreateRenderTexture(
            captureWidth,
            captureHeight,
            "UI_Blur_Camera_Capture",
            FilterMode.Bilinear);

        capturedTexture = CreateRenderTexture(
            blurWidth,
            blurHeight,
            "UI_Blur_Background_Capture",
            FilterMode.Bilinear);

        capturedScreenWidth = captureWidth;
        capturedScreenHeight = captureHeight;
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
