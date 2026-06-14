using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    public void Setup(BattleRewardData reward, Sprite remnantIcon)
    {
        if (reward == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        Sprite icon = reward.Icon;

        if (reward.Type == BattleRewardType.Remnant)
            icon = remnantIcon;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = reward.Name;
    }
}