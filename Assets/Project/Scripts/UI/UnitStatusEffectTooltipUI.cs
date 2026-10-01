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
/// UnitStatusEffectTooltipUI 오브젝트 자체는 고정하고,
/// EffectRoot 아래의 UnitStatusEffectTooltipItemUI만 마우스를 따라 이동합니다.
/// </summary>
public class UnitStatusEffectTooltipUI : MonoBehaviour
{
    private static UnitStatusEffectTooltipUI instance;

    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform itemRoot;
    [Tooltip("EffectRoot 아래에 미리 배치한 UnitStatusEffectTooltipItemUI입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private UnitStatusEffectTooltipItemUI itemView;

    [Header("Monster Status Tooltip Prefab")]
    [Tooltip("월드 몬스터 호버 시 EffectRoot 아래에 생성할 UnitStatusEffectTooltipItemUI 프리팹입니다.")]
    [SerializeField] private UnitStatusEffectTooltipItemUI monsterItemPrefab;
    [Tooltip("몬스터 상태효과 툴팁 항목 사이의 세로 간격입니다.")]
    [SerializeField, Min(0f)] private float monsterItemVerticalSpacing = 8f;

    [Header("Status Tooltip Position")]
    [SerializeField] private float statusTooltipCursorOffsetX = 215f;
    [SerializeField] private float statusTooltipCursorOffsetY = 70f;
    [SerializeField, Min(0f)] private float statusTooltipScreenPaddingX = 175f;
    [SerializeField, Min(0f)] private float statusTooltipScreenPaddingY = 30f;
    [SerializeField] private bool followMousePosition = true;

    [Header("Status Tooltip Fade")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.12f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.05f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Sorting")]
    [SerializeField] private bool forceTooltipToFront = true;
    [SerializeField] private int sortingOrderOffset = 20;

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Canvas tooltipCanvas;
    private RectTransform itemRect;
    private RectTransform itemParentRect;
    private Coroutine fadeCoroutine;
    private Object currentOwner;
    private bool isVisible;
    private UnitStatusEffectTooltipSide currentSide = UnitStatusEffectTooltipSide.Right;
    private Object monsterPrefabOwner;
    private readonly List<UnitStatusEffectTooltipItemUI> monsterItemInstances = new();
    private readonly List<RectTransform> monsterItemRects = new();

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
        DestroyMonsterPrefabImmediate();

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


    /// <summary>
    /// 월드 몬스터 호버 전용 표시입니다.
    /// 씬에 미리 배치된 itemView는 사용하지 않고,
    /// 유효한 상태효과 개수만큼 monsterItemPrefab을 EffectRoot 아래에 생성합니다.
    /// </summary>
    public void ShowMonsterPrefab(
        Object owner,
        IReadOnlyList<StatusEffectRuntimeData> statusEffects,
        Vector2 rightScreenPosition,
        Vector2 leftScreenPosition)
    {
        InitializeIfNeeded();

        if (monsterItemPrefab == null || itemRoot == null)
        {
            HideMonsterPrefab(owner);
            return;
        }

        List<StatusEffectRuntimeData> validEffects = GetValidStatusEffects(statusEffects);
        if (validEffects.Count <= 0)
        {
            HideMonsterPrefab(owner);
            return;
        }

        if (monsterPrefabOwner != owner)
            DestroyMonsterPrefabImmediate();

        monsterPrefabOwner = owner;

        // 상태효과 수나 내용이 달라졌을 수 있으므로 몬스터 호버 시작 시 목록을 다시 구성합니다.
        RebuildMonsterPrefabItems(validEffects);

        BringToFront();
        gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        UpdateMonsterPrefabPosition(owner, rightScreenPosition, leftScreenPosition);
    }

    private void RebuildMonsterPrefabItems(IReadOnlyList<StatusEffectRuntimeData> validEffects)
    {
        DestroyMonsterPrefabItems();

        if (validEffects == null || monsterItemPrefab == null || itemRoot == null)
            return;

        for (int i = 0; i < validEffects.Count; i++)
        {
            StatusEffectRuntimeData data = validEffects[i];
            if (data == null)
                continue;

            UnitStatusEffectTooltipItemUI item = Instantiate(monsterItemPrefab, itemRoot, false);
            RectTransform rect = item.transform as RectTransform;

            item.Set(data);
            item.gameObject.SetActive(true);
            item.transform.SetAsLastSibling();

            monsterItemInstances.Add(item);
            monsterItemRects.Add(rect);
        }
    }

    /// <summary>
    /// 생성된 몬스터 툴팁 프리팹 목록만 이동합니다.
    /// UnitStatusEffectTooltipUI와 EffectRoot의 좌표는 변경하지 않습니다.
    /// </summary>
    public void UpdateMonsterPrefabPosition(
        Object owner,
        Vector2 rightScreenPosition,
        Vector2 leftScreenPosition)
    {
        InitializeIfNeeded();

        if (monsterItemRects.Count <= 0 || itemRoot == null)
            return;

        if (monsterPrefabOwner != null && owner != null && monsterPrefabOwner != owner)
            return;

        // 기본적으로 Collider2D의 오른쪽을 기준으로 배치합니다.
        LayoutMonsterPrefabList(rightScreenPosition, UnitStatusEffectTooltipSide.Right);
        Canvas.ForceUpdateCanvases();

        // 오른쪽 배치 시 목록이 화면 오른쪽 경계를 넘으려 하면
        // Clamp로 밀어 넣지 않고 Collider2D의 왼쪽 기준으로 배치를 전환합니다.
        if (IsMonsterPrefabListOverflowingRight())
        {
            LayoutMonsterPrefabList(leftScreenPosition, UnitStatusEffectTooltipSide.Left);
            Canvas.ForceUpdateCanvases();
        }

        // 세로 방향과, 아주 좁은 해상도에서 왼쪽마저 벗어나는 예외만 최종 보정합니다.
        ClampMonsterPrefabListInsideCanvas();
    }

    private void LayoutMonsterPrefabList(
        Vector2 screenPosition,
        UnitStatusEffectTooltipSide side)
    {
        Camera uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemRoot,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float horizontalOffset = Mathf.Abs(statusTooltipCursorOffsetX);
        Vector2 firstItemPosition = localPoint;
        firstItemPosition.x += side == UnitStatusEffectTooltipSide.Left
            ? -horizontalOffset
            : horizontalOffset;
        firstItemPosition.y += statusTooltipCursorOffsetY;

        float accumulatedY = 0f;
        for (int i = 0; i < monsterItemRects.Count; i++)
        {
            RectTransform rect = monsterItemRects[i];
            if (rect == null)
                continue;

            if (i > 0)
            {
                RectTransform previousRect = monsterItemRects[i - 1];
                float previousHeight = previousRect != null
                    ? Mathf.Abs(previousRect.rect.height)
                    : 0f;
                float currentHeight = Mathf.Abs(rect.rect.height);

                accumulatedY += (previousHeight * 0.5f) +
                                (currentHeight * 0.5f) +
                                monsterItemVerticalSpacing;
            }

            rect.anchoredPosition = firstItemPosition + Vector2.down * accumulatedY;
        }
    }

    private bool IsMonsterPrefabListOverflowingRight()
    {
        if (!TryGetMonsterPrefabCanvasBounds(out _, out float maxX, out _, out _))
            return false;

        Rect canvasBounds = canvasRect.rect;
        float allowedMaxX = canvasBounds.xMax - statusTooltipScreenPaddingX;
        return maxX > allowedMaxX;
    }

    private bool TryGetMonsterPrefabCanvasBounds(
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minY = float.PositiveInfinity;
        maxY = float.NegativeInfinity;

        if (monsterItemRects.Count <= 0 || canvasRect == null)
            return false;

        bool foundAnyCorner = false;
        Vector3[] worldCorners = new Vector3[4];

        for (int rectIndex = 0; rectIndex < monsterItemRects.Count; rectIndex++)
        {
            RectTransform rect = monsterItemRects[rectIndex];
            if (rect == null)
                continue;

            rect.GetWorldCorners(worldCorners);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 canvasLocal = canvasRect.InverseTransformPoint(worldCorners[i]);
                minX = Mathf.Min(minX, canvasLocal.x);
                maxX = Mathf.Max(maxX, canvasLocal.x);
                minY = Mathf.Min(minY, canvasLocal.y);
                maxY = Mathf.Max(maxY, canvasLocal.y);
                foundAnyCorner = true;
            }
        }

        return foundAnyCorner;
    }

    /// <summary>
    /// 몬스터 상태효과 목록 전체의 실제 외곽을 Root Canvas 기준으로 검사하여
    /// 화면 밖으로 나간 경우 모든 항목을 같은 거리만큼 안쪽으로 이동합니다.
    /// </summary>
    private void ClampMonsterPrefabListInsideCanvas()
    {
        if (monsterItemRects.Count <= 0 || itemRoot == null || canvasRect == null)
            return;

        if (!TryGetMonsterPrefabCanvasBounds(
                out float minX,
                out float maxX,
                out float minY,
                out float maxY))
        {
            return;
        }

        Rect canvasBounds = canvasRect.rect;
        float allowedMinX = canvasBounds.xMin + statusTooltipScreenPaddingX;
        float allowedMaxX = canvasBounds.xMax - statusTooltipScreenPaddingX;
        float allowedMinY = canvasBounds.yMin + statusTooltipScreenPaddingY;
        float allowedMaxY = canvasBounds.yMax - statusTooltipScreenPaddingY;

        Vector2 canvasLocalCorrection = Vector2.zero;

        if (minX < allowedMinX)
            canvasLocalCorrection.x += allowedMinX - minX;
        else if (maxX > allowedMaxX)
            canvasLocalCorrection.x -= maxX - allowedMaxX;

        if (minY < allowedMinY)
            canvasLocalCorrection.y += allowedMinY - minY;
        else if (maxY > allowedMaxY)
            canvasLocalCorrection.y -= maxY - allowedMaxY;

        if (canvasLocalCorrection.sqrMagnitude <= 0.0001f)
            return;

        Vector3 worldCorrection = canvasRect.TransformVector(
            new Vector3(canvasLocalCorrection.x, canvasLocalCorrection.y, 0f));
        Vector3 parentLocalCorrection = itemRoot.InverseTransformVector(worldCorrection);
        Vector2 anchoredCorrection = new Vector2(parentLocalCorrection.x, parentLocalCorrection.y);

        for (int i = 0; i < monsterItemRects.Count; i++)
        {
            RectTransform rect = monsterItemRects[i];
            if (rect != null)
                rect.anchoredPosition += anchoredCorrection;
        }
    }

    public void HideMonsterPrefab(Object owner)
    {
        if (monsterPrefabOwner != null && owner != null && monsterPrefabOwner != owner)
            return;

        DestroyMonsterPrefabImmediate();

        if (!isVisible && canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void DestroyMonsterPrefabImmediate()
    {
        monsterPrefabOwner = null;
        DestroyMonsterPrefabItems();
    }

    private void DestroyMonsterPrefabItems()
    {
        for (int i = 0; i < monsterItemInstances.Count; i++)
        {
            UnitStatusEffectTooltipItemUI item = monsterItemInstances[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        monsterItemInstances.Clear();
        monsterItemRects.Clear();
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

        if (itemRect == null || itemParentRect == null || canvasRect == null)
            return;

        currentSide = side;

        Camera uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        // 마우스 화면 좌표를 ItemUI의 실제 부모(EffectRoot) 로컬 좌표로 변환합니다.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemParentRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float horizontalOffset = Mathf.Abs(statusTooltipCursorOffsetX);
        Vector2 target = localPoint;
        target.x += side == UnitStatusEffectTooltipSide.Left
            ? -horizontalOffset
            : horizontalOffset;
        target.y += statusTooltipCursorOffsetY;

        // 중요: UnitStatusEffectTooltipUI(panelRect)는 움직이지 않습니다.
        // 실제로 마우스를 따라가는 것은 UnitStatusEffectTooltipItemUI뿐입니다.
        itemRect.anchoredPosition = target;

        // 레이아웃 변경이 같은 프레임에 있었다면 실제 외곽 좌표를 갱신합니다.
        Canvas.ForceUpdateCanvases();
        ClampItemInsideCanvas();
    }

    /// <summary>
    /// ItemUI의 실제 네 모서리를 Root Canvas 기준으로 검사하여
    /// 화면 밖으로 나간 만큼 ItemUI만 안쪽으로 이동시킵니다.
    /// </summary>
    private void ClampItemInsideCanvas()
    {
        ClampRectInsideCanvas(itemRect, itemParentRect);
    }

    private void ClampRectInsideCanvas(RectTransform targetRect, RectTransform targetParentRect)
    {
        if (targetRect == null || targetParentRect == null || canvasRect == null)
            return;

        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 canvasLocal = canvasRect.InverseTransformPoint(worldCorners[i]);
            minX = Mathf.Min(minX, canvasLocal.x);
            maxX = Mathf.Max(maxX, canvasLocal.x);
            minY = Mathf.Min(minY, canvasLocal.y);
            maxY = Mathf.Max(maxY, canvasLocal.y);
        }

        Rect canvasBounds = canvasRect.rect;
        float allowedMinX = canvasBounds.xMin + statusTooltipScreenPaddingX;
        float allowedMaxX = canvasBounds.xMax - statusTooltipScreenPaddingX;
        float allowedMinY = canvasBounds.yMin + statusTooltipScreenPaddingY;
        float allowedMaxY = canvasBounds.yMax - statusTooltipScreenPaddingY;

        Vector2 canvasLocalCorrection = Vector2.zero;

        if (minX < allowedMinX)
            canvasLocalCorrection.x += allowedMinX - minX;
        else if (maxX > allowedMaxX)
            canvasLocalCorrection.x -= maxX - allowedMaxX;

        if (minY < allowedMinY)
            canvasLocalCorrection.y += allowedMinY - minY;
        else if (maxY > allowedMaxY)
            canvasLocalCorrection.y -= maxY - allowedMaxY;

        if (canvasLocalCorrection.sqrMagnitude <= 0.0001f)
            return;

        Vector3 worldCorrection = canvasRect.TransformVector(
            new Vector3(canvasLocalCorrection.x, canvasLocalCorrection.y, 0f));
        Vector3 parentLocalCorrection = targetParentRect.InverseTransformVector(worldCorrection);

        targetRect.anchoredPosition += new Vector2(parentLocalCorrection.x, parentLocalCorrection.y);
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

        itemRect = itemView != null ? itemView.transform as RectTransform : null;
        itemParentRect = itemRect != null ? itemRect.parent as RectTransform : itemRoot;

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

    private static List<StatusEffectRuntimeData> GetValidStatusEffects(
        IReadOnlyList<StatusEffectRuntimeData> statusEffects)
    {
        List<StatusEffectRuntimeData> result = new();
        if (statusEffects == null)
            return result;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData data = statusEffects[i];
            if (data != null && data.IsValid())
                result.Add(data);
        }

        return result;
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
