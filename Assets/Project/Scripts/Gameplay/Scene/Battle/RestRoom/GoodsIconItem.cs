using Relic.Gameplay.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GoodsIconItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text price;
    [SerializeField] private Text legacyPrice;

    [Header("State")]
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color purchasedIconColor = new(1f, 1f, 1f, 0.35f);

    private RestRoomShopGoods goods;
    private Action<GoodsIconItem, RestRoomShopGoods> onClicked;
    private RectTransform rectTransform;
    private bool isPurchased;

    public RestRoomShopGoods Goods => goods;

    private void Awake()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    private void OnDisable()
    {
        TimelineSkillHoverPopupUI.Instance?.Hide(this);
    }

    public void Initialize(
        RestRoomShopGoods goods,
        Action<GoodsIconItem, RestRoomShopGoods> onClicked)
    {
        AutoBind();

        this.goods = goods;
        this.onClicked = onClicked;
        isPurchased = false;

        SetIcon(goods?.Icon, goods);
        SetPriceText(goods != null ? goods.Price.ToString() : string.Empty);
        SetInteractable(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void MarkPurchased()
    {
        isPurchased = true;
        SetInteractable(false);
        SetPriceText("SOLD");

        if (iconImage != null)
            iconImage.color = purchasedIconColor;
    }

    public void ApplyDefaultLayout(Vector2 itemSize)
    {
        AutoBind();

        itemSize = new Vector2(
            Mathf.Max(1f, itemSize.x),
            Mathf.Max(1f, itemSize.y));

        if (iconImage != null && iconImage.transform != transform)
        {
            RectTransform iconRect = iconImage.rectTransform;
            float iconSide = Mathf.Min(itemSize.x * 0.74f, itemSize.y * 0.62f);

            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSide, iconSide);
            iconRect.anchoredPosition = new Vector2(0f, itemSize.y * 0.12f);
            iconRect.localScale = Vector3.one;
        }

        RectTransform priceRect = ResolvePriceRect();

        if (priceRect != null && priceRect.transform != transform)
        {
            priceRect.anchorMin = new Vector2(0.5f, 0.5f);
            priceRect.anchorMax = new Vector2(0.5f, 0.5f);
            priceRect.pivot = new Vector2(0.5f, 0.5f);
            priceRect.sizeDelta = new Vector2(itemSize.x, Mathf.Max(24f, itemSize.y * 0.22f));
            priceRect.anchoredPosition = new Vector2(0f, -itemSize.y * 0.36f);
            priceRect.localScale = Vector3.one;
        }

        if (price != null)
            price.alignment = TextAlignmentOptions.Center;

        if (legacyPrice != null)
            legacyPrice.alignment = TextAnchor.MiddleCenter;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TimelineSkillHoverPopupUI.Instance?.Hide(this);
    }

    private void HandleClick()
    {
        if (isPurchased || goods == null)
            return;

        onClicked?.Invoke(this, goods);
    }

    private void SetIcon(Sprite icon, RestRoomShopGoods sourceGoods)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;

        string skillId = sourceGoods != null && sourceGoods.Kind == RestRoomShopGoodsKind.Skill && sourceGoods.Skill != null
            ? sourceGoods.Skill.SkillId
            : null;

        iconImage.color = SkillRarityUtility.GetSkillIconColor(skillId, normalIconColor);
        iconImage.enabled = icon != null || iconImage.gameObject == gameObject;
    }

    private void SetPriceText(string text)
    {
        if (price != null)
            price.text = text;

        if (legacyPrice != null)
            legacyPrice.text = text;
    }

    private void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private RectTransform ResolvePriceRect()
    {
        if (price != null)
            return price.rectTransform;

        if (legacyPrice != null)
            return legacyPrice.rectTransform;

        return null;
    }

    private void ShowTooltip()
    {
        if (goods == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        string title = string.IsNullOrWhiteSpace(goods.DisplayName) ? goods.Id : goods.DisplayName;
        string description = goods.Description;

        if (goods.Kind == RestRoomShopGoodsKind.Skill && goods.Skill != null)
            description = SkillTooltipFormatter.BuildSkillDescription(goods.Skill, null);

        if (string.IsNullOrWhiteSpace(description))
            description = goods.Id;

        TimelineSkillHoverPopupUI.Instance?.Show(title, description, null, rectTransform, this);
    }

    private void AutoBind()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            button = GetComponentInChildren<Button>(true);

        Image namedIconImage = FindImage("IconImage");

        if (namedIconImage != null &&
            (iconImage == null || iconImage.gameObject == gameObject))
        {
            iconImage = namedIconImage;
        }

        if (iconImage == null)
            iconImage = FindBestFallbackImage();

        if (price == null)
            price = FindTextMeshPro("price");

        if (legacyPrice == null)
            legacyPrice = FindLegacyText("price");
    }

    private Image FindImage(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private Image FindBestFallbackImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].gameObject != gameObject)
                return images[i];
        }

        return images.Length > 0 ? images[0] : null;
    }

    private TMP_Text FindTextMeshPro(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);

        if (child != null && child.TryGetComponent(out TMP_Text namedText))
            return namedText;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private Text FindLegacyText(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);

        if (child != null && child.TryGetComponent(out Text namedText))
            return namedText;

        Text[] texts = GetComponentsInChildren<Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform match = FindChildRecursive(child, childName);

            if (match != null)
                return match;
        }

        return null;
    }
}
