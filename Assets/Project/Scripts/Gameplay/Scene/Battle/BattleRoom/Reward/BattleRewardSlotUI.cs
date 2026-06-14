using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleRewardSlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private BattleRewardData reward;
    private Sprite remnantIcon;
    private Action<BattleRewardSlotUI> onClick;
    private Action<BattleRewardSlotUI> onFocus;

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
        Action<BattleRewardSlotUI> clickCallback,
        Action<BattleRewardSlotUI> focusCallback)
    {
        reward = rewardData;
        remnantIcon = fallbackRemnantIcon;
        onClick = clickCallback;
        onFocus = focusCallback;

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

        if (reward.Type == BattleRewardType.Remnant && icon == null)
            icon = remnantIcon;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
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

    public void OnSelect(BaseEventData eventData)
    {
        if (reward == null)
            return;

        onFocus?.Invoke(this);
    }
}
