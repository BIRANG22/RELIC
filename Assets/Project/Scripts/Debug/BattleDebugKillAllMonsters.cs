using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDebugKillAllMonsters : MonoBehaviour
{
    [SerializeField] private KeyCode killKey = KeyCode.K;
    [SerializeField] private int debugPlayerDamage = 1;

    private void Update()
    {
        if (Input.GetKeyDown(killKey))
            KillAllMonstersForDebug();
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

        PrepareBattleStateForDebugWin();

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
