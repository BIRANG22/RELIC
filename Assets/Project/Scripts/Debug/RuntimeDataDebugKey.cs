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

        for (int i = 0; i < 3; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            Debug.Log($"Slot {i}: {(string.IsNullOrWhiteSpace(characterId) ? "Empty" : characterId)}");
        }
    }

    private void PrintCharacterData()
    {
        var characterStore = DataManager.Instance.CharacterRuntimeStore;

        Debug.Log("[CharacterRuntime]");

        foreach (var pair in characterStore.GetAll())
        {
            var data = pair.Value;

            string skills = "None";

            if (data.EquippedSkillIds != null)
            {
                var validSkills = System.Array.FindAll(
                    data.EquippedSkillIds,
                    skill => !string.IsNullOrEmpty(skill)
                );

                if (validSkills.Length > 0)
                {
                    skills = string.Join(", ", validSkills);
                }
            }

            Debug.Log(
                $"ID:{data.CharacterId} / Lv:{data.Level} / " +
                $"HP:{data.CurrentHealth} / Stamina:{data.CurrentStamina} / Resource:{data.CurrentResource} / " +
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