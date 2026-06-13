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

        int resolvedSlotIndex = ResolveSlotIndex();
        CharacterSelectionState.Instance.SelectPartySlot(resolvedSlotIndex);

        Debug.Log($"[PartySlotButton] Selected party slot: {resolvedSlotIndex}", this);
    }

    private int ResolveSlotIndex()
    {
        if (TryGetSlotIndexFromObjectName(out int nameSlotIndex))
            return nameSlotIndex;

        PartySlot partySlot = GetComponent<PartySlot>();

        if (partySlot != null)
            return partySlot.PartyIndex;

        return slotIndex;
    }

    private bool TryGetSlotIndexFromObjectName(out int result)
    {
        result = -1;

        string objectName = gameObject.name;

        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        int lastSeparatorIndex = objectName.LastIndexOf('_');

        if (lastSeparatorIndex < 0 || lastSeparatorIndex >= objectName.Length - 1)
            return false;

        string numberText = objectName.Substring(lastSeparatorIndex + 1);
        return int.TryParse(numberText, out result);
    }
}
