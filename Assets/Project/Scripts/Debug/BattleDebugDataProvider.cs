using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleDebugDataProvider : MonoBehaviour
{
    [Header("Debug Characters")]
    [SerializeField] private string[] debugCharacterIds = new string[DebugBattlePartySetup.DefaultDebugPartySize];

    [Header("Debug Grid Indices")]
    [SerializeField] private int[] debugGridIndices = { 12 };

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

        if (!HasAnyDebugCharacterId())
        {
            if (!DebugBattlePartySetup.TryCreateDefaultParty(dm))
                Debug.LogError("[BattleDebugDataProvider] Failed to create default debug party.");

            return;
        }

        for (int i = 0; i < debugCharacterIds.Length && i < DebugBattlePartySetup.DefaultDebugPartySize; i++)
        {
            string characterId = debugCharacterIds[i];

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            int gridIndex = i;

            if (debugGridIndices != null && i < debugGridIndices.Length)
                gridIndex = debugGridIndices[i];

            if (!DebugBattlePartySetup.TryCreateSingleCharacterParty(dm, characterId, gridIndex))
                Debug.LogError($"[BattleDebugDataProvider] Failed to create debug character: {characterId}");
            else
                Debug.Log($"[BattleDebugDataProvider] Slot {i}: {characterId} / Grid {gridIndex}");

            ApplySkillOverrides(BattleEffectDebugTool.GetPartyRuntime(0));
            break;
        }

        DebugBattlePartySetup.EnsureSkillVfxTestSkill(dm);
    }

    private void ApplySkillOverrides(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return;

        runtimeData.MoveSkillId = defaultMoveSkillId;
        runtimeData.PassiveSkillId = defaultPassiveSkillId;
        runtimeData.UniqueSkillId = defaultUniqueSkillId;
        runtimeData.AbilitySkillId = defaultAbilitySkillId;
        runtimeData.EquippedSkillIds = new string[4]
        {
            defaultUniqueSkillId,
            defaultAbilitySkillId,
            defaultFreeSkillId1,
            defaultFreeSkillId2
        };
    }

    private bool HasAnyDebugCharacterId()
    {
        if (debugCharacterIds == null)
            return false;

        for (int i = 0; i < debugCharacterIds.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(debugCharacterIds[i]))
                return true;
        }

        return false;
    }
}
