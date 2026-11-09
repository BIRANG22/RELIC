# SoundDatabase VFX Direct Reference Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 프로젝트 소유 실사용 VFX의 직접 AudioClip 참조를 제거하고 SoundDatabase에서만 VFX 사운드를 관리한다.

**Architecture:** `SoundDatabase`에 그리드 효과 전용 VFX SFX 목록을 추가하되, 기존 Player/Monster 목록과 동일한 프리팹 키 딕셔너리에 등록한다. 프리팹 내 AudioSource는 모두 제거하고 `BattleVfxAudioUtility`가 DB 큐만 재생하도록 유지한다.

**Tech Stack:** Unity, C#, ScriptableObject YAML, NUnit EditMode

**Spec:** `AI_Docs/2026-11-09-sound-database-vfx-direct-reference-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 사용하지 않는다.
- 외부 다운로드 원본과 아트 테스트 씬은 수정하지 않는다.

## Review Focus

- 세 VFX 분류 모두 초기화 후 같은 프리팹 조회 API로 검색되어야 한다.
- DB 목록 간 중복 프리팹의 기존 선등록 우선 동작이 유지되어야 한다.
- 13개 대상 프리팹에서 `AudioSource`와 오디오 GUID가 모두 제거되어야 한다.
- 미등록 3개의 클립과 기본 재생 설정이 DB 큐에 정확히 이전되어야 한다.
- DB 미등록 프리팹이 내장 오디오로 우회 재생되지 않아야 한다.

---

### Task 1: 회귀 테스트 추가

**Files:**
- Create: `Assets/Tests/EditMode~/SoundDatabaseVfxOwnershipTests.cs`

- [x] 세 분류의 DB 조회와 대상 프리팹 직접 참조 금지를 검사하는 실패 테스트를 작성한다.
- [x] 현재 데이터에서 테스트가 실패하는 이유를 확인한다.

### Task 2: SoundDatabase 분류 및 데이터 이전

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/SoundDatabase.cs`
- Modify: `Assets/DB/SoundDatabase.asset`

- [x] `gridEffectVfxSfxList` 직렬화 목록과 읽기 전용 프로퍼티를 추가한다.
- [x] 초기화 시 새 목록을 기존 VFX 프리팹 조회 딕셔너리에 등록한다.
- [x] 그리드 효과 2개와 몬스터 준비 VFX 1개의 큐를 추가한다.

### Task 3: 프리팹 직접 참조 제거

**Files:**
- Modify: 조사된 캐릭터 VFX 10개, 그리드 VFX 2개, 몬스터 VFX 1개

- [x] 각 프리팹의 GameObject 컴포넌트 목록에서 AudioSource 참조를 제거한다.
- [x] 각 프리팹의 AudioSource YAML 블록을 제거한다.
- [x] 오디오 GUID 전수 검색으로 대상 프리팹의 직접 참조가 0건인지 확인한다.

### Task 4: 검증

- [x] EditMode 테스트 어셈블리를 컴파일한다.
- [x] 런타임 어셈블리를 컴파일한다.
- [x] `git diff --check`와 대상 GUID/AudioSource 정적 검사를 수행한다.
- [x] Unity 에디터 화면 및 실제 청음은 미검증 항목으로 보고한다.
