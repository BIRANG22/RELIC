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
    private const string BlurCanvasNamePrefix = "UIBlurCanvas_";
    private const string BlurGraphicName = "BlurGraphic";
    private const int BlurCanvasSortingOrder = -32768;

    [Header("Blur")]
    [SerializeField, Range(0f, 8f)] private float blurRadius = 1.6f;

    [Header("Darken")]
    [SerializeField, Range(0f, 1f)] private float darken = 0.2f;

    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");
    private static readonly int DarkenId = Shader.PropertyToID("_Darken");

    private Image backgroundImage;
    private bool originalBackgroundImageEnabled;
    private bool capturedBackgroundImageState;

    private GameObject blurCanvasObject;
    private Canvas blurCanvas;
    private RawImage blurGraphic;
    private Material runtimeMaterial;

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
        CaptureOriginalBackgroundImageState();
        DisableOriginalBackgroundVisual();
        EnsureBlurCanvas();
        EnsureMaterial();
        ApplyRaycastSetting();
        ApplyBlurCanvasSorting();

        if (blurCanvasObject != null)
            blurCanvasObject.SetActive(true);

        CaptureAndRefreshBackground();
        ApplyMaterialProperties();
    }

    private void OnDisable()
    {
        if (blurCanvasObject != null)
            blurCanvasObject.SetActive(false);

        RestoreOriginalBackgroundImageState();
    }

    private void OnDestroy()
    {
        RestoreOriginalBackgroundImageState();

        if (blurCanvasObject != null)
        {
            if (Application.isPlaying)
                Destroy(blurCanvasObject);
            else
                DestroyImmediate(blurCanvasObject);

            blurCanvasObject = null;
            blurCanvas = null;
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
        if (blurCanvasObject != null && blurCanvas != null && blurGraphic != null)
            return;

        // 이전 버전에서 이 오브젝트 아래에 생성했던 BlurGraphic이 남아 있으면 제거합니다.
        Transform oldChild = transform.Find(BlurGraphicName);
        if (oldChild != null)
        {
            if (Application.isPlaying)
                Destroy(oldChild.gameObject);
            else
                DestroyImmediate(oldChild.gameObject);
        }

        blurCanvasObject = new GameObject(
            BlurCanvasNamePrefix + GetInstanceID(),
            typeof(RectTransform),
            typeof(Canvas));

        blurCanvas = blurCanvasObject.GetComponent<Canvas>();
        blurCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blurCanvas.overrideSorting = true;
        blurCanvas.sortingOrder = BlurCanvasSortingOrder;
        ApplyLowestSortingLayer();

        GameObject blurObject = new GameObject(
            BlurGraphicName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        RectTransform blurRect = blurObject.GetComponent<RectTransform>();
        blurRect.SetParent(blurCanvasObject.transform, false);
        blurRect.anchorMin = Vector2.zero;
        blurRect.anchorMax = Vector2.one;
        blurRect.pivot = new Vector2(0.5f, 0.5f);
        blurRect.offsetMin = Vector2.zero;
        blurRect.offsetMax = Vector2.zero;
        blurRect.localScale = Vector3.one;

        blurGraphic = blurObject.GetComponent<RawImage>();
        blurGraphic.raycastTarget = false;
        blurGraphic.color = Color.white;
        blurGraphic.uvRect = new Rect(0f, 0f, 1f, 1f);

        SetLayerRecursively(blurCanvasObject, LayerMask.NameToLayer("UI"));
    }

    private void ApplyBlurCanvasSorting()
    {
        if (blurCanvas == null)
            return;

        blurCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blurCanvas.overrideSorting = true;
        blurCanvas.sortingOrder = BlurCanvasSortingOrder;
        ApplyLowestSortingLayer();
    }

    private void ApplyLowestSortingLayer()
    {
        if (blurCanvas == null)
            return;

        SortingLayer[] sortingLayers = SortingLayer.layers;
        if (sortingLayers != null && sortingLayers.Length > 0)
            blurCanvas.sortingLayerID = sortingLayers[0].id;
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
            hideFlags = HideFlags.HideAndDontSave
        };

        blurGraphic.material = runtimeMaterial;
    }

    private void CaptureAndRefreshBackground()
    {
        if (blurGraphic == null)
            return;

        Texture captured = UIBlurBackgroundCaptureManager.CaptureBackgroundNow();
        if (captured != null)
            blurGraphic.texture = captured;
    }

    private void ApplyMaterialProperties()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(BlurRadiusId, blurRadius);
        runtimeMaterial.SetFloat(DarkenId, darken);
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
