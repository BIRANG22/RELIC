# Exploration Result Count-Up Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 탐사 결과 패널의 소요 시간과 세 종류 노드 입장 횟수를 0부터 최종값까지 동시에 카운트업한다.

**Architecture:** `ExplorationResultPanelUI` 내부의 단일 코루틴이 하나의 진행률로 네 표시값을 갱신한다. 계산된 탐사 결과는 변경하지 않고, 표시값 보간과 생명주기 정리만 UI 계층에서 담당한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-25-exploration-result-count-up-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 기존 방문 집계와 전투 결과 데이터는 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 조작을 하지 않는다.

## Review Focus

- 최종값이 0인 필드는 전 구간에서 0을 유지한다.
- 진행률은 0~1로 제한되어 음수나 초과값이 표시되지 않는다.
- 패널 재오픈 시 이전 코루틴이 중복 실행되지 않는다.
- 패널 비활성화 시 중간값이 남지 않고 최종값으로 정리된다.
- `Time.timeScale == 0`이어도 `unscaledDeltaTime`으로 카운트업이 완료된다.

---

### Task 1: 표시값 보간 규칙

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 최종 소요 초, 일반·정예·사건 노드 횟수, 정규화 진행률
- Produces: `CalculateCountUpSeconds(float targetSeconds, float progress)`, `CalculateCountUpCount(int targetCount, float progress)`

- [ ] 진행률 0, 0.5, 1과 범위 밖 진행률에서 초·횟수 보간값을 검증하는 실패 테스트를 작성한다.
- [ ] 런타임 C# 프로젝트 빌드로 새 API 부재에 따른 실패를 확인한다.
- [ ] 입력값과 진행률을 0 이상 및 0~1 범위로 제한하는 두 순수 계산 메서드를 구현한다.
- [ ] 런타임 C# 프로젝트 빌드로 테스트 컴파일과 구현 컴파일이 통과하는지 확인한다.

### Task 2: 패널 카운트업 연출

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: Task 1의 보간 메서드, `BattleExplorationElapsedTime.Format(float)`
- Produces: 직렬화된 `countUpDuration`, 단일 카운트업 코루틴 및 안전한 중단 처리

- [ ] 소스 구조 테스트에 지속 시간 노출, `Time.unscaledDeltaTime`, 코루틴 중복 중단, 네 텍스트의 카운트업 갱신을 검증하는 assertion을 추가한다.
- [ ] 런타임 C# 프로젝트 빌드에서 구조 테스트가 실패하거나 구현 요구가 충족되지 않았음을 확인한다.
- [ ] `ApplyStructuredResult`가 즉시 최종값을 쓰는 대신 네 목표값으로 카운트업을 시작하도록 변경한다.
- [ ] 재오픈 시 기존 코루틴을 중단하고, 비활성화 시 최종 표시값을 적용한 뒤 핸들을 해제한다.
- [ ] 지속 시간이 0이거나 플레이 모드가 아니면 최종값을 즉시 적용한다.
- [ ] 런타임 및 Editor C# 프로젝트 빌드를 실행해 컴파일 성공을 확인한다.

### Task 3: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

**Interfaces:**
- Consumes: Tasks 1-2 결과
- Produces: 검증 기록

- [ ] 가능한 비-batchmode EditMode 테스트 실행 경로가 있으면 관련 테스트를 실행한다.
- [ ] `git diff --check`로 공백 오류가 없는지 확인한다.
- [ ] diff를 검토해 침식 레벨, 홍진석, 방문 집계 및 전투 상태가 변경되지 않았는지 확인한다.
- [ ] Unity 에디터에서 패널 실제 재생 속도 확인이 필요한지 완료 보고에 기록한다.
