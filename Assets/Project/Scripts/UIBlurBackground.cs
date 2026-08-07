using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 반투명 검은 배경 Image에 붙이는 블러 컴포넌트입니다.
/// 기존 Image의 활성/비활성 타이밍과 Raycast 차단은 그대로 사용하고,
/// 내부에 블러 화면용 RawImage를 자동 생성합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UIBlurBackground : MonoBehaviour
{
    private const string BlurShaderName = "UI/DustiumBackgroundBlur";
    private const string BlurGraphicName = "BlurGraphic";

    [Header("Blur")]
    [SerializeField, Range(0f, 8f)] private float blurRadius = 1.6f;

    [Header("Darken")]
    [SerializeField, Range(0f, 1f)] private float darken = 0.2f;

    [Header("Input")]
    [Tooltip("기존 배경 Image가 뒤쪽 UI 입력을 막도록 유지합니다.")]
    [SerializeField] private bool blockRaycasts = true;

    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");
    private static readonly int DarkenId = Shader.PropertyToID("_Darken");

    private Image inputBlockImage;
    private RawImage blurGraphic;
    private Material runtimeMaterial;
    private bool registered;

    private void Awake()
    {
        inputBlockImage = GetComponent<Image>();
        ApplyRaycastSetting();
        EnsureBlurGraphic();
        EnsureMaterial();
        RefreshCapturedTexture();
    }

    private void OnEnable()
    {
        if (!registered)
        {
            UIBlurBackgroundCaptureManager.RegisterBlurPanel();
            registered = true;
        }

        ApplyRaycastSetting();
        EnsureBlurGraphic();
        EnsureMaterial();
        RefreshCapturedTexture();
        ApplyMaterialProperties();
    }

    private void OnDisable()
    {
        if (registered)
        {
            UIBlurBackgroundCaptureManager.UnregisterBlurPanel();
            registered = false;
        }
    }

    private void OnDestroy()
    {
        if (registered)
        {
            UIBlurBackgroundCaptureManager.UnregisterBlurPanel();
            registered = false;
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
        if (inputBlockImage == null)
            inputBlockImage = GetComponent<Image>();

        ApplyRaycastSetting();
        ApplyMaterialProperties();
    }
#endif

    private void ApplyRaycastSetting()
    {
        if (inputBlockImage != null)
            inputBlockImage.raycastTarget = blockRaycasts;
    }

    private void EnsureBlurGraphic()
    {
        if (blurGraphic != null)
            return;

        Transform existing = transform.Find(BlurGraphicName);
        if (existing != null)
            blurGraphic = existing.GetComponent<RawImage>();

        if (blurGraphic == null)
        {
            GameObject blurObject = new GameObject(
                BlurGraphicName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));

            RectTransform blurRect = blurObject.GetComponent<RectTransform>();
            blurRect.SetParent(transform, false);
            blurRect.anchorMin = Vector2.zero;
            blurRect.anchorMax = Vector2.one;
            blurRect.offsetMin = Vector2.zero;
            blurRect.offsetMax = Vector2.zero;
            blurRect.localScale = Vector3.one;
            blurRect.SetAsFirstSibling();

            blurGraphic = blurObject.GetComponent<RawImage>();
        }

        blurGraphic.raycastTarget = false;
        blurGraphic.color = Color.white;
        // ScreenCapture RenderTexture는 UI RawImage 기준으로 Y축이 뒤집혀 보일 수 있으므로 세로 UV를 반전합니다.
        blurGraphic.uvRect = new Rect(0f, 1f, 1f, -1f);
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
        ApplyMaterialProperties();
    }

    private void RefreshCapturedTexture()
    {
        if (blurGraphic == null)
            return;

        Texture captured = UIBlurBackgroundCaptureManager.CapturedTexture;
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
}
