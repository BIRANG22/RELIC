using System;
using Discord.Sdk;

public static class DiscordPresencePolicy
{
    public static bool TryValidateApplicationId(ulong applicationId, out string error)
    {
        if (applicationId == 0UL)
        {
            error = "Discord Application ID는 0일 수 없습니다.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static DiscordPresenceStatus FromUpdateResult(bool successful)
    {
        return successful
            ? DiscordPresenceStatus.Ready
            : DiscordPresenceStatus.Unavailable;
    }

    public static bool IsExpectedClientUnavailable(ErrorType errorType)
    {
        return errorType == ErrorType.NetworkError ||
               errorType == ErrorType.ClientNotReady ||
               errorType == ErrorType.Disabled ||
               errorType == ErrorType.RPCError;
    }

    public static bool ShouldRetry(DiscordPresenceStatus status)
    {
        return status == DiscordPresenceStatus.Initializing ||
               status == DiscordPresenceStatus.Unavailable;
    }

    public static bool InvokeSafely(Action operation, Action<Exception> onError)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            operation();
            return true;
        }
        catch (Exception exception)
        {
            onError?.Invoke(exception);
            return false;
        }
    }
}
