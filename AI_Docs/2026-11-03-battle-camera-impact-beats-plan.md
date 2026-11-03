# Battle Camera Impact Beats Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 모든 피해 명중에 압축, 고정, 방향성 반동, 오버슈트 복귀 비트를 적용한다.

**Architecture:** `BattleHitImpactFeedback`가 이미 계산하는 안정적인 수평 방향을 카메라 연출에 전달하고, `BattleCameraController`가 결과를 계산하지 않는 순수 연출 비트를 재생한다. 근접은 방향성 XY 비트를, 원거리는 기존의 약한 Z 비트를 유지한다.

**Tech Stack:** Unity 6, C#, Coroutine, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-03-battle-camera-impact-beats-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- Unity batchmode 테스트를 실행하지 않는다.
- 전투 결과와 카메라 연출을 분리하고, 방향은 기존 커맨드/타겟에서 얻은 안정적인 값만 사용한다.
- 커밋, Push, PR, 브랜치/worktree 작업을 수행하지 않는다.

## Review Focus

- 방향 값이 `0` 또는 범위 밖이어도 반동 방향이 안정적으로 정규화되는가.
- 음수로 직렬화된 강도가 반대 방향 연출을 만들지 않는가.
- 카메라가 중간에 이동해도 임팩트 오프셋 제거가 기준 위치를 망가뜨리지 않는가.
- 히트스톱 중단/비활성화 시 Time Scale과 VFX pause가 복구되는가.
- 원거리 공격의 기존 약한 Z 임팩트가 근접 방향성 킥으로 바뀌지 않는가.

---

### Task 1: 방향성 임팩트 비트 계산

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraImpactBeatTests.cs`

**Interfaces:**
- Consumes: 수평 방향 `int`, 압축/반동/오버슈트 강도 `float`
- Produces: `NormalizeImpactDirection(int)`, `CalculateImpactBeatOffset(int, float)`

- [ ] 방향 정규화와 오프셋 부호/클램프를 검증하는 실패 테스트를 작성한다.
- [ ] 테스트 어셈블리 컴파일에서 새 API 부재로 실패함을 확인한다.
- [ ] 순수 계산 API와 인스펙터 비트 설정값을 최소 구현한다.
- [ ] 테스트 어셈블리 컴파일을 다시 확인한다.

### Task 2: 근접 충돌 비트 재생과 방향 전달

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraImpactBeatTests.cs`

**Interfaces:**
- Consumes: `PlayDamageImpact(int horizontalDirection)`
- Produces: 압축 -> 기존 Impact Lock -> 반동 -> 오버슈트 복귀 시퀀스

- [ ] 좌우 방향에서 압축과 반동이 서로 반대 부호임을 검증한다.
- [ ] `BattleHitImpactFeedback`가 `ResolveHorizontalDirection` 결과를 카메라에 전달한다.
- [ ] 기존 무인자 API는 기본 방향을 사용하는 호환 오버로드로 유지한다.
- [ ] 원거리 API는 기존 방향 비적용 동작을 유지한다.

### Task 3: 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`

**Interfaces:**
- Consumes: Task 1~2의 코드와 테스트
- Produces: 컴파일 결과 및 수동 검증 잔여 항목

- [ ] 런타임 프로젝트를 MSBuild로 컴파일한다.
- [ ] 에디터/테스트 프로젝트를 MSBuild로 컴파일한다.
- [ ] 변경 diff와 멀티플레이 경계를 점검한다.
