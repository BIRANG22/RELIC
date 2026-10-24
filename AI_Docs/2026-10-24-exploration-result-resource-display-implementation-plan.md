# Exploration Result Resource Display Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 정산패널에서 침식 점수를 숫자로만 표시하고 레드더스티움 아이콘과 보상 수치를 화면 안에 배치한다.

**Architecture:** 문자열 형식은 기존 순수 포맷 함수에서 결정한다. 레드더스티움은 기존 그룹을 레이아웃 단위로 유지하고 자식 RectTransform을 그룹 로컬 좌표로 정규화한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-24-exploration-result-resource-display-design.md`

## Global Constraints

- Unity 및 패키지 버전을 변경하지 않는다.
- 기존 Battle 씬 사용자 변경을 보존한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에서만 수정한다.
- Unity batchmode를 실행하지 않는다.
- 커밋, Push, PR, 브랜치 조작을 하지 않는다.

## Review Focus

- 침식 점수 0도 `0`으로 표시한다.
- 레드더스티움 아이콘과 수치가 모두 활성 상태를 유지한다.
- 두 요소가 동일한 `RedDustium` 그룹에 속한다.
- 자식 좌표에 패널 기준 좌표가 중복 적용되지 않는다.
- 기존 보상 수치 계산과 `+ 숫자` 형식은 유지한다.

---

### Task 1: 표시 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `ExplorationResultPanelUI.FormatErosionLevel`, Battle 씬 YAML
- Produces: 숫자 전용 형식 및 레드더스티움 로컬 배치 요구사항

- [ ] 숫자 전용 침식 형식과 그룹 내부 배치를 검증하는 실패 테스트를 작성한다.
- [ ] 기존 구현과 씬 좌표에서 실패 조건을 확인한다.

### Task 2: 문자열 및 씬 배치 수정

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Produces: `FormatErosionLevel(int)` 숫자 문자열, 화면 안의 RedDustium 그룹

- [ ] 포맷 함수를 숫자 전용으로 변경한다.
- [ ] 그룹 크기와 두 자식의 로컬 좌표를 수정한다.
- [ ] C# 프로젝트 빌드와 씬 참조 검사를 실행한다.
