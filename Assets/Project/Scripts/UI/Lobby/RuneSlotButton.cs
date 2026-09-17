using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [Tooltip("RuneSlotButton 루트 Image입니다. 룬 장착 여부와 관계없이 원래 색상을 유지합니다.")]
    [SerializeField] private Image rootImage;
    [Tooltip("호버 시 색상이 변경되는 Back 이미지입니다. 비어 있으면 자식 이름 'Back'으로 자동 연결합니다.")]
    [SerializeField] private Image backImage;

    [Header("Hover Scale Effect")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleInDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("잠긴 슬롯에 표시할 unlock 텍스트 오브젝트입니다. 비어 있으면 자식 이름 'unlock'으로 자동으로 찾습니다.")]
    [SerializeField] private GameObject unlockObject;

    [Header("Rune Slot Lock Visual")]
    [Tooltip("슬롯의 Line 이미지입니다. 비어 있으면 자식 이름 'Line'으로 자동으로 찾습니다.")]
    [SerializeField] private Image lineImage;
    [Tooltip("파편이 장착된 슬롯에 표시할 Select_Line 오브젝트입니다. 비어 있으면 자식 이름 'Select_Line'으로 자동으로 찾습니다.")]
    [SerializeField] private GameObject selectLineObject;
    private static readonly Color LockedLineColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    private static readonly Color UnlockedLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);

    [Header("Border Color")]
    [SerializeField] private string equippedBorderColorHex = "#4E66DF";

    private RuneSettingPanel owner;
    private int slotIndex;
    private RuneData equippedRune;
    private bool isLocked;

    private Color normalBorderColor = Color.white;
    private Color rootImageOriginalColor = Color.black;
    private bool isRootImageColorCached;
    private bool isNormalBorderColorCached;
    private int shownInfoVersion = -1;
    private bool isPointerInside;
    private Color originalBackColor = Color.white;
    private bool isBackColorCached;
    private Vector3 originalScale = Vector3.one;
    private bool isScaleCached;
    private Coroutine hoverScaleCoroutine;
    private bool suppressClickOnce;

    public int SlotIndex => slotIndex;
    public RuneData EquippedRune => equippedRune;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        ResolveUnlockObject();
        ResolveLineImage();
        ResolveSelectLineObject();
        ResolveHoverVisualReferences();
        CacheOriginalBackColor();
        CacheOriginalScale();
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();
        ApplySelectLineState();
    }

    private void OnEnable()
    {
        ResolveUnlockObject();
        ResolveLineImage();
        ResolveSelectLineObject();
        ResolveHoverVisualReferences();
        CacheOriginalBackColor();
        CacheOriginalScale();
        SetBackHoverState(false);
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();
        ApplySelectLineState();
        ApplyEquippedScaleState();
    }

    private void OnDisable()
    {
        SetBackHoverState(false);
        StopHoverScaleEffect(true);

        if (isPointerInside)
        {
            LobbyInfoHoverState.EndRuneHover();
            isPointerInside = false;
        }
    }

    public void Init(RuneSettingPanel panel, int index)
    {
        owner = panel;
        slotIndex = index;

        ResolveUnlockObject();
        ResolveLineImage();
        ResolveSelectLineObject();
        ResolveHoverVisualReferences();
        CacheOriginalBackColor();
        CacheOriginalScale();
        SetBackHoverState(false);
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();
        ApplySelectLineState();
        ApplyEquippedScaleState();

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetBackHoverState(true);
        StartHoverScaleEffect();

        if (!isPointerInside)
        {
            LobbyInfoHoverState.BeginRuneHover();
            isPointerInside = true;
        }

        if (owner != null)
        {
            owner.ShowRuneSlotInfo(slotIndex, equippedRune, isLocked);
            shownInfoVersion = LobbyInfoHoverState.CurrentVersion;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetBackHoverState(false);
        CacheOriginalScale();
        if (scaleTarget != null && isScaleCached)
        {
            Vector3 targetScale = equippedRune != null
                ? originalScale * hoverScale
                : originalScale;
            StartScaleTransition(targetScale);
        }

        if (isPointerInside)
        {
            LobbyInfoHoverState.EndRuneHover();
            isPointerInside = false;
        }

        // 프리뷰에서는 기본 안내 정보로 돌아가고,
        // 룬 세팅에서는 마지막으로 확인한 정보를 유지합니다.
        if (owner != null && owner.ShouldClearInfoOnHoverExit && shownInfoVersion >= 0)
            owner.ClearRuneInfoFromHover(shownInfoVersion);

        shownInfoVersion = -1;
    }

    public void Execute()
    {
        if (suppressClickOnce)
        {
            suppressClickOnce = false;
            return;
        }

        if (owner == null)
            return;

        owner.HandleRuneSlotClick(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || equippedRune == null || isLocked)
            return;

        if (owner.TryBeginRuneDrag(equippedRune, this, iconImage, eventData))
            suppressClickOnce = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.UpdateRuneDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndRuneDrag(eventData);
        StartCoroutine(ClearDragClickSuppressionNextFrame());
    }

    private IEnumerator ClearDragClickSuppressionNextFrame()
    {
        yield return null;
        suppressClickOnce = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.DropDraggedRuneOnSlot(this);
    }

    public void SetRune(RuneData runeData)
    {
        equippedRune = runeData;

        if (nameText != null)
            nameText.text = equippedRune != null ? GameDataLocalization.RuneName(equippedRune) : "";

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(equippedRune);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = GetRuneDisplayColor(equippedRune);
        }

        ApplyBorderVisualState();
        ApplyRootImageVisualState();
        ApplySelectLineState();
        ApplyEquippedScaleState();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        ResolveUnlockObject();
        ResolveLineImage();
        ApplyLockVisualState();
        SetBackHoverState(isPointerInside);

        // 잠긴 슬롯도 클릭을 받아 SettingWarningUI를 표시해야 하므로 버튼은 비활성화하지 않습니다.
        if (button != null)
            button.interactable = true;

        ApplyBorderVisualState();
        ApplyRootImageVisualState();
    }

    private void ResolveHoverVisualReferences()
    {
        if (backImage == null)
        {
            Transform backTransform = FindDeepChild(transform, "Back");
            if (backTransform != null)
                backImage = backTransform.GetComponent<Image>();
        }

        if (scaleTarget == null)
            scaleTarget = transform;
    }

    private void CacheOriginalBackColor()
    {
        ResolveHoverVisualReferences();

        if (isBackColorCached || backImage == null)
            return;

        originalBackColor = backImage.color;
        isBackColorCached = true;
    }

    private void SetBackHoverState(bool hovered)
    {
        ResolveHoverVisualReferences();
        CacheOriginalBackColor();

        if (backImage == null)
            return;

        Color target = originalBackColor;
        // 잠긴 슬롯은 호버 시 스케일만 반응하고 Back 색상은 변경하지 않습니다.
        if (hovered && !isLocked && ColorUtility.TryParseHtmlString("#3C4476", out Color hoverColor))
            target = hoverColor;

        Color color = backImage.color;
        color.r = target.r;
        color.g = target.g;
        color.b = target.b;
        backImage.color = color;
    }

    private void CacheOriginalScale()
    {
        ResolveHoverVisualReferences();

        if (scaleTarget == null || isScaleCached)
            return;

        originalScale = scaleTarget.localScale;
        isScaleCached = true;
    }

    private void StartHoverScaleEffect()
    {
        if (!isActiveAndEnabled)
            return;

        CacheOriginalScale();

        if (scaleTarget == null)
            return;

        StartScaleTransition(originalScale * hoverScale);
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

    private void ApplyEquippedScaleState()
    {
        CacheOriginalScale();

        if (scaleTarget == null || !isScaleCached)
            return;

        Vector3 targetScale = (equippedRune != null || isPointerInside)
            ? originalScale * hoverScale
            : originalScale;

        if (isActiveAndEnabled)
            StartScaleTransition(targetScale);
        else
            scaleTarget.localScale = targetScale;
    }

    private void StartScaleTransition(Vector3 targetScale)
    {
        if (!isActiveAndEnabled || scaleTarget == null)
            return;

        if (hoverScaleCoroutine != null)
        {
            StopCoroutine(hoverScaleCoroutine);
            hoverScaleCoroutine = null;
        }

        hoverScaleCoroutine = StartCoroutine(ScaleTransitionRoutine(targetScale));
    }

    private IEnumerator ScaleTransitionRoutine(Vector3 targetScale)
    {
        float safeDuration = Mathf.Max(0.01f, scaleInDuration);
        float elapsed = 0f;
        Vector3 startScale = scaleTarget.localScale;

        while (elapsed < safeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            scaleTarget.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        scaleTarget.localScale = targetScale;
        hoverScaleCoroutine = null;
    }

    private void ResolveUnlockObject()
    {
        if (unlockObject == null)
        {
            Transform unlockTransform = FindDeepChild(transform, "unlock");
            if (unlockTransform != null)
                unlockObject = unlockTransform.gameObject;
        }

        DisableUnlockRaycastBlocking();
    }

    private void DisableUnlockRaycastBlocking()
    {
        if (unlockObject == null)
            return;

        Graphic[] graphics = unlockObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        CanvasGroup[] canvasGroups = unlockObject.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].blocksRaycasts = false;
        }
    }


    private void ResolveSelectLineObject()
    {
        if (selectLineObject != null)
            return;

        Transform selectLineTransform = FindDeepChild(transform, "Select_Line");
        if (selectLineTransform == null)
            selectLineTransform = FindDeepChild(transform, "SelectLine");

        if (selectLineTransform != null)
            selectLineObject = selectLineTransform.gameObject;
    }

    private void ApplySelectLineState()
    {
        ResolveSelectLineObject();

        if (selectLineObject != null)
            selectLineObject.SetActive(equippedRune != null);
    }

    private void ResolveLineImage()
    {
        if (lineImage != null)
            return;

        Transform lineTransform = FindDeepChild(transform, "Line");
        if (lineTransform != null)
            lineImage = lineTransform.GetComponent<Image>();
    }

    private void ApplyLockVisualState()
    {
        if (unlockObject != null)
            unlockObject.SetActive(isLocked);

        if (lineImage != null)
        {
            Color rgb = isLocked ? LockedLineColor : UnlockedLineColor;
            Color color = lineImage.color;
            color.r = rgb.r;
            color.g = rgb.g;
            color.b = rgb.b;
            lineImage.color = color;
        }
    }

    private void CacheRootImage()
    {
        if (rootImage == null)
            rootImage = GetComponent<Image>();

        if (rootImage == null || isRootImageColorCached)
            return;

        rootImageOriginalColor = rootImage.color;
        isRootImageColorCached = true;
    }

    private void ApplyRootImageVisualState()
    {
        CacheRootImage();

        if (rootImage == null)
            return;

        // 룬 장착/해제 시에도 RuneSlotButton 자체의 배경색은 변경하지 않습니다.
        rootImage.color = rootImageOriginalColor;

        // Button Color Tint가 Target Graphic에 흰색을 덮어쓰지 않도록
        // 루트 Image가 Target Graphic인 경우 모든 상태의 Tint를 흰색으로 고정합니다.
        // 실제 표시색은 rootImage.color(#000000 등)가 그대로 유지됩니다.
        if (button != null && button.targetGraphic == rootImage)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;

            rootImage.CrossFadeColor(Color.white, 0f, true, true);
        }
    }

    private void CacheBorderImage()
    {
        if (borderImage != null)
            return;

        Transform borderTransform = FindDeepChild(transform, "Border");

        if (borderTransform == null)
            borderTransform = FindDeepChild(transform, "Frame");

        if (borderTransform == null)
            borderTransform = FindDeepChild(transform, "Outline");

        if (borderTransform != null)
            borderImage = borderTransform.GetComponent<Image>();

    }

    private void CacheNormalBorderColor()
    {
        if (borderImage == null)
            return;

        if (isNormalBorderColorCached)
            return;

        normalBorderColor = borderImage.color;
        isNormalBorderColorCached = true;
    }

    private void ApplyBorderVisualState()
    {
        if (borderImage == null)
            return;

        CacheNormalBorderColor();

        if (equippedRune != null)
        {
            borderImage.color = GetEquippedBorderColor();
            return;
        }

        borderImage.color = normalBorderColor;
    }

    private Color GetEquippedBorderColor()
    {
        if (ColorUtility.TryParseHtmlString(equippedBorderColorHex, out Color color))
            return color;

        if (ColorUtility.TryParseHtmlString("#4E66DF", out color))
            return color;

        return normalBorderColor;
    }

    private Color GetRuneDisplayColor(RuneData runeData)
    {
        // 룬을 장착 슬롯에 표시할 때도 아이콘 스프라이트의 원본 색상을 유지합니다.
        // 장착 여부는 테두리와 별도 장착 표시로만 구분합니다.
        return Color.white;
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private Sprite GetRuneIcon(RuneData runeData)
    {
        if (runeData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase.TryGetIcon(runeData.RuneId, out var icon))
            return icon;

        return null;
    }
}
