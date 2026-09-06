using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveRelicButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Root")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Display")]
    [SerializeField] private Image relicIconImage;
    [SerializeField] private TMP_Text usesText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Auto Bind Names")]
    [SerializeField] private string backgroundObjectName = "BackGround";
    [SerializeField] private string relicIconObjectName = "IconImage";
    [SerializeField] private string usesTextObjectName = "Text (TMP)";

    [Header("Color")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color hoverBackgroundColor = new(0.30588236f, 0.4f, 0.8745098f, 1f);
    [SerializeField] private Color disabledBackgroundColor = new(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color usableColor = Color.white;
    [SerializeField] private Color disabledColor = new(1f, 1f, 1f, 0.45f);

    private SkillListPanel owner;
    private CharacterRuntimeData runtimeData;
    private ActiveRelicAvailability availability;
    private bool canClick;
    private bool isPointerOver;
    private int lastSelectFrame = -1;

    private void Awake()
    {
        BindMissingReferences();
        ConfigureButton();
        ApplyVisualState();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        ApplyVisualState();
    }

    public void Setup(
        SkillListPanel ownerPanel,
        CharacterRuntimeData runtime,
        ActiveRelicAvailability activeRelicAvailability)
    {
        owner = ownerPanel;
        runtimeData = runtime;
        availability = activeRelicAvailability;
        canClick = availability != null && availability.CanUse;
        isPointerOver = false;

        BindMissingReferences();
        ConfigureButton();
        ApplyRelicData();
        ApplyVisualState();
    }

    public void Refresh(ActiveRelicAvailability activeRelicAvailability)
    {
        availability = activeRelicAvailability;
        canClick = availability != null && availability.CanUse;

        ConfigureButton();
        ApplyRelicData();
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canClick)
            return;

        isPointerOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null)
            owner.IgnoreOutsideCloseForFrames(2);
    }

    private void HandleButtonClicked()
    {
        if (lastSelectFrame == Time.frameCount)
            return;

        lastSelectFrame = Time.frameCount;

        if (!canClick || owner == null)
            return;

        owner.IgnoreOutsideCloseForFrames(2);
        owner.SelectActiveRelic(this);
    }

    private void ApplyRelicData()
    {
        if (usesText != null)
        {
            int remaining = availability != null ? availability.RemainingUses : 0;
            int max = availability != null ? availability.MaxUses : 0;
            usesText.text = $"{remaining}/{max}";
        }

        if (relicIconImage == null)
            return;

        relicIconImage.sprite = GetRelicIcon();
        relicIconImage.enabled = relicIconImage.sprite != null;
    }

    private Sprite GetRelicIcon()
    {
        string relicId = availability?.RelicId;

        if (string.IsNullOrWhiteSpace(relicId) ||
            DataManager.Instance == null ||
            DataManager.Instance.RelicIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon)
            ? icon
            : null;
    }

    private void ApplyVisualState()
    {
        Color color = canClick ? usableColor : disabledColor;

        if (backgroundImage != null)
            backgroundImage.color = GetBackgroundColor();

        if (relicIconImage != null)
            relicIconImage.color = color;

        if (usesText != null)
            usesText.color = color;

        if (button != null)
            button.interactable = canClick;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = canClick ? 1f : 0.65f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = canClick;
        }
    }

    private Color GetBackgroundColor()
    {
        if (!canClick)
            return disabledBackgroundColor;

        return isPointerOver ? hoverBackgroundColor : normalBackgroundColor;
    }

    private void BindMissingReferences()
    {
        if (backgroundImage == null)
        {
            Transform found = FindChildRecursive(transform, backgroundObjectName);
            if (found != null)
                backgroundImage = found.GetComponent<Image>();
        }

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (relicIconImage == null)
        {
            Transform found = FindChildRecursive(transform, relicIconObjectName);
            if (found != null)
                relicIconImage = found.GetComponent<Image>();
        }

        if (usesText == null)
        {
            Transform found = FindChildRecursive(transform, usesTextObjectName);
            if (found != null)
                usesText = found.GetComponent<TMP_Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void ConfigureButton()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            button = gameObject.AddComponent<Button>();

        button.targetGraphic = backgroundImage != null ? backgroundImage : relicIconImage;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normalBackgroundColor;
        colors.highlightedColor = hoverBackgroundColor;
        colors.pressedColor = hoverBackgroundColor;
        colors.selectedColor = normalBackgroundColor;
        colors.disabledColor = disabledBackgroundColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
        button.interactable = canClick;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);

            if (found != null)
                return found;
        }

        return null;
    }
}
