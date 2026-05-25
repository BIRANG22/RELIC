using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class PartySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Border")]
    [SerializeField] private Image normalBorderImg;
    [SerializeField] private Image selectedBorderImg;

    [Header("Portrait")]
    [SerializeField] private Image portraitImg;

    private CharPick charPick;
    private string currentCharacterId;
    private int partyIndex;

    public string CurrentCharacterId => currentCharacterId;
    public bool IsEmpty => string.IsNullOrWhiteSpace(currentCharacterId);
    public int PartyIndex => partyIndex;

    public void Init(CharPick pick, int index)
    {
        charPick = pick;
        partyIndex = index;

        LoadFromRuntimeStore();
    }

    public void SetChar(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[PartySlot] DataManager instance is missing.");
            return;
        }

        bool success = DataManager.Instance.PartyRuntimeStore.SetCharacter(
            partyIndex,
            characterId
        );

        if (!success)
            return;

        currentCharacterId = characterId;

        if (DataManager.Instance.PartyRuntimeStore.GetGridIndex(partyIndex) < 0)
        {
            DataManager.Instance.PartyRuntimeStore.SetGridIndex(
                partyIndex,
                partyIndex
            );
        }

        RefreshVisual();

        Debug.Log(
            $"[PartySlot] 파티 저장 완료 / " +
            $"PartyIndex: {partyIndex}, " +
            $"CharacterId: {characterId}, " +
            $"GridIndex: {DataManager.Instance.PartyRuntimeStore.GetGridIndex(partyIndex)}"
        );
    }

    public void Clear()
    {
        currentCharacterId = null;

        if (DataManager.Instance != null)
            DataManager.Instance.PartyRuntimeStore.ClearSlot(partyIndex);

        RefreshVisual();
    }

    private void LoadFromRuntimeStore()
    {
        currentCharacterId = null;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[PartySlot] DataManager instance is missing.");
            RefreshVisual();
            return;
        }

        currentCharacterId =
            DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        bool hasCharacter = !string.IsNullOrWhiteSpace(currentCharacterId);

        if (normalBorderImg != null)
            normalBorderImg.enabled = !hasCharacter;

        if (selectedBorderImg != null)
            selectedBorderImg.enabled = hasCharacter;

        if (portraitImg != null)
        {
            portraitImg.enabled = hasCharacter;
            portraitImg.sprite = hasCharacter
                ? GetCharacterPortrait(currentCharacterId)
                : null;
            portraitImg.color = Color.white;
        }
    }

    private Sprite GetCharacterPortrait(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out var icon))
            return icon;

        return null;
    }

    public bool HasCharacter()
    {
        return !string.IsNullOrWhiteSpace(currentCharacterId);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsEmpty)
            return;

        Clear();
    }
}