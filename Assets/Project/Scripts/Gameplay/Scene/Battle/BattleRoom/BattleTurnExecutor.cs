using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnExecutor : MonoBehaviour
{
    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;

    private bool isExecuting;

    public void ExecuteTurn()
    {
        if (isExecuting)
            return;

        StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        isExecuting = true;

        isExecuting = true;

        if (moveGhostPreview != null)
            moveGhostPreview.ClearAll();

        BattleActionBatchBuilder builder = new();
        BattleActionRunner runner = new(gridManager);

        List<BattleActionBatch> batches = builder.Build(timelineController);

        Debug.Log($"[BattleTurnExecutor] Batch Count:{batches.Count}");

        for (int i = 0; i < batches.Count; i++)
        {
            Debug.Log($"[BattleTurnExecutor] Run Batch:{i}");
            yield return runner.RunBatch(batches[i]);
        }

        if (timelineController != null)
            timelineController.ClearAllReservations();

        isExecuting = false;
    }
}