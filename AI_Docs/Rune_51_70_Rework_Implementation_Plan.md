# Rune 51~70 Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rune_51~70의 신규 전투 효과와 취약·약화 전역 수치를 ID 기반 전투 구조에 적용한다.

**Architecture:** `BattleEquipmentEffectService`의 기존 장비 효과 수집과 전투 이벤트 훅을 재사용한다. 수치 판정은 `RuneEffectRules`에 분리하고 데이터는 `GameData.xlsx`를 원본으로 런타임 CSV를 재생성한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode, GameData.xlsx, sectioned runtime CSV

**Spec:** `AI_Docs/Rune_51_70_Rework_Design.md`

## Global Constraints

- Rune_71~75는 변경하지 않는다.
- 전투 상태 변경은 기존 ID와 결과 이벤트 경계를 유지한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- batchmode, 커밋, Push, PR을 실행하지 않는다.

## Review Focus

- 중독·출혈처럼 상태가 중복 적용될 때 실제 부여량만 사용한다.
- 사망 대상과 자기 자신은 인접 아군 효과에서 제외한다.
- 카르마 최대 도달 효과가 이미 최대인 상태의 추가 회복에서 반복되지 않는다.
- 다음 턴 첫 무료 스킬 상태는 정확히 한 번만 소비한다.
- 다중 타격과 다중 대상에서 보너스가 타격마다 중복 계산되지 않도록 한다.

---

### Task 1: 순수 룬 규칙과 전역 상태 배율

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/RuneEffectRules.cs`
- Modify: `BattleDamageModifierUtility.cs`
- Test: `Assets/Tests/EditMode~/RuneEffectRulesTests.cs`

- [ ] 실패 테스트로 취약 50/75%, 약화 30%, HP·공격 횟수·대상 수 조건을 고정한다.
- [ ] RED 컴파일을 확인한다.
- [ ] 최소 규칙 구현과 피해 계산 연결을 추가한다.
- [ ] GREEN 컴파일 및 규칙 직접 호출을 확인한다.

### Task 2: 룬 이벤트 효과 연결

**Files:**
- Modify: `BattleEquipmentEffectService.cs`
- Modify: `BattleEffectBase.cs`, `BattleEffectUtility.cs`, `BattleTurnExecutor.cs` 및 필요한 예약/실행 훅
- Test: `Assets/Tests/EditMode~/RuneEffectRulesTests.cs`

- [ ] 턴 시작·종료·예약·적중·상태 부여·회복·처치·카르마 최대 이벤트의 실패 테스트를 추가한다.
- [ ] RED를 확인한다.
- [ ] Rune_51~70 효과 ID를 기존 이벤트 흐름에 연결한다.
- [ ] GREEN 컴파일과 핵심 상태 전이를 확인한다.

### Task 3: 룬 DB 원본과 런타임 데이터 갱신

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Test: `Assets/Tests/EditMode~/RuneDatabaseMappingTests.cs`

- [ ] Rune_51~70 기대 매핑 및 Rune_71~75 불변 테스트를 추가하고 RED를 확인한다.
- [ ] 원본 워크북의 Rune/Effect 행을 갱신한다.
- [ ] 공식 변환 스크립트로 런타임 CSV를 재생성한다.
- [ ] 데이터 검사, 렌더 검사, C# 컴파일을 수행한다.

### Task 4: 최종 회귀 검증

- [ ] 변경 파일 `git diff --check`를 수행한다.
- [ ] Assembly-CSharp 및 Editor 프로젝트를 컴파일한다.
- [ ] 순수 규칙과 DB 매핑 스모크 검증을 수행한다.
- [ ] Unity Test Runner 미실행 항목과 기존 경고를 완료 보고에 기록한다.

