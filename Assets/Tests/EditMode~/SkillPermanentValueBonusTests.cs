using NUnit.Framework;
using Relic.Gameplay.Data;

public class SkillPermanentValueBonusTests
{
    [Test]
    public void AddPermanentValueBonus_AccumulatesByCharacterAndSkill()
    {
        SkillRuntimeStore store = new();

        store.AddPermanentValueBonus("Char_01", "Skill_01", 2);
        store.AddPermanentValueBonus("Char_01", "Skill_01", 4);

        Assert.That(store.Get("Char_01", "Skill_01").PermanentValueBonus, Is.EqualTo(6));
        Assert.That(store.GetPermanentValueBonus("Char_02", "Skill_01"), Is.Zero);
    }

    [Test]
    public void ResetBattleValueBonuses_ClearsOnlyBattleBonus()
    {
        SkillRuntimeStore store = new();
        store.AddPermanentValueBonus("Char_01", "Skill_01", 2);
        store.AddBattleValueBonus("Char_01", "Skill_01", 4);

        store.ResetBattleValueBonuses();

        Assert.That(store.GetPermanentValueBonus("Char_01", "Skill_01"), Is.EqualTo(2));
        Assert.That(store.GetBattleValueBonus("Char_01", "Skill_01"), Is.Zero);
    }

    [Test]
    public void SkillPreview_IncludesPermanentAndBattleValueBonus()
    {
        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 10 };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_01",
            SkillType = SkillType.Attack,
            EffectIds = "E_Strike",
            ValueRate = "9",
            CountRate = "1"
        };
        SkillRuntimeStore store = new();
        store.AddPermanentValueBonus("Char_01", "Skill_01", 1);
        store.AddBattleValueBonus("Char_01", "Skill_01", 2);

        BattlePlayerSkillPreview preview = BattlePlayerSkillPreviewCalculator.CreatePreview(
            new PlayerReservedCommand(caster, skill),
            store);

        Assert.That(preview.EffectValues, Is.EqualTo(new[] { 12 }));
    }
}
