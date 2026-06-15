using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackEffect : BattleEffectBase
{
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

    private Vector2Int GetDirectionOffset(BattleDirection direction)
    {
        if (direction == BattleDirection.Left)
            return Vector2Int.left;

        return Vector2Int.right;
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
        {
            Debug.Log("[KnockbackEffect] 플레이어 이동 실패: 그리드 밖");
            return false;
        }

        int targetIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, target.CharacterId))
        {
            Debug.Log($"[KnockbackEffect] 플레이어 이동 실패: 점유칸 {targetIndex}");
            return false;
        }

        target.SetGridIndex(targetIndex);
        target.transform.position = gridManager.GetWorldPositionByIndex(targetIndex);

        Debug.Log($"[KnockbackEffect] 플레이어 이동 성공: {targetIndex}");

        return true;
    }

    private bool TryMoveMonster(MonsterUnit target, Vector2Int offset, GridManager gridManager)
    {
        Debug.Log(
    $"[KnockbackEffect] Target:{target.RuntimeData.Name} / " +
    $"RuntimeId:{target.RuntimeData.RuntimeId} / " +
    $"Cells:{string.Join(",", target.OccupiedGridIndices)} / " +
    $"Main:{target.MainGridIndex}"
);

        if (target == null || target.RuntimeData == null)
            return false;

        List<int> currentCells = new List<int>(target.OccupiedGridIndices);
        List<int> movedCells = new();

        for (int i = 0; i < currentCells.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(currentCells[i]);
            Vector2Int targetCoord = currentCoord + offset;

            if (!gridManager.IsValidCoord(targetCoord))
            {
                Debug.Log("[KnockbackEffect] 몬스터 이동 실패: 그리드 밖");
                return false;
            }

            movedCells.Add(gridManager.CoordToIndex(targetCoord));

            Debug.Log(
    $"[KnockbackEffect] CellMove / " +
    $"CurrentIndex:{currentCells[i]} / " +
    $"CurrentCoord:{currentCoord} / " +
    $"TargetCoord:{targetCoord} / " +
    $"TargetIndex:{gridManager.CoordToIndex(targetCoord)} / " +
    $"Offset:{offset}"
);
        }

        for (int i = 0; i < movedCells.Count; i++)
        {
            int targetIndex = movedCells[i];

            if (currentCells.Contains(targetIndex))
                continue;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, target))
            {
                Debug.Log($"[KnockbackEffect] 몬스터 이동 실패: 점유칸 {targetIndex}");
                return false;
            }
        }

        int movedMainIndex = movedCells[0];

        int oldMainIndex = target.MainGridIndex;
        int newMainIndex = movedCells.Count > 0 ? movedCells[0] : oldMainIndex;

        Vector3 oldWorldPos = target.transform.position;
        Vector3 oldCellWorldPos = gridManager.GetWorldPositionByIndex(oldMainIndex);
        Vector3 newCellWorldPos = gridManager.GetWorldPositionByIndex(newMainIndex);
        Vector3 delta = newCellWorldPos - oldCellWorldPos;

        target.SetOccupiedCells(movedCells);
        target.transform.position = oldWorldPos + delta;

        Debug.Log(
            $"[KnockbackEffect] MonsterMoveSuccess / " +
            $"OldMain:{oldMainIndex} / NewMain:{newMainIndex} / " +
            $"OldCellPos:{oldCellWorldPos} / NewCellPos:{newCellWorldPos} / " +
            $"Delta:{delta} / " +
            $"OldWorld:{oldWorldPos} / NewWorld:{target.transform.position}"
            );
        return true;
    }
}