# Remove Hit Facing Flip Implementation Plan

> **For agentic workers:** Implement directly in the current workspace using test-first changes and fresh compile verification.

**Goal:** 실제 피해를 받은 플레이어와 몬스터가 피격 전 방향을 유지하게 한다.

**Architecture:** 피해 계산과 후속 전투 판정은 유지하고, 피해 처리 이후 프레젠테이션/런타임 방향을 변경하는 전용 호출만 제거한다. 비피해 효과 및 명시적인 이동·공격 방향 전환은 그대로 둔다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-15-remove-hit-facing-flip-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode는 사용하지 않는다.
- 전투 피해 결과와 방향 연출을 분리한다.

## Review Focus

- 플레이어 피격 후 방향 변경 호출이 남지 않아야 한다.
- 몬스터 피격 후 방향 변경 호출이 남지 않아야 한다.
- 반격과 분열용 `receivedDamage` 판정은 유지되어야 한다.
- 비피해 효과의 방향 전환은 유지되어야 한다.

### Task 1: 피해 후 자동 방향 전환 제거

**Files:**
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`

- [ ] 양쪽 피해 처리 소스에 피격 방향 변경 메서드가 없음을 검사하는 회귀 테스트를 추가한다.
- [ ] 현재 코드에서 테스트 조건이 실패하는지 확인한다.
- [ ] 피해 직후 방향 변경 호출과 전용 메서드, 불필요해진 플레이어 HP·실드 스냅샷을 제거한다.
- [ ] 런타임·에디터 컴파일과 diff 검사를 실행한다.

