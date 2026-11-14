using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

public static class ArabellaPhaseTransition
{
    public static event Action PhaseTwoApplied;
    public static event Action PhaseTwoPresentationStarted;
    public static event Action PhaseTwoPresentationCompleted;
    public const string PhaseOneMonsterId = "Mon_12_1";
    public const string PhaseTwoMonsterId = "Mon_12_2";
    public const string PhaseOneDefeatedAnimationState = "PhaseOneDefeated";

    // 2페이즈 HP 표시가 차오르는 최대 시간(기본 전투 속도 기준, 초).
    private const float PhaseTwoHPFillDuration = 1f;

    // Incremented only when phase one successfully changes to phase two.
    public static int TurnInterruptVersion { get; private set; }

    private static MonsterUnit pendingMonster;
    private static MonsterRuntimeData pendingPhaseTwo;

    public static bool HasPendingTransition => pendingMonster != null && pendingPhaseTwo != null;
    public static string PendingMonsterRuntimeId =>
        pendingMonster != null && pendingMonster.RuntimeData != null
            ? pendingMonster.RuntimeData.RuntimeId
            : "";

    public static int RemoveCommandsAfterTimelineSlot(
        IList<BattleActionBatch> batches,
        int completedTimelineSlotIndex,
        string monsterRuntimeId)
    {
        if (batches == null || string.IsNullOrWhiteSpace(monsterRuntimeId))
            return 0;

        int removed = 0;
        for (int i = 0; i < batches.Count; i++)
        {
            BattleActionBatch batch = batches[i];
            if (batch == null || batch.TimelineSlotIndex <= completedTimelineSlotIndex)
                continue;

            removed += batch.RemoveMonsterCommandsByRuntimeId(monsterRuntimeId);
        }

        return removed;
    }

    public static bool IsTransitionPending(MonsterUnit monster)
    {
        return pendingMonster != null && pendingMonster == monster;
    }

    // Any damage route that reduces phase one to zero must queue phase two before victory checks.
    public static bool TryQueueIfDefeated(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null ||
            !string.Equals(monster.RuntimeData.MonsterId, PhaseOneMonsterId, StringComparison.Ordinal) ||
            monster.RuntimeData.CurrentHP > 0)
            return false;

        if (IsTransitionPending(monster))
            return true;

        MonsterMasterData phaseTwoMaster =
            DataManager.Instance?.MonsterDatabase?.Get(PhaseTwoMonsterId);
        if (!TryCreatePhaseTwo(monster.RuntimeData, phaseTwoMaster, out MonsterRuntimeData phaseTwo))
        {
            Debug.LogError("[ArabellaPhaseTransition] Failed to create phase two after phase-one HP reached zero.");
            return false;
        }

