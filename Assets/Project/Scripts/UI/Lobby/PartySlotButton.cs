using UnityEngine;

public class PartySlotButton : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private bool playClickSound = false;

    public void Execute()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[PartySlotButton] CharacterSelectionState instance is missing.");
            return;
        }

        CharacterSelectionState.Instance.SelectPartySlot(slotIndex);
    }
}