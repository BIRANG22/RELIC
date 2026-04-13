using UnityEngine;

public class SkillSelectButtonUI : MonoBehaviour
{
    public string skillName;
    public Sprite skillIcon;

    public void OnClickSkill()
    {
        SkillEquipUIController.Instance.SelectSkill(this);
    }
}