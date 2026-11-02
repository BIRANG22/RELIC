# Save Recovery, Battle Rendering, and Arabella Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 강제 종료 저장 불일치, 전투 렌더 순서 오류, 아라벨라 등장 Animator 충돌을 회귀 테스트와 함께 수정한다.

**Architecture:** 저장본은 원자적 파일 교체와 유효성 기반 복원 경계를 사용한다. 전투 표현은 논리적 발 위치 기반 정렬과 시각적 이동을 분리하며, 아라벨라 등장 클립은 `SpriteTransformSequence`만 재생한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity prefab/Animator YAML

**Spec:** `AI_Docs/2026-11-02_Save_Recovery_Battle_Rendering_Arabella_Design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 전투 결과 계산과 표현 계층을 분리한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 손상된 원본과 정상 백업이 함께 있을 때 백업을 복원하는지 검사한다.
- 이어하기 불가 저장본에서 로비 영구 데이터는 보존하고 전투 임시 데이터만 제거하는지 검사한다.
- 저장 교체 실패가 기존 정상 원본을 손상시키지 않는지 검사한다.
- 분열 연출 시작·중간·종료 프레임에서 정렬 기준이 목적지에 고정되는지 검사한다.
- 아라벨라 등장 프리팹이 Controller 없이 PlayableGraph 클립을 재생할 수 있는 구성을 유지하는지 검사한다.

---

### Task 1: 저장 파일 내구성과 복원 분류

**Files:**
- Modify: `Assets/Project/Scripts/Core/SaveSystem.cs`
- Test: `Assets/Tests/EditMode~/SaveSystemRecoveryTests.cs`

**Interfaces:**
- Produces: 저장본 분류/정리 함수와 원자적 저장·백업 읽기 경로

- [ ] 실패 테스트로 비유효 탐사 스냅샷의 전투 임시 상태 정리를 고정한다.
- [ ] 실패 테스트로 손상 원본에서 백업 읽기 동작을 고정한다.
- [ ] 최소 구현으로 로비 안전 스냅샷 정리와 원자적 파일 교체를 추가한다.
- [ ] 관련 테스트와 컴파일을 확인한다.

### Task 2: 논리 정렬 좌표와 Muck 분열 연출 분리

**Files:**
- Modify: `Assets/Project/Scripts/Core/YSortSprite.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleDeathService.cs`
- Test: `Assets/Tests/EditMode~/BattleRenderSortingTests.cs`

**Interfaces:**
- Produces: 일시적으로 고정 가능한 Y 정렬 기준과 분열 연출의 시각적 이동 경계

- [ ] 실패 테스트로 정렬 기준 Y override 동작을 고정한다.
- [ ] 최소 구현으로 override API를 추가한다.
- [ ] 분열 시작 시 목적지 Y를 고정하고 종료 시 해제하도록 구현한다.
- [ ] 관련 테스트와 컴파일을 확인한다.

### Task 3: 캐릭터·몬스터·그림자·효과 정렬 감사

**Files:**
- Modify: `Assets/Project/PrefabsR/Character/*/*_BattlePrefab.prefab`
- Modify: `Assets/Project/PrefabsR/Monster/**/*.prefab`
- Modify as required: grid-effect/VFX prefab YAML under `Assets/Project/PrefabsR/Battle`
- Test: `Assets/Tests/EditMode~/BattleRenderPrefabAuditTests.cs`

**Interfaces:**
- Consumes: Task 2의 발 위치 기반 정렬 규칙
- Produces: 프리팹 전수 감사 규칙

- [ ] 실패하는 YAML 감사 테스트로 전투 그림자의 Sorting Layer와 상대 order를 고정한다.
- [ ] 전투 프리팹의 그림자 설정을 최소 수정한다.
- [ ] 그리드 효과/VFX의 명백한 layer/order 이탈을 같은 감사에서 수정한다.
- [ ] 감사 테스트와 컴파일을 확인한다.

### Task 4: 아라벨라 등장 재생 주체 단일화

**Files:**
- Modify: `Assets/Project/PrefabsR/Battle/BackGround/St1_boss.prefab`
- Modify or remove if unreferenced: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Controller/Mon_B_01_appear.controller`
- Test: `Assets/Tests/EditMode~/ArabellaAppearPresentationTests.cs`

**Interfaces:**
- Produces: Controller 자동 전이 없이 `SpriteTransformSequence`만 등장 클립을 재생하는 구성

- [ ] 실패하는 YAML 테스트로 배경 appear Animator의 Controller 미참조를 고정한다.
- [ ] 프리팹 Controller 참조를 제거하고 단계별 flip/clip 구성을 검증한다.
- [ ] 관련 테스트와 컴파일을 확인한다.

### Task 5: 전체 회귀 검증

**Files:**
- Verify all modified files

- [ ] EditMode 테스트 소스를 프로젝트 테스트 경로에서 정적 검토한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] `git diff --check`와 변경 파일 목록을 확인한다.
- [ ] 에디터 화면에서만 확인 가능한 항목을 완료 보고의 미검증 항목으로 기록한다.
