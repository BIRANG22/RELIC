# Runtime Max Resource Display Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 이벤트 보너스가 포함된 런타임 최대 마나·체력을 전투 파티 HUD에서 중복 합산하지 않는다.

**Architecture:** `CharacterRuntimeData.MaxCost`와 `MaxHP`를 최종 전투값으로 사용한다. 두 파티 HUD는 공통 정적 해석 함수를 통해 동일한 값을 표시하고 같은 값으로 변경 여부를 계산한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-29-runtime-max-resource-display-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity 에디터를 직접 제어하거나 batchmode 테스트를 실행하지 않는다.
- UI는 전투 결과를 계산하지 않고 런타임 최종 상태만 표시한다.

## Review Focus

- 런 보너스가 0이어도 기존 최대값이 유지되는가.
- 런 보너스가 양수일 때 다시 합산되지 않는가.
- 최대 체력과 최대 마나가 같은 기준을 사용하는가.
- null 런타임은 안전한 0으로 처리되는가.
- 두 파티 HUD의 fill과 변경 감지 해시가 같은 기준을 사용하는가.

---

### Task 1: 파티 HUD 최대값 기준 통합

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Modify: `Assets/Project/Scripts/BattlePartyCharacterPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleRuntimeMaxResourceDisplayTests.cs`

**Interfaces:**
- Produces: `BattleCharacterPanelUI.ResolvePartyMaxHP(CharacterRuntimeData)`, `ResolvePartyMaxCost(CharacterRuntimeData)`

- [ ] 이벤트 보너스가 이미 포함된 `MaxHP`/`MaxCost`를 그대로 반환하는 실패 테스트를 작성한다.
- [ ] 신규 테스트가 기존 중복 합산 동작을 잡는지 확인한다.
- [ ] 두 정적 해석 함수를 구현하고 두 HUD의 fill 및 해시 계산에 적용한다.
- [ ] 런타임과 EditMode 테스트 어셈블리 컴파일을 확인한다.
- [ ] 전체 diff와 마나 회복 상한 호출부를 재검토한다.

