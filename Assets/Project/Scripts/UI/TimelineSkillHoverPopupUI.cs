using UnityEngine;

public class TimelineSkillHoverPopupUI : MonoBehaviour
{
    private static TimelineSkillHoverPopupUI instance;

    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private TimelineSkillHoverPopupView itemPrefab;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 70f);
    [SerializeField] private float screenPadding = 12f;
    [SerializeField] private bool moveToHoveredIcon = true;

    [Header("Sorting")]
    [SerializeField] private bool forceTooltipToFront = true;
    [SerializeField] private int sortingOrderOffset = 20;

    [Header("Visibility")]
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool hideWhenNoSkill = true;

    private TimelineSkillHoverPopupView spawnedItem;
    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private Canvas popupCanvas;
    private Vector2 lastScreenPosition;
    private bool initialized;

    public static TimelineSkillHoverPopupUI Instance
    {
        get
        {
            if (instance == null)
                instance = FindBestPopupInScene();

            if (instance != null)
                instance.InitializeIfNeeded();

            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null || gameObject.activeInHierarchy)
            instance = this;

        InitializeIfNeeded();

        if (hideOnAwake)
            SetVisible(false);
    }

    private void OnValidate()
    {
        initialized = false;
        InitializeIfNeeded();
    }

    private void LateUpdate()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f)
            return;

        if (moveToHoveredIcon)
            UpdatePosition(lastScreenPosition);
    }

    public void Show(BattleTimelinePreviewEntry entry, RectTransform hoveredIconRect)
    {
        if (entry == null)
        {
            if (hideWhenNoSkill)
                Hide();

            return;
        }

        Show(entry.SkillName, entry.SkillEffectDescription, hoveredIconRect);
    }

    public void Show(string skillName, string effectDescription, RectTransform hoveredIconRect)
    {
        InitializeIfNeeded();

        if (string.IsNullOrWhiteSpace(skillName) && hideWhenNoSkill)
        {
            Hide();
            return;
        }

        TimelineSkillHoverPopupView item = GetOrCreateItem();
        if (item == null)
        {
            Debug.LogWarning("[TimelineSkillHoverPopupUI] Item Prefab이 연결되지 않았고, 사용할 팝업 View도 찾지 못했습니다.");
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BringToFront();

        item.Set(skillName, effectDescription);
        SetVisible(true);

        if (moveToHoveredIcon)
            UpdatePosition(GetScreenPosition(hoveredIconRect));
    }

    public void Hide()
    {
        InitializeIfNeeded();
        SetVisible(false);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (itemRoot == null)
            itemRoot = FindChildRectTransform("TimelineRoot");

        if (itemRoot == null)
            itemRoot = FindChildRectTransform("ItemRoot");

        if (itemRoot == null)
            itemRoot = panelRect;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            canvasRect = rootCanvas.transform as RectTransform;

        popupCanvas = GetComponent<Canvas>();

        if (spawnedItem == null)
            spawnedItem = GetComponentInChildren<TimelineSkillHoverPopupView>(true);

        if (spawnedItem != null && itemPrefab == null && spawnedItem.transform.parent != null)
            itemPrefab = null;

        initialized = true;
    }

    private TimelineSkillHoverPopupView GetOrCreateItem()
    {
        InitializeIfNeeded();

        if (spawnedItem != null)
            return spawnedItem;

        if (itemRoot != null)
            spawnedItem = itemRoot.GetComponentInChildren<TimelineSkillHoverPopupView>(true);

        if (spawnedItem != null)
            return spawnedItem;

        if (itemPrefab == null)
            return null;

        RectTransform parent = itemRoot != null ? itemRoot : panelRect;
        spawnedItem = Instantiate(itemPrefab, parent);
        spawnedItem.gameObject.name = itemPrefab.gameObject.name;
        spawnedItem.gameObject.SetActive(true);
        return spawnedItem;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private Vector2 GetScreenPosition(RectTransform hoveredIconRect)
    {
        if (hoveredIconRect == null)
            return Input.mousePosition;

        Canvas canvas = hoveredIconRect.GetComponentInParent<Canvas>();
        Camera uiCamera = GetCanvasCamera(canvas);

        Vector3[] corners = new Vector3[4];
        hoveredIconRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, center);
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        InitializeIfNeeded();

        if (panelRect == null || canvasRect == null)
            return;

        lastScreenPosition = screenPosition;

        Camera uiCamera = GetCanvasCamera(rootCanvas);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
            return;

        Vector2 targetPosition = localPoint + screenOffset;
        Vector2 panelSize = GetRectSize(panelRect);
        Vector2 pivot = panelRect.pivot;
        Rect canvasBounds = canvasRect.rect;

        float minX = canvasBounds.xMin + screenPadding + panelSize.x * pivot.x;
        float maxX = canvasBounds.xMax - screenPadding - panelSize.x * (1f - pivot.x);
        float minY = canvasBounds.yMin + screenPadding + panelSize.y * pivot.y;
        float maxY = canvasBounds.yMax - screenPadding - panelSize.y * (1f - pivot.y);

        if (minX <= maxX)
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);

        if (minY <= maxY)
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        panelRect.anchoredPosition = targetPosition;
    }

    private Vector2 GetRectSize(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return new Vector2(300f, 120f);

        Vector2 size = rectTransform.rect.size;

        if (size.x <= 0f)
            size.x = rectTransform.sizeDelta.x;

        if (size.y <= 0f)
            size.y = rectTransform.sizeDelta.y;

        if (size.x <= 0f)
            size.x = 300f;

        if (size.y <= 0f)
            size.y = 120f;

        return size;
    }

    private Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    private void BringToFront()
    {
        if (forceTooltipToFront)
            transform.SetAsLastSibling();

        if (!forceTooltipToFront)
            return;

        if (popupCanvas == null)
            popupCanvas = GetComponent<Canvas>();

        if (popupCanvas == null)
            popupCanvas = gameObject.AddComponent<Canvas>();

        popupCanvas.overrideSorting = true;

        Canvas parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
        int baseOrder = parentCanvas != null ? parentCanvas.sortingOrder : 0;
        popupCanvas.sortingOrder = baseOrder + sortingOrderOffset;
    }

    private RectTransform FindChildRectTransform(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found as RectTransform : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static TimelineSkillHoverPopupUI FindBestPopupInScene()
    {
        TimelineSkillHoverPopupUI[] popups = FindObjectsByType<TimelineSkillHoverPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (popups == null || popups.Length <= 0)
            return null;

        TimelineSkillHoverPopupUI best = null;
        for (int i = 0; i < popups.Length; i++)
        {
            TimelineSkillHoverPopupUI popup = popups[i];
            if (popup == null)
                continue;

            if (popup.gameObject.activeInHierarchy)
                return popup;

            if (best == null)
                best = popup;
        }

        return best;
    }
}
