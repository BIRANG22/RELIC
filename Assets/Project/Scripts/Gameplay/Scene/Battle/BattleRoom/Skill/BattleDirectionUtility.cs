using UnityEngine;

public static class BattleDirectionUtility
{
    public static Vector2Int RotateOffset(Vector2Int offset, BattleDirection direction)
    {
        switch (direction)
        {
            case BattleDirection.Right:
                return offset;

            case BattleDirection.Left:
                return new Vector2Int(-offset.x, offset.y);

            default:
                return offset;
        }
    }
}