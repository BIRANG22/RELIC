using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class UIBlurBackgroundManager : MonoBehaviour
{
    private const string RootName = "SharedBlurRoot";
    private const string ShaderName = "UI/DustiumBackgroundBlur";
    private const string SharedCanvasName = "SharedBlurCanvas";
    private const int SharedBlurSortingOrder = 9000;

    private static UIBlurBackgroundManager instance;

    private readonly List<UIBlurBackground> requesters = new();

    private UIBlurBackground activeRequester;
    private Canvas sharedCanvas;
    private RawImage sharedBackground;
    private Material material;
    private bool cameraPauseActive;
    private bool isRefreshing;

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

            if (!hasRequesters)
            {
                UpdateCameraPause(false);
                return;
            }

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

    private void LateUpdate()
    {
        if (RemoveInvalidRequesters())
            RefreshPresentation();

        if (activeRequester == null)
            activeRequester = GetTopValidRequester();
        if (activeRequester == null)
            return;

        Apply(activeRequester);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        requesters.Clear();
        activeRequester = null;
        if (sharedCanvas != null)
            sharedCanvas.gameObject.SetActive(false);
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
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject background = new("SharedBlurBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        background.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        sharedBackground = background.GetComponent<RawImage>();
        sharedBackground.raycastTarget = false;

        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
        {
            material = new Material(shader) { name = "SharedBlurMaterial" };
            sharedBackground.material = material;
        }

        canvasObject.SetActive(false);
        RefreshWorldCamera();
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

        material.SetFloat("_BlurRadius", requester.BlurRadius);
        material.SetFloat("_Darken", requester.Darken);
        material.SetFloat("_Saturation", requester.Saturation);
        material.SetFloat("_Contrast", requester.Contrast);
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
