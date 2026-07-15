using System.Threading;

/// <summary>
/// 로비의 스킬/룬 공용 정보 영역에서 현재 표시 주체와 호버 순서를 관리합니다.
/// 빠르게 다른 항목으로 이동했을 때 이전 항목의 지연 초기화가 새 정보를 덮어쓰는 것을 방지합니다.
/// </summary>
internal static class LobbyInfoHoverState
{
    public enum InfoOwner
    {
        None,
        Skill,
        Rune
    }

    public const float ClearDelaySeconds = 0.05f;

    private static int version;
    private static int runeHoverCount;
    private static int skillHoverCount;
    private static int currentOwner;

    public static int CurrentVersion => Volatile.Read(ref version);
    public static bool IsRuneHovered => Volatile.Read(ref runeHoverCount) > 0;
    public static bool IsSkillHovered => Volatile.Read(ref skillHoverCount) > 0;
    public static InfoOwner CurrentOwner => (InfoOwner)Volatile.Read(ref currentOwner);

    public static int NotifySkillInfoShown()
    {
        // 룬 위에 마우스가 있는 동안에는 늦게 들어온 스킬 호버가 표시 주체를 빼앗지 못합니다.
        if (IsRuneHovered)
            return CurrentVersion;

        Interlocked.Exchange(ref currentOwner, (int)InfoOwner.Skill);
        return Interlocked.Increment(ref version);
    }

    public static int NotifyRuneInfoShown()
    {
        Interlocked.Exchange(ref currentOwner, (int)InfoOwner.Rune);
        return Interlocked.Increment(ref version);
    }

    // 기존 호출부 호환용입니다. 새 코드에서는 스킬/룬 전용 함수를 사용합니다.
    public static void NotifyInfoShown()
    {
        Interlocked.Increment(ref version);
    }

    public static void BeginRuneHover()
    {
        Interlocked.Increment(ref runeHoverCount);
        NotifyRuneInfoShown();
    }

    public static void EndRuneHover()
    {
        int next = Interlocked.Decrement(ref runeHoverCount);
        if (next < 0)
            Interlocked.Exchange(ref runeHoverCount, 0);
    }

    /// <summary>
    /// 스킬 아이콘에 포인터가 들어왔음을 기록합니다.
    /// 기존 SkillIconButton 호출부와의 호환을 위해 유지합니다.
    /// </summary>
    public static void BeginSkillHover()
    {
        Interlocked.Increment(ref skillHoverCount);

        // 룬 호버가 우선이므로 룬 위에 있는 동안에는 표시 주체를 바꾸지 않습니다.
        if (!IsRuneHovered)
            NotifySkillInfoShown();
    }

    /// <summary>
    /// 스킬 아이콘에서 포인터가 빠졌음을 기록합니다.
    /// </summary>
    public static void EndSkillHover()
    {
        int next = Interlocked.Decrement(ref skillHoverCount);
        if (next < 0)
            Interlocked.Exchange(ref skillHoverCount, 0);

        Interlocked.Increment(ref version);
    }

    public static bool CanClearSkillInfo(int capturedVersion)
    {
        return !IsRuneHovered &&
               CurrentOwner == InfoOwner.Skill &&
               CurrentVersion == capturedVersion;
    }

    public static bool CanClearRuneInfo(int capturedVersion)
    {
        return !IsRuneHovered &&
               CurrentOwner == InfoOwner.Rune &&
               CurrentVersion == capturedVersion;
    }

    public static bool IsCurrent(int capturedVersion)
    {
        return CurrentVersion == capturedVersion;
    }
}
