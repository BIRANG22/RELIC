# HardModeSlot 사각 외곽 글로우 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 슬롯 뒤에서도 네 방향으로 보이는 큰 사각형 붉은 글로우를 적용한다.

**Architecture:** 전용 투명 PNG를 Sprite로 추가하고 기존 `ExplorationResultHardModeBackdropBlur.prefab`의 Sprite와 크기만 교체한다. 생성 및 정렬 코드는 변경하지 않는다.

**Tech Stack:** Unity 6, UGUI Image, PNG Sprite, NUnit EditMode asset tests

**Spec:** `AI_Docs/2026-10-25-hard-mode-square-backdrop-glow-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 기존 내부 원형 블러와 외곽 블러 배치 로직은 유지한다.
- 커밋, Push, PR, 브랜치 및 worktree 조작은 하지 않는다.

## Review Focus

- Sprite가 원형 글로우 GUID를 계속 사용하지 않는지 확인한다.
- 프리팹 크기가 슬롯보다 충분히 큰 `160 x 160`인지 확인한다.
- 투명 배경과 부드러운 사각 감쇠가 유지되는지 확인한다.
- `raycastTarget = false`가 유지되는지 확인한다.
- 기존 배치·활성 상태 동기화 코드가 변경되지 않는지 확인한다.

---

### Task 1: 사각 글로우 Sprite 및 프리팹 교체

**Files:**
- Create: `Assets/Project/Art/Image/UI/Result/HardModeSlotSquareGlow.png`
- Create: `Assets/Project/Art/Image/UI/Result/HardModeSlotSquareGlow.png.meta`
- Modify: `Assets/Project/PrefabsR/ExplorationResultHardModeBackdropBlur.prefab`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 기존 `hardModeBackdropBlurPrefab` 바인딩 및 위치·정렬 로직
- Produces: `160 x 160` 사각 외곽 글로우 프리팹

- [ ] **Step 1:** 새 Sprite GUID, `160 x 160`, 입력 비활성화를 요구하는 실패 테스트를 작성한다.
- [ ] **Step 2:** 기존 원형 Sprite와 `150 x 150` 크기 때문에 테스트가 실패함을 확인한다.
- [ ] **Step 3:** 투명 사각 글로우 PNG와 Sprite 메타를 추가하고 프리팹을 교체한다.
- [ ] **Step 4:** 구조 검증, 테스트 포함 Editor 컴파일, `git diff --check`를 실행한다.
- [ ] **Step 5:** 사용자 승인 전 커밋하지 않는다.
