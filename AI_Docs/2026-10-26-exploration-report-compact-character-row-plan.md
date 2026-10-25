# ExplorationReport Compact Character Row Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 정보 아이콘을 캐릭터 초상 좌하단에 고정하고 세 캐릭터 결과 행 높이를 310으로 압축한다.

**Architecture:** `Battle.unity`에서 각 정보 아이콘의 부모를 대응 Portrait Viewport로 변경하고 좌하단 앵커를 적용한다. Row의 `VerticalLayoutGroup`에서 아이콘 행이 제거된 뒤 Row 높이만 310으로 줄이며 기존 UI 참조와 자식 크기는 유지한다.

**Tech Stack:** Unity 6, Unity UI, YAML 씬 직렬화, NUnit EditMode 테스트

**Spec:** `AI_Docs/2026-10-26-exploration-report-compact-character-row-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 BatchMode 테스트를 수행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 세 아이콘이 각각 올바른 Portrait Viewport의 자식인지 검증한다.
- 아이콘의 좌하단 앵커, 6px 여백, 40x40 크기가 세 행에서 동일한지 검증한다.
- 세 Row 높이가 모두 310인지 검증한다.
- 기존 정보 아이콘과 Hover/Owner 참조가 유지되는지 검증한다.
- Row 프레임의 Stretch 설정과 다른 콘텐츠 크기가 변경되지 않았는지 검증한다.

---

### Task 1: 캐릭터 행 계층 및 높이 압축

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 기존 정보 아이콘 직렬화 참조와 Portrait Viewport/Row RectTransform
- Produces: Portrait 내부 좌하단 정보 아이콘과 높이 310의 캐릭터 행

- [ ] **Step 1: 실패하는 씬 구조 테스트 작성**

`CharacterReport_PlacesInformationIconsInsidePortraitsAndUsesCompactRows`에서 세 아이콘 부모, 좌하단 RectTransform 값, Row 크기를 검증한다.

- [ ] **Step 2: 테스트의 사전 실패 조건 확인**

현재 씬에서 아이콘 부모가 Row이고 Row 높이가 460임을 정적 검사한다.

- [ ] **Step 3: 씬 YAML 수정**

각 Viewport 자식 목록에 대응 아이콘을 추가하고 Row 자식 목록에서는 제거한다. 아이콘을 좌하단 `40x40`, `(6,6)` 안쪽 여백으로 배치하고 Row 높이를 310으로 변경한다.

- [ ] **Step 4: 정적 검증과 컴파일**

씬 구조 검증 스크립트와 `Assembly-CSharp-Editor.csproj` 빌드를 실행한다.

- [ ] **Step 5: 계획 완료 표시**

모든 체크박스를 완료로 갱신하고 변경 범위를 자체 검토한다.
