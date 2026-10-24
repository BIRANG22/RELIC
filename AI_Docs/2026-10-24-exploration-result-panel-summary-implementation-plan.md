# Exploration Result Panel Summary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 정산패널이 실제 방문 노드 횟수와 침식 레벨을 표시하고 아이콘 배치를 인스펙터에서 조정할 수 있게 한다.

**Architecture:** 방문 횟수는 `MapRuntimeData`의 안정적인 노드 ID를 입력으로 받는 순수 집계 함수가 계산하고 UI는 결과만 표시한다. 침식 레벨과 슬롯 레이아웃은 명시적 직렬화 참조 및 설정값으로 구성한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-24-exploration-result-panel-summary-design.md`

## Global Constraints

- Unity 및 패키지 버전을 변경하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode를 실행하지 않는다.
- 기존 작업 공간과 Battle 씬의 사용자 변경을 보존한다.
- 커밋, Push, PR, 브랜치 조작을 하지 않는다.

## Review Focus

- 방문했지만 클리어하지 않은 노드도 집계한다.
- 중복 방문 ID는 한 번만 집계한다.
- 존재하지 않거나 형식이 잘못된 방문 ID는 무시한다.
- 일반·정예·사건 외 노드 타입은 세 표시값을 오염시키지 않는다.
- 침식 선택이 없을 때도 `Lv. 0`을 표시한다.

---

### Task 1: 방문 노드 집계

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/ExplorationResultData.cs`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `MapRuntimeData.VisitedMapIds`, `MapRuntimeData.GeneratedNodes`
- Produces: `ExplorationNodeVisitSummary.Count(MapRuntimeData runtime)`

- [ ] 방문 기록이 클리어 기록과 무관하게 타입별로 집계되는 실패 테스트를 작성한다.
- [ ] 테스트가 새 API 부재로 실패하는지 확인한다.
- [ ] 중복 제거 및 타입 매핑을 수행하는 순수 집계 구조를 구현한다.
- [ ] 관련 테스트와 컴파일을 다시 실행한다.

### Task 2: 정산 UI 연결과 인스펙터 레이아웃

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `ExplorationNodeVisitSummary.Count(MapRuntimeData runtime)`
- Produces: 방문 횟수 표시, `FormatErosionLevel(int score)`, 직렬화된 슬롯 크기·간격·열 설정

- [ ] 침식 레벨 형식과 인스펙터 노출을 검증하는 실패 테스트를 작성한다.
- [ ] 예상 실패를 확인한다.
- [ ] UI가 방문 집계 결과를 사용하고 레이아웃 설정값으로 슬롯을 배치하게 구현한다.
- [ ] 관련 테스트와 컴파일을 다시 실행한다.

### Task 3: Battle 씬 침식 레벨 텍스트

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `ExplorationResultPanelUI.erosionLevelText`
- Produces: `ExplorationReportErosionLevel` TMP 오브젝트와 명시적 씬 참조

- [ ] 씬 오브젝트와 연결을 검증하는 실패 테스트를 작성한다.
- [ ] 예상 실패를 확인한다.
- [ ] 기존 씬 변경을 보존하며 TMP 텍스트와 참조를 최소 범위로 추가한다.
- [ ] EditMode 테스트 소스 검증과 C# 컴파일을 수행한다.

### Task 4: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

**Interfaces:**
- Consumes: Tasks 1-3 결과
- Produces: 검증 기록

- [ ] `git diff --check`를 실행한다.
- [ ] 런타임 및 Editor C# 프로젝트를 빌드한다.
- [ ] Unity 에디터에서 수동 확인이 필요한 항목을 구분해 기록한다.
