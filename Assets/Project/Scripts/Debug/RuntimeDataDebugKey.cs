using UnityEngine;
using Relic.Gameplay.Data;

public class RuntimeDataDebugKey : Singleton<RuntimeDataDebugKey>
{
    [SerializeField] private KeyCode debugKey = KeyCode.BackQuote;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
            PrintRuntimeData();
    }

    private void PrintRuntimeData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[RuntimeDataDebug] DataManager is missing.");
            return;
        }

        Debug.Log("========== Runtime Data Debug ==========");

        PrintPartyData();
        PrintCharacterData();
        PrintSkillData();

        Debug.Log("========================================");
    }

    private void PrintPartyData()
    {
        var partyStore = DataManager.Instance.PartyRuntimeStore;

        Debug.Log("[PartyRuntime]");

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            int gridIndex = partyStore.GetSpawnGridIndex(i);

            Debug.Log(
                $"Slot {i}: {(string.IsNullOrWhiteSpace(characterId) ? "Empty" : characterId)} / Grid: {gridIndex}"
            );
        }
    }

    private void PrintCharacterData()
    {
        var characterStore = DataManager.Instance.CharacterRuntimeStore;

        Debug.Log("[CharacterRuntime]");

        foreach (var pair in characterStore.GetAll())
        {
            var data = pair.Value;

            string equippedSkills = string.Join(", ", data.EquippedSkillIds);

            string skills =
                $"Move:{data.MoveSkillId}, " +
                $"Passive:{data.PassiveSkillId}, " +
                $"Unique:{data.UniqueSkillId}, " +
                $"Ability:{data.AbilitySkillId}, " +
                $"Slot1:{data.EquippedSkillIds[0]}, " +
                $"Slot2:{data.EquippedSkillIds[1]}, " +
                $"Slot3:{data.EquippedSkillIds[2]}, " +
                $"Slot4:{data.EquippedSkillIds[3]}";

            Debug.Log(
                $"ID:{data.CharacterId} / Lv:{data.Level} / " +
                $"HP:{data.CurrentHP} / Cost:{data.CurrentCost} / Resource:{data.CurrentResource} / " +
                $"Skills:{skills}"
            );
        }
    }

    private void PrintSkillData()
    {
        var skillStore = DataManager.Instance.SkillRuntimeStore;

        Debug.Log("[SkillRuntime]");

        foreach (var pair in skillStore.GetAll())
        {
            var data = pair.Value;

            Debug.Log(
                $"ID:{data.SkillId} / Lv:{data.Level} / " +
                $"Unlocked:{data.IsUnlocked}"
            );
        }
    }
}
