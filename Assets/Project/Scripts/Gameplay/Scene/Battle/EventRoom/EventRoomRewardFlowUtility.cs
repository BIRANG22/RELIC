using Relic.Gameplay.Data;
using UnityEngine;

public static class EventRoomRewardFlowUtility
{
    private const string OptionalSecondChestEventId = "Event_02_A";
    private const string MiningEventId = "Event_05";

    public static bool CanSkipUnresolvedEvent(EventDefinition definition)
    {
        if (definition == null)
            return false;

        return EventIdUtility.Normalize(definition.EventId) == OptionalSecondChestEventId;
    }

    public static bool ShouldOpenPendingRewards(
        EventChoiceExecutionResult result,
        int pendingRewardCount)
    {
        return ShouldOpenPendingRewards(result, pendingRewardCount, result.HasNextEvent);
    }

    public static bool ShouldOpenPendingRewards(
        EventChoiceExecutionResult result,
        int pendingRewardCount,
        bool hasContinuingEvent)
    {
        return result.Accepted &&
               pendingRewardCount > 0 &&
               !hasContinuingEvent;
    }

    public static bool ShouldKeepRewardsPending(
        EventChoiceExecutionResult result,
        int pendingRewardCount)
    {
        return result.Accepted &&
               pendingRewardCount > 0 &&
               result.HasNextEvent;
    }

    public static bool ShouldCompleteAfterFailedChoice(
        EventData choice,
        EventChoiceExecutionResult result)
    {
        if (choice == null || !result.Accepted || result.Succeeded)
            return false;

        return EventIdUtility.Normalize(choice.EventId) == MiningEventId &&
               (choice.ChoiceOrder == 1 || choice.ChoiceOrder == 2);
    }

    public static BattleRewardData CreateRemnantReward(int amount)
    {
        return new BattleRewardData
        {
            Type = BattleRewardType.Remnant,
            RewardId = "0",
            SourceKey = "EventRoom|Remnant",
            Amount = Mathf.Max(0, amount),
            Name = "더스티움",
            Description = string.Empty
        };
    }

    public static BattleRewardData CreateRelicReward(RelicData relic, Sprite icon)
    {
        string relicId = relic != null && !string.IsNullOrWhiteSpace(relic.FragmentId)
            ? relic.FragmentId.Trim()
            : string.Empty;

        return new BattleRewardData
        {
            Type = BattleRewardType.Relic,
            RewardId = relicId,
            SourceKey = $"EventRoom|Relic|{relicId}",
            Amount = 1,
            Icon = icon,
            Name = relic != null ? GameDataLocalization.RelicName(relic) : relicId,
            Description = relic != null ? GameDataLocalization.RelicDescription(relic) : string.Empty
        };
    }

    public static BattleRewardData CreateSkillReward(SkillMasterData skill, Sprite icon)
    {
        string skillId = skill != null && !string.IsNullOrWhiteSpace(skill.SkillId)
            ? skill.SkillId.Trim()
            : string.Empty;

        return new BattleRewardData
        {
            Type = BattleRewardType.Skill,
            RewardId = skillId,
            SourceKey = $"EventRoom|Skill|{skillId}",
            Amount = 1,
            Icon = icon,
            Name = skill != null ? GameDataLocalization.SkillName(skill) : skillId,
            Description = BuildSkillDescription(skill)
        };
    }

    private static string BuildSkillDescription(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        string rarityName = SkillRarityUtility.GetDisplayName(skill.Rarity);
        string description = GameDataLocalization.SkillDetails(skill);

        if (string.IsNullOrWhiteSpace(description))
            description = GameLocalization.Get("battle.available_skill", "획득 가능한 스킬입니다.");

        return string.IsNullOrWhiteSpace(rarityName)
            ? description
            : $"[{rarityName}] {description}";
    }
}
