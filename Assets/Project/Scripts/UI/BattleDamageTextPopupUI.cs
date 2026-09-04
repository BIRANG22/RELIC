using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Monster;
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
        UniqueResource,
        HealthRecovery
    }
    private const string AutoInstanceName = "BattleDamageTextPopupUI_Auto";

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;
    private Canvas battleOverlayCanvas;

    [Header("Popup Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector2 randomScreenOffset = new Vector2(26f, 12f);
    [SerializeField] private Vector2 randomDisappearCanvasOffset = new Vector2(12f, 8f);
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float upwardCanvasMove = 18f;

    [Header("Popup Timing")]
    [Tooltip("HUD가 먼저 보인 뒤 팝업이 시작되기까지의 지연 시간입니다.")]
    [SerializeField, Min(0f)] private float popupStartDelay = 0.12f;
    [Tooltip("카르마/마나/체력 회복 팝업을 순차 표시할 때 다음 팝업이 시작되는 간격입니다.")]
    [SerializeField, Min(0f)] private float recoveryPopupInterval = 0.3f;

    [Header("Popup Movement")]
    [SerializeField] private Vector2 horizontalCanvasMoveRange = new Vector2(-34f, 34f);
    [SerializeField] private float arcCanvasHeight = 20f;

    [Header("Digit Images")]
    [Tooltip("Assign digit sprites in order. Element 0 = digit 0, Element 1 = digit 1, ... Element 9 = digit 9.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Vector2 digitSize = new Vector2(52f, 72f);
    [SerializeField] private float digitVisualScaleMultiplier = 1.5f;
    [SerializeField] private float digitSpacing = -4f;
    [SerializeField] private Color digitColor = new Color32(187, 187, 187, 255);
    [SerializeField] private bool preserveDigitAspect = true;
    [SerializeField] private bool useNativeDigitSize = false;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.68f;

    [Header("Recovery Icons")]
    [Tooltip("체력 회복 숫자 앞에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite healthRecoveryIcon;
    [Tooltip("마나 회복 숫자 앞에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite costRecoveryIcon;
    [Tooltip("카르마 회복 숫자 앞에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite uniqueResourceIcon;
    [SerializeField] private Vector2 recoveryIconSize = new Vector2(52f, 52f);
    [SerializeField] private float recoveryPrefixSpacing = 2f;

    [Header("Text Fallback")]
    [Tooltip("Use temporary text popup when one or more digit sprites are missing.")]
    [SerializeField] private bool useTextFallbackWhenDigitMissing = true;
    [SerializeField] private float startFontSize = 72f;
    [SerializeField] private float endFontSize = 38f;
    [SerializeField] private Color damageColor = new Color32(187, 187, 187, 255);
    [SerializeField] private Color costRecoveryColor = new Color32(50, 111, 197, 255);
    [SerializeField] private Color armorGainColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color poisonDamageColor = new Color32(25, 197, 36, 255);
    [SerializeField] private Color uniqueResourceColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color healthRecoveryColor = new Color32(208, 53, 53, 255);
    [SerializeField] private float straightUpCanvasMove = 30f;
    [SerializeField] private float armorUpCanvasMove = 16f;
    [SerializeField] private float poisonDownCanvasMove = 28f;
    [SerializeField] private bool useBoldText = true;

    private sealed class RecoveryPopupRequest
    {
        public Transform Target;
        public int Value;
        public PopupType PopupType;
        public int Sequence;
    }

    private static BattleDamageTextPopupUI instance;
    private readonly List<RecoveryPopupRequest> recoveryPopupQueue = new();
    private Coroutine recoveryPopupRoutine;
    private bool recoverySequenceHeld;
    private int recoverySequenceCounter;

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

        popup.UseBattleCanvas();
        popup.ShowAfterDelay(target, damage, PopupType.Damage);
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

    public static void ShowHealthRecovery(Transform target, int value)
    {
        ShowTyped(target, value, PopupType.HealthRecovery);
    }


    public static void ShowEventHealthRecovery(Transform target, int value, Canvas preferredCanvas, float fontSize)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UsePreferredCanvas(preferredCanvas);
        popup.ShowCustomTextInternal(target, "생명력 +" + value, PopupType.HealthRecovery, fontSize);
    }

    public static void ShowEventHealthLoss(Transform target, int value, Canvas preferredCanvas, float fontSize)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UsePreferredCanvas(preferredCanvas);
        popup.ShowCustomTextInternal(target, "생명력 -" + value, PopupType.Damage, fontSize);
    }

    public static void ShowEventManaRegenGain(Transform target, int value, Canvas preferredCanvas, float fontSize)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UsePreferredCanvas(preferredCanvas);
        popup.ShowCustomTextInternal(target, "마나 재생량 +" + value, PopupType.CostRecovery, fontSize);
    }

    public static void ShowEventMaxHealthGain(Transform target, int value, Canvas preferredCanvas, float fontSize)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UsePreferredCanvas(preferredCanvas);
        popup.ShowCustomTextInternal(target, "\uCD5C\uB300 \uC0DD\uBA85\uB825 +" + value, PopupType.HealthRecovery, fontSize);
    }

    public static void ShowEventMaxManaGain(Transform target, int value, Canvas preferredCanvas, float fontSize)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UsePreferredCanvas(preferredCanvas);
        popup.ShowCustomTextInternal(target, "\uCD5C\uB300 \uB9C8\uB098 +" + value, PopupType.CostRecovery, fontSize);
    }

    public static void ShowUniqueResource(Transform target, string resourceName, int value)
    {
        // resourceName은 기존 호출부 호환을 위해 유지합니다.
        // 전투 팝업에는 명칭을 쓰지 않고 아이콘 + "+숫자"만 표시합니다.
        ShowTyped(target, value, PopupType.UniqueResource);
    }

    private static void ShowTyped(Transform target, int value, PopupType popupType)
    {
        if (target == null || value <= 0)
            return;

        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.UseBattleCanvas();

        if (IsRecoveryPopup(popupType))
            popup.QueueRecoveryPopup(target, value, popupType);
        else
            popup.ShowAfterDelay(target, value, popupType);
    }

    public static void BeginRecoveryPopupSequence()
    {
        BattleDamageTextPopupUI popup = GetOrCreateInstance();
        if (popup == null)
            return;

        popup.recoverySequenceHeld = true;
    }

    public static void EndRecoveryPopupSequence()
    {
        if (instance == null)
            return;

        instance.recoverySequenceHeld = false;
        instance.SortRecoveryQueueByType();
        instance.StartRecoveryQueueIfNeeded();
    }

    private static bool IsRecoveryPopup(PopupType popupType)
    {
        return popupType == PopupType.UniqueResource ||
               popupType == PopupType.CostRecovery ||
               popupType == PopupType.HealthRecovery;
    }

    private void ShowAfterDelay(Transform target, int value, PopupType popupType)
    {
        ShowTargetHudImmediately(target);
        StartCoroutine(ShowAfterDelayRoutine(target, value, popupType));
    }

    private IEnumerator ShowAfterDelayRoutine(Transform target, int value, PopupType popupType)
    {
        if (popupStartDelay > 0f)
            yield return new WaitForSecondsRealtime(popupStartDelay);

        if (target != null)
            ShowInternal(target, value, popupType);
    }

    public static void PrepareTargetHud(Transform target)
    {
        ShowTargetHudImmediately(target);
    }

    private static void ShowTargetHudImmediately(Transform target)
    {
        if (target == null)
            return;

        BattleCharacter player = target.GetComponentInParent<BattleCharacter>();
        if (player != null)
        {
            BattleCharacterHUDController controller = BattleCharacterHUDController.Instance;
            if (controller != null)
                controller.ShowCharacterHudForEffect(player);

            return;
        }

        MonsterUnit monster = target.GetComponentInParent<MonsterUnit>();
        if (monster != null)
            monster.ShowTemporaryHUDForEffect();
    }

    private void QueueRecoveryPopup(Transform target, int value, PopupType popupType)
    {
        recoveryPopupQueue.Add(new RecoveryPopupRequest
        {
            Target = target,
            Value = value,
            PopupType = popupType,
            Sequence = recoverySequenceCounter++
        });

        if (!recoverySequenceHeld)
            StartRecoveryQueueIfNeeded();
    }

    private void StartRecoveryQueueIfNeeded()
    {
        if (recoverySequenceHeld || recoveryPopupRoutine != null || recoveryPopupQueue.Count == 0)
            return;

        recoveryPopupRoutine = StartCoroutine(PlayRecoveryPopupQueueRoutine());
    }

    private IEnumerator PlayRecoveryPopupQueueRoutine()
    {
        while (!recoverySequenceHeld && recoveryPopupQueue.Count > 0)
        {
            RecoveryPopupRequest request = recoveryPopupQueue[0];
            recoveryPopupQueue.RemoveAt(0);

            if (request != null && request.Target != null && request.Value > 0)
            {
                ShowTargetHudImmediately(request.Target);

                if (popupStartDelay > 0f)
                    yield return new WaitForSecondsRealtime(popupStartDelay);

                if (request.Target != null)
                    ShowInternal(request.Target, request.Value, request.PopupType);
            }

            if (recoveryPopupQueue.Count > 0 && recoveryPopupInterval > 0f)
                yield return new WaitForSecondsRealtime(recoveryPopupInterval);
        }

        recoveryPopupRoutine = null;

        if (!recoverySequenceHeld && recoveryPopupQueue.Count > 0)
            StartRecoveryQueueIfNeeded();
    }

    private void SortRecoveryQueueByType()
    {
        recoveryPopupQueue.Sort((a, b) =>
        {
            int priorityCompare = GetRecoveryPopupPriority(a.PopupType).CompareTo(GetRecoveryPopupPriority(b.PopupType));
            return priorityCompare != 0 ? priorityCompare : a.Sequence.CompareTo(b.Sequence);
        });
    }

    private static int GetRecoveryPopupPriority(PopupType popupType)
    {
        return popupType switch
        {
            PopupType.UniqueResource => 0,
            PopupType.CostRecovery => 1,
            PopupType.HealthRecovery => 2,
            _ => 3
        };
    }

    private void ShowCustomTextInternal(Transform target, string message, PopupType popupType, float fontSize = -1f)
    {
        EnsureReferences();

        if (targetCanvas == null || canvasRect == null || string.IsNullOrWhiteSpace(message))
            return;

        Vector2 screenOffset = GetRandomScreenOffset();

        if (!TryGetCanvasPosition(target, screenOffset, out Vector2 anchoredPosition))
            return;

        ShowCustomTextPopup(target, screenOffset, anchoredPosition, message, popupType, fontSize);
    }

    private static BattleDamageTextPopupUI GetOrCreateInstance()
    {
        if (instance != null && instance.gameObject.activeInHierarchy)
            return instance;

        instance = FindFirstObjectByType<BattleDamageTextPopupUI>(FindObjectsInactive.Exclude);

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

    private void UsePreferredCanvas(Canvas preferredCanvas)
    {
        if (preferredCanvas == null || !preferredCanvas.gameObject.activeInHierarchy)
        {
            EnsureReferences();
            return;
        }

        targetCanvas = preferredCanvas;
        canvasRect = targetCanvas.GetComponent<RectTransform>();
        uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;
    }

    private void UseBattleCanvas()
    {
        Canvas battleCanvas = FindBattleCanvas();
        battleOverlayCanvas = GetOrCreateBattleOverlayCanvas(battleCanvas);

        if (battleOverlayCanvas == null)
        {
            EnsureReferences();
            return;
        }

        targetCanvas = battleOverlayCanvas;
        canvasRect = targetCanvas.GetComponent<RectTransform>();
        uiCamera = null;
    }

    private Canvas GetOrCreateBattleOverlayCanvas(Canvas sourceCanvas)
    {
        if (battleOverlayCanvas != null)
            return battleOverlayCanvas;

        GameObject existing = GameObject.Find("BattleDamageTextOverlayCanvas");
        if (existing != null)
        {
            battleOverlayCanvas = existing.GetComponent<Canvas>();
            if (battleOverlayCanvas != null)
                return battleOverlayCanvas;
        }

        GameObject go = new GameObject("BattleDamageTextOverlayCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        CanvasScaler sourceScaler = sourceCanvas != null
            ? sourceCanvas.GetComponent<CanvasScaler>()
            : null;

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

        battleOverlayCanvas = canvas;
        return battleOverlayCanvas;
    }

    private void EnsureReferences()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (targetCanvas == null || !targetCanvas.gameObject.activeInHierarchy)
            targetCanvas = FindBattleCanvas();

        if (targetCanvas == null)
            targetCanvas = CreateFallbackCanvas();

        if (targetCanvas != null && canvasRect != targetCanvas.GetComponent<RectTransform>())
            canvasRect = targetCanvas.GetComponent<RectTransform>();

        if (targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            uiCamera = null;
        else if (uiCamera == null && targetCanvas != null)
            uiCamera = targetCanvas.worldCamera;
    }

    private Canvas FindBattleCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

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
        rect.sizeDelta = GetDigitPopupSize(value.Length, popupType);
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

        Color popupColor = GetPopupColor(popupType);
        int siblingIndex = 0;

        if (ShouldShowRecoveryPrefix(popupType))
        {
            if (CreateRecoveryIcon(rect, popupType, siblingIndex, popupColor))
                siblingIndex++;

            CreatePlusText(rect, siblingIndex, popupColor);
            siblingIndex++;
        }

        for (int i = 0; i < value.Length; i++)
        {
            int digit = value[i] - '0';
            CreateDigitImage(rect, digit, siblingIndex + i, popupColor);
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

    private Vector2 GetDigitPopupSize(int digitCount, PopupType popupType)
    {
        digitCount = Mathf.Max(1, digitCount);

        Vector2 effectiveDigitSize = GetEffectiveDigitSize();
        float width = effectiveDigitSize.x * digitCount + digitSpacing * Mathf.Max(0, digitCount - 1);
        float height = effectiveDigitSize.y;

        if (ShouldShowRecoveryPrefix(popupType))
        {
            Sprite icon = GetRecoveryIcon(popupType);
            if (icon != null)
            {
                width += recoveryIconSize.x + recoveryPrefixSpacing;
                height = Mathf.Max(height, recoveryIconSize.y);
            }

            width += effectiveDigitSize.x * 0.55f + recoveryPrefixSpacing;
        }

        width = Mathf.Max(effectiveDigitSize.x, width);
        return new Vector2(width, height);
    }

    private bool ShouldShowRecoveryPrefix(PopupType popupType)
    {
        return popupType == PopupType.CostRecovery ||
               popupType == PopupType.UniqueResource ||
               popupType == PopupType.HealthRecovery;
    }

    private Sprite GetRecoveryIcon(PopupType popupType)
    {
        return popupType switch
        {
            PopupType.CostRecovery => costRecoveryIcon,
            PopupType.UniqueResource => uniqueResourceIcon,
            PopupType.HealthRecovery => healthRecoveryIcon,
            _ => null
        };
    }

    private bool CreateRecoveryIcon(RectTransform parent, PopupType popupType, int order, Color popupColor)
    {
        Sprite iconSprite = GetRecoveryIcon(popupType);
        if (iconSprite == null)
            return false;

        GameObject imageObject = new GameObject("RecoveryIcon");
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = iconSprite;
        image.color = popupColor;
        image.preserveAspect = true;

        RectTransform imageRect = image.GetComponent<RectTransform>();
        imageRect.sizeDelta = recoveryIconSize;
        imageRect.SetSiblingIndex(order);

        LayoutElement layoutElement = imageObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = recoveryIconSize.x;
        layoutElement.preferredHeight = recoveryIconSize.y;
        layoutElement.minWidth = recoveryIconSize.x;
        layoutElement.minHeight = recoveryIconSize.y;

        return true;
    }

    private void CreatePlusText(RectTransform parent, int order, Color popupColor)
    {
        Vector2 effectiveDigitSize = GetEffectiveDigitSize();
        float plusWidth = effectiveDigitSize.x * 0.55f;

        GameObject plusObject = new GameObject("RecoveryPlus");
        plusObject.transform.SetParent(parent, false);

        TextMeshProUGUI plusText = plusObject.AddComponent<TextMeshProUGUI>();
        plusText.raycastTarget = false;
        plusText.alignment = TextAlignmentOptions.Center;
        plusText.textWrappingMode = TextWrappingModes.NoWrap;
        plusText.color = popupColor;
        plusText.fontSize = GetEffectiveStartFontSize();
        plusText.text = "+";

        if (useBoldText)
            plusText.fontStyle = FontStyles.Bold;

        RectTransform plusRect = plusText.GetComponent<RectTransform>();
        plusRect.sizeDelta = new Vector2(plusWidth, effectiveDigitSize.y);
        plusRect.SetSiblingIndex(order);

        LayoutElement layoutElement = plusObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = plusWidth + recoveryPrefixSpacing;
        layoutElement.preferredHeight = effectiveDigitSize.y;
        layoutElement.minWidth = plusWidth + recoveryPrefixSpacing;
        layoutElement.minHeight = effectiveDigitSize.y;
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
        text.text = ShouldShowRecoveryPrefix(popupType) ? $"+{damage}" : damage.ToString();

        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateTextFallbackAndDestroy(rect, text, canvasGroup, target, screenOffset, popupType));
    }


    private void ShowCustomTextPopup(Transform target, Vector2 screenOffset, Vector2 anchoredPosition, string message, PopupType popupType, float fontSize = -1f)
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
        float popupStartFontSize = fontSize > 0f ? fontSize : GetEffectiveStartFontSize();
        float defaultStartFontSize = Mathf.Max(1f, GetEffectiveStartFontSize());
        float popupEndFontSize = fontSize > 0f
            ? fontSize * (GetEffectiveEndFontSize() / defaultStartFontSize)
            : GetEffectiveEndFontSize();
        text.fontSize = popupStartFontSize;
        text.text = message;

        if (useBoldText)
            text.fontStyle = FontStyles.Bold;

        CanvasGroup canvasGroup = textObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(AnimateCustomTextAndDestroy(rect, text, canvasGroup, target, screenOffset, popupType, popupStartFontSize, popupEndFontSize));
    }

    private IEnumerator AnimateCustomTextAndDestroy(
        RectTransform rect,
        TMP_Text text,
        CanvasGroup canvasGroup,
        Transform target,
        Vector2 screenOffset,
        PopupType popupType,
        float popupStartFontSize,
        float popupEndFontSize)
    {
        if (rect == null || text == null || canvasGroup == null)
            yield break;

        float holdElapsed = 0f;
        Vector2 targetCanvasPosition = rect.anchoredPosition;
        Vector2 endOffset = GetEndOffset(popupType);

        text.fontSize = popupStartFontSize;
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
            text.fontSize = Mathf.Lerp(popupStartFontSize, popupEndFontSize, eased);
            rect.anchoredPosition = targetCanvasPosition + GetAnimatedOffset(endOffset, eased, popupType);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private Vector2 GetRandomScreenOffset()
    {
        return new Vector2(
            Random.Range(-randomScreenOffset.x, randomScreenOffset.x),
            Random.Range(-randomScreenOffset.y, randomScreenOffset.y));
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
            PopupType.HealthRecovery => healthRecoveryColor,
            _ => damageColor
        };
    }

    private Vector2 GetEndOffset(PopupType popupType)
    {
        Vector2 baseOffset = popupType switch
        {
            PopupType.CostRecovery => Vector2.up * straightUpCanvasMove,
            PopupType.ArmorGain => Vector2.up * armorUpCanvasMove,
            PopupType.PoisonDamage => Vector2.down * poisonDownCanvasMove,
            PopupType.UniqueResource => Vector2.up * armorUpCanvasMove,
            PopupType.HealthRecovery => Vector2.up * armorUpCanvasMove,
            _ => GetDisappearEndOffset()
        };

        return baseOffset + GetRandomDisappearOffset();
    }

    private Vector2 GetRandomDisappearOffset()
    {
        return new Vector2(
            Random.Range(-randomDisappearCanvasOffset.x, randomDisappearCanvasOffset.x),
            Random.Range(-randomDisappearCanvasOffset.y, randomDisappearCanvasOffset.y));
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
