# Relic Intro Blocker Disable Error Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 유물 추출 후 패널 비활성화 과정에서 입력 차단기의 sibling 위치 변경 오류가 발생하지 않게 한다.

**Architecture:** 기존 `BattleRewardRelicPanelUI.SetIntroInputBlocked(bool)`의 역할을 유지하되, sibling 정렬은 활성화 경로에서만 수행한다. 실제 패널 비활성화 동작을 회귀 테스트로 보호한다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-10-relic-intro-blocker-disable-design.md`

## Global Constraints

- 문서와 테스트 위치는 프로젝트 `AGENTS.md` 규칙을 따른다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 수행하지 않는다.
- 유물 추출 데이터와 전투 상태 로직은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 차단기 최초 활성화 시 sibling index 0을 유지하는가.
- 부모 패널 비활성화 중 계층 순서를 변경하지 않는가.
- 차단 해제 후 차단기가 비활성화되는가.
- 다시 열었을 때 기존 차단기를 재사용해 정상 활성화하는가.
- 유물 추출 지급 로직에 영향이 없는가.

---

### Task 1: 입력 차단기 비활성화 회귀 수정

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleRewardRelicIntroInputLockTests.cs`
- Modify: `Assets/Project/Scripts/BattleRewardRelicPanelUI.cs`

**Interfaces:**
- Consumes: `SetIntroInputBlocked(bool blocked)`
- Produces: 부모 비활성화 중 sibling 변경 없는 입력 차단기 정리

- [x] 부모 패널 비활성화 시 오류 로그가 없어야 하는 회귀 테스트 작성
- [x] 기존 구현의 실패 원인이 `SetAsFirstSibling()`임을 확인
- [x] sibling 정렬을 `blocked == true` 경로로 제한
- [x] 회귀 테스트 포함 어셈블리 컴파일
- [x] 런타임 어셈블리 컴파일 및 `git diff --check` 검증
