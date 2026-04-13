using UnityEngine;

public class CharacterSelectButtonUI : MonoBehaviour
{
    [Header("Character UI")]
    public GameObject highlightObject;
    public GameObject skillListObject;

    public void OnClickCharacter()
    {
        SkillEquipUIController.Instance.SelectCharacter(this);
    }

    public void ShowHighlight(bool value)
    {
        if (highlightObject != null)
            highlightObject.SetActive(value);
    }
}