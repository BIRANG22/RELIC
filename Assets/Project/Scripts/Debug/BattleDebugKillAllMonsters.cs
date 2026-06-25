using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDebugKillAllMonsters : MonoBehaviour
{
    [SerializeField] private KeyCode killKey = KeyCode.K;
    [SerializeField] private KeyCode damagePlayersKey = KeyCode.J;
    [SerializeField] private int debugPlayerDamage = 1;

    private void Update()
    {
        if (Input.GetKeyDown(killKey))
            KillAllMonstersForDebug();

        if (Input.GetKeyDown(damagePlayersKey))
            DamagePlayersForDebug();
    }

    public void KillAllMonstersForDebug()
    {
        BattleDeathService deathService = new(null, null, null);
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            monster.RuntimeData.CurrentHP = 0;
            monster.RuntimeData.CurrentShield = 0;

            monster.RefreshHUD();
            deathService.HandleMonsterDead(monster);

            Debug.Log($"[DebugKill] Monster:{monster.RuntimeData.MonsterId}");
        }

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.CheckBattleEnd();
    }

    public void DamagePlayersForDebug()
    {
        int damage = Mathf.Max(0, debugPlayerDamage);

        if (damage <= 0)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            BattleEffectUtility.DamagePlayer(character, damage);
            Debug.Log(
                $"[DebugDamage] Player:{character.RuntimeData.CharacterId} Damage:{damage}"
            );
        }

        new BattleHUDService().RefreshHUDs();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.CheckBattleEnd();
    }
}

