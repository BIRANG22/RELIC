using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("Slot Icon")]
    public Image skillIconImage;

    public void OnClickSlot()
    {
        SkillEquipUIController.Instance.SelectSlot(this);
    }

    public void SetSkill(Sprite icon)
    {
        if (skillIconImage == null)
        {
            Debug.LogWarning($"{name}: skillIconImage가 연결되지 않았습니다.");
            return;
        }

        skillIconImage.sprite = icon;
        skillIconImage.enabled = true;
    }

    public void ClearSkill()
    {
        if (skillIconImage == null)
            return;

        skillIconImage.sprite = null;
        skillIconImage.enabled = false;
    }
}