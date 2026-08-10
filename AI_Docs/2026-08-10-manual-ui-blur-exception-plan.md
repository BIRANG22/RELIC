# Manual UI Blur Exception Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 블러 패널 캡처에서 모든 UI를 기본 제외하고, `UIBlurBackground` 인스펙터에 직접 연결한 UI만 블러에 포함한다.

**Architecture:** `UIBlurBackground`가 `blurredUiRoots` 배열을 소유하고 캡처 매니저에 전달한다. `UIBlurBackgroundCaptureManager`는 씬 전체 `UIBlurInclude` 자동 검색을 사용하지 않고, 전달받은 루트만 캡처 예외로 처리한다. 블러 패널 표시 중에는 예외 원본 UI를 숨겨 흐릿한 복사본과 선명한 원본이 겹치지 않게 한다.

**Tech Stack:** Unity 6, Unity UI, TextMesh Pro, C# EditMode tests, YAML scene cleanup

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치, worktree 작업은 수행하지 않는다.
- 전투 결과와 멀티플레이 동기화 데이터는 변경하지 않는다.

---

### Task 1: Inspector-driven blur target API

**Files:**
- Modify: `Assets/Project/Scripts/UIBlurBackground.cs`
- Modify: `Assets/Project/Scripts/UIBlurBackgroundCaptureManager.cs`
- Test: `Assets/Tests/EditMode~/UIBlurManualExceptionTests.cs`

**Interfaces:**
- Produces: `UIBlurBackground.BlurredUiRoots`
- Produces: `UIBlurBackgroundCaptureManager.GetValidBlurredUiRoots(IEnumerable<GameObject> roots)`
- Produces: `UIBlurBackgroundCaptureManager.CaptureBackgroundNow(IReadOnlyList<GameObject> blurredUiRoots)`

- [x] Write failing tests for null/duplicate filtering and `UIBlurBackground` serialized target exposure.
- [x] Run targeted test compile and confirm failure because the new API does not exist.
- [x] Add `blurredUiRoots` to `UIBlurBackground`.
- [x] Add `GetValidBlurredUiRoots` and overloaded `CaptureBackgroundNow` to the capture manager.
- [x] Run targeted test compile and confirm success.

### Task 2: Remove automatic marker capture flow

**Files:**
- Modify: `Assets/Project/Scripts/UIBlurBackground.cs`
- Modify: `Assets/Project/Scripts/UIBlurBackgroundCaptureManager.cs`

**Interfaces:**
- Consumes: `UIBlurBackgroundCaptureManager.CaptureBackgroundNow(IReadOnlyList<GameObject> blurredUiRoots)`
- Removes runtime dependency on scene-wide `UIBlurInclude` discovery.

- [x] Replace parameterless capture call in `UIBlurBackground` with explicit target call.
- [x] Remove `BeginBlurPresentation` and `EndBlurPresentation` calls from `UIBlurBackground`.
- [x] Remove automatic `UIBlurInclude` search and hidden-marker lists from `UIBlurBackgroundCaptureManager`.
- [x] Keep `RegisterBlurPanel`, `UnregisterBlurPanel`, `BeginBlurPresentation`, and `EndBlurPresentation` as compatibility no-ops if external code still calls them.

### Task 3: UI visibility isolation during capture and presentation

**Files:**
- Modify: `Assets/Project/Scripts/UIBlurBackground.cs`
- Modify: `Assets/Project/Scripts/UIBlurBackgroundCaptureManager.cs`
- Test: `Assets/Tests/EditMode~/UIBlurManualExceptionTests.cs`

**Interfaces:**
- Produces: `UIBlurBackgroundCaptureManager.IsTransformUnderAnyRoot(Transform target, IReadOnlyList<GameObject> roots)`

- [x] Write failing tests for root/child containment.
- [x] Hide active UI `CanvasRenderer` objects during capture except explicit roots.
- [x] Temporarily restore explicit roots for capture.
- [x] Hide explicit source roots while the blur panel is visible.
- [x] Restore explicit source roots when the blur panel closes.

### Task 4: Lobby scene cleanup

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/UIBlurManualExceptionTests.cs`

**Interfaces:**
- Verifies: Lobby scene no longer contains `UIBlurInclude` component references.

- [x] Write a failing scene YAML test that asserts the lobby scene does not contain the `UIBlurInclude` script guid.
- [x] Remove the existing `UIBlurInclude` component from `CharacterSetting`.
- [x] Run the scene YAML test compile.

### Task 5: Final verification

**Files:**
- Check: changed source, scene, tests, docs

- [x] Run MSBuild compile.
- [x] Compile targeted tests with Unity reference response files.
- [x] Search for old automatic marker usage in source and lobby scene.
- [x] Review `git diff`.
- [x] Report changed files, implementation, verification, unverified items, multiplayer impact, and commit/Push/PR status.
