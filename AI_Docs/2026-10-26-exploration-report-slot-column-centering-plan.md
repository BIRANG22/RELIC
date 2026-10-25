# ExplorationReport Slot Column Centering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 유물 및 기억 슬롯 열을 각 슬롯 루트의 가로 중앙에 정렬한다.

**Architecture:** `ExplorationResultCharacterRowUI`에 순수 중앙 오프셋 계산 함수를 추가하고 `BindIconSlots`가 해당 오프셋을 슬롯 x 좌표에 더한다. 인스펙터의 슬롯 수·열 수·간격·스케일 설정은 그대로 소비한다.

**Tech Stack:** Unity 6, C#, Unity UI RectTransform, NUnit EditMode

**Spec:** `AI_Docs/2026-10-26-exploration-report-slot-column-centering-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 직접 제어 및 batchmode 테스트를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 루트가 슬롯 열보다 넓을 때 정확히 중앙정렬되는지 확인한다.
- 슬롯 열이 루트보다 넓을 때 음수 오프셋을 만들지 않는지 확인한다.
- 단일 열과 0개 입력에서 안전한지 확인한다.
- 유물 6열과 기억 4열 설정 모두 같은 계산을 사용하는지 확인한다.
- 세로 행 계산과 슬롯 데이터 바인딩이 바뀌지 않는지 확인한다.

---

### Task 1: 슬롯 열 중앙 오프셋 적용

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 루트 폭, 최대 슬롯 수, 열 수, 가로 간격, 슬롯 실제 표시 폭
- Produces: `CalculateCenteredSlotStartX(float rootWidth, int maxCount, int columns, float horizontalSpacing, float slotVisualWidth) -> float`

- [x] **Step 1: 중앙 오프셋 실패 테스트 작성**
- [x] **Step 2: 구현 전 실패 확인**
- [x] **Step 3: 계산 함수 및 BindIconSlots 적용**
- [x] **Step 4: 구조 검사와 Editor 어셈블리 빌드**
- [x] **Step 5: 변경 범위 검토**
