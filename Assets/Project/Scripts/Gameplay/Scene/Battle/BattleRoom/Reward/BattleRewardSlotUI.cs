using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleRewardSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private BattleRewardData reward;
    private Sprite remnantIcon;
    private Color remnantIconColor = Color.white;
    private Action<BattleRewardSlotUI> onClick;
    private Action<BattleRewardSlotUI> onFocus;
    private Action<BattleRewardSlotUI> onExit;

    public BattleRewardData Reward => reward;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(
        BattleRewardData rewardData,
        Sprite fallbackRemnantIcon,
        Color fallbackRemnantIconColor,
        Action<BattleRewardSlotUI> clickCallback,
        Action<BattleRewardSlotUI> focusCallback,
        Action<BattleRewardSlotUI> exitCallback)
    {
        reward = rewardData;
        remnantIcon = fallbackRemnantIcon;
        remnantIconColor = fallbackRemnantIconColor;
        onClick = clickCallback;
        onFocus = focusCallback;
        onExit = exitCallback;

        if (reward == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Refresh();
    }

    public void SetClaimed()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        Sprite icon = reward.Icon;
        Color iconColor = Color.white;

        if (reward.Type == BattleRewardType.Remnant)
        {
            if (icon == null)
                icon = remnantIcon;

            iconColor = remnantIconColor;
        }
        else if (reward.Type == BattleRewardType.Skill)
        {
            iconColor = SkillRarityUtility.GetSkillIconColor(reward.RewardId);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = iconColor;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = reward.GetDisplayName();
    }

    private void HandleClick()
    {
        if (reward == null)
            return;

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
        if (reward == null)
            return;

        onExit?.Invoke(this);
    }
}
