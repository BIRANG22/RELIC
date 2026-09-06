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

                if (WouldMovePlayerOutsideGrid(playerTarget, offset, context.GridManager))
                    break;

                if (!TryMovePlayer(
                        playerTarget,
                        offset,
                        context.GridManager,
                        out int blockingUnitGridIndex))
                {
                    if (blockingUnitGridIndex >= 0)
                        HandlePlayerUnitCollision(playerTarget, blockingUnitGridIndex, context.GridManager);
                    else
                        ApplyCrashEffect(context, playerTarget);

                    break;
                }

                if (playerTarget.RuntimeData.IsDead)
                    break;
            }
            else if (monsterTarget != null)
            {
                if (WouldMoveMonsterOutsideGrid(monsterTarget, offset, context.GridManager))
                    break;

                if (!TryMoveMonster(
                        monsterTarget,
                        offset,
                        context.GridManager,
                        out int blockingUnitGridIndex))
                {
                    if (blockingUnitGridIndex >= 0)
                        HandleMonsterUnitCollision(monsterTarget, blockingUnitGridIndex, context.GridManager);
                    else
                        ApplyCrashEffect(context, monsterTarget);

                    break;
                }

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
        GridManager gridManager,
        out int blockingUnitGridIndex)
    {
        blockingUnitGridIndex = -1;

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
        {
            blockingUnitGridIndex = targetIndex;
            return false;
        }

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null && gridEffectController.IsBlocked(targetIndex))
        {
            TryDamageBlockedGridEffect(gridEffectController, targetIndex);
            return false;
        }

        // 판정용 그리드 위치는 즉시 갱신합니다.
        // 화면 이동은 전체 넉백 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.SetGridIndex(targetIndex);

        if (gridEffectController != null)
            gridEffectController.ApplyToPlayer(targetIndex, target);

        return true;
    }

    private static bool TryMoveMonster(
        MonsterUnit target,
        Vector2Int offset,
        GridManager gridManager,
        out int blockingUnitGridIndex)
    {
        blockingUnitGridIndex = -1;

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
            {
                TryDamageBlockedGridEffect(gridEffectController, targetIndex);
                return false;
            }

            movedCells.Add(targetIndex);
        }

        for (int i = 0; i < movedCells.Count; i++)
        {
            int targetIndex = movedCells[i];

            if (currentCells.Contains(targetIndex))
                continue;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, target))
            {
                blockingUnitGridIndex = targetIndex;
                return false;
            }
        }

        if (movedCells.Count <= 0)
            return false;

        // 점유 그리드는 즉시 갱신합니다.
        // 화면 이동은 전체 넉백 판정이 끝난 뒤 최종 칸까지 한 번만 재생합니다.
        target.SetOccupiedCells(movedCells);

        if (gridEffectController != null)
        {
            for (int i = 0; i < movedCells.Count; i++)
            {
                int enteredGridIndex = movedCells[i];

                if (currentCells.Contains(enteredGridIndex))
                    continue;

                gridEffectController.ApplyToMonster(enteredGridIndex, target);

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
                out BattleCharacter blockingCharacter,
                movingPlayer.CharacterId))
        {
            int damageToMovingPlayer = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(blockingCharacter.RuntimeData);
            int damageToBlockingPlayer = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(movingPlayer.RuntimeData);

            ApplyCrashToPlayer(movingPlayer, gridManager, damageToMovingPlayer);
            ApplyCrashToPlayer(blockingCharacter, gridManager, damageToBlockingPlayer);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingPlayer,
                blockingCharacter,
                null);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingCharacter,
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
                out BattleCharacter blockingCharacter))
        {
            int damageToMovingMonster = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(blockingCharacter.RuntimeData);

            bool movingMonsterKilled =
                ApplyCrashToMonster(movingMonster, gridManager, damageToMovingMonster);
            ApplyCrashToPlayer(blockingCharacter, gridManager, baseCrashDamage);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingCharacter,
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

    private static void TryDamageBlockedGridEffect(
        BattleGridEffectController controller,
        int gridIndex)
    {
        if (controller == null || !controller.IsBlocked(gridIndex))
            return;

        controller.TryDamageEffect(gridIndex, 2, out _);
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
