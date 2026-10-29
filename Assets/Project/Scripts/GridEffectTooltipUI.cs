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
        EnsureInlineIcons(toolTipText);

        BringToFront();
        SetVisible(true);
        SetPosition(screenPosition);
    }

    private static void EnsureInlineIcons(TMP_Text text)
    {
        if (text == null)
            return;

        SkillEffectInlineIconRenderer renderer = text.GetComponent<SkillEffectInlineIconRenderer>();
        if (renderer == null)
            renderer = text.gameObject.AddComponent<SkillEffectInlineIconRenderer>();

        renderer.Bind(text);
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
        // GridEffect 설명의 {E_...} 토큰도 게임데이터 효과 이름으로 치환합니다.
        return SkillDescriptionFormatter.ReplaceEffectTokens(result);
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

        SetPositionFromWorldCenter(new Vector2(screen.x, screen.y));
    }

    private void SetPositionFromWorldSideAnchors()
    {
        if (worldAnchorCamera == null)
            return;

        // 좌/우 경계가 아닌 그리드 이펙트의 정중앙을 기준으로 위치를 계산합니다.
        Vector3 center = (lastWorldLeftAnchor + lastWorldRightAnchor) * 0.5f;
        Vector3 screen = worldAnchorCamera.WorldToScreenPoint(center);
        if (screen.z < 0f)
            return;

        SetPositionFromWorldCenter(new Vector2(screen.x, screen.y));
    }

    private void SetPositionFromWorldCenter(Vector2 centerScreen)
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

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, centerScreen, uiCamera, out Vector2 centerLocal))
            return;

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        // 인스펙터 오프셋은 캔버스 로컬 단위로 적용합니다.
        // 화면 경계를 벗어나는 축만 오프셋 방향을 뒤집습니다.
        Vector2 offset = screenOffset;
        Vector2 bestOffset = offset;
        float bestOverflow = float.PositiveInfinity;
        for (int candidate = 0; candidate < 4; candidate++)
        {
            Vector2 candidateOffset = new Vector2(
                candidate == 1 || candidate == 3 ? -offset.x : offset.x,
                candidate == 2 || candidate == 3 ? -offset.y : offset.y);

            PlaceTooltipCenter(parentRect, uiCamera, centerLocal + candidateOffset);
            GetTooltipScreenBounds(uiCamera, out float minX, out float minY, out float maxX, out float maxY);
            float paddingX = Mathf.Max(0f, screenPadding.x);
            float paddingY = Mathf.Max(0f, screenPadding.y);
            float overflow = Mathf.Max(0f, paddingX - minX)
                           + Mathf.Max(0f, maxX - (Screen.width - paddingX))
                           + Mathf.Max(0f, paddingY - minY)
                           + Mathf.Max(0f, maxY - (Screen.height - paddingY));
            if (overflow < bestOverflow)
            {
                bestOverflow = overflow;
                bestOffset = candidateOffset;
            }
            if (overflow <= 0.01f)
                break;
        }

        PlaceTooltipCenter(parentRect, uiCamera, centerLocal + bestOffset);
        // 두 방향 모두 공간이 부족한 작은 화면에서만 마지막 경계 보정이 발생합니다.
        if (bestOverflow > 0.01f)
            ClampRenderedRectToScreen(parentRect, uiCamera);
    }

    private void PlaceTooltipCenter(RectTransform parentRect, Camera uiCamera, Vector2 targetLocalCenter)
    {
        // RectTransform의 Pivot 설정과 무관하게 보이는 사각형의 중앙을 맞춥니다.
        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector3 targetWorld = parentRect.TransformPoint(new Vector3(targetLocalCenter.x, targetLocalCenter.y, 0f));
        tooltipRect.position += targetWorld - worldCenter;
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
