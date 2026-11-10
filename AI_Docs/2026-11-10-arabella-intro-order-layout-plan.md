# Arabella Intro Order And Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어와 보스를 함께 생성한 뒤 아라벨라 등장 애니메이션과 대사, 보스 이름 연출을 순차 재생하며 대사 박스를 머리 위 최상단에 표시한다.

**Architecture:** `BattleSceneController`가 스폰 Presenter와 기존 이름 연출의 순서를 조정한다. 씬 데이터는 이름 연출 지연만 제거하고, 프리팹 데이터는 대사 Canvas 위치와 정렬만 담당한다.

**Tech Stack:** Unity 6, C#, Animator/TMP/Canvas YAML, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-10-arabella-intro-order-layout-design.md`

## Global Constraints

- 문서와 테스트 위치는 AGENTS.md 규칙을 따른다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 수행하지 않는다.
- 현재 작업공간에서 작업하며 커밋, Push, PR, 브랜치, worktree 작업을 하지 않는다.
- UI와 애니메이션은 전투 결과를 계산하지 않는다.

## Review Focus

- Presenter 누락 시에도 이름 연출과 게이트 종료가 진행되는가.
- 이름 연출 누락 시에도 전투가 영구 대기하지 않는가.
- 이어하기에서 두 연출이 반복되지 않는가.
- Battle과 DebugBattle의 이름 연출 시간이 동일한가.
- Canvas가 Unit 레이어에서 머리 위에 렌더링되는가.

---

### Task 1: 호출 순서 회귀 테스트 및 인트로 재구성

**Files:**
- Modify: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`

**Interfaces:**
- Consumes: `CompleteDeferredMonsterSpawn()`, `ArabellaSpawnPresentation.Play()`, `PlayCurrentBossReveal()`
- Produces: `Spawn → Presenter → BossReveal → Gate release` 순서

- [x] 실패하는 호출 순서 테스트 작성
- [x] 현재 구현에서 RED 확인
- [x] 인트로 코루틴을 승인된 순서로 최소 수정
- [x] 테스트 계약과 런타임 컴파일 확인

### Task 1-1: 보스와 파티 동시 생성

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: 보스방 로드 시 `deferMonsterSpawn=false`, 튜토리얼 전용 지연 정책 유지

- [x] 실패하는 보스 스폰 지연 조건 테스트 작성
- [x] 현재 구현에서 RED 확인
- [x] 보스 인트로 조건을 몬스터 스폰 지연 조건에서 제거
- [x] 초기 행동 계획이 인트로 게이트를 계속 기다리는지 정적 검증

### Task 2: 빈 시간 제거 및 대사 Canvas 배치

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab`
- Modify: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: reveal time 0/1초, Canvas local Y 16, Unit sorting layer/order 300, Appear01 첫 프레임 준비

- [x] 실패하는 씬·프리팹 YAML 계약 테스트 작성
- [x] 현재 에셋에서 RED 확인
- [x] 두 씬과 프리팹 값을 수정
- [x] YAML 계약 검증

### Task 3: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

- [x] `git diff --check` 실행
- [x] 런타임 어셈블리 재빌드
- [x] 새 테스트 포함 에디터 어셈블리 재빌드
- [x] Unity 실행 화면 미검증 항목 기록

### Task 4: 인스펙터 연출 조정값

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaSpawnPresentation.cs`
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab`
- Modify: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: 대사 위치·크기·시간, 애니메이션 배속, 카메라 오프셋·줌·블렌드 시간 인스펙터 설정

- [x] 실패하는 직렬화 필드 및 카메라 계약 테스트 작성
- [x] 현재 구현에서 RED 확인
- [x] Presenter 연출 설정과 안전한 카메라 복원 구현
- [x] 프리팹 기본값 연결 및 컴파일 검증

### Task 5: 취소 정리 및 카메라 소유권 보호

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaSpawnPresentation.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: 인트로 취소 시 대사·애니메이터·카메라 즉시 정리, 기존 cinematic 연출이 있을 때 카메라 제어권 획득 거부

- [x] 취소 정리와 카메라 독점 획득 회귀 테스트 작성
- [x] 현재 구현의 계약 미충족 확인
- [x] Presenter 명시적 취소 API와 Controller 취소 순서 구현
- [x] cinematic 카메라 독점 획득 API 구현
