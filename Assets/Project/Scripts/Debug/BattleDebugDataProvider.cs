using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleDebugDataProvider : MonoBehaviour
{
    [Header("Debug Characters")]
    [SerializeField] private string[] debugCharacterIds = new string[3];

    [Header("Debug Grid Indices")]
    [SerializeField] private int[] debugGridIndices = { 0, 1, 2 };

    public void CreateDebugData()
    {
        var dm = DataManager.Instance;

        dm.PartyRuntimeStore.Clear();

        for (int i = 0; i < debugCharacterIds.Length && i < 3; i++)
        {
            string characterId = debugCharacterIds[i];

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            int gridIndex = i;

            if (debugGridIndices != null && i < debugGridIndices.Length)
                gridIndex = debugGridIndices[i];

            CharacterRuntimeData runtimeData = new CharacterRuntimeData
            {
                CharacterId = characterId,
                Level = 1,
                Exp = 0,

                CurrentHealth = 100,
                CurrentStamina = 100,
                CurrentResource = 0,
                CurrentMoveLevel = 0,

                IsUnlocked = true,

                MoveSkillId = "S_Move_1",
                PassiveSkillId = "S_Passive_01",

                AbilitySkillId1 = "S_Ability_01",
                AbilitySkillId2 = null,

                UniqueSkillId = "S_Unique_01",

                EquippedItemIds = new List<string>()
            };

            dm.CharacterRuntimeStore.AddOrUpdate(runtimeData);
            dm.PartyRuntimeStore.SetSlot(i, characterId, gridIndex);

            Debug.Log($"[BattleDebugDataProvider] Slot {i}: {characterId} / Grid {gridIndex}");
        }
    }
}