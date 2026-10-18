using System.IO;
using NUnit.Framework;

public class BattleStatusScaleFeedbackRemovalTests
{
    private const string HitFeedbackPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs";

    private const string EffectUtilityPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectUtility.cs";

    private const string ConsecutiveActionPlanPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleConsecutiveActionPlan.cs";

    [Test]
    public void StatusFeedback_DoesNotChangeCharacterScale()
    {
        string source = File.ReadAllText(HitFeedbackPath);

        Assert.That(source, Does.Not.Contain("localScale"));
        Assert.That(source, Does.Not.Contain("StatusPulse"));
    }

    [Test]
    public void StatusApplication_DoesNotInvokeScaleFeedback()
    {
        string effectUtility = File.ReadAllText(EffectUtilityPath);
        string consecutiveActionPlan = File.ReadAllText(ConsecutiveActionPlanPath);

        Assert.That(effectUtility, Does.Not.Contain("PlayStatusHitFeedback"));
        Assert.That(consecutiveActionPlan, Does.Not.Contain("StatusPulse"));
    }
}
