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
    [Header("Disabled Visual")]
    [SerializeField, Range(0f, 1f)] private float disabledImageBrightness = 0.7f;

    private Action refreshRequested;
    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;
    private ButtonAnimationCoroutine[] buttonEffects = Array.Empty<ButtonAnimationCoroutine>();
    private bool[] buttonEffectInitialEnabledStates = Array.Empty<bool>();
    private bool effectsDisabledByRefreshLimit;
    private Image[] refreshImages = Array.Empty<Image>();
    private Color[] refreshImageOriginalColors = Array.Empty<Color>();
    private bool refreshImageColorsCached;

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
        SetButtonEffectsEnabledByRemainingCount(remainingCount);
        ApplyRefreshImageBrightness(remainingCount <= 0);
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

        EnsureButtonEffects();
        EnsureRefreshImages();

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

    private void EnsureButtonEffects()
    {
        if (buttonEffects != null && buttonEffects.Length > 0)
            return;

        buttonEffects = GetComponentsInChildren<ButtonAnimationCoroutine>(true);
        buttonEffectInitialEnabledStates = new bool[buttonEffects.Length];

        for (int i = 0; i < buttonEffects.Length; i++)
        {
            if (buttonEffects[i] != null)
                buttonEffectInitialEnabledStates[i] = buttonEffects[i].enabled;
        }
    }

    private void SetButtonEffectsEnabledByRemainingCount(int remainingCount)
    {
        EnsureButtonEffects();

        if (remainingCount <= 0)
        {
            for (int i = 0; i < buttonEffects.Length; i++)
            {
                ButtonAnimationCoroutine effect = buttonEffects[i];
                if (effect == null)
                    continue;

                effect.ForceClearState(false);
                effect.enabled = false;
            }

            effectsDisabledByRefreshLimit = true;
            return;
        }

        if (!effectsDisabledByRefreshLimit)
            return;

        for (int i = 0; i < buttonEffects.Length; i++)
        {
            if (buttonEffects[i] == null)
                continue;

            bool shouldEnable =
                i < buttonEffectInitialEnabledStates.Length &&
                buttonEffectInitialEnabledStates[i];
            buttonEffects[i].enabled = shouldEnable;
        }

        effectsDisabledByRefreshLimit = false;
    }


    private void EnsureRefreshImages()
    {
        if (refreshImageColorsCached)
            return;

        refreshImages = GetComponentsInChildren<Image>(true);
        refreshImageOriginalColors = new Color[refreshImages.Length];

        for (int i = 0; i < refreshImages.Length; i++)
        {
            if (refreshImages[i] != null)
                refreshImageOriginalColors[i] = refreshImages[i].color;
        }

        refreshImageColorsCached = true;
    }

    private void ApplyRefreshImageBrightness(bool disabled)
    {
        EnsureRefreshImages();

        for (int i = 0; i < refreshImages.Length; i++)
        {
            Image image = refreshImages[i];
            if (image == null)
                continue;

            Color original = i < refreshImageOriginalColors.Length
                ? refreshImageOriginalColors[i]
                : image.color;

            if (!disabled)
            {
                image.color = original;
                continue;
            }

            image.color = new Color(
                original.r * disabledImageBrightness,
                original.g * disabledImageBrightness,
                original.b * disabledImageBrightness,
                original.a);
        }
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
