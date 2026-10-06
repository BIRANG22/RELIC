using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public class CharacterSkillRewardAndEffectsIntegrationTests
{
    [Test]
    public void CompatibleReward_XReservation_ResultAndPermanentGrowth_KeepStableIds()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Core_01",
            CharacterId = "Char_01",
            Category = Category.Core,
            Rarity = SkillRarity.Common,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X",
            EffectIds = "E_Strike;E_ValueUpOnKill",
            ValueRate = "2X;2",
            CountRate = "1;1"
        };

        IReadOnlyList<SkillMasterData> candidates = SkillRewardRoller.GetCandidates(
            new[] { skill },
            SkillRarity.Common,
            true,
            new[] { "Char_01" });
        Assert.That(candidates, Has.Count.EqualTo(1));

        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 11 };
        PlayerReservedCommand command = new(caster, skill);
        Assert.That(command.ResolvedX, Is.EqualTo(3));
        Assert.That(command.ResolvedEffectValues[0], Is.EqualTo(6));

        command.ExecutionResult.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = "Monster_Runtime_01",
            TargetGridIndex = 12,
            Attempted = true,
            Hit = true,
            DamageAmount = 6,
            Killed = true
        });

        SkillRuntimeStore store = new();
        store.AddPermanentValueBonus(
            command.CharacterId,
            command.SkillId,
            NewSkillEffectRules.ResolvePermanentValueBonus(
                command.ExecutionResult.KillCount,
                2));

        Assert.That(command.ExecutionResult.KillCount, Is.EqualTo(1));
        Assert.That(store.GetPermanentValueBonus("Char_01", "S_Core_01"), Is.EqualTo(2));
    }

    [Test]
    public void BattleRandom_SameSeedProducesSameTargetOrder()
    {
        string[] targets = { "M1", "M2", "M3" };
        List<string> first = PickSequence(targets, 8675309, 8);
        List<string> second = PickSequence(targets, 8675309, 8);

        Assert.That(second, Is.EqualTo(first));
    }

    private static List<string> PickSequence(
        IReadOnlyList<string> targets,
        int seed,
        int count)
    {
        BattleRandom.SetSeed(seed);
        List<string> result = new();
        for (int i = 0; i < count; i++)
            result.Add(BattleRandom.Pick(targets));
        BattleRandom.ClearSeed();
        return result;
    }
}
