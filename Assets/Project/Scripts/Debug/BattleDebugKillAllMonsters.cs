using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDebugKillAllMonsters : MonoBehaviour
{
    private static readonly bool CheatEnabled = true;

    [SerializeField] private KeyCode killKey = KeyCode.K;
    [SerializeField] private KeyCode killPlayersKey = KeyCode.J;
    [SerializeField] private int debugPlayerDamage = 1;

    private void Update()
    {
        if (!CheatEnabled)
            return;

        if (Input.GetKeyDown(killKey))
            KillAllMonstersForDebug();

        if (Input.GetKeyDown(killPlayersKey))
            KillAllPlayersForDebug();
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

        if (AreAllMonstersDead())
            PrepareBattleStateForDebugWin();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.CheckBattleEnd();
    }

    private static bool AreAllMonstersDead()
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster?.RuntimeData != null && !monster.RuntimeData.IsDead)
                return false;
        }

        return true;
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

    public void KillAllPlayersForDebug()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        KillPlayersForDebug(characters);
        PrepareBattleStateForDebugWin();
        new BattleHUDService().RefreshHUDs();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.CheckBattleEnd();
    }

    private static void KillPlayersForDebug(BattleCharacter[] characters)
    {
        if (characters == null)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];
            if (character == null || character.RuntimeData == null)
                continue;

            if (!character.RuntimeData.IsDead)
                character.RuntimeData.HandleDeath();

            BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
            if (animator != null)
                animator.PlayDead();

            Debug.Log($"[DebugKill] Player:{character.RuntimeData.CharacterId}");
        }
    }

    private void PrepareBattleStateForDebugWin()
    {
        BattleTurnExecutor turnExecutor = Object.FindFirstObjectByType<BattleTurnExecutor>(
            FindObjectsInactive.Include
        );

        if (turnExecutor != null)
        {
            turnExecutor.ForceStopBattleExecutionForRoomEnd();
            return;
        }

        SkillListPanel skillListPanel = Object.FindFirstObjectByType<SkillListPanel>(
            FindObjectsInactive.Include
        );

        if (skillListPanel != null)
            skillListPanel.CloseForBattleExecution();

        MoveGhostPreview moveGhostPreview = Object.FindFirstObjectByType<MoveGhostPreview>(
            FindObjectsInactive.Include
        );

        if (moveGhostPreview != null)
            moveGhostPreview.ClearAll();

        BattleTimelineController timelineController = Object.FindFirstObjectByType<BattleTimelineController>(
            FindObjectsInactive.Include
        );

        if (timelineController == null)
            return;

        timelineController.SetSlotSelectionLocked(false);
        timelineController.SetSelectedCharacterScaleFeedbackActive(false);
        timelineController.ClearAllReservations();
        timelineController.ResetTimelineBarsForNewBattleRoom();
    }
}
