using UnityEngine;

public class SkillSelectButtonUI : MonoBehaviour
{
    [Header("Skill Info")]
    public string skillName;
    public Sprite skillIcon;

    public void OnClickSkill()
    {
        SkillEquipUIController.Instance.SelectSkill(this);
    }
}