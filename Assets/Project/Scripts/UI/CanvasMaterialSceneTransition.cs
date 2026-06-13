using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMaterialSceneTransition : Singleton<CanvasMaterialSceneTransition>
{
    [Header("Root")]
    [SerializeField] private GameObject transitionRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Canvas Sorting")]
    [SerializeField] private bool ensureRootCanvas = true;
    [SerializeField] private int canvasSortingOrder = 5000;

    [Header("Transition Graphics")]
    [SerializeField] private Graphic leftGraphic;
    [SerializeField] private Graphic rightGraphic;

    [Header("Materials")]
    [SerializeField] private Material leftMaterial;
    [SerializeField] private Material rightMaterial;

    [Header("Material Progress")]
    [SerializeField] private bool useMaterialProgress = true;
    [SerializeField] private string progressPropertyName = "_Progress";
    [SerializeField] private float openedValue = 0f;
    [SerializeField] private float closedValue = 1f;

    [Header("Position Transition")]
    [SerializeField] private bool usePositionTransition = true;
    [SerializeField] private RectTransform leftRectTransform;
    [SerializeField] private RectTransform rightRectTransform;
    [SerializeField] private Vector3 leftOpenedLocalPosition = new Vector3(-1500f, 0f, 0f);
    [SerializeField] private Vector3 leftClosedLocalPosition = new Vector3(-500f, 0f, 0f);
    [SerializeField] private Vector3 rightOpenedLocalPosition = new Vector3(1500f, 0f, 0f);
    [SerializeField] private Vector3 rightClosedLocalPosition = new Vector3(500f, 0f, 0f);

    [Header("Sound")]
    [SerializeField] private bool playTransitionSound = true;
    [SerializeField] private SfxType transitionSfx = SfxType.SceneTransition;
    [SerializeField] private float transitionSfxVolumeMultiplier = 1f;

    [Header("Timing")]
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closedHoldDuration = 0.05f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Material runtimeLeftMaterial;
    private Material runtimeRightMaterial;
    private bool isInitialized;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
        {
            return;
        }

        InitializeIfNeeded();
        SetOpenedImmediate();
        HideRoot();
    }

    public async Task PlayCloseAsync()
    {
        InitializeIfNeeded();

        if (isPlaying)
        {
            return;
        }

        isPlaying = true;
        ShowRoot();
        PlayTransitionSfx();
        await AnimateTransitionAsync(true, closeDuration, closeCurve);
        SetClosedImmediate();
    }

    public async Task PlayOpenAsync()
    {
        InitializeIfNeeded();

        ShowRoot();
        await AnimateTransitionAsync(false, openDuration, openCurve);
        SetOpenedImmediate();
        HideRoot();
        isPlaying = false;
    }

    public async Task HoldClosedAsync()
    {
        if (closedHoldDuration <= 0f)
        {
            return;
        }

        float elapsedTime = 0f;

        while (elapsedTime < closedHoldDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            await Task.Yield();
        }
    }

    public void SetOpenedImmediate()
    {
        InitializeIfNeeded();
        SetProgress(openedValue);
        SetPositionOpened();
    }

    public void SetClosedImmediate()
    {
        InitializeIfNeeded();
        SetProgress(closedValue);
        SetPositionClosed();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        if (transitionRoot == null)
        {
            transitionRoot = gameObject;
        }

        EnsureRootCanvas();

        if (canvasGroup == null)
        {
            canvasGroup = transitionRoot.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = transitionRoot.AddComponent<CanvasGroup>();
        }

        if (leftRectTransform == null && leftGraphic != null)
        {
            leftRectTransform = leftGraphic.rectTransform;
        }

        if (rightRectTransform == null && rightGraphic != null)
        {
            rightRectTransform = rightGraphic.rectTransform;
        }

        InitializeRuntimeMaterials();
        isInitialized = true;
    }

    private void EnsureRootCanvas()
    {
        if (!ensureRootCanvas)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortingOrder;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void InitializeRuntimeMaterials()
    {
        if (leftGraphic != null)
        {
            Material sourceMaterial = leftMaterial != null ? leftMaterial : leftGraphic.material;

            if (sourceMaterial != null)
            {
                runtimeLeftMaterial = new Material(sourceMaterial);
                leftGraphic.material = runtimeLeftMaterial;
            }
        }

        if (rightGraphic != null)
        {
            Material sourceMaterial = rightMaterial != null ? rightMaterial : rightGraphic.material;

            if (sourceMaterial != null)
            {
                runtimeRightMaterial = new Material(sourceMaterial);
                rightGraphic.material = runtimeRightMaterial;
            }
        }
    }

    private void PlayTransitionSfx()
    {
        if (!playTransitionSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(transitionSfx, transitionSfxVolumeMultiplier);
    }

    private async Task AnimateTransitionAsync(bool closing, float duration, AnimationCurve curve)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            if (closing)
            {
                SetProgress(Mathf.Lerp(openedValue, closedValue, curveValue));
                SetPositions(
                    Vector3.Lerp(leftOpenedLocalPosition, leftClosedLocalPosition, curveValue),
                    Vector3.Lerp(rightOpenedLocalPosition, rightClosedLocalPosition, curveValue)
                );
            }
            else
            {
                SetProgress(Mathf.Lerp(closedValue, openedValue, curveValue));
                SetPositions(
                    Vector3.Lerp(leftClosedLocalPosition, leftOpenedLocalPosition, curveValue),
                    Vector3.Lerp(rightClosedLocalPosition, rightOpenedLocalPosition, curveValue)
                );
            }

            await Task.Yield();
        }
    }

    private void SetProgress(float value)
    {
        if (!useMaterialProgress)
        {
            return;
        }

        SetMaterialProgress(runtimeLeftMaterial, value);
        SetMaterialProgress(runtimeRightMaterial, value);
    }

    private void SetMaterialProgress(Material material, float value)
    {
        if (material == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(progressPropertyName) && material.HasProperty(progressPropertyName))
        {
            material.SetFloat(progressPropertyName, value);
        }
    }

    private void SetPositionOpened()
    {
        SetPositions(leftOpenedLocalPosition, rightOpenedLocalPosition);
    }

    private void SetPositionClosed()
    {
        SetPositions(leftClosedLocalPosition, rightClosedLocalPosition);
    }

    private void SetPositions(Vector3 leftPosition, Vector3 rightPosition)
    {
        if (!usePositionTransition)
        {
            return;
        }

        if (leftRectTransform != null)
        {
            leftRectTransform.localPosition = leftPosition;
        }

        if (rightRectTransform != null)
        {
            rightRectTransform.localPosition = rightPosition;
        }
    }

    private void ShowRoot()
    {
        if (transitionRoot != null)
        {
            transitionRoot.SetActive(true);
            transitionRoot.transform.SetAsLastSibling();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        Canvas.ForceUpdateCanvases();
    }

    private void HideRoot()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (transitionRoot != null)
        {
            transitionRoot.SetActive(false);
        }
    }
}
