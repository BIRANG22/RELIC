using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnExecutor : MonoBehaviour
{
    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleRoomLoader roomLoader;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;

    [Header("Intro Text")]
    [SerializeField] private string battleProgressMessage = "전투 진행";

    [Header("SFX")]
    [SerializeField] private bool playBattleProgressSfx = true;
    [SerializeField] private SfxType battleProgressSfxType = SfxType.BattleProgressText;
    [SerializeField, Range(0f, 1f)] private float battleProgressSfxVolume = 1f;

    private bool isExecuting;

    public void ExecuteTurn()
    {
        if (isExecuting)
        {
            ShowBattleWarning("이미 턴을 실행 중입니다.");
            Debug.LogWarning("[BattleTurnExecutor] Already executing.");
            return;
        }

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        isExecuting = true;

        try
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            BattleActionBatchBuilder builder = new();
            BattleActionRunner runner = new(gridManager, monsterSpawner, roomLoader);

            List<BattleActionBatch> batches = builder.Build(timelineController);

            if (batches == null || batches.Count <= 0)
            {
                ShowBattleWarning("실행할 행동이 없습니다.");
                yield break;
            }

            yield return ShowBattleProgressIntroTextRoutine();

            for (int i = 0; i < batches.Count; i++)
            {
                yield return runner.RunBatch(batches[i]);

                if (BattleResultChecker.Instance != null &&
                    BattleResultChecker.Instance.CheckBattleEnd())
                {
                    ClearTimeline();
                    yield break;
                }
            }

            runner.ApplyTurnEndEffects();

            ClearTimeline();

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                yield break;
            }

            if (roomLoader != null)
            {
                roomLoader.RecoverPlayerCostsToMax();
                roomLoader.PlanNextMonsterTurns();
            }
        }
        finally
        {
            isExecuting = false;
        }
    }

    private IEnumerator ShowBattleProgressIntroTextRoutine()
    {
        PlaySfx(playBattleProgressSfx, battleProgressSfxType, battleProgressSfxVolume);
        yield return BattleMapIntroText.ShowMessageAndWait(battleProgressMessage);
    }

    private void PlaySfx(bool play, SfxType sfxType, float volume)
    {
        if (!play)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxType, volume);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void ClearTimeline()
    {
        if (timelineController != null)
            timelineController.ClearAllReservations();
    }
}