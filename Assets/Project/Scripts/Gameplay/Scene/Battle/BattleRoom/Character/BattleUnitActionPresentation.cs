using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BattleUnitActionPresentation
{
    [Tooltip("이 연출에 연결할 몬스터 스킬 ID (예: S_Monster_40). 비어 있으면 기존 ActionIndex 방식에서 사용됩니다.")]
    public string monsterSkillId;

    [Tooltip("Action 전에 잠깐 보여줄 준비 애니메이션 State. 비어 있거나 Animator에 State가 없으면 즉시 Action을 재생합니다.")]
    public string prepareStateName;

    [Min(0f)]
    [Tooltip("Prepare State를 유지할 시간(초). Animator 재생 속도의 영향을 받습니다.")]
    public float prepareDuration = 0.15f;

    [Tooltip("Prepare 단계가 시작될 때 시전자 위치에 재생할 VFX입니다.")]
    public BattleVfxEntry prepareVfx;

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
