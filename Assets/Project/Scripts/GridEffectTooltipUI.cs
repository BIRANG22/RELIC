using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class GridEffectTooltipUI : MonoBehaviour
{
    private static GridEffectTooltipUI instance;

    [Header("References")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text nameText;

    [FormerlySerializedAs("descriptionText")]
    [SerializeField] private TMP_Text toolTipText;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new(24f, -24f);
    [SerializeField] private Vector2 screenPadding = new(16f, 16f);
    [SerializeField] private bool followMouse = true;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.1f;

    private Canvas rootCanvas;
    private Camera canvasCamera;
    private Object currentOwner;
    private Vector2 lastScreenPosition;
    private Coroutine fadeCoroutine;
    private bool targetVisible;
    private bool useWorldAnchor;
    private Vector3 lastWorldAnchor;
    private Vector3 lastWorldLeftAnchor;
    private Vector3 lastWorldRightAnchor;
    private bool hasWorldSideAnchors;
    private Camera worldAnchorCamera;

    /// <summary>
    /// 씬에 사용자가 직접 배치한 GridEffectTooltipUI를 반환합니다.
    /// UI를 런타임에 자동 생성하지 않습니다.
    /// </summary>
    public static GridEffectTooltipUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<GridEffectTooltipUI>(FindObjectsInactive.Include);

        if (instance != null)
        {
            instance.InitializeReferences();
            return instance;
        }

        Debug.LogWarning(
            "[GridEffectTooltipUI] 씬에서 GridEffectTooltipUI를 찾을 수 없습니다. " +
            "전투 UI에 GridEffectTooltipUI 오브젝트를 만들고 스크립트를 연결해 주세요.");

        return null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "[GridEffectTooltipUI] 씬에 GridEffectTooltipUI가 여러 개 있습니다. " +
                "하나만 사용해 주세요.",
                this);
            return;
        }

        instance = this;
        InitializeReferences();
        SetVisibleImmediate(false);
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
        {
            if (currentOwner != null || (canvasGroup != null && canvasGroup.alpha > 0f))
            {
                currentOwner = null;
                SetVisibleImmediate(false);
            }

            return;
        }

        if (currentOwner == null)
        {
            if (canvasGroup != null && canvasGroup.alpha > 0f)
                SetVisible(false);

            return;
        }

        if (useWorldAnchor)
        {
            UpdateWorldAnchorPosition();
        }
        else if (followMouse)
        {
            SetPosition(Input.mousePosition);
        }
        else
        {
            SetPosition(lastScreenPosition);
        }
    }

    public void Show(Object owner, string gridEffectId, Vector2 screenPosition)
    {
        if (owner == null || string.IsNullOrWhiteSpace(gridEffectId))
            return;

        GridEffectDatabase database = DataManager.Instance?.GridEffectDatabase;

        if (database == null ||
            !database.TryGet(gridEffectId.Trim(), out GridEffectData data) ||
            data == null)
        {
            Hide(owner);
            return;
        }

        Show(owner, data, screenPosition, null);
    }

    public void Show(Object owner, GridEffectData data, Vector2 screenPosition)
    {
        Show(owner, data, screenPosition, null);
    }

    public void Show(Object owner, GridEffectData data, Vector2 screenPosition, int? remainingDuration)
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
        {
            currentOwner = null;
            SetVisibleImmediate(false);
            return;
        }

        if (owner == null || data == null)
            return;

        InitializeReferences();

        if (nameText == null || toolTipText == null)
        {
            Debug.LogWarning(
                "[GridEffectTooltipUI] Name Text 또는 Tool Tip Text가 연결되지 않았습니다. " +
                "Inspector에서 직접 연결해 주세요.",
                this);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        currentOwner = owner;
        useWorldAnchor = false;
        hasWorldSideAnchors = false;
        worldAnchorCamera = null;
        lastScreenPosition = screenPosition;

        nameText.text = GameDataLocalization.GridEffectName(data);
        toolTipText.text = FormatToolTip(data, remainingDuration);

        BringToFront();
        SetVisible(true);
        SetPosition(screenPosition);
    }

    private static string FormatToolTip(GridEffectData data, int? remainingDuration)
    {
        if (data == null || string.IsNullOrEmpty(data.ToolTip))
            return string.Empty;

        string result = GameDataLocalization.GridEffectTooltip(data);
        string valueRate = data.ValueRate.ToString();
        int duration = remainingDuration ?? data.Duration;

        // GridEffect GameData 툴팁의 플레이스홀더를 실제 수치로 치환합니다.
        // <br> 같은 TMP Rich Text 태그는 건드리지 않습니다.
        result = result.Replace("{ValueRate}", valueRate);
        result = result.Replace("{ValueRate1}", valueRate);
        result = result.Replace("{Duration}", Mathf.Max(0, duration).ToString());
        return result;
    }

    public void Hide(Object owner)
    {
        if (currentOwner != null && owner != null && currentOwner != owner)
            return;

        currentOwner = null;
        useWorldAnchor = false;
        hasWorldSideAnchors = false;
        worldAnchorCamera = null;
        SetVisible(false);
    }

    /// <summary>
    /// 월드 오브젝트 기준으로 툴팁을 표시합니다.
    /// 월드 좌표를 매 프레임 화면 좌표로 다시 변환하므로 해상도나 화면 비율이 바뀌어도
    /// 그리드 이펙트와 툴팁 사이의 UI 오프셋이 동일하게 유지됩니다.
    /// </summary>
    public void ShowWorld(Object owner, GridEffectData data, Vector3 worldAnchor, Camera sourceCamera, int? remainingDuration = null)
    {
        if (sourceCamera == null)
            return;

        Vector2 initialScreenPosition = sourceCamera.WorldToScreenPoint(worldAnchor);
        Show(owner, data, initialScreenPosition, remainingDuration);

        if (currentOwner != owner)
            return;

        useWorldAnchor = true;
        hasWorldSideAnchors = false;
        lastWorldAnchor = worldAnchor;
        worldAnchorCamera = sourceCamera;
        UpdateWorldAnchorPosition();
    }

    /// <summary>
    /// 월드 오브젝트의 좌/우 기준점을 함께 전달합니다.
    /// 기본적으로 오른쪽 기준점에 표시하고, 오른쪽 배치 시 화면 밖으로 나가면
    /// 자동으로 왼쪽 기준점으로 전환합니다.
    /// </summary>
    public void ShowWorldAutoSide(
        Object owner,
        GridEffectData data,
        Vector3 leftWorldAnchor,
        Vector3 rightWorldAnchor,
        Camera sourceCamera,
        int? remainingDuration = null)
    {
        if (sourceCamera == null)
            return;

        Vector2 initialScreenPosition = sourceCamera.WorldToScreenPoint(rightWorldAnchor);
        Show(owner, data, initialScreenPosition, remainingDuration);

        if (currentOwner != owner)
            return;

        useWorldAnchor = true;
        hasWorldSideAnchors = true;
        lastWorldLeftAnchor = leftWorldAnchor;
        lastWorldRightAnchor = rightWorldAnchor;
        lastWorldAnchor = rightWorldAnchor;
        worldAnchorCamera = sourceCamera;
        UpdateWorldAnchorPosition();
    }

    public void SetWorldAnchor(Vector3 worldAnchor, Camera sourceCamera)
    {
        if (sourceCamera == null)
            return;

        useWorldAnchor = true;
        hasWorldSideAnchors = false;
        lastWorldAnchor = worldAnchor;
        worldAnchorCamera = sourceCamera;
        UpdateWorldAnchorPosition();
    }

    public void SetWorldSideAnchors(Vector3 leftWorldAnchor, Vector3 rightWorldAnchor, Camera sourceCamera)
    {
        if (sourceCamera == null)
            return;

        useWorldAnchor = true;
        hasWorldSideAnchors = true;
        lastWorldLeftAnchor = leftWorldAnchor;
        lastWorldRightAnchor = rightWorldAnchor;
        lastWorldAnchor = rightWorldAnchor;
        worldAnchorCamera = sourceCamera;
        UpdateWorldAnchorPosition();
    }

    public void SetPosition(Vector2 screenPosition)
    {
        useWorldAnchor = false;
        SetPositionFromScreen(screenPosition, false);
    }

    private void UpdateWorldAnchorPosition()
    {
        if (!useWorldAnchor || worldAnchorCamera == null)
            return;

        if (hasWorldSideAnchors)
        {
            SetPositionFromWorldSideAnchors();
            return;
        }

        Vector3 screen = worldAnchorCamera.WorldToScreenPoint(lastWorldAnchor);
        if (screen.z < 0f)
            return;

        SetPositionFromScreen(new Vector2(screen.x, screen.y), true);
    }

    private void SetPositionFromWorldSideAnchors()
    {
        InitializeReferences();

        if (tooltipRect == null || worldAnchorCamera == null)
            return;

        RectTransform parentRect = tooltipRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3 rightScreen3 = worldAnchorCamera.WorldToScreenPoint(lastWorldRightAnchor);
        Vector3 leftScreen3 = worldAnchorCamera.WorldToScreenPoint(lastWorldLeftAnchor);
        if (rightScreen3.z < 0f || leftScreen3.z < 0f)
            return;

        Vector2 rightScreen = new(rightScreen3.x, rightScreen3.y);
        Vector2 leftScreen = new(leftScreen3.x, leftScreen3.y);

        // screenOffset.x는 좌우 간격, screenOffset.y는 세로 오프셋으로 사용합니다.
        float horizontalGap = Mathf.Abs(screenOffset.x);
        float verticalOffset = screenOffset.y;

        // 1) 기본적으로 오른쪽에 배치합니다. 툴팁의 실제 왼쪽 외곽을
        //    GridEffect 오른쪽 기준점 + 간격에 맞춥니다.
        PlaceTooltipEdgeAtScreenPoint(
            parentRect,
            uiCamera,
            rightScreen,
            horizontalGap,
            verticalOffset,
            placeOnRight: true);

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        GetTooltipScreenBounds(uiCamera, out float minX, out _, out float maxX, out _);
        float paddingX = Mathf.Max(0f, screenPadding.x);

        // 2) 오른쪽 외곽이 화면을 넘으려 할 때만 왼쪽으로 전환합니다.
        if (maxX > Screen.width - paddingX)
        {
            PlaceTooltipEdgeAtScreenPoint(
                parentRect,
                uiCamera,
                leftScreen,
                horizontalGap,
                verticalOffset,
                placeOnRight: false);
        }

        // 3) 좌/우 전환 후에도 매우 좁은 화면에서 남는 초과분만 마지막으로 보정합니다.
        ClampRenderedRectToScreen(parentRect, uiCamera);
    }

    private void PlaceTooltipEdgeAtScreenPoint(
        RectTransform parentRect,
        Camera uiCamera,
        Vector2 anchorScreen,
        float horizontalGap,
        float verticalOffset,
        bool placeOnRight)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                anchorScreen,
                uiCamera,
                out Vector2 anchorLocal))
        {
            return;
        }

        Vector3 localPosition = tooltipRect.localPosition;
        localPosition.x = anchorLocal.x;
        localPosition.y = anchorLocal.y;
        tooltipRect.localPosition = localPosition;

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        GetTooltipScreenBounds(uiCamera, out float minX, out _, out float maxX, out _);

        float targetEdgeX = placeOnRight
            ? anchorScreen.x + horizontalGap
            : anchorScreen.x - horizontalGap;
        float currentEdgeX = placeOnRight ? minX : maxX;
        float deltaX = targetEdgeX - currentEdgeX;

        Vector2 currentPivotScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, tooltipRect.position);
        Vector2 correctedPivotScreen = new(
            currentPivotScreen.x + deltaX,
            anchorScreen.y + verticalOffset);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                correctedPivotScreen,
                uiCamera,
                out Vector2 correctedLocal))
        {
            return;
        }

        localPosition = tooltipRect.localPosition;
        localPosition.x = correctedLocal.x;
        localPosition.y = correctedLocal.y;
        tooltipRect.localPosition = localPosition;
    }

    private void GetTooltipScreenBounds(
        Camera uiCamera,
        out float minX,
        out float minY,
        out float maxX,
        out float maxY)
    {
        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        minX = float.MaxValue;
        minY = float.MaxValue;
        maxX = float.MinValue;
        maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            minX = Mathf.Min(minX, screenCorner.x);
            minY = Mathf.Min(minY, screenCorner.y);
            maxX = Mathf.Max(maxX, screenCorner.x);
            maxY = Mathf.Max(maxY, screenCorner.y);
        }
    }

    private void SetPositionFromScreen(Vector2 screenPosition, bool offsetInCanvasUnits)
    {
        InitializeReferences();

        if (tooltipRect == null)
            return;

        RectTransform parentRect = tooltipRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        lastScreenPosition = screenPosition;

        Vector2 pointToConvert = offsetInCanvasUnits
            ? screenPosition
            : screenPosition + screenOffset;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                pointToConvert,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        if (offsetInCanvasUnits)
            localPoint += screenOffset;

        Vector3 localPosition = tooltipRect.localPosition;
        localPosition.x = localPoint.x;
        localPosition.y = localPoint.y;
        tooltipRect.localPosition = localPosition;

        ClampRenderedRectToScreen(parentRect, uiCamera);
    }

    private void ClampRenderedRectToScreen(RectTransform parentRect, Camera uiCamera)
    {
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            minX = Mathf.Min(minX, screenCorner.x);
            minY = Mathf.Min(minY, screenCorner.y);
            maxX = Mathf.Max(maxX, screenCorner.x);
            maxY = Mathf.Max(maxY, screenCorner.y);
        }

        float paddingX = Mathf.Max(0f, screenPadding.x);
        float paddingY = Mathf.Max(0f, screenPadding.y);
        float deltaX = 0f;
        float deltaY = 0f;

        if (minX < paddingX)
            deltaX = paddingX - minX;
        else if (maxX > Screen.width - paddingX)
            deltaX = (Screen.width - paddingX) - maxX;

        if (minY < paddingY)
            deltaY = paddingY - minY;
        else if (maxY > Screen.height - paddingY)
            deltaY = (Screen.height - paddingY) - maxY;

        if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f))
            return;

        Vector2 currentScreenPivot = RectTransformUtility.WorldToScreenPoint(uiCamera, tooltipRect.position);
        Vector2 correctedScreenPivot = currentScreenPivot + new Vector2(deltaX, deltaY);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                correctedScreenPivot,
                uiCamera,
                out Vector2 correctedLocalPoint))
        {
            return;
        }

        Vector3 correctedPosition = tooltipRect.localPosition;
        correctedPosition.x = correctedLocalPoint.x;
        correctedPosition.y = correctedLocalPoint.y;
        tooltipRect.localPosition = correctedPosition;
    }

    private void InitializeReferences()
    {
        // Inspector에서 연결한 값은 절대 교체하지 않습니다.
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        canvasCamera = rootCanvas != null ? rootCanvas.worldCamera : null;
    }

    private void BringToFront()
    {
        if (tooltipRect != null)
            tooltipRect.SetAsLastSibling();
    }

    private void SetVisible(bool visible)
    {
        InitializeReferences();

        if (canvasGroup == null)
            return;

        if (targetVisible == visible && fadeCoroutine != null)
            return;

        targetVisible = visible;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (visible && !gameObject.activeSelf)
            gameObject.SetActive(true);

        // 비활성화된 GameObject에서는 코루틴을 시작할 수 없습니다.
        // 그리드 효과 제거 과정에서 HoverTarget.OnDisable()이 Hide()를 다시 호출할 수 있으므로,
        // 이미 Hierarchy에서 비활성화된 상태라면 즉시 상태만 정리합니다.
        if (!gameObject.activeInHierarchy)
        {
            SetVisibleImmediate(visible);
            return;
        }

        if (fadeDuration <= 0f)
        {
            SetVisibleImmediate(visible);
            return;
        }

        fadeCoroutine = StartCoroutine(FadeVisibilityRoutine(visible));
    }

    private IEnumerator FadeVisibilityRoutine(bool visible)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float targetAlpha = visible ? 1f : 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;

        fadeCoroutine = null;

        if (!visible && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void SetVisibleImmediate(bool visible)
    {
        targetVisible = visible;
        InitializeReferences();

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!visible && Application.isPlaying && gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
