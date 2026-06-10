using Relic.Gameplay.Data;

public class MonsterReservedCommand
{
    public MonsterRuntimeData UserRuntime { get; private set; }
    public MonsterSkillData SkillData { get; private set; }

    public string MonsterId => UserRuntime != null ? UserRuntime.MonsterId : "";
    public string RuntimeId => UserRuntime != null ? UserRuntime.RuntimeId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";
    public string SkillName => SkillData != null ? SkillData.Name : "";

    public MonsterReservedCommand(MonsterRuntimeData userRuntime, MonsterSkillData skillData)
    {
        UserRuntime = userRuntime;
        SkillData = skillData;
    }
}