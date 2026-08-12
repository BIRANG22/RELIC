using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleRoomCleaner : MonoBehaviour
{
    public void PrepareForMapSelection()
    {
        ClearPartyBattleRoomTemporaryStatusEffects();
        StopBattleExecution();
        ResetBattleRoomLoaders();
        ClearBattleUnits();
    }

    public void Clean()
    {
        PrepareForMapSelection();
    }

    private static void StopBattleExecution()
    {
        BattleTurnExecutor[] executors = Object.FindObjectsByType<BattleTurnExecutor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < executors.Length; i++)
            executors[i]?.ForceStopBattleExecutionForRoomEnd();
    }

    private static void ResetBattleRoomLoaders()
    {
        BattleRoomLoader[] loaders = Object.FindObjectsByType<BattleRoomLoader>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < loaders.Length; i++)
        {
            if (loaders[i] != null)
                loaders[i].ResetLoadedStateForNextBattle(true);
        }
    }

    private static void ClearBattleUnits()
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            characters[i].gameObject.SetActive(false);
            DestroyUnityObject(characters[i].gameObject);
        }

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null)
                continue;

            monsters[i].DestroyHUD();
            monsters[i].gameObject.SetActive(false);
            DestroyUnityObject(monsters[i].gameObject);
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void ClearPartyBattleRoomTemporaryStatusEffects()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
                continue;

            runtimeData.ClearBattleRoomTemporaryStatusEffects();
        }
    }
}
