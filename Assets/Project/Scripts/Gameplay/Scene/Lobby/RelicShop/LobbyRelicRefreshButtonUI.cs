using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LobbyRelicRefreshButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [Header("Disabled Visual")]
    [SerializeField, Range(0f, 1f)] private float disabledImageBrightness = 0.7f;

    private Action<int> refreshRequested;
    private int slotIndex;
    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;
    private Image[] refreshImages = Array.Empty<Image>();
    private Color[] refreshImageOriginalColors = Array.Empty<Color>();
    private bool refreshImageColorsCached;
    private UIChildHoverTransform childHoverTransform;
    private ButtonAnimationCoroutine[] buttonEffects = Array.Empty<ButtonAnimationCoroutine>();
    private bool[] buttonEffectInitialEnabledStates = Array.Empty<bool>();
    private bool buttonEffectStatesCached;
    private bool lastMenuPanelOpen;
    private bool menuPanelStateInitialized;
    private Coroutine clickVisualResetCoroutine;
    private bool selectionColorsNormalized;

    private void Awake()
    {
        EnsureView();
        RefreshMenuPanelInteractionState(true);
    }

    private void Update()
    {
        RefreshMenuPanelInteractionState(false);
    }

    public void Initialize(int index, Action<int> callback)
    {
        slotIndex = Mathf.Max(0, index);
        refreshRequested = callback;
        EnsureView();
    }

    public void SetState(int price, bool interactable)
    {
        if (!EnsureView())
            return;

        priceText.text = Mathf.Max(0, price).ToString();
        button.interactable = interactable;
        ApplyRefreshImageBrightness(!interactable);

        bool effectsInteractable = interactable && !UIPanelButton.IsMenuPanelOpen;
        ApplyHoverInteractable(effectsInteractable);
        ApplyButtonAnimationInteractable(effectsInteractable);
    }

    private bool EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null && !selectionColorsNormalized)
        {
            button.colors = NormalizeSelectionColors(button.colors);
            selectionColorsNormalized = true;
        }

        if (iconImage == null)
            iconImage = transform.Find("Image")?.GetComponent<Image>();

        if (priceText == null)
            priceText = transform.Find("Price")?.GetComponent<TMP_Text>();

        EnsureRefreshImages();
        EnsureHoverTransform();
        EnsureButtonEffects();

        if (button == null || priceText == null)
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
            if (!ShouldApplyBrightness(image, button != null ? button.targetGraphic : null))
                continue;

            Color original = refreshImageOriginalColors[i];
            image.color = disabled
                ? new Color(original.r * disabledImageBrightness,
                            original.g * disabledImageBrightness,
                            original.b * disabledImageBrightness,
                            original.a)
                : original;
        }
    }

    public static bool ShouldApplyBrightness(Image image, Graphic buttonTargetGraphic)
    {
        return image != null && image != buttonTargetGraphic;
    }


    private void EnsureHoverTransform()
    {
        if (childHoverTransform == null)
            childHoverTransform = GetComponent<UIChildHoverTransform>();
    }

    private void ApplyHoverInteractable(bool interactable)
    {
        EnsureHoverTransform();
        if (childHoverTransform == null)
            return;

        if (!interactable)
            childHoverTransform.ApplyDefaultImmediately();

        childHoverTransform.enabled = interactable;
    }

    private void EnsureButtonEffects()
    {
        if (buttonEffectStatesCached)
            return;

        buttonEffects = GetComponentsInChildren<ButtonAnimationCoroutine>(true);
        buttonEffectInitialEnabledStates = new bool[buttonEffects.Length];
        for (int i = 0; i < buttonEffects.Length; i++)
        {
            if (buttonEffects[i] != null)
                buttonEffectInitialEnabledStates[i] = buttonEffects[i].enabled;
        }

        buttonEffectStatesCached = true;
    }

    private void ApplyButtonAnimationInteractable(bool interactable)
    {
        EnsureButtonEffects();

        for (int i = 0; i < buttonEffects.Length; i++)
        {
            ButtonAnimationCoroutine effect = buttonEffects[i];
            if (effect == null)
                continue;

            if (!interactable)
            {
                effect.ForceClearState(false);
                effect.enabled = false;
                continue;
            }

            bool shouldEnable =
                i < buttonEffectInitialEnabledStates.Length &&
                buttonEffectInitialEnabledStates[i];
            effect.enabled = shouldEnable;
        }
    }

    private void RefreshMenuPanelInteractionState(bool force)
    {
        bool menuPanelOpen = UIPanelButton.IsMenuPanelOpen;
        if (!force && menuPanelStateInitialized && menuPanelOpen == lastMenuPanelOpen)
            return;

        lastMenuPanelOpen = menuPanelOpen;
        menuPanelStateInitialized = true;

        bool effectsInteractable =
            button != null &&
            button.interactable &&
            !menuPanelOpen;

        ApplyHoverInteractable(effectsInteractable);
        ApplyButtonAnimationInteractable(effectsInteractable);
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
        if (button != null && button.interactable && !UIPanelButton.IsMenuPanelOpen)
        {
            refreshRequested?.Invoke(slotIndex);
            if (clickVisualResetCoroutine != null)
                StopCoroutine(clickVisualResetCoroutine);
            clickVisualResetCoroutine = StartCoroutine(ResetClickVisualStateAfterClick());
        }
    }

    private IEnumerator ResetClickVisualStateAfterClick()
    {
        yield return new WaitForEndOfFrame();

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && button != null)
        {
            if (eventSystem.currentSelectedGameObject == button.gameObject)
                eventSystem.SetSelectedGameObject(null);

            button.OnDeselect(new BaseEventData(eventSystem));
            button.OnPointerExit(new PointerEventData(eventSystem));
        }

        ResetClickVisualState();
        yield return null;

        if (eventSystem != null && button != null &&
            eventSystem.currentSelectedGameObject == button.gameObject)
            eventSystem.SetSelectedGameObject(null);

        ApplyRestingButtonColor();
        clickVisualResetCoroutine = null;
    }

    private void ApplyRestingButtonColor()
    {
        if (button == null || button.targetGraphic == null)
            return;

        RectTransform rect = button.transform as RectTransform;
        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        bool pointerInside = rect != null && RectTransformUtility.RectangleContainsScreenPoint(
            rect, Input.mousePosition, camera);
        button.targetGraphic.CrossFadeColor(
            GetRestingColor(button.colors, pointerInside), 0f, true, true);
    }

    public static Color GetRestingColor(ColorBlock colors, bool pointerInside)
    {
        Color color = pointerInside ? colors.highlightedColor : colors.normalColor;
        return color * colors.colorMultiplier;
    }

    public static ColorBlock NormalizeSelectionColors(ColorBlock colors)
    {
        colors.selectedColor = colors.normalColor;
        return colors;
    }

    private void ResetClickVisualState()
    {
        EnsureButtonEffects();
        for (int i = 0; i < buttonEffects.Length; i++)
        {
            ButtonAnimationCoroutine effect = buttonEffects[i];
            if (effect != null)
                effect.ForceClearState(false);
        }
    }
}
