using UnityEngine;

public class CharacterSelectionState : MonoBehaviour
{
    public static CharacterSelectionState Instance { get; private set; }

    public CharacterType CurrentCharacter { get; private set; } = CharacterType.None;
    public string CurrentCharacterId { get; private set; }
    public int CurrentPartySlotIndex { get; private set; } = -1;
    public int CurrentSkillSlotIndex { get; private set; } = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SelectPartySlot(int slotIndex)
    {
        CurrentPartySlotIndex = slotIndex;
    }

    public void SelectCharacter(CharacterType characterType, string characterId)
    {
        CurrentCharacter = characterType;
        CurrentCharacterId = characterId;
    }

    public void SelectSkillSlot(int slotIndex)
    {
        CurrentSkillSlotIndex = slotIndex;
    }
}