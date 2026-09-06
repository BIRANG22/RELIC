# Initial Default Party Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 최초 데이터 준비 시 빈 파티에 `Char_01`, `Char_02`, `Char_03`을 기본 편성한다.

**Architecture:** `InitialDefaultPartySetup`이 마스터 데이터 검증, 캐릭터 런타임 생성, 파티 슬롯 배치를 담당한다. `Bootstrap`은 저장 로드 이후 서비스를 호출하며 기존 파티가 있으면 아무것도 변경하지 않는다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에 작성한다.
- 파티 상태는 Scene Object가 아닌 `CharacterId`와 인덱스로 저장한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR은 진행하지 않는다.

---

### Task 1: 빈 파티 기본 구성 서비스

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Runtime/InitialDefaultPartySetup.cs`
- Test: `Assets/Tests/EditMode~/InitialDefaultPartySetupTests.cs`

**Interfaces:**
- Consumes: `CharacterDatabase`, `CharacterRuntimeStore`, `PartyRuntimeStore`, `RelicDatabase`
- Produces: `InitialDefaultPartySetup.TryInitialize(...)`

- [x] **Step 1: 실패 테스트 작성**
  - 빈 파티 기본 구성, 기존 파티 보존, 필수 마스터 데이터 누락 시 원자적 실패를 검증한다.
- [x] **Step 2: RED 확인**
  - 테스트 소스를 컴파일해 `InitialDefaultPartySetup` 타입 부재로 실패하는지 확인한다.
- [x] **Step 3: 최소 구현**
  - `Char_01~03` 마스터를 사전 검증한 후 누락 런타임과 슬롯 0~2, 그리드 6~8을 생성한다.
- [x] **Step 4: GREEN 확인**
  - 테스트 소스 컴파일과 직접 호출 가능한 순수 로직 검증을 통과시킨다.

### Task 2: Bootstrap 연결

**Files:**
- Modify: `Assets/Project/Scripts/Core/Bootstrap.cs`

**Interfaces:**
- Consumes: `InitialDefaultPartySetup.TryInitialize(DataManager)`
- Produces: 저장 로드 이후 최초 기본 파티 초기화

- [x] **Step 1: 최소 연결**
  - `SaveSystem.Instance.TryLoadProgress()` 직후 초기화 서비스를 호출한다.
- [x] **Step 2: 전체 검증**
  - 런타임/Editor 어셈블리 빌드, 테스트 소스 컴파일, `git diff --check`를 실행한다.
