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
    public void ExecuteChoice_RollTableRt002AddsRunMaxHpBonus()
    {
        CharacterRuntimeData character = new()
        {
            CharacterId = "C_001",
            MaxHP = 10,
            CurrentHP = 4
        };

        EventChoiceExecutionContext context = new()
        {
            PartyCharacters = new List<CharacterRuntimeData> { character },
            RollThreeDice = () => 16
        };

        EventData choice = new()
        {
            ChoiceType = "Dice",
            ResultType = "RollTable",
            ResultTarget = "파티 전원 최대 체력",
            ResultValue = "RT002"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(character.RunMaxHPBonus, Is.EqualTo(8));
            Assert.That(character.MaxHP, Is.EqualTo(18));
            Assert.That(character.CurrentHP, Is.EqualTo(12));
            Assert.That(result.ResultMessage, Does.Contain("8"));
        });
    }

    [Test]
    public void ExecuteChoice_ModifyMaxCostAddsRunMaxCostBonus()
    {
        CharacterRuntimeData character = new()
        {
            CharacterId = "C_001",
            MaxCost = 3,
            CurrentCost = 3
        };

        EventChoiceExecutionContext context = new()
        {
            PartyCharacters = new List<CharacterRuntimeData> { character }
        };

        EventData choice = new()
        {
            ChoiceType = "Immediate",
            ResultType = "Modify",
            ResultTarget = "최대 코스트",
            ResultValue = "+2"
        };

        EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(character.RunMaxCostBonus, Is.EqualTo(2));
            Assert.That(character.MaxCost, Is.EqualTo(5));
            Assert.That(character.CurrentCost, Is.EqualTo(3));
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
    public void CanSelectChoice_DisablesChoiceThatRequiresUnsupportedTargetSelectionUi()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                SkillInventoryIds = new List<string> { "Skill_A" }
            }
        };
        EventData choice = new()
        {
            ChoiceType = "CostExchange",
            ResultTarget = "선택 기억",
            ResultType = "GainRandom"
        };

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(canSelect, Is.False);
            Assert.That(reason, Does.Contain("대상 선택"));
        });
    }

    [Test]
    public void CanSelectChoice_BlocksSelectedRelicCostWhenOnlyInventoryRelicExists()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                OwnedRelicIds = new List<string> { "Relic_Inventory" }
            }
        };
        EventData choice = CreateSelectedRelicExchangeChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(canSelect, Is.False);
            Assert.That(reason, Does.Contain("장착"));
        });
    }

    [Test]
    public void CanSelectChoice_AllowsSelectedRelicCostWhenEquippedRelicExists()
    {
        CharacterRuntimeData character = new()
        {
            CharacterId = "C_001",
            EquippedRelicIds = new[] { "Relic_Equipped", null, null, null, null, null, null }
        };
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData(),
            PartyCharacters = new List<CharacterRuntimeData> { character }
        };
        EventData choice = CreateSelectedRelicExchangeChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(canSelect, Is.True);
            Assert.That(reason, Is.Empty);
        });
    }

    [Test]
    public void CanSelectChoice_BlocksRedDustiumCostWhenInsufficient()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                Remnant = EventChoiceExecutionService.DefaultTradeRemnantCost - 1
            }
        };
        EventData choice = CreateRedDustiumTradeChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(canSelect, Is.False);
            Assert.That(reason, Does.Contain(EventChoiceExecutionService.DefaultTradeRemnantCost.ToString()));
        });
    }

    [Test]
    public void ExecuteChoice_RedDustiumCostConsumesDefaultTradeCost()
    {
        BattleRuntimeData runtime = new()
        {
            Remnant = EventChoiceExecutionService.DefaultTradeRemnantCost
        };
        int grantCount = 0;
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = runtime,
            GrantRandomRelic = (out string message) =>
            {
                grantCount++;
                message = "유물 획득";
                return true;
            }
        };
        EventData choice = CreateRedDustiumTradeChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(runtime.Remnant, Is.Zero);
            Assert.That(grantCount, Is.EqualTo(1));
            Assert.That(result.ResultMessage, Does.Contain(EventChoiceExecutionService.DefaultTradeRemnantCost.ToString()));
        });
    }

    [Test]
    public void ExecuteChoice_SelectedEquippedRelicCostConsumesEquippedRelicBeforeGrantingRandomRelic()
    {
        CharacterRuntimeData character = new()
        {
            CharacterId = "C_001",
            EquippedRelicIds = new[] { null, null, "Relic_Old", null, null, null, null }
        };
        int revokeCount = 0;
        int grantCount = 0;
        EventChoiceExecutionContext context = new()
        {
            PartyCharacters = new List<CharacterRuntimeData> { character },
            SelectedEquippedRelicCost = new EventChoiceEquippedRelicCost("C_001", 2, "Relic_Old"),
            RevokeEquippedRelic = (EventChoiceEquippedRelicCost cost, out string message) =>
            {
                if (cost.CharacterId != character.CharacterId ||
                    cost.RelicSlotIndex != 2 ||
                    cost.RelicId != "Relic_Old")
                {
                    message = "선택한 유물이 잘못되었습니다.";
                    return false;
                }

                character.EquippedRelicIds[cost.RelicSlotIndex] = null;
                revokeCount++;
                message = "유물 Relic_Old 상실";
                return true;
            },
            GrantRandomRelic = (out string message) =>
            {
                grantCount++;
                message = "유물 획득";
                return true;
            }
        };
        EventData choice = CreateSelectedRelicExchangeChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(revokeCount, Is.EqualTo(1));
            Assert.That(grantCount, Is.EqualTo(1));
            Assert.That(character.EquippedRelicIds[2], Is.Null.Or.Empty);
            Assert.That(result.ResultMessage, Does.Contain("Relic_Old"));
        });
    }

    [Test]
    public void CanSelectChoice_AllowsAwakenWhenUpgradeableEquippedMemoryExists()
    {
        EventChoiceExecutionContext context = new()
        {
            HasUpgradeableEquippedSkill = () => true
        };
        EventData choice = CreateAwakenMemoryChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.That(canSelect, Is.True);
        Assert.That(reason, Is.Empty);
    }

    [Test]
    public void CanSelectChoice_BlocksAwakenWhenOnlyInventoryMemoryExists()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                SkillInventoryIds = new List<string> { "Skill_Base_001" }
            },
            HasUpgradeableEquippedSkill = () => false
        };
        EventData choice = CreateAwakenMemoryChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.That(canSelect, Is.False);
        Assert.That(reason, Does.Contain("장착"));
    }

    [Test]
    public void ExecuteChoice_AwakenUsesSelectedEquippedMemoryTarget()
    {
        EventChoiceSkillAwakenTarget target = new(
            "C_001",
            EventChoiceSkillSlotKind.Equipped,
            2,
            "Skill_Base_001",
            "Skill_Base_002");
        EventChoiceSkillAwakenTarget receivedTarget = default;
        EventChoiceExecutionContext context = new()
        {
            SelectedSkillAwakenTarget = target,
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 0f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget received, out string message) =>
            {
                receivedTarget = received;
                message = "기억 강화: Skill_Base_002";
                return true;
            }
        };
        EventData choice = CreateAwakenMemoryChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(receivedTarget.CharacterId, Is.EqualTo("C_001"));
            Assert.That(receivedTarget.SlotKind, Is.EqualTo(EventChoiceSkillSlotKind.Equipped));
            Assert.That(receivedTarget.SlotIndex, Is.EqualTo(2));
            Assert.That(receivedTarget.SkillId, Is.EqualTo("Skill_Base_001"));
            Assert.That(receivedTarget.UpgradeSkillId, Is.EqualTo("Skill_Base_002"));
            Assert.That(result.ResultMessage, Does.Contain("Skill_Base_002"));
        });
    }

    [Test]
    public void ExecuteChoice_AwakenWithoutSelectedMemoryTargetIsRejected()
    {
        bool upgradeCalled = false;
        EventChoiceExecutionContext context = new()
        {
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 0f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget _, out string message) =>
            {
                upgradeCalled = true;
                message = string.Empty;
                return true;
            }
        };
        EventData choice = CreateAwakenMemoryChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(upgradeCalled, Is.False);
            Assert.That(result.ResultMessage, Does.Contain("기억"));
        });
    }

    private static EventData CreateAwakenMemoryChoice()
    {
        return new EventData
        {
            ChoiceType = "Chance",
            SelectCondition = "미각성 기억 1개 이상",
            ResultType = "Awaken",
            ResultTarget = "선택 기억",
            ResultValue = "각성",
            SuccessRate = "100%"
        };
    }

    private static EventData CreateSelectedRelicExchangeChoice()
    {
        return new EventData
        {
            ChoiceType = "CostExchange",
            SelectCondition = "유물 1개 이상 보유",
            CostType = "유물",
            CostTarget = "선택 유물",
            CostValue = "1개",
            ResultType = "GainRandom",
            ResultTarget = "유물",
            ResultValue = "1개"
        };
    }

    private static EventData CreateRedDustiumTradeChoice()
    {
        return new EventData
        {
            ChoiceType = "CostExchange",
            CostType = "레드 더스티움",
            CostTarget = "파티",
            CostValue = "수량 TBD",
            ResultType = "GainRandom",
            ResultTarget = "유물",
            ResultValue = "1개"
        };
    }
}
