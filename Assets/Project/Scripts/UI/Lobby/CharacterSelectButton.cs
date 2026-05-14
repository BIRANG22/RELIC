using UnityEngine;
using Relic.Gameplay.Data;

public class CharacterSelectButton : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private string characterId;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    public void Execute()
    {
        PlayClickSound();

        if (!SelectCharacterState())
            return;

        CreateOrUpdateRuntimeData();
        SaveCharacterToPartySlot();
    }

    private void PlayClickSound()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);
    }

    private bool SelectCharacterState()
    {
        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectButton] CharacterSelectionState instance is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[CharacterSelectButton] CharacterId is empty.");
            return false;
        }

        CharacterSelectionState.Instance.SelectCharacter(characterType, characterId);
        return true;
    }

    private void CreateOrUpdateRuntimeData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectButton] DataManager instance is missing.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var master))
        {
            Debug.LogWarning($"[CharacterSelectButton] Character master not found: {characterId}");
            return;
        }

        var runtimeStore = DataManager.Instance.CharacterRuntimeStore;

        if (!runtimeStore.TryGet(characterId, out var runtime))
        {
            runtime = new CharacterRuntimeData
            {
                CharacterId = master.CharacterId,
                Level = 1,
                Exp = 0,
                CurrentHealth = master.MaxHealth,
                CurrentStamina = master.MaxStamina,
                CurrentResource = master.MaxResource,
                IsUnlocked = master.IsDefaultProvided
            };

            runtimeStore.AddOrUpdate(runtime);
            Debug.Log("[CharacterRuntime] Created");
        }
        else
        {
            Debug.Log("[CharacterRuntime] Already Exists");
        }
    }

    private void SaveCharacterToPartySlot()
    {
        if (CharacterSelectionState.Instance == null)
            return;

        int slotIndex = CharacterSelectionState.Instance.CurrentPartySlotIndex;

        if (slotIndex < 0)
        {
            Debug.LogWarning("[CharacterSelectButton] Party slot is not selected.");
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectButton] DataManager instance is missing.");
            return;
        }

        DataManager.Instance.PartyRuntimeStore.SetCharacter(slotIndex, characterId);
    }
}