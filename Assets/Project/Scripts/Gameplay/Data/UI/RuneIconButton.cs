using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneIconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;

    [Header("Hover Scale Effect")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float breathMaxScale = 1.16f;
    [SerializeField] private float scaleInDuration = 0.08f;
    [SerializeField] private float breathSpeed = 4f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Equipped UI")]
    [SerializeField] private GameObject equippedObject;

    [Header("Locked UI")]
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text requiredLevelText;

    [Header("Purchase Selection UI")]
    [SerializeField] private GameObject selectedObject;

    private RuneSettingPanel owner;
    private RuneData currentRuneData;

    private bool isLocked;
    private bool isEquipped;
    private bool isCommonRune;
    private bool isPurchased = true;
    private bool isPurchaseSelected;
    private int requiredLevel;

    private Vector3 originalScale = Vector3.one;
    private bool isScaleCached;
    private Coroutine hoverScaleCoroutine;
    private int shownInfoVersion = -1;
    private bool isPointerInside;
    private Color originalIconColor = Color.white;
    private bool isIconColorCached;

    public RuneData CurrentRuneData => currentRuneData;

    private void Awake()
    {
        ResolveEquippedUIReferences();
        ResolveLockedUIReferences();
        ResolvePurchaseSelectionUI();
        CacheOriginalScale();
        CacheOriginalIconColor();
    }

    private void OnEnable()
    {
        ResolveEquippedUIReferences();
        ResolveLockedUIReferences();
        ResolvePurchaseSelectionUI();
        CacheOriginalScale();
        CacheOriginalIconColor();
    }

    private void OnDisable()
    {
        if (isPointerInside)
        {
            LobbyInfoHoverState.EndRuneHover();
            isPointerInside = false;
        }

        RefreshUnlockHoverState();
        StopHoverScaleEffect(true);
    }

    public void Init(RuneSettingPanel panel)
    {
        owner = panel;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void SetRuneData(RuneData runeData)
    {
        SetRuneData(runeData, false, 0);
    }

    public void SetRuneData(RuneData runeData, bool locked, int requiredLevel)
    {
        StopHoverScaleEffect(true);

        currentRuneData = runeData;
        isLocked = locked;
        this.requiredLevel = requiredLevel;
        isEquipped = false;

        bool hasRune = currentRuneData != null;
        gameObject.SetActive(hasRune);

        if (!hasRune)
        {
            SetEquippedState(false);
            SetLockedState(false, 0);
            return;
        }

        if (nameText != null)
            nameText.text = GameDataLocalization.RuneName(currentRuneData);

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(currentRuneData);
            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
        }

        SetLockedState(isLocked, this.requiredLevel);
        RefreshPurchaseSelectionVisual();
        ApplyIconVisualState();
    }

    public void SetEquippedState(bool equipped)
    {
        isEquipped = equipped;

        ResolveEquippedUIReferences();

        if (equippedObject != null)
            equippedObject.SetActive(equipped);

        ApplyIconVisualState();
    }

    public void SetPurchaseState(bool commonRune, bool purchased, bool selectedForPurchase)
    {
        isCommonRune = commonRune;
        isPurchased = purchased;
        isPurchaseSelected = selectedForPurchase;
        RefreshPurchaseSelectionVisual();
        ApplyIconVisualState();
    }

    public void SetPurchaseSelected(bool selected)
    {
        isPurchaseSelected = selected;
        RefreshPurchaseSelectionVisual();
        ApplyIconVisualState();
    }


    private void ResolveEquippedUIReferences()
    {
        if (equippedObject != null)
            return;

        Transform installationTransform = transform.Find("Installation");
        if (installationTransform == null)
            installationTransform = FindDeepChild(transform, "Installation");

        if (installationTransform != null)
            equippedObject = installationTransform.gameObject;
    }

    private void ResolvePurchaseSelectionUI()
    {
        if (selectedObject != null)
            return;

        Transform selectedTransform = transform.Find("Selected");
        if (selectedTransform == null)
            selectedTransform = FindDeepChild(transform, "Selected");

        if (selectedTransform != null)
            selectedObject = selectedTransform.gameObject;
    }

    private void RefreshPurchaseSelectionVisual()
    {
        ResolvePurchaseSelectionUI();

        if (selectedObject != null)
            selectedObject.SetActive(isCommonRune && !isPurchased && isPurchaseSelected);
    }

    private void ResolveLockedUIReferences()
    {
        if (lockedObject == null)
        {
            Transform unlockTransform = transform.Find("unlock");
            if (unlockTransform == null)
                unlockTransform = FindDeepChild(transform, "unlock");

            if (unlockTransform != null)
                lockedObject = unlockTransform.gameObject;
        }

        if (requiredLevelText == null && lockedObject != null)
            requiredLevelText = lockedObject.GetComponentInChildren<TMP_Text>(true);
    }

    private static Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindDeepChild(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void SetLockedState(bool locked, int level)
    {
        ResolveLockedUIReferences();

        if (requiredLevelText != null)
        {
            requiredLevelText.gameObject.SetActive(locked);
            requiredLevelText.text = locked ? "LV. " + level : "";
        }

        if (button != null)
            button.interactable = currentRuneData != null;

        RefreshUnlockHoverState();
    }

    private void RefreshUnlockHoverState()
    {
        ResolveLockedUIReferences();

        if (lockedObject == null)
            return;

        // 공용룬 구매 UI에서는 unlock 오브젝트를 사용하지 않습니다.
        // 전용룬의 레벨 잠금 표시만 기존 방식으로 유지합니다.
        if (isCommonRune)
        {
            lockedObject.SetActive(false);
            return;
        }

        lockedObject.SetActive(isLocked);
    }

    private void ApplyIconVisualState()
    {
        if (iconImage == null)
            return;

        if (isLocked)
        {
            iconImage.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            return;
        }

        if (isCommonRune && !isPurchased)
        {
            // 구매 대상으로 선택되어도 아이콘 밝기는 바꾸지 않습니다.
            Color baseColor = GetRuneDisplayColor(currentRuneData);
            baseColor.r *= 0.35f;
            baseColor.g *= 0.35f;
            baseColor.b *= 0.35f;
            iconImage.color = baseColor;
            return;
        }

        iconImage.color = GetRuneDisplayColor(currentRuneData);
    }

    public void Execute()
    {
        if (owner == null)
        {
            Debug.LogWarning("[RuneIconButton] owner is null.");
            return;
        }

        if (currentRuneData == null)
            return;

        owner.ShowRuneInfo(currentRuneData);
        owner.TrySelectRuneIcon(currentRuneData, isLocked, requiredLevel);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPointerInside)
        {
            LobbyInfoHoverState.BeginRuneHover();
            isPointerInside = true;
        }
        RefreshUnlockHoverState();
        ShowCurrentRuneInfo();
        shownInfoVersion = LobbyInfoHoverState.CurrentVersion;
        StartHoverScaleEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerInside)
        {
            LobbyInfoHoverState.EndRuneHover();
            isPointerInside = false;
        }
        RefreshUnlockHoverState();
        StopHoverScaleEffect(true);

        // 프리뷰에서는 호버가 끝나면 기본 안내 정보로 돌아갑니다.
        // 룬 세팅에서는 마지막으로 확인한 정보를 유지합니다.
        if (owner != null && owner.ShouldClearInfoOnHoverExit && shownInfoVersion >= 0)
            owner.ClearRuneInfoFromHover(shownInfoVersion);

        shownInfoVersion = -1;
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowCurrentRuneInfo();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        owner?.HandleRuneIconDeselected(this);
    }

    private void ShowCurrentRuneInfo()
    {
        if (owner == null || currentRuneData == null)
            return;

        owner.ShowRuneInfo(currentRuneData);
    }

    private void CacheOriginalScale()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        if (scaleTarget == null)
            return;

        if (isScaleCached)
            return;

        originalScale = scaleTarget.localScale;
        isScaleCached = true;
    }

    private void StartHoverScaleEffect()
    {
        if (!isActiveAndEnabled)
            return;

        if (currentRuneData == null)
            return;

        CacheOriginalScale();

        if (scaleTarget == null)
            return;

        StopHoverScaleEffect(false);
        hoverScaleCoroutine = StartCoroutine(HoverScaleRoutine());
    }

    private void StopHoverScaleEffect(bool resetScale)
    {
        if (hoverScaleCoroutine != null)
        {
            StopCoroutine(hoverScaleCoroutine);
            hoverScaleCoroutine = null;
        }

        if (resetScale && scaleTarget != null && isScaleCached)
            scaleTarget.localScale = originalScale;
    }

    private IEnumerator HoverScaleRoutine()
    {
        float safeScaleInDuration = Mathf.Max(0.01f, scaleInDuration);
        float elapsed = 0f;

        Vector3 startScale = scaleTarget.localScale;
        Vector3 firstTargetScale = originalScale * hoverScale;

        while (elapsed < safeScaleInDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / safeScaleInDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            scaleTarget.localScale = Vector3.LerpUnclamped(startScale, firstTargetScale, t);
            yield return null;
        }

        float time = 0f;
        float minScale = hoverScale;
        float maxScale = Mathf.Max(hoverScale, breathMaxScale);

        while (true)
        {
            time += GetDeltaTime() * breathSpeed;

            float pingPong = (Mathf.Sin(time) + 1f) * 0.5f;
            float currentScale = Mathf.Lerp(minScale, maxScale, pingPong);

            scaleTarget.localScale = originalScale * currentScale;
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private Color GetRuneDisplayColor(RuneData runeData)
    {
        CacheOriginalIconColor();
        return originalIconColor;
    }

    private void CacheOriginalIconColor()
    {
        if (isIconColorCached || iconImage == null)
            return;

        originalIconColor = iconImage.color;
        isIconColorCached = true;
    }

    private bool IsRuneNumberInRange(string runeId, int min, int max)
    {
        int runeNumber = GetTrailingNumber(runeId);
        return runeNumber >= min && runeNumber <= max;
    }

    private int GetTrailingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return -1;

        int end = value.Length - 1;

        while (end >= 0 && char.IsWhiteSpace(value[end]))
            end--;

        if (end < 0 || !char.IsDigit(value[end]))
            return -1;

        int start = end;

        while (start >= 0 && char.IsDigit(value[start]))
            start--;

        string numberText = value.Substring(start + 1, end - start);

        if (int.TryParse(numberText, out int number))
            return number;

        return -1;
    }

    private Color ParseColorOrWhite(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;

        return Color.white;
    }

    private Sprite GetRuneIcon(RuneData runeData)
    {
        if (runeData == null)
        {
            Debug.LogWarning("[RuneIconButton] RuneData is null.");
            return null;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[RuneIconButton] DataManager is null.");
            return null;
        }

        if (DataManager.Instance.RuneIconDatabase == null)
        {
            Debug.LogWarning("[RuneIconButton] RuneIconDatabase is null.");
            return null;
        }

        if (DataManager.Instance.RuneIconDatabase.TryGetIcon(runeData.RuneId, out var icon))
            return icon;

        Debug.LogWarning($"[RuneIconButton] Icon Missing: {runeData.RuneId}");
        return null;
    }
}
