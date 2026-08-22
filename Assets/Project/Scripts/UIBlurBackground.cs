using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝업이 열릴 때 게임 화면 캡처를 블러하여 배경으로 표시합니다.
/// 블러 이미지는 기존 UI Canvas의 자식으로 넣지 않고,
/// 별도의 Screen Space - Overlay Canvas에서 매우 낮은 Sorting Order로 렌더합니다.
/// 따라서 다른 일반 UI Canvas는 블러 이미지보다 위에 표시됩니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UIBlurBackground : MonoBehaviour
{
    private const string BlurShaderName = "UI/DustiumBackgroundBlur";
    private const string BlurGraphicName = "BlurGraphic";
    private const int BlurCanvasSortingOrder = -32000;

    [Header("Blur")]
    [SerializeField, Range(0f, 8f)] private float blurRadius = 4f;

    [Header("Darken")]
    [SerializeField, Range(0f, 1f)] private float darken = 0.75f;

    [Header("Color Adjustment")]
    [Tooltip("0이면 흑백, 1이면 원본 채도입니다.")]
    [SerializeField, Range(0f, 1.5f)] private float saturation = 0.4f;

    [Tooltip("1이면 원본 대비, 1보다 작으면 부드럽고 흐릿하게, 1보다 크면 대비가 강해집니다.")]
    [SerializeField, Range(0f, 2f)] private float contrast = 0.8f;

    [Header("Manual UI Blur Exceptions")]
    [Tooltip("이 블러 배경에 함께 흐리게 담을 UI 루트입니다. 비워두면 모든 UI가 캡처에서 제외됩니다.")]
    [SerializeField] private GameObject[] blurredUiRoots = new GameObject[0];

    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");
    private static readonly int DarkenId = Shader.PropertyToID("_Darken");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    private static readonly Dictionary<GameObject, SourceRootHideState> HiddenSourceRoots =
        new Dictionary<GameObject, SourceRootHideState>();

    private Image backgroundImage;
    private bool originalBackgroundImageEnabled;
    private bool capturedBackgroundImageState;
    private bool cameraPauseRegistered;

    private GameObject blurCanvasObject;
    private RawImage blurGraphic;
    private Material runtimeMaterial;
    private readonly List<GameObject> hiddenSourceRoots = new List<GameObject>();
    private readonly List<GameObject> runtimeBlurredUiRoots = new List<GameObject>();
    private readonly List<GameObject> effectiveBlurredUiRoots = new List<GameObject>();

    private sealed class SourceRootHideState
    {
        public CanvasGroup CanvasGroup;
        public bool AddedCanvasGroup;
        public float OriginalAlpha;
        public bool OriginalInteractable;
        public bool OriginalBlocksRaycasts;
        public int RefCount;
    }

    public IReadOnlyList<GameObject> BlurredUiRoots => GetEffectiveBlurredUiRoots();

    public void SetRuntimeBlurredUiRoots(IEnumerable<GameObject> roots)
    {
        runtimeBlurredUiRoots.Clear();

        List<GameObject> validRoots = UIBlurBackgroundCaptureManager.GetValidBlurredUiRoots(roots);
        runtimeBlurredUiRoots.AddRange(validRoots);
    }

    public static UIBlurBackground EnsureForPanel(GameObject panelRoot)
    {
        if (panelRoot == null)
            return null;

        UIBlurBackground existing = panelRoot.GetComponentInChildren<UIBlurBackground>(true);
        if (existing != null)
            return existing;

        const string runtimeBackgroundName = "__AutoBlurBackground";
        Transform existingTransform = panelRoot.transform.Find(runtimeBackgroundName);
        GameObject backgroundObject;

        if (existingTransform != null)
        {
            backgroundObject = existingTransform.gameObject;
        }
        else
        {
            backgroundObject = new GameObject(
                runtimeBackgroundName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            backgroundObject.transform.SetParent(panelRoot.transform, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rectTransform = backgroundObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            Image image = backgroundObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
        }

        UIBlurBackground blur = backgroundObject.GetComponent<UIBlurBackground>();
        if (blur == null)
            blur = backgroundObject.AddComponent<UIBlurBackground>();

        return blur;
    }

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        CaptureOriginalBackgroundImageState();
        DisableOriginalBackgroundVisual();
        EnsureBlurCanvas();
        EnsureMaterial();
        ApplyRaycastSetting();
    }

    private void OnEnable()
    {
        RegisterCameraPause();
        CaptureOriginalBackgroundImageState();
        DisableOriginalBackgroundVisual();
        EnsureBlurCanvas();
        EnsureMaterial();
        ApplyRaycastSetting();
        ApplyBlurCanvasSorting();

        HideBlurCanvasForCapture();
        bool captured = CaptureAndRefreshBackground();
        ApplyMaterialProperties();
        ShowBlurCanvasAfterCapture();

        if (captured)
            HideBlurredUiSources();
    }

    private void OnDisable()
    {
        UnregisterCameraPause();
        RestoreBlurredUiSources();
        if (blurCanvasObject != null)
            blurCanvasObject.SetActive(false);

        RestoreOriginalBackgroundImageState();
    }

    private void OnDestroy()
    {
        UnregisterCameraPause();
        RestoreBlurredUiSources();
        RestoreOriginalBackgroundImageState();

        if (blurCanvasObject != null)
        {
            if (Application.isPlaying)
                Destroy(blurCanvasObject);
            else
                DestroyImmediate(blurCanvasObject);

            blurCanvasObject = null;
            blurGraphic = null;
        }

        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);

            runtimeMaterial = null;
        }
    }


    private void RegisterCameraPause()
    {
        if (cameraPauseRegistered)
            return;

        CameraMouseParallaxController.BeginUiPanelPause();
        cameraPauseRegistered = true;
    }

    private void UnregisterCameraPause()
    {
        if (!cameraPauseRegistered)
            return;

        CameraMouseParallaxController.EndUiPanelPause();
        cameraPauseRegistered = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        ApplyRaycastSetting();
        ApplyBlurCanvasSorting();
        ApplyMaterialProperties();
    }
