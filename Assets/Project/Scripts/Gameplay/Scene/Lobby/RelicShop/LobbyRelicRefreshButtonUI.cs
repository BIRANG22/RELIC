using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicRefreshButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    private Action refreshRequested;
    private bool clickListenerRegistered;

    private void Awake()
    {
        EnsureView();
    }

    public static LobbyRelicRefreshButtonUI Create(Transform parent, Sprite icon, Action callback)
    {
        GameObject root = new("RelicRefreshButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(150f, 180f);
        root.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0f);

        LobbyRelicRefreshButtonUI view = root.AddComponent<LobbyRelicRefreshButtonUI>();
        view.button = root.GetComponent<Button>();
        view.iconImage = CreateIcon(rect);
        view.priceText = CreatePriceText(rect);
        view.Initialize(icon, callback);
        return view;
    }

    public void Initialize(Sprite icon, Action callback)
    {
        refreshRequested = callback;
        EnsureView();

        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void SetState(int price, bool interactable)
    {
        EnsureView();
        priceText.text = Mathf.Max(0, price).ToString();
        button.interactable = interactable;
    }

    private void EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (transform is not RectTransform rect)
            return;

        if (iconImage == null)
            iconImage = transform.Find("RefreshIcon")?.GetComponent<Image>();
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

        button.onClick.AddListener(RequestRefresh);
        clickListenerRegistered = true;
    }

    private void RequestRefresh()
    {
        if (button.interactable)
            refreshRequested?.Invoke();
    }

    private static Image CreateIcon(RectTransform parent)
    {
        GameObject iconObject = new("RefreshIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        iconRect.sizeDelta = new Vector2(125f, 125f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        return iconImage;
    }

    private static TMP_Text CreatePriceText(RectTransform parent)
    {
        GameObject priceObject = new("Price", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform priceRect = (RectTransform)priceObject.transform;
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
