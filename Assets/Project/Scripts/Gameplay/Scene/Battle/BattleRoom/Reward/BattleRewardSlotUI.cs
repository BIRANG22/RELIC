using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleRewardSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;


    private BattleRewardData reward;
    private Sprite remnantIcon;
    private Sprite relicRewardIcon;
    private Sprite memoryRewardIcon;
    private Color remnantIconColor = Color.white;
    private Action<BattleRewardSlotUI> onClick;
    private Action<BattleRewardSlotUI> onFocus;
    private Action<BattleRewardSlotUI> onExit;
    private bool pointerDownOnSlot;
    private int validClickFrame = -1;

    public BattleRewardData Reward => reward;
    public RectTransform IconRectTransform => iconImage != null ? iconImage.rectTransform : null;
    public Color CurrentIconColor => iconImage != null ? iconImage.color : Color.white;

    private void Awake()
    {
        // Prefab hierarchy: RewardSlot / Icon, Text.
        if (iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) iconImage = icon.GetComponent<Image>();
        }
        if (nameText == null)
        {
            Transform text = transform.Find("Text");
            if (text != null) nameText = text.GetComponent<TMP_Text>();
        }
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            // Discard old prefab OnClick assignments that can call reward logic on hover exit.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(
        BattleRewardData rewardData,
        Sprite fallbackRemnantIcon,
        Color fallbackRemnantIconColor,
        Sprite fallbackRelicRewardIcon,
        Sprite fallbackMemoryRewardIcon,
        Action<BattleRewardSlotUI> clickCallback,
        Action<BattleRewardSlotUI> focusCallback,
        Action<BattleRewardSlotUI> exitCallback)
    {
        reward = rewardData;
        remnantIcon = fallbackRemnantIcon;
        remnantIconColor = fallbackRemnantIconColor;
        relicRewardIcon = fallbackRelicRewardIcon;
        memoryRewardIcon = fallbackMemoryRewardIcon;
        onClick = clickCallback;
        onFocus = focusCallback;
        onExit = exitCallback;
        ProtectDynamicNameText();

        if (reward == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Refresh();
    }

    public void RefreshLocalization()
    {
        if (isActiveAndEnabled && reward != null)
            Refresh();
    }

    private void ProtectDynamicNameText()
    {
        if (nameText == null)
            return;

        if (nameText.GetComponent<LocalizationIgnore>() == null)
            nameText.gameObject.AddComponent<LocalizationIgnore>();
        if (nameText.GetComponent<LocalizationAutoBindingIgnore>() == null)
            nameText.gameObject.AddComponent<LocalizationAutoBindingIgnore>();

        LocalizedTMPText staticWriter = nameText.GetComponent<LocalizedTMPText>();
        if (staticWriter != null)
            staticWriter.enabled = false;
        DynamicLocalizedTMPText dynamicWriter = nameText.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null)
            dynamicWriter.enabled = false;
    }

    public void SetClaimed()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        Sprite icon = reward.Icon;
        Color iconColor = Color.white;
        string displayText;

        switch (reward.Type)
        {
            case BattleRewardType.Remnant:
                icon = remnantIcon != null ? remnantIcon : reward.Icon;
                iconColor = remnantIconColor;
                displayText = $"레드 더스티움\n{Mathf.Max(0, reward.Amount)}";
                break;
            case BattleRewardType.Item:
                // Item icon and localized item name are populated by BattleRewardPanelUI.
                displayText = string.IsNullOrWhiteSpace(reward.Name) ? reward.RewardId : reward.Name;
                break;
            case BattleRewardType.Relic:
                icon = relicRewardIcon;
                displayText = "유물 획득";
                break;
            case BattleRewardType.Skill:
                icon = memoryRewardIcon;
                displayText = "기억 발현";
                break;
            default:
                displayText = reward.GetDisplayName();
                break;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = iconColor;
            iconImage.enabled = icon != null;
            // Reward icons represent reward categories, not the hidden skill identity.
            SkillUpgradeMarkStyle.ApplyShared(iconImage, (string)null);
        }

        if (nameText != null)
            nameText.text = displayText;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownOnSlot = eventData != null &&
            eventData.button == PointerEventData.InputButton.Left;
        validClickFrame = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pointerDownOnSlot && eventData != null &&
            eventData.button == PointerEventData.InputButton.Left &&
            eventData.pointerCurrentRaycast.gameObject != null &&
            (eventData.pointerCurrentRaycast.gameObject == gameObject ||
             eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(transform)))
            validClickFrame = Time.frameCount;
        pointerDownOnSlot = false;
    }

    private void HandleClick()
    {
        if (reward == null || validClickFrame != Time.frameCount)
            return;
        validClickFrame = -1;
        onClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (reward == null)
            return;

        onFocus?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerDownOnSlot = false;
        validClickFrame = -1;
        if (reward == null)
            return;

        onExit?.Invoke(this);
    }
}
