using System;

/// <summary>
/// 현재 선택된 Trial 정보를 씬 전환 후에도 유지하는 런타임 상태입니다.
/// 아무 Trial도 선택하지 않은 상태(SelectedMask == 0)가 기본 난이도입니다.
/// </summary>
public static class TrialSelectionState
{
    public const int TrialCount = 3;

    private static int selectedMask;

    /// <summary>
    /// 선택된 Trial을 비트 마스크로 반환합니다.
    /// Trial 0, 1, 2는 각각 1, 2, 4 비트를 사용합니다.
    /// </summary>
    public static int SelectedMask => selectedMask;

    /// <summary>
    /// 하나 이상의 Trial이 선택되어 있는지 반환합니다.
    /// </summary>
    public static bool HasAnySelected => selectedMask != 0;

    /// <summary>
    /// Trial 선택 상태가 변경될 때 호출됩니다.
    /// </summary>
    public static event Action SelectionChanged;

    public static bool IsSelected(int trialIndex)
    {
        if (!IsValidIndex(trialIndex))
            return false;

        int bit = 1 << trialIndex;
        return (selectedMask & bit) != 0;
    }

    public static void Toggle(int trialIndex)
    {
        if (!IsValidIndex(trialIndex))
            return;

        SetSelected(trialIndex, !IsSelected(trialIndex));
    }

    public static void SetSelected(int trialIndex, bool selected)
    {
        if (!IsValidIndex(trialIndex))
            return;

        int bit = 1 << trialIndex;
        int nextMask = selected
            ? selectedMask | bit
            : selectedMask & ~bit;

        ApplyMask(nextMask);
    }

    public static void SetMask(int mask)
    {
        int validMask = (1 << TrialCount) - 1;
        ApplyMask(mask & validMask);
    }

    public static void Clear()
    {
        ApplyMask(0);
    }

    private static void ApplyMask(int nextMask)
    {
        if (selectedMask == nextMask)
            return;

        selectedMask = nextMask;
        SelectionChanged?.Invoke();
    }

    private static bool IsValidIndex(int trialIndex)
    {
        return trialIndex >= 0 && trialIndex < TrialCount;
    }
}
