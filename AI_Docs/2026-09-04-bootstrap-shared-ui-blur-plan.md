# Bootstrap Shared UI Blur Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bootstrap 단일 Shared Blur UI와 URP Color 복사 Feature로 UI 블러 캡처 시스템을 교체한다.

**Architecture:** Renderer Feature가 월드 Color를 전역 RTHandle로 제공하고, Bootstrap 공용 매니저가 한 Canvas와 Material을 관리한다. 패널 컴포넌트는 Request/Release만 하며 기존 캡처 경로는 삭제한다.

**Tech Stack:** Unity 6, URP 17, 2D Renderer, Unity UI, NUnit EditMode.

**Spec:** `AI_Docs/2026-09-04-bootstrap-shared-ui-blur-design.md`

## Global Constraints

- `Camera.Render`, ScreenCapture, 패널별 RenderTexture·Canvas·Material 생성 금지.
- Feature는 PC Renderer와 2D Renderer Data에 모두 연결한다.
- 공용 Canvas는 Bootstrap에서 하나만 유지하고 Popup보다 아래에서 렌더한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.

### Task 1: Renderer Color Source

**Files:** Create `Assets/Project/Scripts/Rendering/UIBackgroundBlurRendererFeature.cs`; modify renderer data assets.

- [ ] Feature와 RTHandle lifecycle을 구현한다.
- [ ] Game/Scene 카메라의 Color를 `_UIBlurSourceTexture`로 복사한다.
- [ ] 두 Renderer Data에 Feature를 연결한다.
- [ ] 해상도 변경과 Dispose를 검증한다.

### Task 2: Bootstrap Shared Blur UI

**Files:** Create `Assets/Project/Scripts/UIBlurBackgroundManager.cs`, `Assets/Project/PrefabsR/SharedBlurRoot.prefab`; modify Lobby/Battle Bootstrap references.

- [ ] requester HashSet과 자동 정리, sceneLoaded UI Camera 재연결을 구현한다.
- [ ] SharedBlurCanvas/Background와 Material을 하나만 생성·관리한다.
- [ ] 중복 요청/해제 EditMode 테스트를 작성하고 통과시킨다.

### Task 3: Panel API 전환과 캡처 제거

**Files:** Modify `UIBlurBackground.cs`, `DustiumBackgroundBlur.shader`, panel callers and tests; delete capture manager/include files and obsolete tests.

- [ ] 기존 blur 설정과 `EnsureForPanel`을 유지하며 Request/Release로 전환한다.
- [ ] 셰이더가 `_UIBlurSourceTexture`를 9-sample blur로 읽게 한다.
- [ ] 캡처 전용 API/필드/런타임 생성 및 사용처를 제거한다.
- [ ] 전체 검색, 컴파일, Unity Editor 수동 시나리오를 검증한다.
