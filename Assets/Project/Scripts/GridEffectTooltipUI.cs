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
        if (currentOwner == null)
        {
            if (canvasGroup != null && canvasGroup.alpha > 0f)
                SetVisible(false);

            return;
        }

        if (followMouse)
            SetPosition(Input.mousePosition);
        else
            SetPosition(lastScreenPosition);
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
        lastScreenPosition = screenPosition;

        nameText.text = data.Name ?? string.Empty;
        toolTipText.text = FormatToolTip(data, remainingDuration);

        BringToFront();
        SetVisible(true);
        SetPosition(screenPosition);
    }

    private static string FormatToolTip(GridEffectData data, int? remainingDuration)
    {
        if (data == null || string.IsNullOrEmpty(data.ToolTip))
            return string.Empty;

        string result = data.ToolTip;
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
        SetVisible(false);
    }

    public void SetPosition(Vector2 screenPosition)
    {
        InitializeReferences();

        if (tooltipRect == null)
            return;

        lastScreenPosition = screenPosition;
        Vector2 target = ClampToScreen(screenPosition + screenOffset);

        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            tooltipRect.position = target;
            return;
        }

        RectTransform parentRect = tooltipRect.parent as RectTransform;

        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                target,
                canvasCamera,
                out Vector2 localPoint))
        {
            tooltipRect.localPosition = localPoint;
        }
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

    private Vector2 ClampToScreen(Vector2 screenPosition)
    {
        if (tooltipRect == null)
            return screenPosition;

        Vector2 size = tooltipRect.rect.size;
        Vector3 scale = tooltipRect.lossyScale;
        size.x *= Mathf.Abs(scale.x);
        size.y *= Mathf.Abs(scale.y);

        float minX = screenPadding.x;
        float maxX = Mathf.Max(minX, Screen.width - screenPadding.x);
        float minY = screenPadding.y;
        float maxY = Mathf.Max(minY, Screen.height - screenPadding.y);

        if (screenPosition.x + size.x > maxX)
            screenPosition.x = Mathf.Max(minX, maxX - size.x);

        if (screenPosition.y - size.y < minY)
            screenPosition.y = Mathf.Min(maxY, minY + size.y);

        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);
        return screenPosition;
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
