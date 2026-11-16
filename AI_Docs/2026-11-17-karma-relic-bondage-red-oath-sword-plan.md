# Karma Relics, Bondage, and Red Oath Sword Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 카르마 유물 조건, 니아의 불꽃 속박 대상, 붉은 서약검의 실제 사용 자원 비용 증가를 데이터 설명과 일치시킨다.

**Architecture:** 전투 효과 서비스는 Command의 `SkillMasterData.ReferenceResource`를 권위 있는 소비 자원으로 사용한다. 상태 효과는 컨텍스트의 명시적 대상에 적용하며, 기존 이동 실행 시점의 속박 소비 로직은 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, CSV runtime data

**Spec:** `AI_Docs/2026-11-17-karma-relic-bondage-red-oath-sword-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- UI/VFX/사운드는 전투 결과를 계산하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 카르마를 소비하지만 `Category.Unique`가 아닌 기억에도 세 유물이 적용되어야 한다.
- `Category.Unique`라도 카르마를 소비하지 않는 기억에는 세 유물이 적용되지 않아야 한다.
- 플레이어 시전자와 몬스터 대상이 함께 있는 속박 컨텍스트에서 몬스터만 상태를 받아야 한다.
- 붉은 서약검은 HP·마나·카르마 중 실제 소비 자원 하나만 증가시켜야 한다.
- 붉은 서약검은 비공격 기억과 이동에는 비용 증가를 적용하지 않아야 한다.

---

### Task 1: 소비 자원 기반 유물 조건

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerReservedCommand.cs`
- Test: `Assets/Tests/EditMode~/BattleEquipmentResourceConditionTests.cs`

**Interfaces:**
- Consumes: `SkillMasterData.ReferenceResource`
- Produces: `BattleEquipmentEffectService.UsesUniqueResource(SkillMasterData)`

- [x] 카르마 소비 Core 기억은 참이고, 마나 소비 Unique 기억은 거짓인 실패 테스트를 작성한다.
- [x] 테스트 또는 컴파일을 실행해 기존 구현이 조건을 만족하지 않음을 확인한다.
- [x] 효과 수치, 효과 횟수, 처치 회복 조건을 `UsesUniqueResource`로 통일한다.
- [x] 관련 테스트와 컴파일을 다시 실행한다.

### Task 2: 속박 명시적 대상 적용

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Abnormal/BondageEffect.cs`
- Test: `Assets/Tests/EditMode~/BondageEffectTargetingTests.cs`

**Interfaces:**
- Consumes: `BattleEffectContext.PlayerTarget`, `BattleEffectContext.MonsterTarget`
- Produces: 명시적 대상에게만 `E_Bondage`를 부여하는 `BondageEffect`

- [x] 플레이어 시전자와 몬스터 대상이 함께 있을 때 몬스터만 속박되는 실패 테스트를 작성한다.
- [x] 명시적 플레이어 대상 적용과 대상 없는 시전자 미적용 테스트를 추가한다.
- [x] 테스트 또는 컴파일로 현재 잘못된 대상 선택을 확인한다.
- [x] 취약·약화 효과와 같은 명시적 대상 분기를 구현한다.
- [x] 대상 테스트와 기존 트리거 감소 테스트를 검증한다.

### Task 3: 붉은 서약검 실제 사용 자원 비용 증가

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Test: `Assets/Tests/EditMode~/BattleEquipmentResourceConditionTests.cs`

**Interfaces:**
- Consumes: `ReferenceResource`, 공격 기억 여부, `E_Attack_Resource_Cost_Delta`
- Produces: `BattleEquipmentEffectService.ApplyResourceCostDelta(ReferenceResource, int, ref int, ref int, ref int)`

- [x] HP·마나·카르마별로 해당 비용만 1 증가하는 실패 테스트를 작성한다.
- [x] 비공격 기억에는 붉은 서약검 델타가 선택되지 않는 테스트를 작성한다.
- [x] 테스트 또는 컴파일로 기존 마나 전용 동작을 확인한다.
- [x] 새 효과 ID를 추가하고 공격 기억의 실제 소비 자원 비용에 델타를 적용한다.
- [x] `X` 비용은 추가 비용을 확보한 뒤 `ResolvedX`와 효과 횟수를 다시 계산한다.
- [x] CSV 효과 및 유물 설명을 `자원 소모량`으로 갱신한다.
- [x] 관련 테스트와 전체 컴파일을 실행한다.

### Task 4: 최종 회귀 검증

**Files:**
- Verify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Verify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Abnormal/BondageEffect.cs`
- Verify: `Assets/Resources/Data/GameDataRuntime.csv`
- Verify: `Assets/Tests/EditMode~/BattleEquipmentResourceConditionTests.cs`
- Verify: `Assets/Tests/EditMode~/BondageEffectTargetingTests.cs`

**Interfaces:**
- Consumes: Task 1~3 결과
- Produces: 컴파일 가능한 회귀 수정 세트

- [x] 변경 파일 diff를 검토해 승인 범위 밖 수정이 없는지 확인한다.
- [x] Assembly-CSharp와 Assembly-CSharp-Editor를 빌드한다.
- [x] 실행하지 못한 Unity 에디터 테스트와 화면 검증 항목을 기록한다.
