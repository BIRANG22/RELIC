# 전투 버그 5건 수정 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 이동 UI, 모르트 이동 범위, 거미 알 HP 표시, 출혈 이동 트리거, 아라벨라 등장 카메라 복귀 문제를 수정한다.

**Architecture:** 전투 상태 변경은 기존 런타임 데이터와 실행 결과 경로에서 처리하고 UI는 결과만 표시한다. 순수 계산 규칙은 테스트 가능한 정적 메서드로 분리하며, 카메라와 HP UI 변경은 연출 계층에 한정한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** 사용자 승인 대화(2026-11-10)

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트는 하지 않는다.
- 전투 상태와 UI/VFX/카메라 연출을 분리한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 하지 않는다.

## Review Focus

- 이동 수치 펄스가 중단·재시작될 때 기준 스케일로 복구되는지 확인한다.
- 모르트 이동 후보가 `Range_18`의 8방향 1칸만 허용하는지 확인한다.
- 알 피격 HP 표시가 일정 시간 뒤 호버 정책으로 복귀하는지 확인한다.
- 여러 칸 이동 중 출혈이 칸마다 피해를 주고 1씩 감소하며 0에서 멈추는지 확인한다.
- 아라벨라 시퀀스 종료 시 추가 카메라 보간이 실행되지 않는지 확인한다.

---

### Task 1: 이동 수치 펄스 복구

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineOrderValuePulseTests.cs`

- [ ] 실패 테스트로 펄스의 종료/중단 시 최종 배율이 1인지 검증한다.
- [ ] 펄스 배율 계산과 복구 경로를 최소 수정한다.
- [ ] EditMode 테스트와 컴파일을 검증한다.

### Task 2: 모르트 이동 범위 통일

**Files:**
- Modify: `Assets/Project/Scripts/MortAI.cs`
- Test: `Assets/Tests/EditMode~/MortMoveRangeTests.cs`

- [ ] 실패 테스트로 허용 오프셋이 8방향 1칸이고 십자 2칸이 제외됨을 검증한다.
- [ ] `S_Monster_29`의 `Range_18`과 동일한 이동 후보 규칙을 사용한다.
- [ ] EditMode 테스트와 컴파일을 검증한다.

### Task 3: 거미 알 피격 HP 표시

**Files:**
- Modify: `Assets/Project/Scripts/GridEffectHpUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/BattleGridEffectController.cs`
- Test: `Assets/Tests/EditMode~/GridEffectHpVisibilityTests.cs`

- [ ] 실패 테스트로 피격 직후 강제 표시와 시간 종료 후 호버 복귀 규칙을 검증한다.
- [ ] 컨트롤러가 피해 결과를 HP UI에 통지하고 UI가 표시 시간을 관리하게 한다.
- [ ] EditMode 테스트와 컴파일을 검증한다.

### Task 4: 이동 칸별 출혈 처리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/BleedMoveTriggerTests.cs`

- [ ] 실패 테스트로 출혈 3 상태에서 3칸 이동 시 피해 3+2+1, 최종 출혈 0을 검증한다.
- [ ] 실제 이동이 성립한 각 칸에서만 피해와 1스택 차감을 수행한다.
- [ ] 사망 또는 출혈 0이면 이후 트리거를 중단한다.
- [ ] EditMode 테스트와 컴파일을 검증한다.

### Task 5: 아라벨라 등장 후 추가 카메라 이동 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaSpawnPresentation.cs`
- Test: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

- [ ] 실패 테스트로 명시적 복귀 뒤 시퀀스 종료가 추가 복귀 보간을 요구하지 않음을 검증한다.
- [ ] 종료 시 현재 카메라 포즈를 유지하면서 시네마틱 소유권만 반환한다.
- [ ] EditMode 테스트와 컴파일을 검증한다.

### Task 6: 전체 검증

**Files:**
- Verify all modified files and tests.

- [ ] 관련 EditMode 테스트 어셈블리를 일반 .NET 테스트 러너로 실행 가능한 범위에서 검증한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 씬·프리팹 YAML 참조와 git diff를 점검한다.