        QueuePhaseTwo(monster, phaseTwo);
        return true;
    }

    public static void QueuePhaseTwo(MonsterUnit monster, MonsterRuntimeData phaseTwo)
    {
        if (monster == null || phaseTwo == null || IsTransitionPending(monster))
            return;

        pendingMonster = monster;
        pendingPhaseTwo = phaseTwo;
        NotifyPhaseTwoStarted();
    }

    public static void PlayPendingPhaseOneDefeatedPresentation()
    {
        if (pendingMonster == null)
            return;

        BattleUnitAnimator animator = pendingMonster.GetComponent<BattleUnitAnimator>();
        animator?.PlayArabellaPhaseOneDefeated();
    }

    // 현재 실행 중인 스킬이 끝난 후에만 2페이즈 데이터와 연출을 적용합니다.
    public static IEnumerator CompletePendingTransitionRoutine()
    {
        MonsterUnit monster = pendingMonster;
        MonsterRuntimeData phaseTwo = pendingPhaseTwo;
        pendingMonster = null;
        pendingPhaseTwo = null;

        if (monster == null || phaseTwo == null)
            yield break;

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
        BattleCameraController cameraController = BattleCameraController.Instance;
        NotifyPhaseTwoPresentationStarted();

        // 현재 행동이 종료된 다음 아라벨라를 화면 중앙에 포커스합니다.
        // 전환 연출이 끝날 때까지 카메라를 유지하고 기존 전투 위치로 돌아갑니다.
        if (cameraController != null)
            yield return cameraController.PlayArabellaPhaseTransitionCamera(monster.transform);

        // 포커스 전에 HUD가 새 HP(105)를 먼저 표시하지 않도록 순서를 고정합니다.
        monster.Initialize(phaseTwo);
        NotifyPhaseTwoApplied();
        monster.BeginPhaseTransitionHUDPresentation();

        MonsterPhaseCameraSequence phaseCameraSequence =
            monster.GetComponentInParent<MonsterPhaseCameraSequence>();

        if (animator != null)
        {
            float speed = Mathf.Max(0.01f,
                BattleConsecutiveActionPresentationContext.BattleExecutionSpeedMultiplier);
            // HP 표시 속도는 2Phase 클립 길이와 별개로 최대 1초만 사용합니다.
            monster.ShowPhaseTransitionHPFill(PhaseTwoHPFillDuration / speed);
            animator.PlayArabellaPhaseTransition();

            if (phaseCameraSequence != null && phaseCameraSequence.EnablePhaseCamera &&
                cameraController != null)
            {
                // 카메라가 시작할 때 등록한 Animator 모니터가 실제 상태 완료를 판정합니다.
                // 고정 시간으로 애니메이션을 종료하거나 전투를 재개하지 않습니다.
                yield return phaseCameraSequence.WaitForPhaseAnimationCompletion();
            }
            else
            {
                // 카메라 컴포넌트를 사용하지 않는 기존 프리팹도 클립 종료를 기다립니다.
                yield return WaitForPhaseAnimationState(monster, "2phase");
            }
        }
        else
        {
            monster.ShowPhaseTransitionHPFill(0f);
        }

        // 애니메이션 종료 후 기존 HUD를 닫습니다. 카메라 Step이 아직 진행 중이면
        // ReturnFromArabellaPhaseTransitionCamera가 나머지 Step 완료 후 복귀합니다.
        monster.EndPhaseTransitionHUDPresentation();

        if (cameraController != null)
            yield return cameraController.ReturnFromArabellaPhaseTransitionCamera();

        NotifyPhaseTwoPresentationCompleted();
    }

    // MonsterPhaseCameraSequence가 없는 경우에도 클립 완료를 확인하기 위한 대체 경로.
    private static IEnumerator WaitForPhaseAnimationState(MonsterUnit monster, string stateName)
    {
        if (monster == null)
            yield break;
        Animator phaseAnimator = monster.GetComponentInChildren<Animator>(true);
        if (phaseAnimator == null || !phaseAnimator.isActiveAndEnabled)
            yield break;

        const int layer = 0;
        int hash = Animator.StringToHash(stateName);
        float elapsed = 0f;
        const float enterTimeout = 1f;
        bool entered = false;
        while (phaseAnimator != null && phaseAnimator.isActiveAndEnabled && elapsed < enterTimeout)
        {
            AnimatorStateInfo current = phaseAnimator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = phaseAnimator.GetNextAnimatorStateInfo(layer);
            if (current.shortNameHash == hash ||
                (phaseAnimator.IsInTransition(layer) && next.shortNameHash == hash))
            {
                entered = true;
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!entered)
        {
            Debug.LogWarning("[ArabellaPhaseTransition] 2phase Animator state was not entered.", monster);
            yield break;
        }

        while (phaseAnimator != null && phaseAnimator.isActiveAndEnabled)
        {
            AnimatorStateInfo current = phaseAnimator.GetCurrentAnimatorStateInfo(layer);
            if (current.shortNameHash == hash && current.normalizedTime >= 1f)
                yield break;
            if (current.shortNameHash != hash &&
                !(phaseAnimator.IsInTransition(layer) &&
                  phaseAnimator.GetNextAnimatorStateInfo(layer).shortNameHash == hash))
            {
                Debug.LogWarning("[ArabellaPhaseTransition] 2phase Animator state was interrupted.", monster);
                yield break;
            }
            yield return null;
        }
    }

    public static void NotifyPhaseTwoStarted()
    {
        unchecked { TurnInterruptVersion++; }
    }

    public static void NotifyPhaseTwoApplied()
    {
        PhaseTwoApplied?.Invoke();
    }

    public static void NotifyPhaseTwoPresentationStarted()
    {
        PhaseTwoPresentationStarted?.Invoke();
    }

    public static void NotifyPhaseTwoPresentationCompleted()
    {
        PhaseTwoPresentationCompleted?.Invoke();
    }

    public static bool IsArabellaMonsterId(string monsterId)
    {
        return string.Equals(monsterId, PhaseOneMonsterId, StringComparison.Ordinal) ||
               string.Equals(monsterId, PhaseTwoMonsterId, StringComparison.Ordinal);
    }

    public static bool TryCreatePhaseTwo(
        MonsterRuntimeData current,
        MonsterMasterData phaseTwoMaster,
        out MonsterRuntimeData phaseTwo)
    {
        phaseTwo = null;

        if (current == null ||
            !current.IsDead ||
            current.IsDeathHandled ||
            !string.Equals(current.MonsterId, PhaseOneMonsterId, StringComparison.Ordinal) ||
            phaseTwoMaster == null ||
            !string.Equals(phaseTwoMaster.MonsterId, PhaseTwoMonsterId, StringComparison.Ordinal))
        {
            return false;
        }

        phaseTwo = new MonsterRuntimeData(current.RuntimeId, phaseTwoMaster)
        {
            Direction = current.Direction
        };

        // 페이즈 전환 시 일반 버프와 디버프는 승계하지 않습니다.
        // 1페이즈부터 보유했던 몬스터 고유 상태는 유지하고,
        // 2페이즈의 기본 고유 상태는 생성자에서 초기화된 그대로 보존합니다.
        if (current.StatusEffects != null)
        {
            foreach (StatusEffectRuntimeData status in current.StatusEffects)
            {
                if (status == null || !status.IsMonsterInnateTrait)
                    continue;

                bool alreadyExists = phaseTwo.StatusEffects.Exists(existing =>
                    existing != null &&
                    string.Equals(existing.EffectId, status.EffectId, StringComparison.Ordinal));
                if (!alreadyExists)
                    phaseTwo.StatusEffects.Add(new StatusEffectRuntimeData(status.EffectId, status.Stack, status.TurnCount)
                    {
                        IsPassive = status.IsPassive,
                        IsMonsterInnateTrait = true
                    });
            }
        }
        return true;
    }

    public static string ResolvePhaseTwoAnimationState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) ||
            stateName.EndsWith("_b", StringComparison.Ordinal))
        {
            return stateName;
        }

        if (stateName == "Idle" ||
            stateName == "Move" ||
            stateName == "Guard" ||
            stateName == "Hit" ||
            stateName == "Dead" ||
            stateName.StartsWith("AttackAction", StringComparison.Ordinal))
        {
            return stateName + "_b";
        }

        return stateName;
    }

    public static List<string> FilterOwnedSkills(
        MonsterRuntimeData runtime,
        IEnumerable<string> candidateSkillIds)
    {
        List<string> result = new();
        if (runtime == null || candidateSkillIds == null)
            return result;

        foreach (string skillId in candidateSkillIds)
        {
            if (!string.IsNullOrWhiteSpace(skillId) &&
                runtime.HasSkill(skillId) &&
                !result.Contains(skillId))
            {
                result.Add(skillId);
            }
        }

        return result;
    }
}
