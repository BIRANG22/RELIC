using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyCharacterSlotListUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private PartyCharacterSlotUI[] slots;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (DataManager.Instance == null)
        {
            ClearAll();
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
            {
                slots[i].Clear();
                continue;
            }

            CharacterMasterData masterData =
                DataManager.Instance.CharacterDatabase.Get(characterId);

            Sprite icon = null;

            if (DataManager.Instance.CharacterIconDatabase != null)
                DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out icon);

            string characterName = masterData != null
                ? masterData.Name
                : characterId;

            slots[i].Set(characterName, icon);
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Clear();
        }
    }
}