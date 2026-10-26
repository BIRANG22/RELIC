using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

public static class ArabellaPhaseTransition
{
    public const string PhaseOneMonsterId = "Mon_12_1";
    public const string PhaseTwoMonsterId = "Mon_12_2";

    // 2페이즈 HP 표시가 차오르는 최대 시간(기본 전투 속도 기준, 초).
    private const float PhaseTwoHPFillDuration = 1f;

    // Incremented only when phase one successfully changes to phase two.
    public static int TurnInterruptVersion { get; private set; }

    // 현재 페이즈 전환으로 인해 아직 시작하지 않은 예약은 실행하지 않습니다.
    public static bool SkipRemainingReservations { get; private set; }

    public static void ResetSkippedReservations()
    {
        SkipRemainingReservations = false;
    }

    public static void MarkRemainingReservationsSkipped()
    {
        SkipRemainingReservations = true;
    }

    private static MonsterUnit pendingMonster;
    private static MonsterRuntimeData pendingPhaseTwo;

    public static bool HasPendingTransition => pendingMonster != null && pendingPhaseTwo != null;

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

        // 현재 행동이 종료된 다음 아라벨라를 화면 중앙에 포커스합니다.
        // 전환 연출이 끝날 때까지 카메라를 유지하고 기존 전투 위치로 돌아갑니다.
        if (cameraController != null)
            yield return cameraController.ZoomTo(monster.transform);

        // 포커스 전에 HUD가 새 HP(105)를 먼저 표시하지 않도록 순서를 고정합니다.
        monster.Initialize(phaseTwo);
        monster.BeginPhaseTransitionHUDPresentation();

        if (animator != null)
        {
            float speed = Mathf.Max(0.01f,
                BattleConsecutiveActionPresentationContext.BattleExecutionSpeedMultiplier);
            float duration = animator.ArabellaPhaseTransitionDuration / speed;
            // 전환 연출은 끝까지 재생하되 HP 게이지는 빠르게 채웁니다.
            float hpFillDuration = Mathf.Min(duration, PhaseTwoHPFillDuration / speed);
            monster.ShowPhaseTransitionHPFill(hpFillDuration);
            animator.PlayArabellaPhaseTransition();
            if (duration > 0f)
                yield return new WaitForSeconds(duration);
        }
        else
        {
            monster.ShowPhaseTransitionHPFill(0f);
        }

        // 전환 애니메이션이 모두 끝난 다음에만 HUD 숨김을 허용합니다.
        monster.EndPhaseTransitionHUDPresentation();

        if (cameraController != null)
            yield return cameraController.ReturnPanelDown();
    }

    public static void NotifyPhaseTwoStarted()
    {
        unchecked { TurnInterruptVersion++; }
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
