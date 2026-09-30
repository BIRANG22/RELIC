using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitStatusEffectTooltipSide
{
    Right,
    Left
}

/// <summary>
/// 상태효과 아이콘용 단일 툴팁입니다.
/// 스킬 툴팁처럼 데이터 설정 -> 표시 -> 마우스 추적 -> 숨김만 처리합니다.
/// 씬에 미리 배치한 UnitStatusEffectTooltipItemUI 하나를 재사용하며 Instantiate/Destroy하지 않습니다.
/// </summary>
public class UnitStatusEffectTooltipUI : MonoBehaviour
{
    private static UnitStatusEffectTooltipUI instance;

    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform itemRoot;
    [Tooltip("EffectRoot 아래에 미리 배치한 UnitStatusEffectTooltipItemUI입니다. 비워두면 비활성 오브젝트까지 포함해 자동으로 찾습니다.")]
    [SerializeField] private UnitStatusEffectTooltipItemUI itemView;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, 0f);
    [SerializeField, Min(0f)] private float screenPadding = 12f;
    [SerializeField] private bool followMousePosition = true;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.12f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.05f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Sorting")]
    [SerializeField] private bool forceTooltipToFront = true;
    [SerializeField] private int sortingOrderOffset = 20;

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Canvas tooltipCanvas;
    private Coroutine fadeCoroutine;
    private Object currentOwner;
    private bool isVisible;
    private UnitStatusEffectTooltipSide currentSide = UnitStatusEffectTooltipSide.Right;

    public static UnitStatusEffectTooltipUI GetOrCreate()
    {
        if (instance != null)
        {
            instance.InitializeIfNeeded();
            return instance;
        }

        UnitStatusEffectTooltipUI[] tooltips = FindObjectsByType<UnitStatusEffectTooltipUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (tooltips == null || tooltips.Length == 0)
            return null;

        for (int i = 0; i < tooltips.Length; i++)
        {
            if (tooltips[i] == null)
                continue;

            instance = tooltips[i];
            instance.InitializeIfNeeded();
            return instance;
        }

        return null;
    }

    private void Awake()
    {
        instance = this;
        InitializeIfNeeded();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        if (!isVisible || !followMousePosition)
            return;

        Vector2 mousePosition = Input.mousePosition;
        UnitStatusEffectTooltipSide side = GetSideForScreenPosition(mousePosition);
        UpdatePosition(mousePosition, side);
    }

    public void Show(
        Object owner,
        IReadOnlyList<StatusEffectRuntimeData> statusEffects,
        Vector2 screenPosition)
    {
        Show(owner, statusEffects, screenPosition, GetSideForScreenPosition(screenPosition));
    }

    public void Show(
        Object owner,
        IReadOnlyList<StatusEffectRuntimeData> statusEffects,
        Vector2 screenPosition,
        UnitStatusEffectTooltipSide side)
    {
        InitializeIfNeeded();

        StatusEffectRuntimeData data = GetFirstValidStatusEffect(statusEffects);
        if (data == null || itemView == null)
        {
            Hide(owner);
            return;
        }

        currentOwner = owner;
        currentSide = side;

        itemView.Set(data);
        if (!itemView.gameObject.activeSelf)
            itemView.gameObject.SetActive(true);

        BringToFront();

        Vector2 position = followMousePosition
            ? (Vector2)Input.mousePosition
            : screenPosition;
        UnitStatusEffectTooltipSide positionSide = followMousePosition
            ? GetSideForScreenPosition(position)
            : side;

        UpdatePosition(position, positionSide);
        ShowWithFade();
    }

    public void Hide(Object owner)
    {
        if (currentOwner != null && owner != null && currentOwner != owner)
            return;

        currentOwner = null;
        HideWithFade();
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        UpdatePosition(screenPosition, GetSideForScreenPosition(screenPosition));
    }

    public void UpdatePosition(Vector2 screenPosition, UnitStatusEffectTooltipSide side)
    {
        InitializeIfNeeded();

        if (panelRect == null || canvasRect == null)
            return;

        currentSide = side;

        Camera uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 panelSize = panelRect.rect.size;
        if (panelSize.x <= 0f)
            panelSize.x = Mathf.Abs(panelRect.sizeDelta.x);
        if (panelSize.y <= 0f)
            panelSize.y = Mathf.Abs(panelRect.sizeDelta.y);

        Vector2 pivot = panelRect.pivot;
        float horizontalGap = Mathf.Abs(screenOffset.x);
        Vector2 target = localPoint;

        if (side == UnitStatusEffectTooltipSide.Left)
        {
            float targetRight = localPoint.x - horizontalGap;
            target.x = targetRight - panelSize.x * (1f - pivot.x);
        }
        else
        {
            float targetLeft = localPoint.x + horizontalGap;
            target.x = targetLeft + panelSize.x * pivot.x;
        }

        target.y = localPoint.y + screenOffset.y;

        Rect bounds = canvasRect.rect;
        float minX = bounds.xMin + screenPadding + panelSize.x * pivot.x;
        float maxX = bounds.xMax - screenPadding - panelSize.x * (1f - pivot.x);
        float minY = bounds.yMin + screenPadding + panelSize.y * pivot.y;
        float maxY = bounds.yMax - screenPadding - panelSize.y * (1f - pivot.y);

        if (minX <= maxX)
            target.x = Mathf.Clamp(target.x, minX, maxX);
        if (minY <= maxY)
            target.y = Mathf.Clamp(target.y, minY, maxY);

        panelRect.anchoredPosition = target;
    }

    private void InitializeIfNeeded()
    {
        if (panelRect == null)
            panelRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (itemRoot == null && panelRect != null)
        {
            Transform effectRoot = panelRect.Find("EffectRoot");
            if (effectRoot != null)
                itemRoot = effectRoot as RectTransform;
        }

        if (itemView == null)
        {
            Transform searchRoot = itemRoot != null ? itemRoot : transform;
            itemView = searchRoot.GetComponentInChildren<UnitStatusEffectTooltipItemUI>(true);
        }

        rootCanvas = FindRootCanvasForPosition();
        canvasRect = rootCanvas != null
            ? rootCanvas.transform as RectTransform
            : transform.parent as RectTransform;

        EnsureTooltipCanvas();
    }

    private Canvas FindRootCanvasForPosition()
    {
        Canvas[] parents = GetComponentsInParent<Canvas>(true);
        if (parents == null || parents.Length == 0)
            return GetComponent<Canvas>();

        for (int i = parents.Length - 1; i >= 0; i--)
        {
            Canvas canvas = parents[i];
            if (canvas != null && canvas != tooltipCanvas)
                return canvas;
        }

        return parents[parents.Length - 1];
    }

    private void EnsureTooltipCanvas()
    {
        if (!forceTooltipToFront)
            return;

        tooltipCanvas = GetComponent<Canvas>();
        if (tooltipCanvas == null)
            tooltipCanvas = gameObject.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();

        if (!forceTooltipToFront)
            return;

        EnsureTooltipCanvas();
        if (tooltipCanvas == null)
            return;

        int highest = 0;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == tooltipCanvas)
                continue;

            highest = Mathf.Max(highest, canvas.sortingOrder);
        }

        tooltipCanvas.sortingOrder = highest + Mathf.Max(1, sortingOrderOffset);
    }

    private void ShowWithFade()
    {
        isVisible = true;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        gameObject.SetActive(true);

        if (fadeInDuration <= 0f)
        {
            canvasGroup.alpha = 1f;
            fadeCoroutine = null;
            return;
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(1f, fadeInDuration, false));
    }

    private void HideWithFade()
    {
        isVisible = false;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (canvasGroup == null || canvasGroup.alpha <= 0f || fadeOutDuration <= 0f)
        {
            HideImmediate();
            return;
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(0f, fadeOutDuration, true));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool hideItemAfter)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;

        if (hideItemAfter && itemView != null)
            itemView.gameObject.SetActive(false);

        fadeCoroutine = null;
    }

    private void HideImmediate()
    {
        isVisible = false;
        currentOwner = null;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (itemView != null)
            itemView.gameObject.SetActive(false);
    }

    private static StatusEffectRuntimeData GetFirstValidStatusEffect(
        IReadOnlyList<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null)
            return null;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData data = statusEffects[i];
            if (data != null && data.IsValid())
                return data;
        }

        return null;
    }

    private static UnitStatusEffectTooltipSide GetSideForScreenPosition(Vector2 screenPosition)
    {
        return screenPosition.x >= Screen.width * 0.5f
            ? UnitStatusEffectTooltipSide.Left
            : UnitStatusEffectTooltipSide.Right;
    }
}
