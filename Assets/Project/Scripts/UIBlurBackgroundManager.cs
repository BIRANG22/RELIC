using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class UIBlurBackgroundManager : MonoBehaviour
{
    private const string RootName = "SharedBlurRoot";
    private const string ShaderName = "UI/DustiumBackgroundBlur";
    private const int SortingOrder = -32000;
    private static UIBlurBackgroundManager instance;
    private readonly HashSet<UIBlurBackground> requesters = new();
    private UIBlurBackground activeRequester;
    private Canvas sharedCanvas;
    private RawImage sharedBackground;
    private Material material;
    private bool cameraPauseActive;

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

    public static bool IsInputBlocked
    {
        get
        {
            if (instance == null)
                return false;

            instance.RemoveInvalidRequesters();
            return instance.requesters.Count > 0;
        }
    }

    public int RequesterCount { get { RemoveInvalidRequesters(); return requesters.Count; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
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
        if (requester == null) return;
        EnsureSharedUI();
        requesters.Add(requester);
        activeRequester = requester;
        Apply(activeRequester);
        RefreshVisibility();
    }

    public void Release(UIBlurBackground requester)
    {
        if (requester != null) requesters.Remove(requester);
        if (activeRequester == requester)
            activeRequester = GetAnyValidRequester();
        RefreshVisibility();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        requesters.Clear();
        activeRequester = null;
        RefreshVisibility();
        RefreshWorldCamera();
    }

    private void LateUpdate()
    {
        RefreshVisibility();
        if (activeRequester == null)
            activeRequester = GetAnyValidRequester();
        if (activeRequester == null)
            return;

        Apply(activeRequester);
        if (material != null && UIBackgroundBlurRendererFeature.SourceTexture != null)
            material.SetTexture("_UIBlurSourceTexture", UIBackgroundBlurRendererFeature.SourceTexture);
    }

    private void EnsureSharedUI()
    {
        if (sharedCanvas != null && sharedBackground != null) return;
        GameObject canvasObject = new("SharedBlurCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        sharedCanvas = canvasObject.GetComponent<Canvas>();
        sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay; sharedCanvas.overrideSorting = true; sharedCanvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f);
        GameObject background = new("SharedBlurBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        background.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = background.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        sharedBackground = background.GetComponent<RawImage>(); sharedBackground.raycastTarget = true;
        Shader shader = Shader.Find(ShaderName);
        if (shader != null) { material = new Material(shader) { name = "SharedBlurMaterial" }; sharedBackground.material = material; }
        canvasObject.SetActive(false); RefreshWorldCamera();
    }

    private void RefreshWorldCamera() { if (sharedCanvas != null) sharedCanvas.worldCamera = Camera.main; }
    private void Apply(UIBlurBackground requester)
    {
        if (material == null) return;
        Texture sourceTexture = UIBackgroundBlurRendererFeature.SourceTexture;
        if (sourceTexture != null)
            material.SetTexture("_UIBlurSourceTexture", sourceTexture);
        material.SetFloat("_BlurRadius", requester.BlurRadius); material.SetFloat("_Darken", requester.Darken); material.SetFloat("_Saturation", requester.Saturation); material.SetFloat("_Contrast", requester.Contrast);
    }
    public static bool IsRequesterPanelObject(GameObject target)
    {
        if (target == null || instance == null)
            return false;

        instance.RemoveInvalidRequesters();

        foreach (UIBlurBackground requester in instance.requesters)
        {
            if (requester == null)
                continue;

            Transform requesterTransform = requester.transform;
            Transform targetTransform = target.transform;
            if (targetTransform.IsChildOf(requesterTransform) || requesterTransform.IsChildOf(targetTransform))
                return true;
        }

        return false;
    }

    private void RefreshVisibility()
    {
        RemoveInvalidRequesters();
        bool hasRequesters = requesters.Count > 0;
        if (sharedCanvas != null)
            sharedCanvas.gameObject.SetActive(hasRequesters);

        UpdateCameraPause(hasRequesters);
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
    private void RemoveInvalidRequesters()
    {
        requesters.RemoveWhere(requester => requester == null || !requester.isActiveAndEnabled);
        if (activeRequester == null || !activeRequester.isActiveAndEnabled)
            activeRequester = GetAnyValidRequester();
    }

    private UIBlurBackground GetAnyValidRequester()
    {
        foreach (UIBlurBackground requester in requesters)
            return requester;

        return null;
    }
}
