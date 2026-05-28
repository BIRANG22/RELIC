using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SpawnGridCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private GameObject selectedObject;

    private SpawnGridPanel owner;
    private int gridIndex;

    public void Init(SpawnGridPanel panel, int index)
    {
        owner = panel;
        gridIndex = index;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Execute);
        }

        Refresh();
    }

    public void Execute()
    {
        if (owner == null)
            return;

        owner.SelectGrid(gridIndex);
    }

    public void Refresh()
    {
        string characterId = GetCharacterIdOnThisGrid();
        bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);

        if (characterIconImage != null)
        {
            characterIconImage.gameObject.SetActive(hasCharacter);
            characterIconImage.enabled = hasCharacter;
            characterIconImage.sprite = hasCharacter ? GetCharacterIcon(characterId) : null;
            characterIconImage.color = Color.white;
        }

        if (selectedObject != null)
            selectedObject.SetActive(hasCharacter);
    }

    private string GetCharacterIdOnThisGrid()
    {
        if (DataManager.Instance == null)
            return null;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetGridIndex(i) == gridIndex)
                return partyStore.GetCharacterId(i);
        }

        return null;
    }

    private Sprite GetCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out var icon))
            return icon;

        return null;
    }
}