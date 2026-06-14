using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleRoomCleaner : MonoBehaviour
{
    public void Clean()
    {
        BattleRoomLoader[] loaders = Object.FindObjectsByType<BattleRoomLoader>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < loaders.Length; i++)
        {
            if (loaders[i] != null)
                loaders[i].ResetLoadedStateForNextBattle(true);
        }

        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
                Destroy(characters[i].gameObject);
        }

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null)
                Destroy(monsters[i].gameObject);
        }
    }
}
