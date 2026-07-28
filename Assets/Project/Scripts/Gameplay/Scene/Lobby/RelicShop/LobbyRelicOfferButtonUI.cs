using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicOfferButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    private string relicId;
    private Action<string> purchaseRequested;
    private bool clickListenerRegistered;

    private void Awake()
    {
        EnsureView();
    }

    public static LobbyRelicOfferButtonUI Create(Transform parent, string objectName)
    {
        var root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(150f, 180f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.72f);

        LobbyRelicOfferButtonUI view = root.AddComponent<LobbyRelicOfferButtonUI>();
        view.button = root.GetComponent<Button>();
        view.iconImage = CreateIcon(rect);
        view.priceText = CreatePriceText(rect);
        view.EnsureClickListener();
        return view;
    }

    public void Bind(LobbyRelicOffer offer, Sprite icon, Action<string> callback)
    {
        EnsureView();
        relicId = offer.RelicId;
        purchaseRequested = callback;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        priceText.text = offer.Price.ToString();
        button.interactable = true;
    }

    public void ShowSold()
    {
        EnsureView();
        priceText.text = "판매 완료";
        button.interactable = false;
    }

    public void ShowEmpty()
    {
        EnsureView();
        relicId = null;
        iconImage.sprite = null;
        iconImage.enabled = false;
        priceText.text = string.Empty;
        button.interactable = false;
    }

    public void SetInteractable(bool interactable)
    {
        EnsureView();

        if (button != null)
            button.interactable = interactable;
    }

    private void EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (transform is not RectTransform rect)
            return;

        if (iconImage == null)
            iconImage = transform.Find("RelicIcon")?.GetComponent<Image>();
        if (iconImage == null)
            iconImage = CreateIcon(rect);

        if (priceText == null)
            priceText = transform.Find("Price")?.GetComponent<TMP_Text>();
        if (priceText == null)
            priceText = CreatePriceText(rect);

        EnsureClickListener();
    }

    private void EnsureClickListener()
    {
        if (button == null || clickListenerRegistered)
            return;

        button.onClick.AddListener(RequestPurchase);
        clickListenerRegistered = true;
    }

    private void RequestPurchase()
    {
        if (button.interactable && !string.IsNullOrWhiteSpace(relicId))
            purchaseRequested?.Invoke(relicId);
    }

    private static Image CreateIcon(RectTransform parent)
    {
        var iconObject = new GameObject("RelicIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        iconRect.sizeDelta = new Vector2(125f, 125f);

        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    private static TMP_Text CreatePriceText(RectTransform parent)
    {
        var priceObject = new GameObject("Price", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var priceRect = (RectTransform)priceObject.transform;
        priceRect.SetParent(parent, false);
        priceRect.anchorMin = new Vector2(0f, 0f);
        priceRect.anchorMax = new Vector2(1f, 0f);
        priceRect.pivot = new Vector2(0.5f, 0f);
        priceRect.anchoredPosition = new Vector2(0f, 10f);
        priceRect.sizeDelta = new Vector2(0f, 36f);

        TMP_Text price = priceObject.GetComponent<TMP_Text>();
        price.alignment = TextAlignmentOptions.Center;
        price.fontSize = 25f;
        price.raycastTarget = false;
        return price;
    }
}
