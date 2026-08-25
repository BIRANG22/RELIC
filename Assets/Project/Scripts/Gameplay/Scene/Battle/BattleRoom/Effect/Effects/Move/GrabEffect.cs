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

        BattleCharacter playerTarget = context.PlayerTarget;
        MonsterUnit monsterTarget = context.MonsterTarget;
        Vector3 startWorldPosition = playerTarget != null
            ? playerTarget.transform.position
            : monsterTarget != null ? monsterTarget.transform.position : Vector3.zero;
        int startGridIndex = playerTarget != null
            ? playerTarget.CurrentGridIndex
            : monsterTarget != null ? monsterTarget.MainGridIndex : -1;

        for (int i = 0; i < moveCount; i++)
        {
            if (playerTarget != null)
            {
                if (BattleEquipmentEffectService.IsForcedMoveImmune(playerTarget.RuntimeData))
                    break;

                if (!TryMovePlayer(playerTarget, offset, context.GridManager))
                    break;
            }
            else if (monsterTarget != null)
            {
                if (!TryMoveMonster(monsterTarget, offset, context.GridManager))
                    break;
            }
        }

        int finalGridIndex = playerTarget != null
            ? playerTarget.CurrentGridIndex
            : monsterTarget != null ? monsterTarget.MainGridIndex : -1;

        if (startGridIndex >= 0 && finalGridIndex >= 0 && startGridIndex != finalGridIndex)
        {
            Transform visualTarget = playerTarget != null ? playerTarget.transform : monsterTarget.transform;
            Vector3 startCellWorldPosition = context.GridManager.GetWorldPositionByIndex(startGridIndex);
            Vector3 finalCellWorldPosition = context.GridManager.GetWorldPositionByIndex(finalGridIndex);
            Vector3 finalWorldPosition =
                startWorldPosition + (finalCellWorldPosition - startCellWorldPosition);

            if (playerTarget != null)
                playerTarget.StartCoroutine(MoveTransformSmooth(visualTarget, startWorldPosition, finalWorldPosition));
            else if (monsterTarget != null)
                monsterTarget.StartCoroutine(MoveTransformSmooth(visualTarget, startWorldPosition, finalWorldPosition));
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

        // 논리 그리드 위치를 즉시 갱신합니다.
        // 화면 이동은 전체 그랩 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.SetGridIndex(targetIndex);
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

        // 점유 그리드를 즉시 갱신합니다.
        // 화면 이동은 전체 그랩 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.MoveOccupiedCells(offset, gridManager);
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
