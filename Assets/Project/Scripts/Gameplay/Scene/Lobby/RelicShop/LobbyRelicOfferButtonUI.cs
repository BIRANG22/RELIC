using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicOfferButtonUI : MonoBehaviour
{
    private Button button;
    private Image iconImage;
    private TMP_Text priceText;
    private string relicId;
    private Action<string> purchaseRequested;

    public static LobbyRelicOfferButtonUI Create(Transform parent, string objectName)
    {
        var root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(150f, 180f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.72f);

        var iconObject = new GameObject("RelicIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        iconRect.sizeDelta = new Vector2(125f, 125f);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var priceObject = new GameObject("Price", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var priceRect = (RectTransform)priceObject.transform;
        priceRect.SetParent(rect, false);
        priceRect.anchorMin = new Vector2(0f, 0f);
        priceRect.anchorMax = new Vector2(1f, 0f);
        priceRect.pivot = new Vector2(0.5f, 0f);
        priceRect.anchoredPosition = new Vector2(0f, 10f);
        priceRect.sizeDelta = new Vector2(0f, 36f);
        TMP_Text price = priceObject.GetComponent<TMP_Text>();
        price.alignment = TextAlignmentOptions.Center;
        price.fontSize = 25f;
        price.raycastTarget = false;

        LobbyRelicOfferButtonUI view = root.AddComponent<LobbyRelicOfferButtonUI>();
        view.button = root.GetComponent<Button>();
        view.iconImage = icon;
        view.priceText = price;
        view.button.onClick.AddListener(view.RequestPurchase);
        return view;
    }

    public void Bind(LobbyRelicOffer offer, Sprite icon, Action<string> callback)
    {
        relicId = offer.RelicId;
        purchaseRequested = callback;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        priceText.text = offer.Price.ToString();
        button.interactable = true;
    }

    public void ShowSold()
    {
        priceText.text = "판매 완료";
        button.interactable = false;
    }

    public void ShowEmpty()
    {
        relicId = null;
        iconImage.enabled = false;
        priceText.text = string.Empty;
        button.interactable = false;
    }

    private void RequestPurchase()
    {
        if (button.interactable && !string.IsNullOrWhiteSpace(relicId))
            purchaseRequested?.Invoke(relicId);
    }
}
