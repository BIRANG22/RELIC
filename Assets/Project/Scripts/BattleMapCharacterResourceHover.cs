using UnityEngine;

/// <summary>
/// 월드 캐릭터의 마우스 오버를 Character Resources UI로 전달합니다.
/// </summary>
public sealed class BattleMapCharacterResourceHover : MonoBehaviour
{
    private BattleCharacterResourcesUI resourcesUI;
    private int partySlotIndex = -1;
    private Transform worldAnchor;
    private BattleCharacterResourcesUI.AnchorMode anchorMode;

    public void Configure(
        BattleCharacterResourcesUI targetUI,
        int slotIndex,
        Transform targetWorldAnchor,
        BattleCharacterResourcesUI.AnchorMode targetAnchorMode)
    {
        resourcesUI = targetUI;
        partySlotIndex = slotIndex;
        worldAnchor = targetWorldAnchor;
        anchorMode = targetAnchorMode;
    }

    private void OnMouseEnter()
    {
        resourcesUI?.ShowForPartySlot(partySlotIndex, worldAnchor, anchorMode);
    }

    private void OnMouseExit()
    {
        resourcesUI?.HideForPartySlot(partySlotIndex);
    }

    private void OnDisable()
    {
        resourcesUI?.HideForPartySlot(partySlotIndex);
    }
}
