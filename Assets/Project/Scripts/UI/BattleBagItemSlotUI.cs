using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBagItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IPointerClickHandler
{
    private static readonly Color NormalIconColor = Color.white;
    private static readonly Color HighlightBorderColor = new Color32(0x4E, 0x67, 0xDF, 0xFF);

    [Header("UI")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject filledRoot;
    [SerializeField] private Button button;

    private string itemId;
    private Action<BattleBagItemSlotUI> onFocus;
    private Action<BattleBagItemSlotUI> onExit;
    private Action<BattleBagItemSlotUI> onClick;

    private bool isSelected;
    private bool isHovered;
    private Color normalBorderColor = Color.white;
    private bool hasCachedNormalBorderColor;

    public string ItemId => itemId;
    public bool HasItem => !string.IsNullOrWhiteSpace(itemId);
    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        AutoBind();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClick);
            button.onClick.AddListener(HandleButtonClick);
        }
    }

    public void Setup(
        string newItemId,
        Action<BattleBagItemSlotUI> focusCallback,
        Action<BattleBagItemSlotUI> exitCallback,
        Action<BattleBagItemSlotUI> clickCallback)
    {
        AutoBind();

        itemId = string.IsNullOrWhiteSpace(newItemId) ? "" : newItemId.Trim();
        onFocus = focusCallback;
        onExit = exitCallback;
        onClick = clickCallback;
        isSelected = false;
        isHovered = false;

        Refresh();
    }

    public void Clear(
        Action<BattleBagItemSlotUI> focusCallback,
        Action<BattleBagItemSlotUI> exitCallback,
        Action<BattleBagItemSlotUI> clickCallback)
    {
        Setup("", focusCallback, exitCallback, clickCallback);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && HasItem;
        RefreshHighlight();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered && HasItem;
        RefreshHighlight();
    }

    public void ResetVisualState()
    {
        isSelected = false;
        isHovered = false;

        if (iconImage != null)
            iconImage.color = NormalIconColor;

        RefreshHighlight();
    }

    private void Refresh()
    {
        bool hasItem = HasItem;

        if (emptyRoot != null)
            emptyRoot.SetActive(!hasItem);

        if (filledRoot != null)
            filledRoot.SetActive(hasItem);

        if (button != null)
            button.interactable = hasItem;

        if (!hasItem)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconImage.color = NormalIconColor;
            }

            if (nameText != null)
                nameText.text = "";

            isSelected = false;
            isHovered = false;
            RefreshHighlight();
            return;
        }

        ItemData item = null;
        Sprite icon = null;

        if (DataManager.Instance != null)
        {
            item = DataManager.Instance.ItemDatabase.Get(itemId);

            if (DataManager.Instance.ItemIconDatabase != null)
                DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.color = NormalIconColor;
        }

        if (nameText != null)
            nameText.text = item != null && !string.IsNullOrWhiteSpace(item.Name) ? item.Name : itemId;

        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (iconImage != null)
            iconImage.color = NormalIconColor;

        if (borderImage == null)
            return;

        borderImage.color = HasItem && (isSelected || isHovered) ? HighlightBorderColor : normalBorderColor;
    }

    private void AutoBind()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (borderImage == null)
            borderImage = GetComponent<Image>();

        if (borderImage == null)
            borderImage = FindChildImageByName("Border", "Frame", "BackGround", "Background");

        if (borderImage != null && !hasCachedNormalBorderColor)
        {
            normalBorderColor = borderImage.color;
            hasCachedNormalBorderColor = true;
        }

        if (iconImage == null)
            iconImage = FindChildImageByName("Icon", "ItemIcon", "ItemImage", "Image");

        if (iconImage == borderImage)
            iconImage = FindChildImageExcept(borderImage, "Icon", "ItemIcon", "ItemImage", "Image");

        if (nameText == null)
            nameText = FindChildTextByName("Name", "ItemName", "Text", "Text (TMP)");
    }

    private Image FindChildImageByName(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDeepChild(transform, names[i]);

            if (child == null || child == transform)
                continue;

            Image image = child.GetComponent<Image>();

            if (image != null)
                return image;
        }

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].transform != transform)
                return images[i];
        }

        return null;
    }


    private Image FindChildImageExcept(Image exceptImage, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDeepChild(transform, names[i]);

            if (child == null || child == transform)
                continue;

            Image image = child.GetComponent<Image>();

            if (image != null && image != exceptImage)
                return image;
        }

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i] != exceptImage && images[i].transform != transform)
                return images[i];
        }

        return null;
    }

    private TMP_Text FindChildTextByName(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDeepChild(transform, names[i]);

            if (child == null)
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
                return text;
        }

        return GetComponentInChildren<TMP_Text>(true);
    }

    private Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void HandleButtonClick()
    {
        if (!HasItem)
            return;

        onClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        SetHovered(true);
        onFocus?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        SetHovered(false);
        onExit?.Invoke(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 버튼 선택/클릭으로는 툴팁을 띄우지 않습니다.
        // 가방 툴팁은 PointerEnter 상태에서만 표시합니다.
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        onClick?.Invoke(this);
    }
}
