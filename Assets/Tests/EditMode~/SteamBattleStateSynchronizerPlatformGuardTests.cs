using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class SteamBattleStateSynchronizerPlatformGuardTests
{
    private const string SourcePath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs";

    [TestCase("TryBroadcastBattleExecution", "Instance.BroadcastBattleExecution(batches);")]
    [TestCase("TryBroadcastStartRelicSelected", "Instance.BroadcastStartRelicSelected(relicId);")]
    [TestCase("TryBroadcastStartRelicChoices", "Instance.SetHostStartRelicChoices(relicIds);")]
    [TestCase("TryApplyKnownStartRelicChoices", "Instance.GetCachedStartRelicChoices();")]
    [TestCase("RequestEquipRelic", "CreateCommand(BattleNetworkCommandType.EquipRelic)")]
    [TestCase("RequestUnequipRelic", "CreateCommand(BattleNetworkCommandType.UnequipRelic)")]
    [TestCase("RequestEquipSkill", "CreateCommand(BattleNetworkCommandType.EquipSkill)")]
    [TestCase("RequestUnequipSkill", "CreateCommand(BattleNetworkCommandType.UnequipSkill)")]
    [TestCase("ApplyTimelineSnapshot", "if (!IsLocalHost())")]
    public void SteamOnlyInternalCall_IsGuardedForUnsupportedPlatforms(
        string methodName,
        string steamOnlyCall)
    {
        string source = File.ReadAllText(SourcePath);
        Match methodMatch = Regex.Match(
            source,
            @"\b(?:public|private)\s+(?:static\s+)?(?:bool|void)\s+" +
            Regex.Escape(methodName) +
            @"\(");
        Assert.That(methodMatch.Success, Is.True, methodName + " method was not found.");
        int methodStart = methodMatch.Index;

        int callStart = source.IndexOf(steamOnlyCall, methodStart, System.StringComparison.Ordinal);
        Assert.That(callStart, Is.GreaterThanOrEqualTo(0), steamOnlyCall + " was not found.");

        string methodPrefix = source.Substring(methodStart, callStart - methodStart);
        int lastSteamGuard = methodPrefix.LastIndexOf(
            "#if STEAMWORKS_NET",
            System.StringComparison.Ordinal);
        int lastGuardEnd = methodPrefix.LastIndexOf(
            "#endif",
            System.StringComparison.Ordinal);

        Assert.That(
            lastSteamGuard,
            Is.GreaterThan(lastGuardEnd),
            methodName + " must not call a Steam-only internal method on WebGL.");
    }
}
