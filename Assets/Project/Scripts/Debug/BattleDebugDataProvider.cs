using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleDebugDataProvider : MonoBehaviour
{
    [Header("Debug Characters")]
    [SerializeField] private string[] debugCharacterIds = new string[3];

    [Header("Debug Grid Indices")]
    [SerializeField] private int[] debugGridIndices = { 0, 1, 2 };

    [Header("Debug Skills")]
    [SerializeField] private string defaultMoveSkillId = "S_Move_1";
    [SerializeField] private string defaultPassiveSkillId = "S_Passive_01";
    [SerializeField] private string defaultUniqueSkillId = "S_Unique_01";
    [SerializeField] private string defaultAbilitySkillId = "S_Ability_01";
    [SerializeField] private string defaultFreeSkillId1 = "S_Public_01";
    [SerializeField] private string defaultFreeSkillId2 = "";

    public void CreateDebugData()
    {
        var dm = DataManager.Instance;

        if (dm == null)
        {
            Debug.LogWarning("[BattleDebugDataProvider] DataManager가 없습니다.");
            return;
        }

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

                CurrentHP = 100,
                CurrentCost = 100,
                CurrentResource = 0,
                CurrentMoveLevel = 0,

                IsUnlocked = true,

                MoveSkillId = defaultMoveSkillId,
                PassiveSkillId = defaultPassiveSkillId,
                UniqueSkillId = defaultUniqueSkillId,
                AbilitySkillId = defaultAbilitySkillId,

                EquippedSkillIds = new string[4]
                {
                    defaultUniqueSkillId,
                    defaultAbilitySkillId,
                    defaultFreeSkillId1,
                    defaultFreeSkillId2
                },

                EquippedRuneIds = new string[12],
            };

            dm.CharacterRuntimeStore.AddOrUpdate(runtimeData);
            dm.PartyRuntimeStore.SetSlot(i, characterId, gridIndex);

            Debug.Log($"[BattleDebugDataProvider] Slot {i}: {characterId} / Grid {gridIndex}");
        }
    }
}