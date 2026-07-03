using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneIconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
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
    [SerializeField] private bool useIconAlphaForEquipped = true;
    [SerializeField, Range(0f, 1f)] private float equippedIconAlpha = 0.35f;
    [SerializeField] private Color equippedIconColor = new Color32(0x82, 0x82, 0x82, 0xFF);
    [SerializeField] private GameObject equippedObject;
    [SerializeField] private bool useEquippedObject = false;

    [Header("Locked UI")]
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text requiredLevelText;

    private RuneSettingPanel owner;
    private RuneData currentRuneData;

    private bool isLocked;
    private bool isEquipped;
    private int requiredLevel;

    private Vector3 originalScale = Vector3.one;
    private bool isScaleCached;
    private Coroutine hoverScaleCoroutine;

    public RuneData CurrentRuneData => currentRuneData;

    private void Awake()
    {
        CacheOriginalScale();
    }

    private void OnEnable()
    {
        CacheOriginalScale();
    }

    private void OnDisable()
    {
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
            nameText.text = currentRuneData.Name;

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(currentRuneData);
            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
        }

        SetLockedState(isLocked, this.requiredLevel);
        ApplyIconVisualState();
    }

    public void SetEquippedState(bool equipped)
    {
        isEquipped = equipped;

        if (useEquippedObject && equippedObject != null)
            equippedObject.SetActive(equipped);
        else if (equippedObject != null)
            equippedObject.SetActive(false);

        ApplyIconVisualState();
    }

    private void SetLockedState(bool locked, int level)
    {
        if (lockedObject != null)
            lockedObject.SetActive(locked);

        if (requiredLevelText != null)
        {
            requiredLevelText.gameObject.SetActive(locked);
            requiredLevelText.text = locked ? "LV. " + level : "";
        }

        if (button != null)
            button.interactable = currentRuneData != null;
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

        Color baseColor = GetRuneDisplayColor(currentRuneData);

        if (isEquipped)
        {
            Color color = baseColor;

            if (useIconAlphaForEquipped)
            {
                color.a = equippedIconAlpha;
            }
            else if (equippedIconColor.a < 1f)
            {
                color.a = equippedIconColor.a;
            }

            iconImage.color = color;
            return;
        }

        iconImage.color = baseColor;
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
        ShowCurrentRuneInfo();
        StartHoverScaleEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHoverScaleEffect(true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowCurrentRuneInfo();
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
        if (runeData == null)
            return Color.white;

        string runeId = runeData.RuneId;

        if (IsRuneNumberInRange(runeId, 1, 5))
            return ParseColorOrWhite("#576DB2");

        if (IsRuneNumberInRange(runeId, 6, 10))
            return ParseColorOrWhite("#4A5681");

        if (IsRuneNumberInRange(runeId, 11, 15))
            return ParseColorOrWhite("#393B6A");

        return Color.white;
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
