using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class UIBlurBackgroundManager : MonoBehaviour
{
    private const string RootName = "SharedBlurRoot";
    private const string ShaderName = "UI/DustiumBackgroundBlur";
    private const string MaterialResourcePath = "UI/DustiumBackgroundBlur";
    private const string SharedCanvasName = "SharedBlurCanvas";
    private const string BlurReplicaRootName = "BlurReplicaRoot";
    private const string UIBlurCameraName = "UIBlurCamera";
    private const string UIBlurTextureName = "_UIBlurUiTexture";
    private const string SettingUpperName = "Setting_upper";
    private const int SharedBlurSortingOrder = 9000;
    private const int UIBlurLayer = 5;
    private const float ReferenceBlurHeight = 1080f;
    private static readonly Vector2 DefaultReferenceResolution = new(1920f, 1080f);

    private static UIBlurBackgroundManager instance;

    private readonly List<UIBlurBackground> requesters = new();
    private readonly Dictionary<GameObject, UIBlurReplicaSource> replicas = new();

    private UIBlurBackground activeRequester;
    private Canvas sharedCanvas;
    private RawImage sharedBackground;
    private Canvas blurReplicaCanvas;
    private Camera uiBlurCamera;
    private RenderTexture uiBlurTexture;
    private Material material;
    private bool cameraPauseActive;
    private bool isRefreshing;
    private bool replicaDirty;
    private bool materialDiagnosticsLogged;
    private bool textureDiagnosticsLogged;

    public static UIBlurBackgroundManager Instance
    {
        get
        {
            if (instance != null) return instance;
            GameObject root = new(RootName, typeof(UIBlurBackgroundManager));
            instance = root.GetComponent<UIBlurBackgroundManager>();
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    public static bool IsInputBlocked => false;

    public int RequesterCount { get { RemoveInvalidRequesters(); return requesters.Count; } }
    public UIBlurBackground TopRequester { get { RemoveInvalidRequesters(); return GetTopValidRequester(); } }

    public static void MarkReplicaDirty()
    {
        if (instance != null)
            instance.replicaDirty = true;
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
        EnsureSharedUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        ReleaseCameraPause();
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (material != null) Destroy(material);
        ReleaseUiBlurTexture();
    }

    public void Request(UIBlurBackground requester)
    {
        if (requester == null)
            return;

        EnsureSharedUI();
        requesters.Remove(requester);
        requesters.Add(requester);
        activeRequester = requester;
        RefreshPresentation();
    }

    public void Release(UIBlurBackground requester)
    {
        if (requester != null)
            requesters.Remove(requester);

        if (activeRequester == requester)
            activeRequester = GetTopValidRequester();

        RefreshPresentation();
    }

    public void RefreshPresentation()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;
        try
        {
            RemoveInvalidRequesters();
            activeRequester = GetTopValidRequester();
            bool hasRequesters = activeRequester != null;

            if (sharedCanvas != null)
                sharedCanvas.gameObject.SetActive(hasRequesters);
            if (blurReplicaCanvas != null)
                blurReplicaCanvas.gameObject.SetActive(hasRequesters);
            if (uiBlurCamera != null)
                uiBlurCamera.enabled = hasRequesters;

            if (!hasRequesters)
            {
                SetReplicaVisibility(null);
                UpdateCameraPause(false);
                return;
            }

            EnsureBlurReplicaUI();
            RebuildReplicas(activeRequester);
            SyncReplicas();
            Apply(activeRequester);
            UpdateCameraPause(true);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public bool ContainsRequester(UIBlurBackground requester)
    {
        RemoveInvalidRequesters();
        return requester != null && requesters.Contains(requester);
    }

    // 동적으로 슬롯을 생성/삭제하는 패널은 기존 replica hierarchy를 재사용할 수 없다.
    public void InvalidateReplicaSource(GameObject source)
    {
        if (source == null || !replicas.TryGetValue(source, out UIBlurReplicaSource replica))
            return;

        replica.Destroy();
        replicas.Remove(source);
        replicaDirty = true;
    }

    private void LateUpdate()
    {
        if (RemoveInvalidRequesters())
            RefreshPresentation();

        if (activeRequester == null)
            activeRequester = GetTopValidRequester();
        if (activeRequester == null)
            return;

        EnsureUiBlurTexture();

        if (replicaDirty)
            SyncReplicas();

        Apply(activeRequester);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        requesters.Clear();
        activeRequester = null;
        if (sharedCanvas != null)
            sharedCanvas.gameObject.SetActive(false);
        if (blurReplicaCanvas != null)
            blurReplicaCanvas.gameObject.SetActive(false);
        if (uiBlurCamera != null)
            uiBlurCamera.enabled = false;
        ClearReplicas();
        RefreshWorldCamera();
        UpdateCameraPause(false);
    }

    private void EnsureSharedUI()
    {
        if (sharedCanvas != null && sharedBackground != null)
            return;

        GameObject canvasObject = new(SharedCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        sharedCanvas = canvasObject.GetComponent<Canvas>();
        sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        sharedCanvas.overrideSorting = true;
        sharedCanvas.sortingOrder = SharedBlurSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = DefaultReferenceResolution;

        GameObject background = new("SharedBlurBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        background.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        sharedBackground = background.GetComponent<RawImage>();
        sharedBackground.raycastTarget = false;

        material = CreateBlurMaterial();
        if (material != null)
            sharedBackground.material = material;

        canvasObject.SetActive(false);
        RefreshWorldCamera();
    }

    private void EnsureBlurReplicaUI()
    {
        EnsureUiBlurTexture();

        if (uiBlurCamera == null)
        {
            GameObject cameraObject = new(UIBlurCameraName, typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            uiBlurCamera = cameraObject.GetComponent<Camera>();
            uiBlurCamera.clearFlags = CameraClearFlags.SolidColor;
            uiBlurCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiBlurCamera.orthographic = true;
            uiBlurCamera.nearClipPlane = 0.01f;
            uiBlurCamera.farClipPlane = 10f;
            uiBlurCamera.cullingMask = 1 << UIBlurLayer;
            uiBlurCamera.allowHDR = false;
            uiBlurCamera.allowMSAA = false;
            uiBlurCamera.enabled = false;
        }
        ConfigureUiBlurCamera();

        if (blurReplicaCanvas != null)
            return;

        GameObject canvasObject = new(BlurReplicaRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = UIBlurLayer;
        canvasObject.transform.SetParent(transform, false);

        blurReplicaCanvas = canvasObject.GetComponent<Canvas>();
        blurReplicaCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        blurReplicaCanvas.worldCamera = uiBlurCamera;
        blurReplicaCanvas.planeDistance = 1f;
        blurReplicaCanvas.overrideSorting = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = DefaultReferenceResolution;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        canvasObject.SetActive(false);
    }

    private void EnsureUiBlurTexture()
    {
        Vector2Int targetSize = GetUiBlurTextureSize();
        if (uiBlurTexture != null &&
            uiBlurTexture.width == targetSize.x &&
            uiBlurTexture.height == targetSize.y)
        {
            ConfigureUiBlurCamera();
            return;
        }

        ReleaseUiBlurTexture();

        RenderTextureDescriptor descriptor = new(
            targetSize.x,
            targetSize.y,
            RenderTextureFormat.ARGB32)
        {
            depthBufferBits = 24,
            msaaSamples = 1,
            useMipMap = false,
            autoGenerateMips = false
        };

        uiBlurTexture = new RenderTexture(descriptor)
        {
            name = UIBlurTextureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        uiBlurTexture.Create();
        Shader.SetGlobalTexture(UIBlurTextureName, uiBlurTexture);
        textureDiagnosticsLogged = false;
        replicaDirty = true;
        ConfigureUiBlurCamera();
    }

    private static Vector2Int GetUiBlurTextureSize()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        return new Vector2Int(width, height);
    }

    private void ConfigureUiBlurCamera()
    {
        if (uiBlurCamera == null)
            return;

        Vector2Int targetSize = uiBlurTexture != null
            ? new Vector2Int(uiBlurTexture.width, uiBlurTexture.height)
            : GetUiBlurTextureSize();

        uiBlurCamera.targetTexture = uiBlurTexture;
        uiBlurCamera.pixelRect = new Rect(0f, 0f, targetSize.x, targetSize.y);
        uiBlurCamera.aspect = targetSize.x / (float)targetSize.y;
        uiBlurCamera.orthographicSize = targetSize.y * 0.5f;
    }

    private void ReleaseUiBlurTexture()
    {
        if (uiBlurTexture == null)
            return;

        if (uiBlurCamera != null)
            uiBlurCamera.targetTexture = null;

        uiBlurTexture.Release();
        Destroy(uiBlurTexture);
        uiBlurTexture = null;
    }

    private void RebuildReplicas(UIBlurBackground topRequester)
    {
        if (blurReplicaCanvas == null)
            return;

        SyncReplicaCanvasScaler(topRequester);

        HashSet<GameObject> desiredSources = new();
        for (int i = 0; i < requesters.Count; i++)
        {
            UIBlurBackground requester = requesters[i];
            if (requester == null)
                continue;

            foreach (GameObject root in requester.BlurredUiRoots)
                AddReplicaSource(root, desiredSources);

            if (requester != topRequester)
                AddReplicaSource(requester.PanelRoot, desiredSources);
        }

        List<GameObject> existingSources = new(replicas.Keys);
        for (int i = 0; i < existingSources.Count; i++)
        {
            GameObject source = existingSources[i];
            if (desiredSources.Contains(source))
                continue;

            replicas[source].Destroy();
            replicas.Remove(source);
        }

        foreach (GameObject source in desiredSources)
        {
            if (replicas.ContainsKey(source))
                continue;

            replicas.Add(
                source,
                new UIBlurReplicaSource(source.transform, blurReplicaCanvas.transform as RectTransform, uiBlurCamera, UIBlurLayer));
        }

        SetReplicaVisibility(desiredSources);
        replicaDirty = true;
    }

    private void SyncReplicaCanvasScaler(UIBlurBackground topRequester)
    {
        if (blurReplicaCanvas == null || topRequester == null)
            return;

        CanvasScaler replicaScaler = blurReplicaCanvas.GetComponent<CanvasScaler>();
        if (replicaScaler == null)
            return;

        CanvasScaler sourceScaler = FindSourceCanvasScaler(topRequester);
        if (sourceScaler == null)
        {
            replicaScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            replicaScaler.referenceResolution = DefaultReferenceResolution;
            replicaScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            replicaScaler.matchWidthOrHeight = 0f;
            return;
        }

        replicaScaler.uiScaleMode = sourceScaler.uiScaleMode;
        replicaScaler.scaleFactor = sourceScaler.scaleFactor;
        replicaScaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
        replicaScaler.referenceResolution = sourceScaler.referenceResolution;
        replicaScaler.screenMatchMode = sourceScaler.screenMatchMode;
        replicaScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
        replicaScaler.physicalUnit = sourceScaler.physicalUnit;
        replicaScaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
        replicaScaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
        replicaScaler.dynamicPixelsPerUnit = sourceScaler.dynamicPixelsPerUnit;
    }

    private static CanvasScaler FindSourceCanvasScaler(UIBlurBackground requester)
    {
        if (requester == null)
            return null;

        foreach (GameObject root in requester.BlurredUiRoots)
        {
            CanvasScaler scaler = GetRootCanvasScaler(root);
            if (scaler != null)
                return scaler;
        }

        return GetRootCanvasScaler(requester.PanelRoot);
    }

    private static CanvasScaler GetRootCanvasScaler(GameObject root)
    {
        if (root == null)
            return null;

        Canvas canvas = root.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
    }

    private static void AddReplicaSource(GameObject source, HashSet<GameObject> desiredSources)
    {
        if (source == null || IsSettingUpperOrChild(source.transform))
            return;

        desiredSources.Add(source);
    }

    private void SetReplicaVisibility(HashSet<GameObject> visibleSources)
    {
        foreach (KeyValuePair<GameObject, UIBlurReplicaSource> pair in replicas)
            pair.Value.SetVisible(visibleSources != null && visibleSources.Contains(pair.Key));
    }

    private void SyncReplicas()
    {
        foreach (UIBlurReplicaSource replica in replicas.Values)
            replica.SyncNow();

        Shader.SetGlobalTexture(UIBlurTextureName, uiBlurTexture);
        if (material != null && uiBlurTexture != null)
            material.SetTexture(UIBlurTextureName, uiBlurTexture);
        replicaDirty = false;
    }

    private void ClearReplicas()
    {
        foreach (UIBlurReplicaSource replica in replicas.Values)
            replica.Destroy();

        replicas.Clear();
        replicaDirty = false;
    }

    private static bool IsSettingUpperOrChild(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == SettingUpperName)
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool RemoveInvalidRequesters()
    {
        int previousCount = requesters.Count;
        for (int i = requesters.Count - 1; i >= 0; i--)
        {
            UIBlurBackground requester = requesters[i];
            if (requester == null || !requester.isActiveAndEnabled || requester.PanelRoot == null || !requester.PanelRoot.activeInHierarchy)
                requesters.RemoveAt(i);
        }

        if (activeRequester == null || !activeRequester.isActiveAndEnabled || !requesters.Contains(activeRequester))
            activeRequester = GetTopValidRequester();

        return requesters.Count != previousCount;
    }

    private UIBlurBackground GetTopValidRequester()
    {
        for (int i = requesters.Count - 1; i >= 0; i--)
        {
            UIBlurBackground requester = requesters[i];
            if (requester != null && requester.isActiveAndEnabled && requester.PanelRoot != null && requester.PanelRoot.activeInHierarchy)
                return requester;
        }

        return null;
    }

    private void UpdateCameraPause(bool shouldPause)
    {
        if (shouldPause == cameraPauseActive)
            return;

        if (shouldPause)
        {
            CameraMouseParallaxController.BeginUiPanelPause();
            cameraPauseActive = true;
            return;
        }

        ReleaseCameraPause();
    }

    private void ReleaseCameraPause()
    {
        if (!cameraPauseActive)
            return;

        CameraMouseParallaxController.EndUiPanelPause();
        cameraPauseActive = false;
    }

    private void RefreshWorldCamera()
    {
        if (sharedCanvas != null)
            sharedCanvas.worldCamera = Camera.main;
    }

    private void Apply(UIBlurBackground requester)
    {
        if (requester == null || material == null)
            return;

        Texture sourceTexture = UIBackgroundBlurRendererFeature.SourceTexture;
        if (sourceTexture != null)
            material.SetTexture("_UIBlurSourceTexture", sourceTexture);
        if (uiBlurTexture != null)
            material.SetTexture(UIBlurTextureName, uiBlurTexture);

        LogTextureDiagnosticsOnce(sourceTexture);

        material.SetFloat("_BlurRadius", GetResolutionScaledBlurRadius(requester.BlurRadius, sourceTexture));
        material.SetFloat("_Darken", requester.Darken);
        material.SetFloat("_Saturation", requester.Saturation);
        material.SetFloat("_Contrast", requester.Contrast);
    }

    private float GetResolutionScaledBlurRadius(float blurRadius, Texture sourceTexture)
    {
        float renderHeight = GetBlurRenderHeight(sourceTexture);
        return blurRadius * (renderHeight / ReferenceBlurHeight);
    }

    private float GetBlurRenderHeight(Texture sourceTexture)
    {
        if (sourceTexture != null && sourceTexture.height > 0)
            return sourceTexture.height;

        if (uiBlurTexture != null && uiBlurTexture.height > 0)
            return uiBlurTexture.height;

        return Mathf.Max(1, Screen.height);
    }

    private Material CreateBlurMaterial()
    {
        Material template = Resources.Load<Material>(MaterialResourcePath);
        if (template != null)
        {
            Material instanceMaterial = new(template) { name = "SharedBlurMaterial" };
            LogMaterialDiagnosticsOnce(instanceMaterial, "Resources material");
            return instanceMaterial;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError(
                "[UIBlurBackgroundManager] UI/DustiumBackgroundBlur shader not found. " +
                "Possible build shader stripping. Expected Resources material at " +
                $"Resources/{MaterialResourcePath}.");
            return null;
        }

        Material fallbackMaterial = new(shader) { name = "SharedBlurMaterial" };
        LogMaterialDiagnosticsOnce(fallbackMaterial, "Shader.Find fallback");
        return fallbackMaterial;
    }

    private void LogMaterialDiagnosticsOnce(Material targetMaterial, string source)
    {
        if (materialDiagnosticsLogged || targetMaterial == null)
            return;

        materialDiagnosticsLogged = true;
        Shader shader = targetMaterial.shader;
        if (shader == null)
        {
            Debug.LogError($"[UIBlurBackgroundManager] Blur material has no shader. Source:{source}");
            return;
        }

        bool hasAllProperties =
            targetMaterial.HasProperty("_UIBlurSourceTexture") &&
            targetMaterial.HasProperty(UIBlurTextureName) &&
            targetMaterial.HasProperty("_BlurRadius") &&
            targetMaterial.HasProperty("_Darken") &&
            targetMaterial.HasProperty("_Saturation") &&
            targetMaterial.HasProperty("_Contrast");

        if (!shader.isSupported || !hasAllProperties)
        {
            Debug.LogError(
                "[UIBlurBackgroundManager] Blur material is not build-ready. " +
                $"Source:{source}, Shader:{shader.name}, Supported:{shader.isSupported}, " +
                $"HasRequiredProperties:{hasAllProperties}");
        }
    }

    private void LogTextureDiagnosticsOnce(Texture sourceTexture)
    {
        if (textureDiagnosticsLogged)
            return;

        textureDiagnosticsLogged = true;
        Debug.Log(
            "[UIBlurBackgroundManager] Blur texture diagnostics. " +
            $"Source:{DescribeTexture(sourceTexture)}, " +
            $"UI:{DescribeRenderTexture(uiBlurTexture)}, " +
            $"ARGB32Supported:{SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32)}");

        if (sourceTexture == null)
        {
            Debug.LogError(
                "[UIBlurBackgroundManager] _UIBlurSourceTexture is null when blur is requested. " +
                "Check that UIBackgroundBlurRendererFeature is present on the active screen renderer.");
        }

        if (uiBlurTexture == null || !uiBlurTexture.IsCreated())
        {
            Debug.LogError("[UIBlurBackgroundManager] _UIBlurUiTexture is not created when blur is requested.");
        }
    }

    private static string DescribeTexture(Texture texture)
    {
        if (texture == null)
            return "null";

        return $"{texture.name} {texture.width}x{texture.height} {texture.dimension}";
    }

    private static string DescribeRenderTexture(RenderTexture texture)
    {
        if (texture == null)
            return "null";

        return
            $"{texture.name} {texture.width}x{texture.height} " +
            $"{texture.graphicsFormat} depth:{texture.depth} created:{texture.IsCreated()}";
    }

    public static bool IsRequesterPanelObject(GameObject target)
    {
        if (target == null || instance == null)
            return false;

        instance.RemoveInvalidRequesters();
        Transform targetTransform = target.transform;
        foreach (UIBlurBackground requester in instance.requesters)
        {
            GameObject panelRoot = requester != null ? requester.PanelRoot : null;
            if (panelRoot == null)
                continue;

            Transform panelTransform = panelRoot.transform;
            if (targetTransform.IsChildOf(panelTransform) || panelTransform.IsChildOf(targetTransform))
                return true;
        }

        return false;
    }
}
