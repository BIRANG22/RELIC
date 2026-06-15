using Relic.Gameplay.Monster;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleTimelineMonsterHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string monsterRuntimeId;

    public void SetMonsterRuntimeId(string runtimeId)
    {
        monsterRuntimeId = runtimeId;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MonsterUnit monster = FindMonster();

        if (monster != null)
            monster.SetTimelineHoverHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MonsterUnit monster = FindMonster();

        if (monster != null)
            monster.SetTimelineHoverHighlight(false);
    }

    private MonsterUnit FindMonster()
    {
        if (string.IsNullOrWhiteSpace(monsterRuntimeId))
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == monsterRuntimeId)
                return monsters[i];
        }

        return null;
    }
}