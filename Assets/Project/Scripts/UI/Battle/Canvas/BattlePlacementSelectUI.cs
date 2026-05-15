using UnityEngine;

public class BattlePlacementSelectUI : MonoBehaviour
{
    [SerializeField] private PlacementCharacterButton[] characterButtons;

    private BattlePlacementController controller;

    public void Open(BattlePlacementController placementController)
    {
        controller = placementController;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        HideAllButtons();
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        HideAllButtons();

        var dm = DataManager.Instance;
        var partyStore = dm.PartyRuntimeStore;
        var iconDatabase = dm.CharacterIconDatabase;

        int buttonIndex = 0;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            string characterId = partyStore.GetCharacterId(slotIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (partyStore.GetGridIndex(slotIndex) >= 0)
                continue;

            if (buttonIndex >= characterButtons.Length)
                break;

            Debug.Log($"[PlacementUI] CharacterId from PartyStore: {characterId}");

            Sprite icon = null;

            if (iconDatabase == null)
            {
                Debug.LogWarning("[PlacementUI] CharacterIconDatabase is null.");
            }
            else if (!iconDatabase.TryGetIcon(characterId, out icon))
            {
                Debug.LogWarning($"[PlacementUI] Icon not found for CharacterId: {characterId}");
            }
            else
            {
                Debug.Log($"[PlacementUI] Icon found: {characterId} / {icon.name}");
            }

            characterButtons[buttonIndex].Setup(slotIndex, icon, controller);

            if (iconDatabase != null)
                iconDatabase.TryGetIcon(characterId, out icon);

            characterButtons[buttonIndex].Setup(slotIndex, icon, controller);
            buttonIndex++;
        }
    }

    private void HideAllButtons()
    {
        foreach (var button in characterButtons)
        {
            if (button != null)
                button.Hide();
        }
    }
}