using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class EventChoiceExecutionServiceTests
{
    [Test]
    public void ExecuteChoice_RollTableRt001HealsPartyByDiceBand()
    {
        CharacterRuntimeData character = new()
        {
            CharacterId = "C_001",
            MaxHP = 10,
            CurrentHP = 1
        };

        EventChoiceExecutionContext context = new()
        {
            PartyCharacters = new List<CharacterRuntimeData> { character },
            RollThreeDice = () => 9
        };

        EventData choice = new()
        {
            ChoiceType = "Dice",
            ResultType = "RollTable",
            ResultTarget = "파티 전원 현재 체력",
            ResultValue = "RT001"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(character.CurrentHP, Is.EqualTo(6));
            Assert.That(result.ResultMessage, Does.Contain("5"));
            Assert.That(result.NextEventId, Is.Empty);
        });
    }

    [Test]
    public void ExecuteChoice_AccumulateThenCommitAddsRedDustiumToBattleRuntime()
    {
        BattleRuntimeData battleRuntime = new()
        {
            Remnant = 10
        };
        EventChoiceSessionState sessionState = new();
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = battleRuntime,
            SessionState = sessionState,
            RollThreeDice = () => 10
        };

        EventData accumulate = new()
        {
            ChoiceType = "Dice",
            SuccessCondition = "9~18",
            ResultType = "Accumulate",
            ResultTarget = "레드 더스티움",
            ResultValue = "소량",
            NextEventId = "Event_05"
        };
        EventData commit = new()
        {
            ChoiceType = "Conditional",
            SelectCondition = "채굴 1회 이상 성공",
            ResultType = "CommitAccumulated",
            ResultTarget = "레드 더스티움",
            ResultValue = "누적량"
        };

        EventChoiceExecutionResult firstResult = EventChoiceExecutionService.Execute(accumulate, context);
        EventChoiceExecutionResult secondResult = EventChoiceExecutionService.Execute(commit, context);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.NextEventId, Is.EqualTo("Event_05"));
            Assert.That(battleRuntime.Remnant, Is.EqualTo(40));
            Assert.That(secondResult.ResultMessage, Does.Contain("30"));
            Assert.That(sessionState.AccumulatedRemnant, Is.Zero);
        });
    }

    [Test]
    public void CanSelectChoice_DisablesChoiceThatRequiresTargetSelectionUi()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                OwnedRelicIds = new List<string> { "Relic_A" }
            }
        };
        EventData choice = new()
        {
            ChoiceType = "CostExchange",
            SelectCondition = "유물 1개 이상 보유",
            CostType = "유물",
            CostValue = "선택 유물",
            ResultType = "GainRandom",
            ResultTarget = "유물"
        };

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(canSelect, Is.False);
            Assert.That(reason, Does.Contain("대상 선택"));
        });
    }
}
