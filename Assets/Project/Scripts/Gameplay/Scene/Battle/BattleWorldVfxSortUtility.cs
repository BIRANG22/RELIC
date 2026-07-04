using UnityEngine;

public static class BattleWorldVfxSortUtility
{
    public static int CalculateSortingOrder(float y, float yMultiplier, int offset)
    {
        float safeMultiplier = Mathf.Max(0.01f, yMultiplier);
        return (int)(-y * safeMultiplier) + offset;
    }
}
