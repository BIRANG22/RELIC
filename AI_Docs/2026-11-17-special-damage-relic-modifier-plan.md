# Special Damage Relic Modifier Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 특수 피해 기억에도 공격 피해 유물을 정확한 최종 기본 피해 단계에서 적용한다.

**Architecture:** `BattleEquipmentEffectService.ModifyPlayerEffectValue`가 효과별 기본 피해 확정과 장비 보정을 한 번만 수행한다. 실행 효과와 프리뷰는 이 결과를 그대로 소비하여 철갑돌진의 방어도 배율을 중복 계산하지 않는다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-17-special-damage-relic-modifier-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 디버그 배틀 독립화 코드는 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 철갑돌진 방어도 계수에 50%를 적용해 2배 피해가 되지 않아야 한다.
- `E_Rush` 이동값은 마력 렌즈로 변경되지 않아야 한다.
- `E_RandomStrike`와 `E_ArmorStrike`에 붉은 서약검 고정 피해가 적용되어야 한다.
- 프리뷰와 실제 실행에서 방어도 배율을 한 번만 계산해야 한다.
- 두 유물은 장착 슬롯 순서대로 적용되어야 한다.

---

### Task 1: 특수 피해 장비 계산 회귀 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/SpecialDamageRelicModifierTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`

- [x] 철갑돌진, 제네시스, Rush 비피해 판정의 실패 테스트를 작성한다.
- [x] 새 테스트를 포함한 에디터 컴파일에서 RED를 확인한다.
- [x] 직접 피해 효과 목록과 방어도 선계산을 구현한다.
- [x] 마력 렌즈 배율을 피해 효과에만 제한한다.

### Task 2: 실행·프리뷰 중복 방어도 계산 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/ArmorStrikeEffect.cs`
- Modify: `Assets/Project/Scripts/BattlePlayerSkillPreviewCalculator.cs`
- Test: `Assets/Tests/EditMode~/SpecialDamageRelicModifierTests.cs`

- [x] 방어도 4에서 마력 렌즈 6, 붉은 서약검 6, 장착 순서에 따라 동시 적용 8 또는 9를 검증한다.
- [x] 실행 효과와 프리뷰가 장비 서비스에서 확정된 피해를 다시 곱하지 않게 수정한다.
- [x] 신규 테스트 포함 에디터 어셈블리와 런타임 어셈블리를 컴파일한다.

### Task 3: 최종 검증

**Files:**
- Verify: 위 코드·테스트·문서

- [x] `git diff --check`를 실행한다.
- [x] 승인 범위 밖 변경이 없는지 diff를 검토한다.
- [x] Unity Test Runner와 실제 전투 화면 미검증 항목을 기록한다.
