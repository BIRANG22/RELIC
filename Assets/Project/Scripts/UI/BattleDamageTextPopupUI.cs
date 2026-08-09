using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleDamageTextPopupUI : MonoBehaviour
{
    public enum PopupType
    {
        Damage,
        CostRecovery,
        ArmorGain,
        PoisonDamage,
        UniqueResource
    }
    private const string AutoInstanceName = "BattleDamageTextPopupUI_Auto";

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Popup Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector2 randomScreenOffset = new Vector2(26f, 12f);
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float upwardCanvasMove = 18f;

    [Header("Popup Movement")]
    [SerializeField] private Vector2 horizontalCanvasMoveRange = new Vector2(-34f, 34f);
    [SerializeField] private float arcCanvasHeight = 20f;

    [Header("Digit Images")]
    [Tooltip("Assign digit sprites in order. Element 0 = digit 0, Element 1 = digit 1, ... Element 9 = digit 9.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Vector2 digitSize = new Vector2(52f, 72f);
    [SerializeField] private float digitVisualScaleMultiplier = 1.5f;
    [SerializeField] private float digitSpacing = -4f;
    [SerializeField] private Color digitColor = new Color(1f, 0.06f, 0.04f, 1f);
    [SerializeField] private bool preserveDigitAspect = true;
    [SerializeField] private bool useNativeDigitSize = false;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.68f;

    [Header("Text Fallback")]
    [Tooltip("Use temporary text popup when one or more digit sprites are missing.")]
    [SerializeField] private bool useTextFallbackWhenDigitMissing = true;
    [SerializeField] private float startFontSize = 72f;
    [SerializeField] private float endFontSize = 38f;
    [SerializeField] private Color damageColor = new Color(1f, 0.06f, 0.04f, 1f);
    [SerializeField] private Color costRecoveryColor = new Color(0.15f, 0.65f, 1f, 1f);
    [SerializeField] private Color armorGainColor = Color.white;
    [SerializeField] private Color poisonDamageColor = new Color(0.2f, 1f, 0.25f, 1f);
    [SerializeField] private Color uniqueResourceColor = Color.white;
    [SerializeField] private float straightUpCanvasMove = 30f;
    [SerializeField] private float armorUpCanvasMove = 16f;
    [SerializeField] private float poisonDownCanvasMove = 28f;
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
        if (target == null || damage < 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();

        if (popup == null)
            return;

        popup.ShowInternal(target, damage, PopupType.Damage);
    }

    public static void ShowCostRecovery(Transform target, int value)
    {
        ShowTyped(target, value, PopupType.CostRecovery);
    }

    public static void ShowArmorGain(Transform target, int value)
    {
        ShowTyped(target, value, PopupType.ArmorGain);
    }

    public static void ShowArmorGain(string characterId, int value)
    {
        if (string.IsNullOrWhiteSpace(characterId) || value <= 0)
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];
            if (character != null && character.CharacterId == characterId)
            {
                ShowArmorGain(character.transform, value);
                return;
            }
        }
    }

    public static void ShowPoisonDamage(Transform target, int value)
    {
        ShowTyped(target, value, PopupType.PoisonDamage);
    }

    public static void ShowUniqueResource(Transform target, string resourceName, int value)
    {
        if (target == null || string.IsNullOrWhiteSpace(resourceName) || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup != null)
            popup.ShowCustomTextInternal(target, $"{resourceName} +{value}", PopupType.UniqueResource);
    }

    private static void ShowTyped(Transform target, int value, PopupType popupType)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup != null)
            popup.ShowInternal(target, value, popupType);
    }


    private void ShowCustomTextInternal(Transform target, string message, PopupType popupType)
    {
        EnsureReferences();

        if (targetCanvas == null || canvasRect == null || string.IsNullOrWhiteSpace(message))
            return;

        Vector2 screenOffset = GetRandomScreenOffset();

        if (!TryGetCanvasPosition(target, screenOffset, out Vector2 anchoredPosition))
            return;

        ShowCustomTextPopup(target, screenOffset, anchoredPosition, message, popupType);
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

    private void ShowInternal(Transform target, int damage, PopupType popupType)
    {
        EnsureReferences();

        if (targetCanvas == null || canvasRect == null)
            return;

        Vector2 screenOffset = GetRandomScreenOffset();

        if (!TryGetCanvasPosition(target, screenOffset, out Vector2 anchoredPosition))
            return;

        if (CanUseDigitSprites(damage))
            ShowDigitPopup(target, screenOffset, anchoredPosition, damage, popupType);
        else if (useTextFallbackWhenDigitMissing)
            ShowTextFallbackPopup(target, screenOffset, anchoredPosition, damage, popupType);
    }

    private bool CanUseDigitSprites(int damage)
    {
        string value = damage.ToString();

        if (digitSprites == null || digitSprites.Length < 10)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            int index = value[i] - '0';

            if (index < 0 || index > 9)
                return false;

            if (digitSprites[index] == null)
                return false;
        }

        return true;
    }

    private void ShowDigitPopup(Transform target, Vector2 screenOffset, Vector2 anchoredPosition, int damage, PopupType popupType)
    {
        string value = damage.ToString();

        GameObject rootObject = new GameObject("DamageDigit_" + value);
        rootObject.transform.SetParent(canvasRect, false);

        RectTransform rect = rootObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = GetDigitPopupSize(value.Length);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one * startScale;

        HorizontalLayoutGroup layoutGroup = rootObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = digitSpacing;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        for (int i = 0; i < value.Length; i++)
        {
            int digit = value[i] - '0';
            CreateDigitImage(rect, digit, i, GetPopupColor(popupType));
        }

        CanvasGroup canvasGroup = rootObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateAndDestroy(rect, canvasGroup, target, screenOffset, popupType));
    }

    private Vector2 GetEffectiveDigitSize()
    {
        float scale = Mathf.Max(0.01f, digitVisualScaleMultiplier);
        return digitSize * scale;
    }

    private Vector2 GetDigitPopupSize(int digitCount)
    {
        digitCount = Mathf.Max(1, digitCount);

        Vector2 effectiveDigitSize = GetEffectiveDigitSize();
        float width = effectiveDigitSize.x * digitCount + digitSpacing * Mathf.Max(0, digitCount - 1);
        width = Mathf.Max(effectiveDigitSize.x, width);

        return new Vector2(width, effectiveDigitSize.y);
    }

    private void CreateDigitImage(RectTransform parent, int digit, int order, Color popupColor)
    {
        GameObject imageObject = new GameObject("Digit_" + digit);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = digitSprites[digit];
        image.color = popupColor;
        image.preserveAspect = preserveDigitAspect;

        RectTransform imageRect = image.GetComponent<RectTransform>();

        if (imageRect != null)
        {
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = GetEffectiveDigitSize();
            imageRect.localScale = Vector3.one;
            imageRect.SetSiblingIndex(order);
        }

        LayoutElement layoutElement = image.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = image.gameObject.AddComponent<LayoutElement>();

        Vector2 effectiveDigitSize = GetEffectiveDigitSize();
        layoutElement.preferredWidth = effectiveDigitSize.x;
        layoutElement.preferredHeight = effectiveDigitSize.y;
        layoutElement.minWidth = effectiveDigitSize.x;
        layoutElement.minHeight = effectiveDigitSize.y;

        if (useNativeDigitSize && image.sprite != null)
        {
            image.SetNativeSize();

            if (imageRect != null)
            {
                layoutElement.preferredWidth = imageRect.sizeDelta.x;
                layoutElement.preferredHeight = imageRect.sizeDelta.y;
                layoutElement.minWidth = imageRect.sizeDelta.x;
                layoutElement.minHeight = imageRect.sizeDelta.y;
            }
        }
    }

    private float GetEffectiveStartFontSize()
    {
        return startFontSize * Mathf.Max(0.01f, digitVisualScaleMultiplier);
    }

    private float GetEffectiveEndFontSize()
    {
        return endFontSize * Mathf.Max(0.01f, digitVisualScaleMultiplier);
    }

    private void ShowTextFallbackPopup(Transform target, Vector2 screenOffset, Vector2 anchoredPosition, int damage, PopupType popupType)
    {
        GameObject textObject = new GameObject("DamageText_" + damage);
        textObject.transform.SetParent(canvasRect, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(330f, 165f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = GetPopupColor(popupType);
        text.fontSize = GetEffectiveStartFontSize();
        text.text = damage.ToString();

        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateTextFallbackAndDestroy(rect, text, canvasGroup, target, screenOffset, popupType));
    }


    private void ShowCustomTextPopup(Transform target, Vector2 screenOffset, Vector2 anchoredPosition, string message, PopupType popupType)
    {
        GameObject textObject = new GameObject("BattlePopupText_" + message);
        textObject.transform.SetParent(canvasRect, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 165f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = GetPopupColor(popupType);
        text.fontSize = GetEffectiveStartFontSize();
        text.text = message;

        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateTextFallbackAndDestroy(rect, text, canvasGroup, target, screenOffset, popupType));
    }

    private Vector2 GetRandomScreenOffset()
    {
        return new Vector2(
            Random.Range(-randomScreenOffset.x, randomScreenOffset.x),
            Random.Range(0f, randomScreenOffset.y));
    }

    private bool TryGetCanvasPosition(Transform target, Vector2 screenOffset, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (target == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null || canvasRect == null)
            return false;

        Vector3 worldPosition = GetTargetWorldPosition(target);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        screenPoint += screenOffset;

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

    private Vector2 GetDisappearEndOffset()
    {
        float minX = Mathf.Min(horizontalCanvasMoveRange.x, horizontalCanvasMoveRange.y);
        float maxX = Mathf.Max(horizontalCanvasMoveRange.x, horizontalCanvasMoveRange.y);
        float horizontalMove = Random.Range(minX, maxX);

        return new Vector2(horizontalMove, upwardCanvasMove);
    }

    private Vector2 GetCurvedOffset(Vector2 endOffset, float t)
    {
        Vector2 position = Vector2.Lerp(Vector2.zero, endOffset, t);

        if (Mathf.Abs(arcCanvasHeight) <= 0.001f)
            return position;

        float arc = Mathf.Sin(t * Mathf.PI) * arcCanvasHeight;
        return position + Vector2.up * arc;
    }


    private Color GetPopupColor(PopupType popupType)
    {
        return popupType switch
        {
            PopupType.CostRecovery => costRecoveryColor,
            PopupType.ArmorGain => armorGainColor,
            PopupType.PoisonDamage => poisonDamageColor,
            PopupType.UniqueResource => uniqueResourceColor,
            _ => digitColor
        };
    }

    private Vector2 GetEndOffset(PopupType popupType)
    {
        return popupType switch
        {
            PopupType.CostRecovery => Vector2.up * straightUpCanvasMove,
            PopupType.ArmorGain => Vector2.up * armorUpCanvasMove,
            PopupType.PoisonDamage => Vector2.down * poisonDownCanvasMove,
            PopupType.UniqueResource => Vector2.up * armorUpCanvasMove,
            _ => GetDisappearEndOffset()
        };
    }

    private Vector2 GetAnimatedOffset(Vector2 endOffset, float t, PopupType popupType)
    {
        return popupType == PopupType.Damage
            ? GetCurvedOffset(endOffset, t)
            : Vector2.Lerp(Vector2.zero, endOffset, t);
    }

    private Vector2 RefreshTargetCanvasPosition(Transform target, Vector2 screenOffset, Vector2 fallbackPosition)
    {
        return TryGetCanvasPosition(target, screenOffset, out Vector2 currentPosition)
            ? currentPosition
            : fallbackPosition;
    }

    private IEnumerator AnimateAndDestroy(
        RectTransform rect,
        CanvasGroup canvasGroup,
        Transform target,
        Vector2 screenOffset,
        PopupType popupType)
    {
        if (rect == null || canvasGroup == null)
            yield break;

        float holdElapsed = 0f;
        Vector2 targetCanvasPosition = rect.anchoredPosition;
        Vector2 endOffset = GetEndOffset(popupType);
        Vector3 startScaleVector = Vector3.one * startScale;
        Vector3 endScaleVector = Vector3.one * endScale;

        rect.localScale = startScaleVector;
        canvasGroup.alpha = 1f;

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
            rect.anchoredPosition = targetCanvasPosition + GetAnimatedOffset(endOffset, eased, popupType);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private IEnumerator AnimateTextFallbackAndDestroy(
        RectTransform rect,
        TMP_Text text,
        CanvasGroup canvasGroup,
        Transform target,
        Vector2 screenOffset,
        PopupType popupType)
    {
        if (rect == null || text == null || canvasGroup == null)
            yield break;

        float holdElapsed = 0f;
        Vector2 targetCanvasPosition = rect.anchoredPosition;
        Vector2 endOffset = GetEndOffset(popupType);

        text.fontSize = GetEffectiveStartFontSize();
        canvasGroup.alpha = 1f;

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
            text.fontSize = Mathf.Lerp(GetEffectiveStartFontSize(), GetEffectiveEndFontSize(), eased);
            rect.anchoredPosition = targetCanvasPosition + GetAnimatedOffset(endOffset, eased, popupType);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(rect.gameObject);
    }

}
