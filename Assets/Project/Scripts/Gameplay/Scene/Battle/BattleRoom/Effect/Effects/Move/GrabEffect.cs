using Relic.Gameplay.Monster;
using UnityEngine;

public class GrabEffect : BattleEffectBase
{
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

        target.SetGridIndex(targetIndex);
        target.transform.position = gridManager.GetWorldPositionByIndex(targetIndex);

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
        }

        int mainIndex = target.MainGridIndex;
        Vector2Int mainCoord = gridManager.IndexToCoord(mainIndex);
        Vector2Int movedMainCoord = mainCoord + offset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        target.MoveOccupiedCells(offset, gridManager);
        target.transform.position = gridManager.GetWorldPositionByIndex(movedMainIndex);

        return true;
    }
}
