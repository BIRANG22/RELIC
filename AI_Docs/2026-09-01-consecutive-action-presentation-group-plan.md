# 연속 동일 행동 연출 그룹 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 슬롯 경계를 넘어 연속된 동일 비이동 행동을 전투 결과는 개별 처리하면서 하나의 가속 연출 그룹으로 재생한다.

**Architecture:** 기존 `BattleActionBatch`와 예약 커맨드는 유지하고, 실행 순서를 읽는 `BattleConsecutiveActionPlan`이 커맨드별 연출 메타데이터를 계산한다. `BattleTurnExecutor`와 `BattleActionRunner`는 이 계획을 사용해 그룹 시작 카메라, 매 행동 애니메이션/VFX, 그룹 종료 외부 임팩트를 분리한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, coroutine presentation flow

**Spec:** `AI_Docs/2026-09-01-consecutive-action-presentation-group-design.md`

## 전역 제약

- 이동 행동과 기존 이동 병합 구조를 변경하지 않는다.
- 각 커맨드의 비용과 전투 결과는 독립적으로 유지한다.
- 전투 판정에 `Time.timeScale` 또는 `UnityEngine.Random`을 새로 사용하지 않는다.
- 네트워크 모델과 스냅샷 필드를 추가하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: 연속 행동 계획 모델과 판정

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleConsecutiveActionPlan.cs`
- Test: `Assets/Tests/EditMode~/BattleConsecutiveActionPlanTests.cs`

**Interfaces:**
- Consumes: `IReadOnlyList<BattleActionBatch>`, 플레이어/몬스터 예약 커맨드
- Produces: `BattleConsecutiveActionPlan.Build(...)`, `GetInfo(PlayerReservedCommand)`, `GetInfo(MonsterReservedCommand)`, `BattleConsecutiveActionInfo`

- [ ] 동일 플레이어 커맨드, 슬롯 경계, 비교 항목 차이, 이동 단절을 검증하는 실패 테스트를 작성한다.
- [ ] 테스트를 실행해 계획 타입이 없어 실패하는 것을 확인한다.
- [ ] 실제 실행 순서로 배치를 펼치고 안정적인 ID, SkillId, 정렬 대상, 방향으로 그룹을 만드는 최소 구현을 작성한다.
- [ ] 테스트를 다시 실행해 통과를 확인한다.

### Task 2: 그룹 연출 정책과 실행 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/BattleConsecutiveActionPlanTests.cs`

**Interfaces:**
- Consumes: `BattleConsecutiveActionInfo`
- Produces: 그룹 시작 카메라, 그룹 종료 임팩트/복귀, 그룹 내부 후처리 생략 정책

- [ ] 그룹 시작/중간/끝의 카메라 진입, 외부 임팩트, 복귀 허용값을 검증하는 실패 테스트를 작성한다.
- [ ] 실패를 확인한 뒤 정책 속성을 구현한다.
- [ ] `BattleTurnExecutor`가 로컬 및 네트워크 실행 배치에서 같은 계획을 만들고 Runner에 전달하도록 연결한다.
- [ ] `BattleActionRunner`가 이동과 단독 행동은 기존 경로를 사용하고 그룹 행동은 경계 정책을 사용하도록 연결한다.
- [ ] 중간 행동의 카메라 임팩트, 히트스톱, 밀림/펄스와 배치 후 정지를 생략하고 마지막 행동에서 한 번 실행하도록 수정한다.
- [ ] 관련 테스트를 실행한다.

### Task 3: 애니메이션/VFX 및 시간 압축

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleWorldVfxHandle.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleConsecutiveActionPlanTests.cs`

**Interfaces:**
- Consumes: 그룹 속도 배율과 경계 정책
- Produces: 연출 시간 `duration / speedMultiplier`, 그룹 내 애니메이션/VFX 가속과 안전한 복원

- [ ] 속도 배율이 1보다 작지 않게 정규화되고 단독 행동은 1배인 실패 테스트를 작성한다.
- [ ] 실패를 확인한 뒤 최소 속도 정책을 구현한다.
- [ ] 그룹 행동의 행동/대상 애니메이션과 생성 VFX가 같은 배율을 사용하도록 연결한다.
- [ ] 그룹 마지막 외부 임팩트의 시간과 슬롯 UI 경계 전환 시간을 배율로 압축한다.
- [ ] 중단 경로에서도 속도와 카메라 보류가 복원되도록 `finally` 정리를 추가한다.
- [ ] 관련 테스트를 실행한다.

### Task 4: 전체 검증

**Files:**
- Verify: `Assets/Tests/EditMode~/BattleConsecutiveActionPlanTests.cs`
- Verify: Unity C# project files

- [ ] 새 EditMode 테스트를 실행해 그룹 판정과 정책을 검증한다.
- [ ] 기존 관련 EditMode 테스트를 실행한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] `git diff --check`와 변경 파일 목록을 확인한다.
- [ ] Unity 에디터 수동 검증 항목을 완료 보고에 명시한다.

