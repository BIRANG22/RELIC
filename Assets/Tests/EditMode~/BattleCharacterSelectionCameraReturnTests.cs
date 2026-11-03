using System.Reflection;
using NUnit.Framework;

public class BattleCharacterSelectionCameraReturnTests
{
    [Test]
    public void ExplicitReturnApi_AllowsCharacterSelectionToRemainActive()
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            "StartReturnDefaultKeepingCharacterSelection",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);
    }

    private static MethodInfo ResolvePolicy()
    {
        return typeof(BattleTimelineController).GetMethod(
            "ShouldReturnCameraForSelectedCharacter",
            BindingFlags.Static | BindingFlags.NonPublic);
    }

    [Test]
    public void IdleSelectedCharacter_ReturnsCamera()
    {
        MethodInfo policy = ResolvePolicy();

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy.Invoke(null, new object[] { false, true }), Is.EqualTo(true));
    }

    [Test]
    public void ActiveSkillSelection_DoesNotReturnCameraBeforeCancellingSelection()
    {
        MethodInfo policy = ResolvePolicy();

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy.Invoke(null, new object[] { true, true }), Is.EqualTo(false));
    }

    [Test]
    public void NoSelectedCharacter_DoesNotConsumeReservationUndoClick()
    {
        MethodInfo policy = ResolvePolicy();

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy.Invoke(null, new object[] { false, false }), Is.EqualTo(false));
    }
}
