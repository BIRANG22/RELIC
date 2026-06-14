using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleRoomCleaner : MonoBehaviour
{
    public void Clean()
    {
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