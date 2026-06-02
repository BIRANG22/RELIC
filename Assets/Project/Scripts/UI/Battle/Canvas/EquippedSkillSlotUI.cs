using UnityEngine;
using UnityEngine.UI;

public class EquippedSkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public void SetSkill(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void Clear()
    {
        if (iconImage == null)
            return;

        iconImage.sprite = null;
        iconImage.enabled = false;
    }
}