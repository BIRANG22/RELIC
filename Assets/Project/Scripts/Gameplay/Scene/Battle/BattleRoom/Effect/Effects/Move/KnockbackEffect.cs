using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackEffect : BattleEffectBase
{
    private const float ForcedMoveAnimationDuration = 0.18f;
    public override string EffectId => "E_Knockback";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null || context.GridManager == null)
            return;

        Vector2Int offset = GetDirectionOffset(context.Direction);
        int moveCount = Mathf.Max(1, context.Value);

        for (int i = 0; i < moveCount; i++)
        {
            if (context.PlayerTarget != null)
            {
                if (WouldMovePlayerOutsideGrid(context.PlayerTarget, offset, context.GridManager))
                    break;

                if (!TryMovePlayer(context.PlayerTarget, offset, context.GridManager))
                {
                    ApplyCrashEffect(context, context.PlayerTarget);
                    break;
                }
            }
            else if (context.MonsterTarget != null)
            {
                if (WouldMoveMonsterOutsideGrid(context.MonsterTarget, offset, context.GridManager))
                    break;

                if (!TryMoveMonster(context.MonsterTarget, offset, context.GridManager))
                {
                    ApplyCrashEffect(context, context.MonsterTarget);
                    break;
                }
            }
        }
    }


    private static bool WouldMovePlayerOutsideGrid(
        BattleCharacter target,
        Vector2Int offset,
        GridManager gridManager)
    {
        if (target == null || gridManager == null || target.CurrentGridIndex < 0)
            return true;

        Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex) + offset;
        return !gridManager.IsValidCoord(targetCoord);
    }

    private static bool WouldMoveMonsterOutsideGrid(
        MonsterUnit target,
        Vector2Int offset,
        GridManager gridManager)
    {
        if (target == null || gridManager == null || target.OccupiedGridIndices == null)
            return true;

        for (int i = 0; i < target.OccupiedGridIndices.Count; i++)
        {
            Vector2Int targetCoord =
                gridManager.IndexToCoord(target.OccupiedGridIndices[i]) + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                return true;
        }

        return false;
    }

    private static Vector2Int GetDirectionOffset(BattleDirection direction)
    {
        return direction == BattleDirection.Left
            ? Vector2Int.left
            : Vector2Int.right;
    }

    private static bool TryMovePlayer(
        BattleCharacter target,
        Vector2Int offset,
        GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        int currentIndex = target.CurrentGridIndex;

        if (currentIndex < 0)
            return false;

        Vector2Int targetCoord = gridManager.IndexToCoord(currentIndex) + offset;

        if (!gridManager.IsValidCoord(targetCoord))
            return false;

        int targetIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, target.CharacterId))
            return false;

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null && gridEffectController.IsBlocked(targetIndex))
            return false;

        Vector3 startPosition = target.transform.position;
        Vector3 targetPosition = gridManager.GetWorldPositionByIndex(targetIndex);

        // 판정용 그리드 위치는 즉시 갱신하고 화면에서는 밀려나는 과정을 보여줍니다.
        target.SetGridIndex(targetIndex);
        target.StartCoroutine(MoveTransformSmooth(target.transform, startPosition, targetPosition));
        return true;
    }

    private static bool TryMoveMonster(
        MonsterUnit target,
        Vector2Int offset,
        GridManager gridManager)
    {
        if (target == null || target.RuntimeData == null)
            return false;

        if (target.OccupiedGridIndices == null || target.OccupiedGridIndices.Count <= 0)
            return false;

        List<int> currentCells = new(target.OccupiedGridIndices);
        List<int> movedCells = new();
        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        for (int i = 0; i < currentCells.Count; i++)
        {
            Vector2Int targetCoord = gridManager.IndexToCoord(currentCells[i]) + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (gridEffectController != null && gridEffectController.IsBlocked(targetIndex))
                return false;

            movedCells.Add(targetIndex);
        }

        for (int i = 0; i < movedCells.Count; i++)
        {
            int targetIndex = movedCells[i];

            if (currentCells.Contains(targetIndex))
                continue;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, target))
                return false;
        }

        if (movedCells.Count <= 0)
            return false;

        int oldMainIndex = target.MainGridIndex;
        int newMainIndex = movedCells[0];
        Vector3 oldWorldPosition = target.transform.position;
        Vector3 oldCellWorldPosition = gridManager.GetWorldPositionByIndex(oldMainIndex);
        Vector3 newCellWorldPosition = gridManager.GetWorldPositionByIndex(newMainIndex);

        Vector3 targetWorldPosition =
            oldWorldPosition + (newCellWorldPosition - oldCellWorldPosition);

        // 점유 그리드는 즉시 갱신하되 몬스터 오브젝트는 부드럽게 밀려나게 합니다.
        target.SetOccupiedCells(movedCells);
        target.StartCoroutine(MoveTransformSmooth(
            target.transform,
            oldWorldPosition,
            targetWorldPosition));
        return true;
    }

    private static void ApplyCrashEffect(
        BattleEffectContext sourceContext,
        BattleCharacter target)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = sourceContext?.PlayerCaster,
            MonsterCaster = sourceContext?.MonsterCaster,
            PlayerTarget = target,
            Direction = sourceContext != null ? sourceContext.Direction : BattleDirection.Right,
            GridManager = sourceContext?.GridManager,
            EffectId = "E_Crash",
            Value = 2,
            Count = 1
        });
    }

    private static void ApplyCrashEffect(
        BattleEffectContext sourceContext,
        Relic.Gameplay.Monster.MonsterUnit target)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = sourceContext?.PlayerCaster,
            MonsterCaster = sourceContext?.MonsterCaster,
            MonsterTarget = target,
            Direction = sourceContext != null ? sourceContext.Direction : BattleDirection.Right,
            GridManager = sourceContext?.GridManager,
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
