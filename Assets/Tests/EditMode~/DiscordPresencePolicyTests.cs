using NUnit.Framework;

public class DiscordPresencePolicyTests
{
    [Test]
    public void TryValidateApplicationId_Zero_IsRejected()
    {
        bool valid = DiscordPresencePolicy.TryValidateApplicationId(0UL, out string error);

        Assert.That(valid, Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void TryValidateApplicationId_RelicId_IsAccepted()
    {
        bool valid = DiscordPresencePolicy.TryValidateApplicationId(
            1533104947875549325UL,
            out string error);

        Assert.That(valid, Is.True);
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void FromUpdateResult_FailedUpdate_RemainsRetryable()
    {
        DiscordPresenceStatus status = DiscordPresencePolicy.FromUpdateResult(false);

        Assert.That(status, Is.EqualTo(DiscordPresenceStatus.Unavailable));
        Assert.That(DiscordPresencePolicy.ShouldRetry(status), Is.True);
    }

    [Test]
    public void FromUpdateResult_SuccessfulUpdate_IsReady()
    {
        DiscordPresenceStatus status = DiscordPresencePolicy.FromUpdateResult(true);

        Assert.That(status, Is.EqualTo(DiscordPresenceStatus.Ready));
    }

    [Test]
    public void InvokeSafely_ThrownSdkException_IsReportedWithoutEscaping()
    {
        System.Exception reported = null;

        bool completed = DiscordPresencePolicy.InvokeSafely(
            () => throw new System.InvalidOperationException("native failure"),
            exception => reported = exception);

        Assert.That(completed, Is.False);
        Assert.That(reported, Is.TypeOf<System.InvalidOperationException>());
        Assert.That(reported.Message, Is.EqualTo("native failure"));
    }

    [Test]
    public void InvokeSafely_SuccessfulSdkOperation_Completes()
    {
        bool invoked = false;

        bool completed = DiscordPresencePolicy.InvokeSafely(
            () => invoked = true,
            _ => Assert.Fail("성공 경로에서 오류 콜백이 호출되었습니다."));

        Assert.That(completed, Is.True);
        Assert.That(invoked, Is.True);
    }
}
