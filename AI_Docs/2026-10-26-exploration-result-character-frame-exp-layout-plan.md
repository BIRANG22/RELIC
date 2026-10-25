# ExplorationResultPanel Character Frame Experience Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 세 캐릭터의 레벨 텍스트와 경험치 바를 캐릭터 정보 폭에 맞추고 여유 있는 프레임 안에 포함한다.

**Architecture:** `Battle.unity`에서 사용자가 직접 조정한 `ExplorationReportExpSlider_2`의 Fill과 LevelUp RectTransform을 기준값으로 삼아 `_0`, `_1`에 동일하게 복제한다. Row/RowFrame의 기존 확장값과 다른 자식의 배치는 유지한다.

**Tech Stack:** Unity 6, Unity UI, YAML 씬 직렬화, NUnit EditMode 테스트

**Spec:** `AI_Docs/2026-10-26-exploration-result-character-frame-exp-layout-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 레벨 텍스트와 경험치 바의 합성 폭이 기존 캐릭터 정보 폭과 크게 달라지지 않아야 한다.
- 세 프레임이 서로 겹치지 않아야 한다.
- 레벨 텍스트와 경험치 수치가 축소된 경험치 바 영역에서 잘리지 않아야 한다.
- 프레임은 위·아래·좌·우에 충분한 여백을 남겨야 한다.
- 세 Row에 동일한 값이 적용되어야 한다.

---

### Task 1: 경험치 영역 정돈 및 프레임 확장

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 기존 Row 3개, 경험치 슬라이더, `LevelUp`, `ExperienceText`, Row 프레임
- Produces: `_2`와 동일한 Fill 길이와 레벨 위치를 사용하는 세 경험치 영역

- [x] **Step 1: 실패하는 배치 테스트 작성**

세 Row 자체, 슬라이더, 레벨 텍스트, 경험치 텍스트 및 프레임이 설계의 정확한 좌표와 크기를 사용하는지 검증한다. 경험치 이외 직접 자식은 보정된 RectTransform 값으로 기존 화면 위치가 유지되는지 검증한다.

- [x] **Step 2: 테스트 실패 확인**

기존 값이 새 좌표 및 크기와 달라 구조 검증이 실패하는지 확인한다.

- [x] **Step 3: Battle.unity RectTransform 수정**

설계 문서에 고정한 값을 세 Row에 동일하게 적용한다.

- [x] **Step 4: 구조와 컴파일 검증**

구조 검사, 에디터 테스트 어셈블리 빌드, `git diff --check`를 수행한다. Unity batchmode는 사용하지 않는다.

- [x] **Step 5: 변경 범위 검토**

인접 프레임 간격과 프레임 내부 여백을 수치로 재확인하고 요청 범위 밖 변경이 없는지 검토한다.
