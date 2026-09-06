using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [Tooltip("RuneSlotButton 루트 Image입니다. 룬 장착 여부와 관계없이 원래 색상을 유지합니다.")]
    [SerializeField] private Image rootImage;
    [Tooltip("잠긴 슬롯에 표시할 unlock 텍스트 오브젝트입니다. 비어 있으면 자식 이름 'unlock'으로 자동으로 찾습니다.")]
    [SerializeField] private GameObject unlockObject;

    [Header("Rune Slot Lock Visual")]
    [Tooltip("슬롯의 Line 이미지입니다. 비어 있으면 자식 이름 'Line'으로 자동으로 찾습니다.")]
    [SerializeField] private Image lineImage;
    private static readonly Color LockedLineColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    private static readonly Color UnlockedLineColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

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

    public int SlotIndex => slotIndex;
    public RuneData EquippedRune => equippedRune;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        ResolveUnlockObject();
        ResolveLineImage();
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();
    }

    private void OnEnable()
    {
        ResolveUnlockObject();
        ResolveLineImage();
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();
    }

    private void OnDisable()
    {
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
        CacheRootImage();
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyLockVisualState();
        ApplyBorderVisualState();
        ApplyRootImageVisualState();

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
        if (owner == null)
            return;

        owner.HandleRuneSlotClick(this);
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
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        ResolveUnlockObject();
        ResolveLineImage();
        ApplyLockVisualState();

        // 잠긴 슬롯도 클릭을 받아 SettingWarningUI를 표시해야 하므로 버튼은 비활성화하지 않습니다.
        if (button != null)
            button.interactable = true;

        ApplyBorderVisualState();
        ApplyRootImageVisualState();
    }

    private void ResolveUnlockObject()
    {
        if (unlockObject != null)
            return;

        Transform unlockTransform = FindDeepChild(transform, "unlock");
        if (unlockTransform != null)
            unlockObject = unlockTransform.gameObject;
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
            lineImage.color = isLocked ? LockedLineColor : UnlockedLineColor;
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
