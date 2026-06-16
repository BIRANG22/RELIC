using UnityEngine;
using UnityEngine.EventSystems;

public class BattleTimelineCharacterHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string characterId;

    public void SetCharacterId(string id)
    {
        characterId = id;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        BattleCharacter character = FindCharacter();

        if (character != null)
            character.SetTimelineHoverHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        BattleCharacter character = FindCharacter();

        if (character != null)
            character.SetTimelineHoverHighlight(false);
    }

    private BattleCharacter FindCharacter()
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            if (characters[i].CharacterId == characterId)
                return characters[i];
        }

        return null;
    }
}
