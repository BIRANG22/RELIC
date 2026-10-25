# ExplorationResultPanel Decoration Placeholders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** ExplorationResultPanel에 라벨 아이콘 4개와 캐릭터/보상 영역 사각 라인 이미지 자리 오브젝트를 추가한다.

**Architecture:** 고정 장식 UI는 `Battle.unity` 계층에 직접 배치한다. 실제 Sprite가 없는 동안에는 `Image`를 비활성화하고 Raycast도 끄며, 프레임은 대상 콘텐츠의 첫 번째 자식 또는 공통 부모의 첫 번째 자식으로 배치해 배경 레이어가 되게 한다.

**Tech Stack:** Unity 6, Unity UI, YAML 씬 직렬화, NUnit EditMode 테스트

**Spec:** `AI_Docs/2026-10-26-exploration-result-decoration-placeholders-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 사용자의 명시적 허가 없이 커밋, Push, PR, 브랜치 또는 worktree 작업을 하지 않는다.

## Review Focus

- Sprite 미연결 상태에서 흰색 기본 이미지가 표시되지 않아야 한다.
- 장식 Image가 UI 입력을 가로채지 않아야 한다.
- 캐릭터 프레임은 각 행보다 10px 크게 둘러야 한다.
- 보상 프레임은 라벨, 슬롯, RedDustium을 모두 포함해야 한다.
- 프레임을 활성화했을 때 콘텐츠보다 뒤에 렌더링되어야 한다.

---

### Task 1: 씬 장식 이미지 자리 오브젝트

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 기존 `ExplorationReportFrame`, `ExplorationReportStageGroup`, `ExplorationReportRow_0~2` RectTransform 계층
- Produces: 아트가 Sprite를 연결해 활성화할 수 있는 8개의 명시적 `Image` 오브젝트

- [ ] **Step 1: 실패하는 씬 구조 테스트 작성**

`BattleScene_ProvidesDecorationImagePlaceholders` 테스트에서 오브젝트 이름, 부모, 위치/크기, 비활성 Image, Raycast 비활성, 프레임의 첫 번째 자식 순서를 검증한다.

- [ ] **Step 2: 테스트 실패 확인**

에디터 테스트 어셈블리를 빌드하고 새 오브젝트가 없어 구조 검증이 실패하는지 확인한다.

- [ ] **Step 3: Battle.unity에 자리 오브젝트 추가**

설계 문서의 이름과 좌표를 그대로 사용해 네 아이콘, 세 행 프레임, 하나의 보상 프레임을 추가한다.

- [ ] **Step 4: 구조와 컴파일 검증**

PowerShell 구조 검사로 모든 직렬화 값을 확인하고 `Assembly-CSharp-Editor.csproj` 빌드가 성공하는지 확인한다. Unity batchmode는 사용하지 않는다.

- [ ] **Step 5: 변경 범위 검토**

`git diff --check`와 diff 검토로 요청 범위 밖 변경, YAML 공백 오류, 기존 사용자 변경 훼손 여부를 확인한다. 커밋은 수행하지 않는다.
