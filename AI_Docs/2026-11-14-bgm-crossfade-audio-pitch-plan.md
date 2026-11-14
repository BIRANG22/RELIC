# BGM Crossfade and Audio Pitch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** BGM을 동시 크로스페이드하고 전투 배속에서 VFX 효과음 피치를 분리한다.

**Architecture:** `AudioManager`가 두 BGM 소스 뱅크를 번갈아 활성화하며 각 뱅크 음량을 독립적으로 보간한다. VFX 효과음은 데이터에 저장된 피치만 사용하고 연출 배속은 재생 피치에 반영하지 않는다.

**Tech Stack:** Unity 6, C#, AudioSource, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-14-bgm-crossfade-audio-pitch-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 하지 않는다.
- 기존 작업 공간과 변경을 보존한다.
- 커밋, Push, PR, 브랜치/worktree 작업을 하지 않는다.

## Review Focus

- 첫 BGM 재생은 페이드인만 하고 불필요한 빈 소스를 만들지 않아야 한다.
- 다층 BGM도 메인과 같은 진행률로 전환되어야 한다.
- 전환 중 새 요청은 이전 코루틴을 중단해도 음량이 갑자기 1로 튀지 않아야 한다.
- 페이드 시간이 0인 경우 즉시 새 뱅크만 재생해야 한다.
- 효과음의 개별 피치와 랜덤 피치 설정은 배속 분리 후에도 유지되어야 한다.

---

### Task 1: 전투 배속과 효과음 피치 분리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleVfxAudioUtility.cs`
- Modify: `Assets/Project/Scripts/Core/Managers/AudioManager.cs`
- Test: `Assets/Tests/EditMode~/SoundDatabaseVfxOwnershipTests.cs`

**Interfaces:**
- Produces: 배속과 무관한 VFX cue 재생 피치

- [ ] 배속이 1, 2, 4여도 cue 피치가 동일한 실패 테스트를 작성한다.
- [ ] 구현 전 테스트 실패를 확인한다.
- [ ] VFX 재생 경로에서 연출 배속 피치 배율을 제거한다.
- [ ] 테스트 컴파일과 프로젝트 빌드를 확인한다.

### Task 2: 이중 BGM 뱅크 크로스페이드

**Files:**
- Modify: `Assets/Project/Scripts/Core/Managers/AudioManager.cs`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`

**Interfaces:**
- Produces: 독립적인 outgoing/incoming fade 진행률 정책과 두 뱅크 전환

- [ ] 두 페이드가 같은 경과 시간에서 동시에 진행되는 정책 테스트를 작성한다.
- [ ] 구현 전 실패를 확인한다.
- [ ] 메인·레이어 AudioSource를 포함하는 두 뱅크와 동시 보간 루틴을 구현한다.
- [ ] 전환 완료 시 이전 뱅크만 정지하고 새 뱅크를 활성 뱅크로 승격한다.
- [ ] 중간 요청과 0초 전환 경계를 검토하고 테스트 컴파일을 확인한다.

### Task 3: 최종 검증

**Files:**
- Verify all files above

- [ ] `git diff --check`를 실행한다.
- [ ] `Assembly-CSharp.csproj`와 요청 테스트 포함 `Assembly-CSharp-Editor.csproj` 빌드를 실행한다.
- [ ] Unity 에디터 청감 검증 미수행 사항을 완료 보고에 기록한다.
