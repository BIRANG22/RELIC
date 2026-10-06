using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class MonsterInfoNameAndPassiveActivationTests
{
    [Test]
    public void ResolveSkillName_UsesExcelLoadedNameBeforeSkillId()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            SkillId = "S_Monster_02",
            Name = "점액투척"
        };

        Assert.That(MonsterInfoDataNameResolver.ResolveSkillName(skill), Is.EqualTo("점액투척"));
    }

    [Test]
    public void ResolveSkillName_FallsBackToSkillIdWhenExcelNameIsBlank()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            SkillId = "S_Monster_02",
            Name = "  "
        };

        Assert.That(MonsterInfoDataNameResolver.ResolveSkillName(skill), Is.EqualTo("S_Monster_02"));
    }

    [Test]
    public void ResolveEffectName_UsesExcelLoadedNameBeforeEffectId()
    {
        EffectMasterData effect = new EffectMasterData
        {
            EffectId = "E_Poison",
            Name = "중독"
        };

        Assert.That(
            MonsterInfoDataNameResolver.ResolveEffectName(effect, "E_Poison"),
            Is.EqualTo("중독"));
    }

    [Test]
    public void ResolveEffectName_FallsBackToEffectIdWhenDataIsMissing()
    {
        Assert.That(
            MonsterInfoDataNameResolver.ResolveEffectName(null, " E_Unknown "),
            Is.EqualTo("E_Unknown"));
    }

    [Test]
    public void ShouldApplyPassive_ReturnsTrueWhenCurrentKarmaIsZero()
    {
        SkillMasterData passiveSkill = new SkillMasterData { SkillId = "S_Passive_01" };
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            CharacterId = "C_01",
            PassiveSkillId = "S_Passive_01",
            CurrentResource = 0
        };

        Assert.That(BattlePassiveSkillService.ShouldApplyPassive(passiveSkill, runtime), Is.True);
    }
}
