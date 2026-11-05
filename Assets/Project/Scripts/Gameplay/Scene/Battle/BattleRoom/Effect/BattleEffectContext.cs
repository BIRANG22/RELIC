using System;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

public class BattleEffectContext
{
    public BattleCharacter PlayerCaster;
    public BattleCharacter PlayerTarget;

    public MonsterUnit MonsterCaster;
    public MonsterUnit MonsterTarget;

    public SkillMasterData PlayerSkillData;
    public MonsterSkillData MonsterSkillData;
    public PlayerReservedCommand PlayerCommand;
    public MonsterReservedCommand MonsterCommand;

    public BattleDirection Direction;
    public GridManager GridManager;

    public string EffectId;
    public int Value;
    public int Count;
    public SkillExecutionResult ExecutionResult;

    // 전투 결과 계산과 분리된 강제 이동 시각 연출 수명 알림입니다.
    public Action ForcedMoveVisualStarted;
    public Action ForcedMoveVisualCompleted;
}
