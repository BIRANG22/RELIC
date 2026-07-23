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
}
