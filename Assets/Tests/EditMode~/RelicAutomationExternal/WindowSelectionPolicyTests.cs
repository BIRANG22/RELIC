using RelicAutomation.Core;
using Xunit;

namespace RelicAutomation.Core.Tests;

public sealed class WindowSelectionPolicyTests
{
    [Fact]
    public void SelectCandidates_RemovesInvisibleInvalidAndOwnWindows()
    {
        WindowSnapshot valid = new((nint)10, 1, "Project", "project.exe", 1280, 720);
        WindowSnapshot noTitle = new((nint)11, 2, string.Empty, "empty.exe", 1280, 720);
        WindowSnapshot noSize = new((nint)12, 3, "Hidden", "hidden.exe", 0, 0);
        WindowSnapshot own = new((nint)13, 4, "Automation", "tool.exe", 800, 600);

        IReadOnlyList<WindowSnapshot> result = WindowSelectionPolicy.SelectCandidates(
            new[] { noTitle, valid, own, noSize },
            own.Handle);

        Assert.Equal(new[] { valid }, result);
    }

    [Fact]
    public void SelectCandidates_SortsByTitleThenProcess()
    {
        WindowSnapshot beta = new((nint)10, 1, "Beta", "z.exe", 1280, 720);
        WindowSnapshot alphaZ = new((nint)11, 2, "Alpha", "z.exe", 1280, 720);
        WindowSnapshot alphaA = new((nint)12, 3, "Alpha", "a.exe", 1280, 720);

        IReadOnlyList<WindowSnapshot> result = WindowSelectionPolicy.SelectCandidates(
            new[] { beta, alphaZ, alphaA },
            (nint)99);

        Assert.Equal(new[] { alphaA, alphaZ, beta }, result);
    }
}
