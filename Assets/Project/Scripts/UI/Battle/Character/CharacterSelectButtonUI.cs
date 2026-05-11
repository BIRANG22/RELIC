using Relic.Gameplay.Data;
using UnityEngine;

public class CharacterSelectButtonUI : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private string characterId;

    public CharacterMasterData CharacterData { get; private set; }
    public Sprite CharacterIcon => CharacterData != null ? CharacterData.Icon : null;
    public string CharacterId => characterId;

    [Header("Character UI")]
    public GameObject highlightObject;
    public GameObject skillListObject;

    private void Start()
    {
        LoadCharacterData();
    }

    private void LoadCharacterData()
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning($"{name}: CharacterId가 비어 있습니다.");
            return;
        }

        CharacterData = DataManager.Instance.CharacterDatabase.Get(characterId);

        if (CharacterData == null)
        {
            Debug.LogWarning($"{name}: CharacterData 없음: {characterId}");
            return;
        }

        if (CharacterData.Icon == null)
        {
            Debug.LogWarning($"{name}: CharacterIcon 없음: {characterId}");
        }
    }

    public void OnClickCharacter()
    {
        SkillEquipUIController.Instance.SelectCharacter(this);
    }

    private void OnMouseDown()
    {
        OnClickCharacter();
    }

    public void ShowHighlight(bool value)
    {
        if (highlightObject != null)
            highlightObject.SetActive(value);

        // 여기서는 켜지 말고, 컨트롤러에서 켜는 게 맞음
        // if (skillListObject != null)
        //     skillListObject.SetActive(true);
    }
}