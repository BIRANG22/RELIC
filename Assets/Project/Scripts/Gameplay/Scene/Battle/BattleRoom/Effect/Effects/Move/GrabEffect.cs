using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
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

                if (playerTarget.RuntimeData.IsDead)
                    break;
            }
            else if (monsterTarget != null)
            {
                if (!TryMoveMonster(monsterTarget, offset, context.GridManager))
                    break;

                if (monsterTarget.RuntimeData.IsDead)
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
        {
            HandlePlayerUnitCollision(target, targetIndex, gridManager);
            return false;
        }

        // 잔해처럼 이동 불가로 등록된 그리드는 강제이동으로도 들어갈 수 없습니다.
        // 그랩 대상이 해당 칸에 부딪히면 이동하지 않고 충돌 고정 피해를 받습니다.
        if (IsGridEffectBlocked(targetIndex))
        {
            TryDamageBlockedGridEffect(targetIndex);
            ApplyCrashToPlayer(target, gridManager);
            return false;
        }

        // 논리 그리드 위치를 즉시 갱신합니다.
        // 화면 이동은 전체 그랩 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.SetGridIndex(targetIndex);

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null)
            gridEffectController.ApplyToPlayer(targetIndex, target);

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

        List<int> currentCells = new(target.OccupiedGridIndices);
        List<int> enteredGridIndices = new();

        for (int i = 0; i < currentCells.Count; i++)
        {
            int currentIndex = currentCells[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            Vector2Int targetCoord = currentCoord + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, target))
            {
                HandleMonsterUnitCollision(target, targetIndex, gridManager);
                return false;
            }

            if (IsGridEffectBlocked(targetIndex))
            {
                TryDamageBlockedGridEffect(targetIndex);
                ApplyCrashToMonster(target, gridManager);
                return false;
            }

            if (!currentCells.Contains(targetIndex))
                enteredGridIndices.Add(targetIndex);
        }

        // 점유 그리드를 즉시 갱신합니다.
        // 화면 이동은 전체 그랩 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.MoveOccupiedCells(offset, gridManager);

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null)
        {
            for (int i = 0; i < enteredGridIndices.Count; i++)
            {
                gridEffectController.ApplyToMonster(enteredGridIndices[i], target);

                if (target.RuntimeData.IsDead)
                    break;
            }
        }

        return true;
    }



    private static void HandlePlayerUnitCollision(
        BattleCharacter movingPlayer,
        int blockingGridIndex,
        GridManager gridManager)
    {
        if (movingPlayer == null || movingPlayer.RuntimeData == null || movingPlayer.RuntimeData.IsDead)
            return;

        const int baseCrashDamage = 2;

        if (BattleOccupancyService.TryGetCharacterAtGrid(
                blockingGridIndex,
                out BattleCharacter blockingPlayer,
                movingPlayer.CharacterId))
        {
            int damageToMovingPlayer = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(blockingPlayer.RuntimeData);
            int damageToBlockingPlayer = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(movingPlayer.RuntimeData);

            ApplyCrashToPlayer(movingPlayer, gridManager, damageToMovingPlayer);
            ApplyCrashToPlayer(blockingPlayer, gridManager, damageToBlockingPlayer);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingPlayer,
                blockingPlayer,
                null);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingPlayer,
                movingPlayer,
                null);
            return;
        }

        if (BattleOccupancyService.TryGetMonsterAtGrid(
                blockingGridIndex,
                out MonsterUnit blockingMonster))
        {
            int damageToBlockingMonster = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(movingPlayer.RuntimeData);

            ApplyCrashToPlayer(movingPlayer, gridManager, baseCrashDamage);
            bool blockingMonsterKilled =
                ApplyCrashToMonster(blockingMonster, gridManager, damageToBlockingMonster);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingPlayer,
                null,
                blockingMonster,
                blockingMonsterKilled);
        }
    }

    private static void HandleMonsterUnitCollision(
        MonsterUnit movingMonster,
        int blockingGridIndex,
        GridManager gridManager)
    {
        if (movingMonster == null || movingMonster.RuntimeData == null || movingMonster.RuntimeData.IsDead)
            return;

        const int baseCrashDamage = 2;

        if (BattleOccupancyService.TryGetCharacterAtGrid(
                blockingGridIndex,
                out BattleCharacter blockingPlayer))
        {
            int damageToMovingMonster = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(blockingPlayer.RuntimeData);

            bool movingMonsterKilled =
                ApplyCrashToMonster(movingMonster, gridManager, damageToMovingMonster);
            ApplyCrashToPlayer(blockingPlayer, gridManager, baseCrashDamage);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingPlayer,
                null,
                movingMonster,
                movingMonsterKilled);
            return;
        }

        if (BattleOccupancyService.TryGetMonsterAtGrid(
                blockingGridIndex,
                out MonsterUnit blockingMonster,
                movingMonster))
        {
            ApplyCrashToMonster(movingMonster, gridManager, baseCrashDamage);
            ApplyCrashToMonster(blockingMonster, gridManager, baseCrashDamage);
        }
    }

    private static bool IsGridEffectBlocked(int gridIndex)
    {
        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        return controller != null && controller.IsBlocked(gridIndex);
    }

    private static void TryDamageBlockedGridEffect(int gridIndex)
    {
        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller == null || !controller.IsBlocked(gridIndex))
            return;

        controller.TryDamageEffect(gridIndex, 2, out _);
    }

    private static void ApplyCrashToPlayer(
        BattleCharacter target,
        GridManager gridManager,
        int damage = 2)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = Mathf.Max(0, damage),
            Count = 1
        });
    }

    private static bool ApplyCrashToMonster(
        MonsterUnit target,
        GridManager gridManager,
        int damage = 2)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return false;

        bool wasAlive = !target.RuntimeData.IsDead;

        new CrashEffect().Execute(new BattleEffectContext
        {
            MonsterTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = Mathf.Max(0, damage),
            Count = 1
        });

        return wasAlive && target.RuntimeData != null && target.RuntimeData.IsDead;
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
