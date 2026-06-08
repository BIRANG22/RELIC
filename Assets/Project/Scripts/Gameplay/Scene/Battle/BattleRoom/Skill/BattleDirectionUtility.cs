using UnityEngine;

public static class BattleDirectionUtility
{
    public static Vector2Int RotateOffset(Vector2Int offset, BattleDirection direction)
    {
        int x = offset.x;
        int y = offset.y;

        switch (direction)
        {
            case BattleDirection.Right:
                return new Vector2Int(x, y);

            case BattleDirection.Up:
                return new Vector2Int(-y, x);

            case BattleDirection.Left:
                return new Vector2Int(-x, -y);

            case BattleDirection.Down:
                return new Vector2Int(y, -x);

            default:
                return offset;
        }
    }
}