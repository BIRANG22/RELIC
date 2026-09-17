using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private GameObject lockObject;
    [Tooltip("기억 버튼의 Line 이미지입니다. 비어 있으면 자식 이름 'Line'으로 자동으로 찾습니다.")]
    [SerializeField] private Image lineImage;
    [Tooltip("현재 선택된 기억에 표시할 SelectLine 오브젝트입니다. 비어 있으면 자식 이름 SelectLine으로 자동으로 찾습니다.")]
    [SerializeField] private GameObject selectLine;
    [Tooltip("마우스 오버 상태를 표시할 Back 이미지입니다. 비어 있으면 자식 이름 'Back'으로 자동으로 찾습니다.")]
    [SerializeField] private Image buttonImage;

    private static readonly Color32 LockedVisualColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    private static readonly Color32 DefaultLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    private static readonly Color32 HoverButtonColor = new Color32(0x3C, 0x44, 0x76, 0xFF);

    private Color originalIconColor = Color.white;
    private Color originalButtonColor = Color.white;
    private bool visualColorsCached;
    private bool isEquippedSelected;
    private bool isPointerInside;


    [Header("Hover Scale Effect")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleInDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;

    private SkillSettingPanel owner;
    private SkillMasterData currentSkillData;
    private int directSlotIndex = -1;

    private bool isLocked;
    private int requiredLevel;

    private Vector3 originalScale = Vector3.one;
    private bool isScaleCached;
    private Coroutine hoverScaleCoroutine;

    public SkillMasterData CurrentSkillData => currentSkillData;

    private void Awake()
    {
        EnsureUiReferences();
        CacheOriginalScale();
        ResolveLineImage();
        ResolveSelectLine();
        CacheVisualColors();
    }

    private void OnEnable()
    {
        EnsureUiReferences();
        CacheOriginalScale();
        ResolveLineImage();
        ResolveSelectLine();
        CacheVisualColors();
        ApplyLineVisualState();
        SetHoverSelected(false);
    }

    private void OnDisable()
    {
        StopHoverScaleEffect(true);
        SetHoverSelected(false);
    }

    public void Init(SkillSettingPanel panel)
    {
        Init(panel, -1);
    }

    public void Init(SkillSettingPanel panel, int slotIndex)
    {
        owner = panel;
        directSlotIndex = slotIndex;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void SetSkillData(
        SkillMasterData skillData,
        bool locked,
        int requiredLv
    )
    {
        EnsureUiReferences();
        // 데이터 갱신 중에는 현재 스케일을 강제로 1.0으로 되돌리지 않습니다.
        // 선택된 스킬은 갱신이 들어와도 1.1 배율을 그대로 유지해야 합니다.
        StopHoverScaleEffect(false);

        currentSkillData = skillData;
        isLocked = locked;
        requiredLevel = requiredLv;

        bool hasSkill = currentSkillData != null;

        // 새 CharacterSettingPanel에서는 버튼 0/1이 고정 UI이므로
        // 데이터가 없더라도 버튼 오브젝트 자체를 비활성화하지 않습니다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!hasSkill)
        {
            if (nameText != null)
                nameText.text = string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                SetRgbPreserveAlpha(iconImage, originalIconColor);
            }

            if (lockObject != null)
                lockObject.SetActive(false);

            if (lockText != null)
                lockText.text = string.Empty;

            ApplyLineVisualState();
            SetEquippedSelected(false);
            return;
        }

        if (nameText != null)
            nameText.text = GameDataLocalization.SkillName(currentSkillData);

        if (iconImage != null)
        {
            Sprite icon = SkillIconUtility.GetSkillIcon(currentSkillData.SkillId);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;

            // 알파값은 변경하지 않고 RGB만 잠금 색으로 바꿉니다.
            if (isLocked)
            {
                SetRgbPreserveAlpha(iconImage, LockedVisualColor);
            }
            else
            {
                SetRgbPreserveAlpha(iconImage, originalIconColor);
                SkillUpgradeMarkStyle.ApplyShared(iconImage, currentSkillData.SkillId);
            }
        }

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (lockText != null)
            lockText.text = isLocked
                ? $"LV.{requiredLevel}"
                : "";

        ApplyLineVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        SetHoverSelected(true);
        StartHoverScaleEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        CacheOriginalScale();
        if (scaleTarget != null && isScaleCached)
        {
            Vector3 targetScale = isEquippedSelected
                ? originalScale * hoverScale
                : originalScale;
            StartScaleTransition(targetScale);
        }

        SetHoverSelected(false);
    }

    public void Execute()
    {
        if (owner == null)
            return;

        if (currentSkillData == null)
            return;

        if (isLocked)
        {
            owner.ShowWarning(
                SettingWarningUI.GetSkillMemoryUnlockLevelMessage(requiredLevel));
            return;
        }

        if (directSlotIndex >= 0)
            owner.SelectSkillDirect(directSlotIndex, currentSkillData);
        else
            owner.SelectSkill(currentSkillData);
    }



    public void SetEquippedSelected(bool selected)
    {
        EnsureUiReferences();
        CacheOriginalScale();
        ResolveSelectLine();

        isEquippedSelected = selected;

        // SelectLine은 현재 선택된 스킬을 나타냅니다.
        if (selectLine != null)
            selectLine.SetActive(selected);

        // 선택된 스킬은 마우스가 빠져도 1.1 배율을 유지합니다.
        if (scaleTarget != null && isScaleCached && isActiveAndEnabled)
        {
            Vector3 targetScale = (selected || isPointerInside)
                ? originalScale * hoverScale
                : originalScale;
            StartScaleTransition(targetScale);
        }
    }

    private void SetHoverSelected(bool hovered)
    {
        EnsureUiReferences();
        CacheVisualColors();

        // Back은 마우스 오버 상태만 표시합니다. 잠긴 기억은 색상을 바꾸지 않습니다.
        // 알파값은 그대로 유지합니다.
        if (buttonImage != null)
        {
            Color target = (hovered && !isLocked)
                ? (Color)HoverButtonColor
                : originalButtonColor;
            SetRgbPreserveAlpha(buttonImage, target);
        }
    }

    private void ResolveSelectLine()
    {
        if (selectLine != null)
            return;

        Transform selectedTransform = transform.Find("SelectLine");
        if (selectedTransform == null)
            selectedTransform = FindChildByName(transform, "SelectLine");

        if (selectedTransform != null)
            selectLine = selectedTransform.gameObject;
    }

    private void ResolveLineImage()
    {
        if (lineImage != null)
            return;

        Transform lineTransform = transform.Find("Line");
        if (lineTransform == null)
            lineTransform = FindChildByName(transform, "Line");

        if (lineTransform != null)
            lineImage = lineTransform.GetComponent<Image>();
    }

    private void ApplyLineVisualState()
    {
        ResolveLineImage();

        if (lineImage == null)
            return;

        Color32 rgb = isLocked ? LockedVisualColor : DefaultLineColor;
        SetRgbPreserveAlpha(lineImage, rgb);
    }


    private static void SetRgbPreserveAlpha(Image image, Color rgbSource)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.r = rgbSource.r;
        color.g = rgbSource.g;
        color.b = rgbSource.b;
        image.color = color;
    }

    private void EnsureUiReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.transition = Selectable.Transition.None;

        // 호버 상태 색상은 SkillIconButton 본체가 아니라 자식 Back 이미지에 적용합니다.
        Transform backTransform = transform.Find("Back");
        if (backTransform == null)
            backTransform = FindChildByName(transform, "Back");

        if (backTransform != null)
            buttonImage = backTransform.GetComponent<Image>();
        else if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("IconImg");
            if (iconTransform == null)
                iconTransform = FindChildByName(transform, "IconImg");

            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (lockObject == null)
        {
            Transform unlockTransform = transform.Find("unlock");
            if (unlockTransform == null)
                unlockTransform = FindChildByName(transform, "unlock");

            if (unlockTransform != null)
            {
                lockObject = unlockTransform.gameObject;

                if (lockText == null)
                    lockText = unlockTransform.GetComponent<TMP_Text>();
            }
        }

        DisableLockRaycastBlocking();
    }

    private void DisableLockRaycastBlocking()
    {
        if (lockObject == null)
            return;

        Graphic[] graphics = lockObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        CanvasGroup[] canvasGroups = lockObject.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].blocksRaycasts = false;
        }
    }

    private void CacheVisualColors()
    {
        if (visualColorsCached)
            return;

        EnsureUiReferences();

        if (iconImage != null)
            originalIconColor = iconImage.color;

        if (buttonImage != null)
            originalButtonColor = buttonImage.color;

        visualColorsCached = true;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform nested = FindChildByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
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
        if (!isActiveAndEnabled || currentSkillData == null)
            return;

        CacheOriginalScale();
        if (scaleTarget == null)
            return;

        StartScaleTransition(originalScale * hoverScale);
    }

    private void StartScaleTransition(Vector3 targetScale)
    {
        if (!isActiveAndEnabled || scaleTarget == null)
            return;

        StopHoverScaleEffect(false);
        hoverScaleCoroutine = StartCoroutine(ScaleTransitionRoutine(targetScale));
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

    private IEnumerator ScaleTransitionRoutine(Vector3 targetScale)
    {
        float duration = Mathf.Max(0.01f, scaleInDuration);
        float elapsed = 0f;
        Vector3 startScale = scaleTarget.localScale;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            scaleTarget.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        scaleTarget.localScale = targetScale;
        hoverScaleCoroutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
