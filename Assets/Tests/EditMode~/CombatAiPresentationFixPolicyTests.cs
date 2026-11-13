using System;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class CombatAiPresentationFixPolicyTests
{
    [Test]
    public void MovementFacingTransition_AttackThenBackwardMove_IsDisplayedOnMoveOnly()
    {
        Assert.That(MonsterMovementFacingPresentationPolicy.ShouldShowOnMove(
            BattleDirection.Right, Vector2Int.left), Is.True);
        Assert.That(MonsterMovementFacingPresentationPolicy.ShouldShowOnMove(
            BattleDirection.Left, Vector2Int.left), Is.False);
    }

    [Test]
    public void MonsterCommand_FinalPlacementGridIndex_IsStableExecutionResult()
    {
        MonsterRuntimeData runtime = new("runtime", null) { CurrentHP = 1 };
        MonsterReservedCommand command = new(runtime, null);

        command.SetFinalPlacementGridIndex(17);

        Assert.That(command.FinalPlacementGridIndex, Is.EqualTo(17));
        Assert.That(command.HasFinalPlacementGridIndex, Is.True);
    }

    [Test]
    public void ArabellaPhaseOneEscape_MinimumSafeDistanceWinsBeforeExtraDistance()
    {
        Assert.That(ArabellaPhaseOneEscapePolicy.IsBetterCandidate(
            candidateMoveDistance: 1, candidateThreatDistance: 3, candidateSpaceScore: 2,
            bestMoveDistance: 2, bestThreatDistance: 4, bestSpaceScore: 99), Is.True);
    }

    [Test]
    public void BoundaryBlock_NeverProducesCollisionDamage()
    {
        Assert.That(MonsterMoveCollisionPolicy.ShouldApplyCrash(
            blockedByUnit: false, blockedByGridEffect: false, blockedByBoundary: true), Is.False);
    }

    [TestCase("Mon_12_1", true, false)]
    [TestCase("Mon_12_2", true, true)]
    [TestCase("Mon_01", true, true)]
    [TestCase("Mon_01", false, false)]
    public void DeathContinuationPolicy_PreservesPhaseOneTransitionException(
        string monsterId, bool dead, bool expectedStop)
    {
        Assert.That(MonsterActionDeathPolicy.ShouldStopAfterStateChange(monsterId, dead),
            Is.EqualTo(expectedStop));
    }

    [Test]
    public void AttackMoveOverlap_OpensAtImpactAndJoinsMoveWithTail()
    {
        MonsterActionOverlapGate gate = new();
        Assert.That(gate.CanStartFollowupMove, Is.False);
        Assert.That(gate.IsComplete, Is.False);

        gate.CommitImpact();
        Assert.That(gate.CanStartFollowupMove, Is.True);

        gate.CompleteMove();
        Assert.That(gate.IsComplete, Is.False);
        gate.CompletePresentationTail();
        Assert.That(gate.IsComplete, Is.True);
    }

    [Test]
    public void PlayerAttackMove_PlaysAttackThenCompletesMoveBeforeHitJudgement()
    {
        const string path =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs";
        string source = File.ReadAllText(path);
        int start = source.IndexOf(
            "private IEnumerator ExecutePlayerDamageHitSequence(",
            StringComparison.Ordinal);
        int end = source.IndexOf("private static bool HasActiveMonsterCounter", start,
            StringComparison.Ordinal);
        string method = source.Substring(start, end - start);

        int attackAction = method.IndexOf("attackerAnimator.PlaySkillAction(command, hitIndex)",
            StringComparison.Ordinal);
        int move = method.IndexOf("ExecutePlayerDirectionalMoveEffect(", StringComparison.Ordinal);
        int prepareCamera = method.IndexOf("bool prepareCameraImpact", StringComparison.Ordinal);
        int stateChange = method.IndexOf("ExecutePlayerEffectSafely(", StringComparison.Ordinal);

        Assert.That(move, Is.GreaterThan(attackAction));
        Assert.That(move, Is.LessThan(prepareCamera));
        Assert.That(move, Is.LessThan(stateChange));
    }

    [Test]
    public void PlayerAttackMove_DoesNotUseDetachedPlayerMoveCoroutine()
    {
        const string path =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs";
        string source = File.ReadAllText(path);
        int start = source.IndexOf(
            "private IEnumerator ExecutePlayerSkillEffectsToMonsters(",
            StringComparison.Ordinal);
        int end = source.IndexOf("private void ExecutePlayerSkillEffectsToPlayer(", start,
            StringComparison.Ordinal);
        string method = source.Substring(start, end - start);

        Assert.That(method.Contains("moveStarted", StringComparison.Ordinal), Is.False);
        Assert.That(method.Contains("moveCompleted", StringComparison.Ordinal), Is.False);
        Assert.That(method.Contains("StartCoroutine(", StringComparison.Ordinal), Is.False);
    }

    [Test]
    public void ArabellaLeap_AppliesLandingGridEffectBeforeDamageAndSkipsIdleWhenDead()
    {
        const string path =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs";
        string source = File.ReadAllText(path);
        int start = source.IndexOf(
            "private IEnumerator ExecuteArabellaLeap(MonsterReservedCommand command)",
            StringComparison.Ordinal);
        int end = source.IndexOf("private IEnumerator ExecuteMonsterMove", start,
            StringComparison.Ordinal);
        string method = source.Substring(start, end - start);

        int landingEffects = method.IndexOf("ApplyGridEffectsToMonster(", StringComparison.Ordinal);
        int damageLoop = method.IndexOf("for (int i = 0; i < hitTargets.Count; i++)",
            StringComparison.Ordinal);
        int deathGuard = method.LastIndexOf("ShouldStopAfterStateChange", StringComparison.Ordinal);
        int idle = method.LastIndexOf("animator.PlayIdle()", StringComparison.Ordinal);

        Assert.That(landingEffects, Is.GreaterThanOrEqualTo(0));
        Assert.That(landingEffects, Is.LessThan(damageLoop));
        Assert.That(deathGuard, Is.GreaterThan(damageLoop));
        Assert.That(idle, Is.GreaterThan(deathGuard));
    }
}
