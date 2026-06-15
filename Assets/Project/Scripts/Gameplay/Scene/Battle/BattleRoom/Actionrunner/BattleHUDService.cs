using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleHUDService
{
    public void RefreshHUDs()
    {
        PlayerHUDSlot[] playerHuds =
            Object.FindObjectsByType<PlayerHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < playerHuds.Length; i++)
        {
            if (playerHuds[i] != null)
                playerHuds[i].Refresh();
        }

        MonsterHUDSlot[] monsterHuds =
            Object.FindObjectsByType<MonsterHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsterHuds.Length; i++)
        {
            if (monsterHuds[i] != null)
                monsterHuds[i].Refresh();
        }
    }

    public void HideUnselectedMonsterHUDs()
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null)
                monsters[i].HideHUDIfNotSelected();
        }
    }

    public void PlayAllAliveIdle()
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            if (characters[i].RuntimeData.CurrentHealth <= 0)
                continue;

            BattleUnitAnimator animator = characters[i].GetComponent<BattleUnitAnimator>();

            if (animator != null)
                animator.PlayIdle();
        }

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.IsDead)
                continue;

            BattleUnitAnimator animator = monsters[i].GetComponent<BattleUnitAnimator>();

            if (animator != null)
                animator.PlayIdle();
        }
    }
}