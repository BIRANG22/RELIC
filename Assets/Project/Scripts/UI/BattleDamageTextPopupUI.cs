using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleDamageTextPopupUI : MonoBehaviour
{
    private const string AutoInstanceName = "BattleDamageTextPopupUI_Auto";

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Popup")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector2 randomScreenOffset = new Vector2(26f, 12f);
    [SerializeField] private float startFontSize = 72f;
    [SerializeField] private float endFontSize = 38f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float upwardCanvasMove = 18f;
    [SerializeField] private Color damageColor = new Color(1f, 0.06f, 0.04f, 1f);
    [SerializeField] private bool useBoldText = true;

    private static BattleDamageTextPopupUI instance;

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

    public static void Show(Transform target, int damage)
    {
        if (target == null || damage <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();

        if (popup == null)
            return;

        popup.ShowInternal(target, damage);
    }

    private static BattleDamageTextPopupUI GetOrCreateInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<BattleDamageTextPopupUI>(FindObjectsInactive.Include);

        if (instance != null)
        {
            instance.EnsureReferences();
            return instance;
        }

        GameObject go = new GameObject(AutoInstanceName);
        instance = go.AddComponent<BattleDamageTextPopupUI>();
        instance.EnsureReferences();
        return instance;
    }

    private void EnsureReferences()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (targetCanvas == null)
            targetCanvas = FindBattleCanvas();

        if (targetCanvas == null)
            targetCanvas = CreateFallbackCanvas();

        if (targetCanvas != null && canvasRect == null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();

        if (targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            uiCamera = null;
        else if (uiCamera == null && targetCanvas != null)
            uiCamera = targetCanvas.worldCamera;
    }

    private Canvas FindBattleCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;

            if (canvases[i].name == "BattleHUDCanvas")
                return canvases[i];
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;

            if (canvases[i].name.Contains("BattleHUD") || canvases[i].name.Contains("HUDCanvas"))
                return canvases[i];
        }

        if (canvases.Length > 0)
            return canvases[0];

        return null;
    }

    private Canvas CreateFallbackCanvas()
    {
        GameObject go = new GameObject("BattleDamageTextCanvas_Auto");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void ShowInternal(Transform target, int damage)
    {
        EnsureReferences();

        if (targetCanvas == null || canvasRect == null)
            return;

        Vector2 anchoredPosition;

        if (!TryGetCanvasPosition(target, out anchoredPosition))
            return;

        GameObject textObject = new GameObject("DamageText_" + damage);
        textObject.transform.SetParent(canvasRect, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 110f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = damageColor;
        text.fontSize = startFontSize;
        text.text = damage.ToString();

        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateAndDestroy(rect, text, canvasGroup));
    }

    private bool TryGetCanvasPosition(Transform target, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (target == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return false;

        Vector3 worldPosition = GetTargetWorldPosition(target);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);

        screenPoint.x += Random.Range(-randomScreenOffset.x, randomScreenOffset.x);
        screenPoint.y += Random.Range(0f, randomScreenOffset.y);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            uiCamera,
            out anchoredPosition);
    }

    private Vector3 GetTargetWorldPosition(Transform target)
    {
        Collider2D collider2D = target.GetComponentInChildren<Collider2D>();

        if (collider2D != null)
        {
            Bounds bounds = collider2D.bounds;
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

    private IEnumerator AnimateAndDestroy(RectTransform rect, TMP_Text text, CanvasGroup canvasGroup)
    {
        if (rect == null || text == null || canvasGroup == null)
            yield break;

        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * upwardCanvasMove;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            text.fontSize = Mathf.Lerp(startFontSize, endFontSize, eased);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}
