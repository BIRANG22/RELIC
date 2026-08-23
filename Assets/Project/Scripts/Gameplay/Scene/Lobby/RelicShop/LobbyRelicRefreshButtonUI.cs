using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicRefreshButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text remainingCountText;

    private Action refreshRequested;
    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;

    private void Awake()
    {
        EnsureView();
    }

    public void Initialize(Action callback)
    {
        refreshRequested = callback;
        EnsureView();
    }

    public void SetState(int price, int remainingCount, bool interactable)
    {
        if (!EnsureView())
            return;

        priceText.text = Mathf.Max(0, price).ToString();
        remainingCountText.text = $"x{Mathf.Max(0, remainingCount)}";
        button.interactable = interactable;
    }

    private bool EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = transform.Find("RefreshIcon")?.GetComponent<Image>();

        if (priceText == null)
            priceText = transform.Find("Price")?.GetComponent<TMP_Text>();

        if (remainingCountText == null)
            remainingCountText = transform.Find("Value")?.GetComponent<TMP_Text>();

        if (button == null || iconImage == null || priceText == null || remainingCountText == null)
        {
            if (!missingViewWarningLogged)
            {
                Debug.LogWarning(
                    $"[LobbyRelicRefreshButtonUI] Serialized view references are missing on '{name}'.",
                    this);
                missingViewWarningLogged = true;
            }

            return false;
        }

        EnsureClickListener();
        return true;
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
        if (button != null && button.interactable)
            refreshRequested?.Invoke();
    }
}
