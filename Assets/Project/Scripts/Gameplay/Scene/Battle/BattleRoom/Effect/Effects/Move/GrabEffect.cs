using Relic.Gameplay.Monster;
using System.Collections;
using UnityEngine;

public class GrabEffect : BattleEffectBase
{
    private const float ForcedMoveAnimationDuration = 0.18f;
    public override string EffectId => "E_Grab";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null || context.GridManager == null)
            return;

        Vector2Int offset = GetReverseDirectionOffset(context.Direction);

        int moveCount = Mathf.Max(1, context.Value);

        for (int i = 0; i < moveCount; i++)
        {
            if (context.PlayerTarget != null)
            {
                if (!TryMovePlayer(context.PlayerTarget, offset, context.GridManager))
                    break;
            }
            else if (context.MonsterTarget != null)
            {
                if (!TryMoveMonster(context.MonsterTarget, offset, context.GridManager))
                    break;
            }
        }
    }

    private Vector2Int GetReverseDirectionOffset(BattleDirection direction)
    {
        return direction == BattleDirection.Left
            ? Vector2Int.right
            : Vector2Int.left;
    }

    private bool TryMovePlayer(BattleCharacter target, Vector2Int offset, GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        int currentIndex = target.CurrentGridIndex;

        if (currentIndex < 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
        Vector2Int targetCoord = currentCoord + offset;

        if (!gridManager.IsValidCoord(targetCoord))
            return false;

        int targetIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, target.CharacterId))
            return false;

        // 잔해처럼 이동 불가로 등록된 그리드는 강제이동으로도 들어갈 수 없습니다.
        // 그랩 대상이 해당 칸에 부딪히면 이동하지 않고 충돌 고정 피해를 받습니다.
        if (IsGridEffectBlocked(targetIndex))
        {
            ApplyCrashToPlayer(target, gridManager);
            return false;
        }

        Vector3 startPosition = target.transform.position;
        Vector3 targetPosition = gridManager.GetWorldPositionByIndex(targetIndex);

        // 논리 그리드 위치를 먼저 갱신하고 화면에서는 부드럽게 이동하는 연출을 재생합니다.
        target.SetGridIndex(targetIndex);
        target.StartCoroutine(MoveTransformSmooth(target.transform, startPosition, targetPosition));

        return true;
    }

    private bool TryMoveMonster(MonsterUnit target, Vector2Int offset, GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        if (target.OccupiedGridIndices == null || target.OccupiedGridIndices.Count <= 0)
            return false;

        if (target.MainGridIndex < 0)
            return false;

        for (int i = 0; i < target.OccupiedGridIndices.Count; i++)
        {
            int currentIndex = target.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            Vector2Int targetCoord = currentCoord + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, target))
                return false;

            if (IsGridEffectBlocked(targetIndex))
            {
                ApplyCrashToMonster(target, gridManager);
                return false;
            }
        }

        int mainIndex = target.MainGridIndex;
        Vector2Int mainCoord = gridManager.IndexToCoord(mainIndex);
        Vector2Int movedMainCoord = mainCoord + offset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        Vector3 startPosition = target.transform.position;
        Vector3 targetPosition = gridManager.GetWorldPositionByIndex(movedMainIndex);

        // 점유 그리드를 먼저 갱신한 뒤 몬스터 오브젝트를 부드럽게 이동시킵니다.
        target.MoveOccupiedCells(offset, gridManager);
        target.StartCoroutine(MoveTransformSmooth(target.transform, startPosition, targetPosition));

        return true;
    }


    private static bool IsGridEffectBlocked(int gridIndex)
    {
        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        return controller != null && controller.IsBlocked(gridIndex);
    }

    private static void ApplyCrashToPlayer(BattleCharacter target, GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = 2,
            Count = 1
        });
    }

    private static void ApplyCrashToMonster(MonsterUnit target, GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            MonsterTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = 2,
            Count = 1
        });
    }

    private static IEnumerator MoveTransformSmooth(
        Transform target,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < ForcedMoveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ForcedMoveAnimationDuration);
            target.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        target.position = targetPosition;
    }
}
