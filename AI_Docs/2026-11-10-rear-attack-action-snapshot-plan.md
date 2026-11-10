# Rear Attack Action Snapshot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 한 공격 행동의 모든 연타는 동일한 기습 판정을 사용하고, 다음 행동은 피격 후 변경된 방향으로 다시 판정한다.

**Architecture:** 행동 로컬 `BattleRearAttackActionSnapshot`이 대상 ID별 최초 기습 판정을 저장한다. 피해 컨텍스트는 스냅샷 값을 우선 사용하고, 피해 결과가 발생한 뒤에만 대상 Runtime 방향과 Facing을 함께 갱신한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, 기존 BattleEffectContext/ActionRunner 구조

**Spec:** `AI_Docs/2026-11-10-rear-attack-action-snapshot-design.md`

## Global Constraints

- 실제 전투 결과는 UI 또는 애니메이션 상태에 의존하지 않는다.
- 대상 키는 `RuntimeId` 또는 `CharacterId`를 사용한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 같은 대상의 연타가 첫 기습 판정을 유지하는지 확인한다.
- 다음 행동이 새 스냅샷으로 방향을 재판정하는지 확인한다.
- 여러 대상의 판정이 RuntimeId별로 섞이지 않는지 확인한다.
- 피해가 없는 경우 대상 방향이 바뀌지 않는지 확인한다.
- 플레이어와 몬스터 기습 효과가 같은 우선순위 규칙을 쓰는지 확인한다.

---

### Task 1: 행동 단위 기습 스냅샷

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleRearAttackActionSnapshot.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectContext.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/BattleDamageModifierUtility.cs`
- Test: `Assets/Tests/EditMode~/BattleRearAttackActionSnapshotTests.cs`

**Interfaces:**
- Produces: `bool BattleRearAttackActionSnapshot.GetOrCapture(string targetRuntimeId, bool liveQualification)`
- Produces: `bool? BattleEffectContext.RearAttackQualifiedAtActionStart`

- [ ] 스냅샷 유지, 새 행동 재판정, 대상별 분리를 검증하는 실패 테스트를 작성한다.
- [ ] 신규 타입 부재로 RED가 되는지 테스트 포함 Editor 컴파일로 확인한다.
- [ ] 스냅샷과 피해 컨텍스트 우선순위 처리를 최소 구현한다.
- [ ] 테스트 포함 Editor 컴파일이 성공하는지 확인한다.

### Task 2: 플레이어 및 몬스터 공격 실행 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`
- Test: `Assets/Tests/EditMode~/BattleRearAttackActionSnapshotTests.cs`

**Interfaces:**
- Consumes: `BattleRearAttackActionSnapshot.GetOrCapture(...)`
- Consumes: `BattleEffectContext.RearAttackQualifiedAtActionStart`

- [ ] 피해 여부에 따른 방향 갱신 정책 테스트를 먼저 추가한다.
- [ ] 플레이어 행동 범위에서 스냅샷 하나를 생성해 모든 피해 효과와 연타에 전달한다.
- [ ] 몬스터 행동 범위에서도 같은 방식으로 스냅샷을 전달한다.
- [ ] 실제 HP 또는 보호막 피해 뒤에만 대상 Runtime 방향과 Facing을 갱신한다.
- [ ] 런타임과 테스트 포함 Editor 어셈블리를 컴파일하고 diff를 점검한다.

