using System;
using UnityEngine.Serialization;

[Serializable]
public class BattleUnitActionPresentation
{
    [FormerlySerializedAs("actionStateName")]
    public string stateName;

    [FormerlySerializedAs("actionVfx")]
    public BattleVfxEntry vfx;

    public bool spawnVfxOnEachTargetGrid;

    public BattleProjectileVfxEntry projectileVfx;

    public static BattleUnitActionPresentation[] CreateArray(int count)
    {
        int safeCount = Math.Max(0, count);
        BattleUnitActionPresentation[] presentations = new BattleUnitActionPresentation[safeCount];

        for (int i = 0; i < presentations.Length; i++)
            presentations[i] = new BattleUnitActionPresentation();

        return presentations;
    }
}
