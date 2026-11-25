# Monster Move Timeline Icon Rotation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터 이동 예약 아이콘을 예약된 8방향 이동 벡터에 맞춰 회전한다.

**Architecture:** `BattleTimelinePreviewEntry`가 예약 명령에서 표시 회전각을 계산하고, `BattleTimelineGroupUI`가 재사용 Image를 바인딩할 때 각도를 적용한다. 전투 상태나 이동 실행 로직은 변경하지 않는다.

**Tech Stack:** Unity, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-25-monster-move-timeline-icon-rotation-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- UI는 전투 결과를 계산하지 않고 예약 결과만 표현한다.

## Review Focus

- 8방향 각각이 기존 이동 경로 UI와 같은 시계 방향 각도를 반환하는지 테스트한다.
- 크기가 1보다 큰 이동 벡터도 방향만 정규화되는지 테스트한다.
- 이동이 아닌 몬스터 행동은 0도를 반환하는지 테스트한다.
- 이동 행동의 오프셋이 0이면 0도를 반환하는지 테스트한다.
- 재사용된 이미지가 비이동 행동에서 0도로 초기화되는지 코드 경로와 컴파일로 확인한다.

---

### Task 1: 이동 아이콘 회전 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`
- Create: `Assets/Tests/EditMode~/MonsterMoveTimelineIconRotationTests.cs`

**Interfaces:**
- Consumes: `MonsterReservedCommand.MoveOffset`, `MonsterSkillData.TimelineNotation`
- Produces: `BattleTimelinePreviewEntry.SkillIconRotationZ : float`

- [ ] **Step 1: 실패 테스트 작성**
  - 8방향, 장거리 벡터 정규화, 비이동, zero offset의 예상 각도를 검증한다.
- [ ] **Step 2: 테스트 컴파일 실패 확인**
  - 프로젝트 테스트 어셈블리 빌드에서 새 속성이 없음을 확인한다.
- [ ] **Step 3: 최소 구현**
  - `SkillIconRotationZ`와 이동 벡터를 8방향 각도로 변환하는 순수 계산을 추가한다.
- [ ] **Step 4: 테스트 어셈블리 컴파일 확인**
  - Unity가 생성한 테스트 프로젝트가 있으면 MSBuild로 컴파일한다.

### Task 2: 타임라인 이미지 회전 적용

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`

**Interfaces:**
- Consumes: `BattleTimelinePreviewEntry.SkillIconRotationZ`
- Produces: 스킬 이미지 바인딩 시 항상 갱신되는 `RectTransform.localEulerAngles.z`

- [ ] **Step 1: 이미지 바인딩에 회전 적용**
  - `SetSkillImage` 직후 엔트리 회전각을 적용하고 비이동 엔트리는 0도로 덮어쓴다.
- [ ] **Step 2: 런타임 및 Editor 어셈블리 컴파일**
  - `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`를 빌드해 오류가 없는지 확인한다.
- [ ] **Step 3: 변경 범위 검토**
  - 전투 상태 변경 없이 UI 표현 파일과 테스트·문서만 변경됐는지 확인한다.
