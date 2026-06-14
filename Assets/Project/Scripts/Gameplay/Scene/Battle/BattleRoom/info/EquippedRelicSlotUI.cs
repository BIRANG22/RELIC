using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedRelicSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Info")]
    [SerializeField] private int partySlotIndex;
    [SerializeField] private int relicSlotIndex;

    [Header("UI")]
    [SerializeField] private Image iconImage;

    private RelicEquipPanelUI owner;

    public int PartySlotIndex => partySlotIndex;
    public int RelicSlotIndex => relicSlotIndex;

    public void Init(RelicEquipPanelUI owner)
    {
        this.owner = owner;
    }

    public void Refresh()
    {
        string characterId = GetCharacterId();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            return;
        }

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                characterId,
                out CharacterRuntimeData character))
        {
            Clear();
            return;
        }

        RelicEquipService.EnsureRelicSlots(character);

        string relicId = character.EquippedRelicIds[relicSlotIndex];
        SetIcon(relicId);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        string characterId = GetCharacterId();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning($"[EquippedRelicSlotUI] 캐릭터 없음 / PartySlot:{partySlotIndex}");
            return;
        }

        owner?.SelectEquipSlot(characterId, relicSlotIndex);

        Debug.Log(
            $"[EquippedRelicSlotUI] 슬롯 선택 / Character:{characterId} / PartySlot:{partySlotIndex} / RelicSlot:{relicSlotIndex + 1}"
        );
    }

    private string GetCharacterId()
    {
        if (DataManager.Instance == null)
            return null;

        return DataManager.Instance.PartyRuntimeStore.GetCharacterId(partySlotIndex);
    }

    private void SetIcon(string relicId)
    {
        if (iconImage == null)
            return;

        if (!string.IsNullOrWhiteSpace(relicId) &&
            DataManager.Instance.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}