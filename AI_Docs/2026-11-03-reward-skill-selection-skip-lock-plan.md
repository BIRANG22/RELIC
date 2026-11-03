# Reward Skill Selection Skip Lock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기억 보상 스킬 선택이 시작되면 넘어가기 버튼의 클릭·호버·사운드를 즉시 비활성화한다.

**Architecture:** `BattleRewardSkillPanelUI`의 기존 `SetSkipRevealState(false)`를 선택 시작 전용 메서드로 감싸고, 즉시 장착 및 교체 선택 진입 경로가 이를 공유한다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-03-reward-skill-selection-skip-lock-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR, 브랜치/worktree 작업을 수행하지 않는다.

## Review Focus

- 즉시 장착 경로와 교체 선택 경로 모두 버튼을 잠그는가.
- 버튼 잠금이 선택 결과 저장 전에 적용되는가.
- 비활성화가 기존 버튼 색상과 오디오 상태도 함께 초기화하는가.
- 새 보상을 열 때 기존 `SetSkipRevealState` 흐름으로 다시 활성화 가능한가.
- 다른 보상 패널에는 영향을 주지 않는가.

---

### Task 1: 선택 시작 시 SkipButton 잠금

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardSkillPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleRewardSkillSkipButtonLockTests.cs`

**Interfaces:**
- Consumes: `SetSkipRevealState(false)`
- Produces: `LockSkipButtonForSelection()`

- [ ] 선택 잠금 메서드가 버튼을 비활성화하는 실패 테스트를 작성한다.
- [ ] 교체 선택 진입과 최종 선택 확정 경로가 잠금 메서드를 호출하는 경계 테스트를 작성한다.
- [ ] 교체 선택 중 다른 카드 Reveal 완료가 버튼을 재활성화하지 않는 회귀 테스트를 작성한다.
- [ ] `UIChildHoverTransform`이 잠금 즉시 기본 위치로 복귀하고 비활성화되는 테스트를 작성한다.
- [ ] 테스트가 기존 코드에서 실패하는지 확인한다.
- [ ] 두 선택 경로에서 잠금 메서드를 호출하도록 최소 구현한다.
- [ ] 테스트 코드와 런타임·에디터 프로젝트를 컴파일한다.
- [ ] diff와 멀티플레이 경계를 검토한다.
