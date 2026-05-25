using UnityEngine;
using UnityEngine.UI;

public class BattleEnterButtonController : MonoBehaviour
{
    [Header("Party Slots")]
    [SerializeField] private PartySlot[] partySlots;

    [Header("Enter Button")]
    [SerializeField] private Button enterButton;


    private void Start()
    {
        RefreshButtonState();

    }

    private void Update()
    {
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (enterButton == null)
            return;

        enterButton.interactable = IsPartyFull();
    }

    private bool IsPartyFull()
    {
        if (partySlots == null || partySlots.Length < 3)
            return false;

        for (int i = 0; i < 3; i++)
        {
            if (partySlots[i] == null)
                return false;

            if (!partySlots[i].HasCharacter())
                return false;
        }

        return true;
    }

}