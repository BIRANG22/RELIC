namespace RelicAutomation.Core;

public sealed record AutomationProfile(
    string Name,
    string EntryNode,
    string TargetNode,
    int RepeatCount,
    int ExpectedClientWidth,
    int ExpectedClientHeight,
    bool StopWhenWindowSizeChanges,
    string ResourceDirectory);

public sealed record ProfileValidationResult(
    bool IsValid,
    string ResourceDirectory,
    IReadOnlyList<string> Errors);
