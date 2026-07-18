using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicRefreshButtonUI : MonoBehaviour
{
    private Button button;
    private TMP_Text priceText;
    private Action refreshRequested;

    public static LobbyRelicRefreshButtonUI Create(Transform parent, Sprite icon, Action callback)
    {
        GameObject root = new("RelicRefreshButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(150f, 180f);
        root.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0f);

        GameObject iconObject = new("RefreshIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        iconRect.sizeDelta = new Vector2(125f, 125f);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        GameObject priceObject = new("Price", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform priceRect = (RectTransform)priceObject.transform;
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

        LobbyRelicRefreshButtonUI view = root.AddComponent<LobbyRelicRefreshButtonUI>();
        view.button = root.GetComponent<Button>();
        view.priceText = price;
        view.refreshRequested = callback;
        view.button.onClick.AddListener(view.RequestRefresh);
        return view;
    }

    public void SetState(int price, bool interactable)
    {
        priceText.text = Mathf.Max(0, price).ToString();
        button.interactable = interactable;
    }

    private void RequestRefresh()
    {
        if (button.interactable)
            refreshRequested?.Invoke();
    }
}
