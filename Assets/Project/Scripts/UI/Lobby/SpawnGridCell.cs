using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SpawnGridCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private GameObject selectedObject;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color selectedColor = Color.blue;

    private Image cellImage;
    private SpawnGridPanel owner;
    private int gridIndex;

    public void Init(SpawnGridPanel panel, int index)
    {
        cellImage = GetComponent<Image>();

        owner = panel;
        gridIndex = index;

        if (button == null)
            button = GetComponent<Button>();

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

        owner.OnClickCell(gridIndex);
    }

    public void Refresh()
    {
        string characterId = GetCharacterIdOnThisGrid();
        bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);
        bool isSelected = owner != null && owner.IsSelectedGrid(gridIndex);

        if (cellImage != null)
        {
            if (isSelected)
                cellImage.color = selectedColor;
            else if (hasCharacter)
                cellImage.color = occupiedColor;
            else
                cellImage.color = normalColor;
        }

        if (characterIconImage != null)
        {
            characterIconImage.gameObject.SetActive(hasCharacter);
            characterIconImage.enabled = hasCharacter;
            characterIconImage.sprite = hasCharacter ? GetCharacterIcon(characterId) : null;
            characterIconImage.color = Color.white;
        }

        if (selectedObject != null)
            selectedObject.SetActive(owner != null && owner.IsSelectedGrid(gridIndex));
    }

    private string GetCharacterIdOnThisGrid()
    {
        if (DataManager.Instance == null)
            return null;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

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