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

    [Header("Border Color")]
    [SerializeField] private string equippedBorderColorHex = "#4E66DF";

    private RuneSettingPanel owner;
    private int slotIndex;
    private RuneData equippedRune;
    private bool isLocked;

    private Color normalBorderColor = Color.white;
    private bool isNormalBorderColorCached;
    private int shownInfoVersion = -1;
    private bool isPointerInside;

    public int SlotIndex => slotIndex;
    public RuneData EquippedRune => equippedRune;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();
    }

    private void OnEnable()
    {
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();
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

        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();

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

        owner.UnequipRuneFromSlot(this);
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
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (button != null)
            button.interactable = !isLocked;

        ApplyBorderVisualState();
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

        if (borderImage == null)
        {
            Image selfImage = GetComponent<Image>();

            if (selfImage != null && selfImage != iconImage)
                borderImage = selfImage;
        }
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
