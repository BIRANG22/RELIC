using System.Threading;

/// <summary>
/// 스킬과 룬이 함께 사용하는 InfoArea의 호버 전환 상태를 관리합니다.
/// 다른 항목으로 바로 이동할 때 이전 항목의 PointerExit가 새 정보를 지우지 않도록 합니다.
/// </summary>
internal static class LobbyInfoHoverState
{
    public const float ClearDelaySeconds = 0.05f;

    private static int version;

    public static int CurrentVersion => Volatile.Read(ref version);

    public static void NotifyInfoShown()
    {
        Interlocked.Increment(ref version);
    }

    public static bool IsCurrent(int capturedVersion)
    {
        return CurrentVersion == capturedVersion;
    }
}
