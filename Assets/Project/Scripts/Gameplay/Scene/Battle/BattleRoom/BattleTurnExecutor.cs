using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnExecutor : MonoBehaviour
{
    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleRoomLoader roomLoader;

    private bool isExecuting;

    public void ExecuteTurn()
    {
        if (isExecuting)
        {
            Debug.LogWarning("[BattleTurnExecutor] Already executing.");
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
            BattleActionRunner runner = new(gridManager);

            List<BattleActionBatch> batches = builder.Build(timelineController);

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

            ClearTimeline();

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                yield break;
            }

            if (roomLoader != null)
                roomLoader.PlanNextMonsterTurns();
        }
        finally
        {
            isExecuting = false;
        }
    }

    private void ClearTimeline()
    {
        if (timelineController != null)
            timelineController.ClearAllReservations();
    }
}