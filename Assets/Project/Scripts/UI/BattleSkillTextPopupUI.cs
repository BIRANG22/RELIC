using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleSkillTextPopupUI : MonoBehaviour
{
    private const string AutoInstanceName = "BattleSkillTextPopupUI_Auto";
    private const string OverlayCanvasName = "BattleSkillTextOverlayCanvas";

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Popup Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector2 randomScreenOffset = new Vector2(26f, 12f);
    [SerializeField] private Vector2 randomDisappearCanvasOffset = new Vector2(12f, 8f);

    [Header("Popup Timing")]
    [SerializeField, Min(0f)] private float holdDuration = 0.3f;
    [SerializeField, Min(0f)] private float duration = 0.55f;

    [Header("Popup Movement")]
    [SerializeField] private Vector2 horizontalCanvasMoveRange = new Vector2(-34f, 34f);
    [SerializeField] private float upwardCanvasMove = 18f;
    [SerializeField] private float arcCanvasHeight = 20f;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.68f;

    [Header("Text Style")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Material fontMaterial;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(1f)] private float fontSize = 44f;
    [SerializeField] private bool useBoldText = true;
    [SerializeField] private Vector2 textBoxSize = new Vector2(420f, 165f);

    private static BattleSkillTextPopupUI instance;
    private Canvas overlayCanvas;

    private void Awake()
    {
        if (instance != null && instance != this)
            return;

        instance = this;
        EnsureReferences();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Show(Transform target, string skillName)
    {
        if (target == null || string.IsNullOrWhiteSpace(skillName))
            return;

        BattleSkillTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UseOverlayCanvas();
        popup.ShowInternal(target, skillName.Trim());
    }

    private static BattleSkillTextPopupUI GetOrCreateInstance()
    {
        if (instance != null && instance.gameObject.activeInHierarchy)
            return instance;

        instance = FindFirstObjectByType<BattleSkillTextPopupUI>(FindObjectsInactive.Exclude);
        if (instance != null)
        {
            instance.EnsureReferences();
            return instance;
        }

        GameObject go = new GameObject(AutoInstanceName);
        instance = go.AddComponent<BattleSkillTextPopupUI>();
        instance.EnsureReferences();
        return instance;
    }

    private void ShowInternal(Transform target, string message)
    {
        EnsureReferences();
        if (targetCanvas == null || canvasRect == null)
            return;

        Vector2 screenOffset = new Vector2(
            Random.Range(-randomScreenOffset.x, randomScreenOffset.x),
            Random.Range(-randomScreenOffset.y, randomScreenOffset.y));

        if (!TryGetCanvasPosition(target, screenOffset, out Vector2 anchoredPosition))
            return;

        GameObject textObject = new GameObject("BattleSkillText_" + message);
        textObject.transform.SetParent(canvasRect, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = textBoxSize;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = textColor;
        text.fontSize = fontSize;
        text.text = message;

        if (fontAsset != null)
            text.font = fontAsset;
        if (fontMaterial != null)
            text.fontMaterial = fontMaterial;
        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateAndDestroy(rect, canvasGroup, target, screenOffset));
    }

    private IEnumerator AnimateAndDestroy(
        RectTransform rect,
        CanvasGroup canvasGroup,
        Transform target,
        Vector2 screenOffset)
    {
        float holdElapsed = 0f;
        Vector2 targetCanvasPosition = rect.anchoredPosition;
        Vector2 endOffset = GetDisappearEndOffset();
        Vector3 startScaleVector = Vector3.one * startScale;
        Vector3 endScaleVector = Vector3.one * endScale;

        rect.localScale = startScaleVector;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
            targetCanvasPosition = RefreshTargetCanvasPosition(target, screenOffset, targetCanvasPosition);
            rect.anchoredPosition = targetCanvasPosition;
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            targetCanvasPosition = RefreshTargetCanvasPosition(target, screenOffset, targetCanvasPosition);
            rect.localScale = Vector3.Lerp(startScaleVector, endScaleVector, eased);
            rect.anchoredPosition = targetCanvasPosition + GetCurvedOffset(endOffset, eased);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private Vector2 RefreshTargetCanvasPosition(
        Transform target,
        Vector2 screenOffset,
        Vector2 fallbackPosition)
    {
        return TryGetCanvasPosition(target, screenOffset, out Vector2 currentPosition)
            ? currentPosition
            : fallbackPosition;
    }

    private bool TryGetCanvasPosition(
        Transform target,
        Vector2 screenOffset,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (target == null || canvasRect == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null)
            return false;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(GetWorldAnchor(target));
        if (screenPosition.z < 0f)
            return false;

        screenPosition += (Vector3)screenOffset;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out anchoredPosition);
    }

    private Vector3 GetWorldAnchor(Transform target)
    {
        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, target.position.z) + worldOffset;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, target.position.z) + worldOffset;
        }

        return target.position + worldOffset;
    }

    private Vector2 GetDisappearEndOffset()
    {
        float minX = Mathf.Min(horizontalCanvasMoveRange.x, horizontalCanvasMoveRange.y);
        float maxX = Mathf.Max(horizontalCanvasMoveRange.x, horizontalCanvasMoveRange.y);
        return new Vector2(
            Random.Range(minX, maxX) + Random.Range(-randomDisappearCanvasOffset.x, randomDisappearCanvasOffset.x),
            upwardCanvasMove + Random.Range(-randomDisappearCanvasOffset.y, randomDisappearCanvasOffset.y));
    }

    private Vector2 GetCurvedOffset(Vector2 endOffset, float t)
    {
        Vector2 position = Vector2.Lerp(Vector2.zero, endOffset, t);
        return position + Vector2.up * (Mathf.Sin(t * Mathf.PI) * arcCanvasHeight);
    }

    private void UseOverlayCanvas()
    {
        EnsureReferences();
        if (overlayCanvas == null)
            overlayCanvas = FindOrCreateOverlayCanvas(targetCanvas);
        if (overlayCanvas == null)
            return;

        targetCanvas = overlayCanvas;
        canvasRect = overlayCanvas.GetComponent<RectTransform>();
        uiCamera = null;
    }

    private void EnsureReferences()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (targetCanvas == null || !targetCanvas.gameObject.activeInHierarchy)
            targetCanvas = FindBattleCanvas();
        if (targetCanvas != null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();
        if (targetCanvas != null)
            uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
    }

    private static Canvas FindBattleCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == "BattleHUDCanvas")
                return canvas;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private Canvas FindOrCreateOverlayCanvas(Canvas sourceCanvas)
    {
        GameObject existing = GameObject.Find(OverlayCanvasName);
        if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            return existingCanvas;

        GameObject go = new GameObject(OverlayCanvasName);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30001;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        CanvasScaler sourceScaler = sourceCanvas != null ? sourceCanvas.GetComponent<CanvasScaler>() : null;
        if (sourceScaler != null)
        {
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        return canvas;
    }
}
