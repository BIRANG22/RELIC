using UnityEngine;
using Relic.Gameplay.Data;

public class EquippedSkillPanelUI : MonoBehaviour
{
    [Header("Character Rows")]
    [SerializeField] private EquippedSkillCharacterRowUI[] characterRows;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[EquippedSkillPanelUI] DataManager is null.");
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;

        for (int i = 0; i < characterRows.Length; i++)
        {
            if (characterRows[i] == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterRows[i].Clear();
                continue;
            }

            if (characterStore.TryGet(characterId, out CharacterRuntimeData characterData))
            {
                characterRows[i].Setup(characterData);
            }
            else
            {
                Debug.LogWarning($"[EquippedSkillPanelUI] CharacterRuntimeData ¾øÀ½: {characterId}");
                characterRows[i].Clear();
            }
        }
    }
}