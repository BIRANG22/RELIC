# Reward Skill Effect Hover And Next Node Lock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 스킬 획득 후보 카드의 효과 설명 호버와 다음 노드 선택 직후 전체 깃발 입력 잠금을 구현한다.

**Architecture:** 기존 `UnitStatusEffectTooltipUI`를 획득 카드에서도 재사용하고, 노드 선택 패널이 단일 입력 잠금 상태를 소유하도록 한다. 두 기능은 독립적으로 구현하고 각각 테스트한 뒤 전체 컴파일로 통합 검증한다.

**Tech Stack:** Unity C#, EventSystem pointer handlers, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-11-reward-skill-effect-hover-and-next-node-lock-design.md`

## Global Constraints

- UI는 효과 계산이나 노드 결과를 변경하지 않는다.
- 기존 씬/프리팹 참조를 재사용하고 신규 데이터는 만들지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에 작성한다.
- Unity 에디터를 직접 제어하지 않는다.

## Review Focus

- 효과가 없는 스킬은 툴팁을 남기지 않는다.
- 카드 사이를 빠르게 이동해도 이전 스킬 효과가 남지 않는다.
- 패널 비활성화 시 효과 툴팁 소유권이 정리된다.
- 첫 노드 선택 이후 다른 선택지의 hover/click/sound raycast가 차단된다.
- 새 선택 사이클과 권한 거부 시 입력이 정상 상태로 유지된다.

---

### Task 1: 획득 후보 카드 효과 툴팁

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardSkillPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleRewardSkillEffectHoverTests.cs`

**Interfaces:**
- Produces: 후보 카드 hover에서 `UnitStatusEffectTooltipUI` 효과 목록 표시/해제

- [ ] 효과 ID 수집 규칙의 실패 테스트를 작성한다.
- [ ] 구현 전 테스트 컴파일 실패를 확인한다.
- [ ] 후보 카드 hover에 효과 목록 표시와 수명주기 정리를 연결한다.
- [ ] 테스트 소스를 포함한 컴파일 성공을 확인한다.

### Task 2: 다음 노드 선택 패널 입력 잠금

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleNextNodeSelectionPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Test: `Assets/Tests/EditMode~/BattleNextNodeSelectionLockTests.cs`

**Interfaces:**
- Produces: `BattleNextNodeSelectionPanel.LockInputAfterSelection()` 및 `IsInputLocked`

- [ ] Open/Lock/Open 입력 상태 전이의 실패 테스트를 작성한다.
- [ ] 구현 전 테스트 실패를 확인한다.
- [ ] CanvasGroup 기반 잠금과 hover 해제를 구현한다.
- [ ] 유효 선택 접수 시 권한 검사 후 즉시 잠금을 호출한다.
- [ ] 전체 컴파일, 정적 호출 순서, diff 검사를 수행한다.
