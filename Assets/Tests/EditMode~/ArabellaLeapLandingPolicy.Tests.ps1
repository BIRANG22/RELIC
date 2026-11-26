$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$policy = Join-Path $repo 'Assets/Project/Scripts/Gameplay/Monster/Ai/ArabellaLeapLandingPolicy.cs'
$checks = @'
using System;
using Relic.Gameplay.Monster;
public static class ArabellaLandingChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static ArabellaLeapLandingPolicy.Target T(int grid, bool right = true, bool push = true)
        => new ArabellaLeapLandingPolicy.Target(grid, right, push);
    public static int Run()
    {
        // Index = x * height + y, matching GridManager (not row-major).
        bool[] blocked = new bool[25]; blocked[12] = true;
        var targets = new[] { T(12) };
        int result = ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets);
        Check(result >= 0 && result != 12 && Math.Abs(result / 5 - 2) <= 1 && Math.Abs(result % 5 - 2) <= 1,
            "Landing must be adjacent to the character, never on its occupied grid");
        blocked[result] = true;
        int retry = ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets, result);
        Check(retry >= 0 && retry != result && !blocked[retry], "Blocked reservation must select a new valid landing");
        for (int i = 0; i < blocked.Length; i++) blocked[i] = true;
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets) == -1,
            "No free landing must cancel");
        blocked[6] = false;
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets) == -1,
            "A free but trapped cell must be rejected");
        blocked = new bool[25]; blocked[2] = true;
        result = ArabellaLeapLandingPolicy.Resolve(5, 5, 24, new[] { 24 }, blocked, new[] { T(2, false) });
        Check(result >= 0 && result / 5 == 0, "Near left edge land on target column so push goes right, not into wall");
        blocked = new bool[25]; blocked[12] = true;
        result = ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0, 5 }, blocked, targets);
        Check(result >= 0 && result / 5 < 4 && result != 12 && result + 5 != 12,
            "Entire multi-cell footprint must fit without overlapping target");
        int first = ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, new[] { T(12), T(17) });
        int reversed = ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, new[] { T(17), T(12) });
        Check(first == reversed, "Selection must be deterministic regardless of target enumeration order");
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, -1, new int[0], blocked, targets) == -1,
            "Invalid caster state must cancel");
        var followup = new int[25]; followup[18] = 1;
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets, -1, followup) == 18,
            "Among safe landing cells prefer a cell with a followup attack");
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, new[] { T(12, true, false) }) >= 0,
            "Immobilized targets must not be forcibly displaced to make room");
        Check(ArabellaLeapLandingPolicy.Resolve(5, 5, 0, new[] { 0 }, blocked, targets, 18) == 18,
            "Keep an equally safe reserved landing rather than jittering the telegraph");
        return 11;
    }
}
'@
$source = [IO.File]::ReadAllText($policy)
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks.Replace('using System;', '').Replace('using Relic.Gameplay.Monster;', '').Replace('ArabellaLeapLandingPolicy', 'Relic.Gameplay.Monster.ArabellaLeapLandingPolicy'))
$count = [ArabellaLandingChecks]::Run()
Write-Output "PASS: $count Arabella landing policy checks"