#endif

    private void CaptureOriginalBackgroundImageState()
    {
        if (backgroundImage == null || capturedBackgroundImageState)
            return;

        originalBackgroundImageEnabled = backgroundImage.enabled;
        capturedBackgroundImageState = true;
    }

    private void DisableOriginalBackgroundVisual()
    {
        if (backgroundImage == null)
            return;

        // 기존 전체화면 Image는 직접 그리지 않습니다.
        // 어둡게 처리는 블러 셰이더의 _Darken 값이 담당합니다.
        backgroundImage.enabled = false;
        backgroundImage.raycastTarget = false;
    }

    private void RestoreOriginalBackgroundImageState()
    {
        if (backgroundImage == null || !capturedBackgroundImageState)
            return;

        backgroundImage.enabled = originalBackgroundImageEnabled;
        backgroundImage.raycastTarget = false;
        capturedBackgroundImageState = false;
    }

    private void EnsureBlurCanvas()
    {
        if (blurCanvasObject != null && blurGraphic != null)
        {
            ApplyBlurCanvasSorting();
            return;
        }

        // 이전 버전에서 이 오브젝트 아래에 생성했던 BlurGraphic이 남아 있으면 제거합니다.
        Transform oldChild = transform.Find(BlurGraphicName);
        if (oldChild != null)
        {
            if (Application.isPlaying)
                Destroy(oldChild.gameObject);
            else
                DestroyImmediate(oldChild.gameObject);
        }

        // 블러 화면은 패널의 RectTransform/Scale/애니메이션을 상속받으면 안 됩니다.
        // 독립된 Screen Space - Overlay Canvas를 만들어 화면 전체에 그대로 표시합니다.
        blurCanvasObject = new GameObject(
            $"{BlurGraphicName}_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        blurCanvasObject.transform.SetParent(null, false);

        Canvas blurCanvas = blurCanvasObject.GetComponent<Canvas>();
        blurCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blurCanvas.overrideSorting = true;
        blurCanvas.sortingOrder = BlurCanvasSortingOrder;

        CanvasScaler scaler = blurCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GameObject graphicObject = new GameObject(
            BlurGraphicName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        RectTransform blurRect = graphicObject.GetComponent<RectTransform>();
        blurRect.SetParent(blurCanvasObject.transform, false);
        blurRect.anchorMin = Vector2.zero;
        blurRect.anchorMax = Vector2.one;
        blurRect.pivot = new Vector2(0.5f, 0.5f);
        blurRect.offsetMin = Vector2.zero;
        blurRect.offsetMax = Vector2.zero;
        blurRect.localScale = Vector3.one;

        blurGraphic = graphicObject.GetComponent<RawImage>();
        blurGraphic.raycastTarget = false;
        blurGraphic.color = Color.white;
        blurGraphic.uvRect = new Rect(0f, 0f, 1f, 1f);

        SetLayerRecursively(blurCanvasObject, gameObject.layer);
    }

    private void ApplyBlurCanvasSorting()
    {
        if (blurCanvasObject == null)
            return;

        Canvas blurCanvas = blurCanvasObject.GetComponent<Canvas>();
        if (blurCanvas == null)
            return;

        if (blurCanvasObject.transform.parent != null)
            blurCanvasObject.transform.SetParent(null, false);

        blurCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blurCanvas.overrideSorting = true;
        blurCanvas.sortingOrder = BlurCanvasSortingOrder;
    }

    private void ApplyRaycastSetting()
    {
        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;

        if (blurGraphic != null)
            blurGraphic.raycastTarget = false;
    }

    private void EnsureMaterial()
    {
        if (blurGraphic == null)
            return;

        if (runtimeMaterial != null)
        {
            blurGraphic.material = runtimeMaterial;
            return;
        }

        Shader blurShader = Shader.Find(BlurShaderName);
        if (blurShader == null)
        {
            Debug.LogWarning(
                $"[UIBlurBackground] '{BlurShaderName}' 셰이더를 찾을 수 없습니다. " +
                "DustiumBackgroundBlur.shader가 프로젝트에 포함되어 있는지 확인해주세요.",
                this);
            return;
        }

        runtimeMaterial = new Material(blurShader)
        {
            name = "UIBlurBackground_RuntimeMaterial",
            hideFlags = HideFlags.DontSaveInBuild
        };

        blurGraphic.material = runtimeMaterial;
    }

    private bool CaptureAndRefreshBackground()
    {
        if (blurGraphic == null)
            return false;

        Texture captured = UIBlurBackgroundCaptureManager.CaptureBackgroundNow(GetEffectiveBlurredUiRoots());
        if (captured != null)
        {
            blurGraphic.texture = captured;
            return true;
        }

        return false;
    }

    private void HideBlurCanvasForCapture()
    {
        if (blurCanvasObject != null)
            blurCanvasObject.SetActive(false);
    }

    private void ShowBlurCanvasAfterCapture()
    {
        if (blurCanvasObject == null)
            return;

        blurCanvasObject.SetActive(true);

        if (blurGraphic == null)
            return;

        CanvasRenderer renderer = blurGraphic.canvasRenderer;
        if (renderer == null)
            return;

        renderer.SetAlpha(1f);
        renderer.cull = false;
    }

    private void HideBlurredUiSources()
    {
        RestoreBlurredUiSources();

        List<GameObject> validRoots = UIBlurBackgroundCaptureManager.GetValidBlurredUiRoots(GetEffectiveBlurredUiRoots());
        for (int i = 0; i < validRoots.Count; i++)
        {
            GameObject root = validRoots[i];
            if (root == null || !root.activeInHierarchy)
                continue;

            HideSourceRoot(root);
            hiddenSourceRoots.Add(root);
        }
    }

    private void RestoreBlurredUiSources()
    {
        for (int i = hiddenSourceRoots.Count - 1; i >= 0; i--)
        {
            GameObject root = hiddenSourceRoots[i];
            if (root == null)
                continue;

            if (!HiddenSourceRoots.TryGetValue(root, out SourceRootHideState state))
                continue;

            state.RefCount = Mathf.Max(0, state.RefCount - 1);
            if (state.RefCount > 0)
                continue;

            RestoreSourceRoot(root, state);
            HiddenSourceRoots.Remove(root);
        }

        hiddenSourceRoots.Clear();
    }

    private static void HideSourceRoot(GameObject root)
    {
        if (root == null)
            return;

        if (!HiddenSourceRoots.TryGetValue(root, out SourceRootHideState state))
        {
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            bool addedCanvasGroup = canvasGroup == null;

            if (canvasGroup == null)
                canvasGroup = root.AddComponent<CanvasGroup>();

            state = new SourceRootHideState
            {
                CanvasGroup = canvasGroup,
                AddedCanvasGroup = addedCanvasGroup,
                OriginalAlpha = canvasGroup.alpha,
                OriginalInteractable = canvasGroup.interactable,
                OriginalBlocksRaycasts = canvasGroup.blocksRaycasts
            };

            HiddenSourceRoots.Add(root, state);
        }

        state.RefCount++;
        ApplyHiddenSourceState(state.CanvasGroup);
    }

    private static void RestoreSourceRoot(GameObject root, SourceRootHideState state)
    {
        if (root == null || state == null)
            return;

        CanvasGroup canvasGroup = state.CanvasGroup;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = state.OriginalAlpha;
            canvasGroup.interactable = state.OriginalInteractable;
            canvasGroup.blocksRaycasts = state.OriginalBlocksRaycasts;
        }

        if (state.AddedCanvasGroup && canvasGroup != null)
        {
            if (Application.isPlaying)
                Destroy(canvasGroup);
            else
                DestroyImmediate(canvasGroup);
        }
    }

    private IReadOnlyList<GameObject> GetEffectiveBlurredUiRoots()
    {
        effectiveBlurredUiRoots.Clear();
        AppendValidBlurredUiRoots(blurredUiRoots, effectiveBlurredUiRoots);
        AppendValidBlurredUiRoots(runtimeBlurredUiRoots, effectiveBlurredUiRoots);
        AppendLobbyQuestPanelRoot(effectiveBlurredUiRoots);
        return effectiveBlurredUiRoots;
    }

    private void AppendLobbyQuestPanelRoot(List<GameObject> target)
    {
        if (target == null)
            return;

        Canvas ownerCanvas = GetComponentInParent<Canvas>();
        if (ownerCanvas == null)
            return;

        Transform questPanelTransform = ownerCanvas.transform.Find(
            "Resolution Viewport/PositionPanel/QuestPanel");

        if (questPanelTransform == null)
            return;

        GameObject questPanel = questPanelTransform.gameObject;
        if (!questPanel.activeInHierarchy || target.Contains(questPanel))
            return;

        target.Add(questPanel);
    }

    private static void AppendValidBlurredUiRoots(IEnumerable<GameObject> roots, List<GameObject> target)
    {
        if (roots == null || target == null)
            return;

        foreach (GameObject root in roots)
        {
            if (root == null || target.Contains(root))
                continue;

            target.Add(root);
        }
    }

    private static void ApplyHiddenSourceState(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ApplyMaterialProperties()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(BlurRadiusId, blurRadius);
        runtimeMaterial.SetFloat(DarkenId, darken);

        if (runtimeMaterial.HasProperty(SaturationId))
            runtimeMaterial.SetFloat(SaturationId, saturation);

        if (runtimeMaterial.HasProperty(ContrastId))
            runtimeMaterial.SetFloat(ContrastId, contrast);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }
}
