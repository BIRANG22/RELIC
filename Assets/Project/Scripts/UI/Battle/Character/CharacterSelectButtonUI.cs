using Relic.Gameplay.Battle;
using UnityEngine;

public class CharacterSelectButtonUI : MonoBehaviour
{
    public GameObject highlightObject;
    public GameObject skillListObject;

    private BattleCharacter battleCharacter;

    public BattleCharacter BattleCharacter => battleCharacter;
    public string CharacterId => battleCharacter != null ? battleCharacter.CharacterId : null;
    private void Awake()
    {
        battleCharacter = GetComponentInParent<BattleCharacter>();
    }

    public void OnClickCharacter()
    {
        if (battleCharacter == null || battleCharacter.RuntimeData == null)
            return;

        if (SkillEquipUIController.Instance != null)
        {
            SkillEquipUIController.Instance.SelectCharacter(this);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectButtonUI] SkillEquipUIController.Instance가 없습니다.");
        }
    }

    private void OnMouseDown()
    {
        OnClickCharacter();
    }

    public void ShowHighlight(bool value)
    {
        if (highlightObject != null)
            highlightObject.SetActive(value);
    }
}