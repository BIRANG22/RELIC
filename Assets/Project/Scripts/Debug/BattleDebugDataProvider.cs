using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleDebugDataProvider : MonoBehaviour
{
    [Header("Debug Characters")]
    [SerializeField] private string[] debugCharacterIds = new string[DebugBattlePartySetup.DefaultDebugPartySize];

    [Header("Debug Grid Indices")]
    [SerializeField] private int[] debugGridIndices = { 12, 17, 22 };

    [Header("Debug Skills")]
    [SerializeField] private string defaultMoveSkillId = "S_Move_1";
    [SerializeField] private string defaultPassiveSkillId = "S_Passive_01";
    [SerializeField] private string defaultUniqueSkillId = "S_Unique_01";
    [SerializeField] private string defaultAbilitySkillId = "S_Ability_01";
    [SerializeField] private string defaultFreeSkillId1 = "S_Public_01";
    [SerializeField] private string defaultFreeSkillId2 = "";


    private void OnValidate()
    {
        EnsureDebugArraySizes();
    }

    private void Awake()
    {
        EnsureDebugArraySizes();
    }

    private void EnsureDebugArraySizes()
    {
        if (debugCharacterIds == null || debugCharacterIds.Length != DebugBattlePartySetup.DefaultDebugPartySize)
            System.Array.Resize(ref debugCharacterIds, DebugBattlePartySetup.DefaultDebugPartySize);

        int previousGridCount = debugGridIndices != null ? debugGridIndices.Length : 0;
        if (debugGridIndices == null || debugGridIndices.Length != DebugBattlePartySetup.DefaultDebugPartySize)
            System.Array.Resize(ref debugGridIndices, DebugBattlePartySetup.DefaultDebugPartySize);

        int[] defaults = { 12, 17, 22 };
        for (int i = previousGridCount; i < debugGridIndices.Length && i < defaults.Length; i++)
            debugGridIndices[i] = defaults[i];
    }

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

        List<string> characterIds = new();
        List<int> gridIndices = new();

        for (int i = 0; i < DebugBattlePartySetup.DefaultDebugPartySize; i++)
        {
            characterIds.Add(debugCharacterIds != null && i < debugCharacterIds.Length
                ? debugCharacterIds[i]
                : string.Empty);
            gridIndices.Add(debugGridIndices != null && i < debugGridIndices.Length
                ? debugGridIndices[i]
                : i);
        }

        if (!DebugBattlePartySetup.TryCreateParty(dm, characterIds, gridIndices))
        {
            Debug.LogError("[BattleDebugDataProvider] Failed to create configured debug party.");
            return;
        }

        for (int i = 0; i < DebugBattlePartySetup.DefaultDebugPartySize; i++)
            ApplySkillOverrides(BattleEffectDebugTool.GetPartyRuntime(i));

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
