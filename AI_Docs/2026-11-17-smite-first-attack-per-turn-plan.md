# Smite First Attack Per Turn Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 강타 수치가 매 턴 첫 번째 공격 행동에만 적용되고 실제 적용 시에만 1 감소하도록 수정한다.

**Architecture:** 강타의 턴별 소비 여부를 `CharacterRuntimeData`의 ID 기반 전투 상태로 관리한다. 피해 계산, 실행 후 소비, 턴 시작 초기화를 하나의 서비스 API로 묶고 데이터 종료 규칙은 트리거 감소로 변경한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, GameData XLSX/CSV

**Spec:** `AI_Docs/2026-11-17-smite-first-attack-per-turn-design.md`

## Global Constraints

- 현재 체크아웃된 브랜치와 작업공간에서 작업한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 하지 않는다.

## Review Focus

- 수치 3인 강타가 같은 턴의 두 번째 공격에 다시 적용되지 않는지 확인한다.
- 다음 턴 첫 공격에는 다시 적용되는지 확인한다.
- 공격하지 않은 턴에는 수치가 유지되는지 확인한다.
- 마지막 수치가 적용된 뒤 상태가 제거되는지 확인한다.
- 예약 피해 미리보기와 실제 피해 계산이 같은 첫 공격 조건을 사용하는지 확인한다.

---

### Task 1: 강타 턴 소비 규칙

**Files:**
- Create: `Assets/Tests/EditMode~/SmiteFirstAttackPerTurnTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/BattleDamageModifierUtility.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/BattlePlayerSkillPreviewCalculator.cs`

**Interfaces:**
- Produces: 강타 준비 확인, 공격 후 소비, 턴 시작 소비 상태 초기화 API

- [ ] 강타 수치 3의 첫 공격 적용, 같은 턴 재적용 차단, 다음 턴 재활성화, 마지막 수치 제거를 검증하는 실패 테스트를 작성한다.
- [ ] 테스트 컴파일이 새 API 부재로 실패하는지 확인한다.
- [ ] 서비스 API와 실행/피해/미리보기 연결을 최소 변경으로 구현한다.
- [ ] 테스트와 C# 컴파일을 다시 실행한다.

### Task 2: 데이터 종료 규칙 동기화

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`

**Interfaces:**
- Consumes: Task 1의 공격 후 소비 규칙
- Produces: `E_Smite.EndTurn = DecreaseOnTrigger`

- [ ] 원본 XLSX의 Effect 행과 런타임 CSV의 `E_Smite` 종료 규칙을 변경한다.
- [ ] 두 데이터 소스가 동일한 값을 갖는지 확인한다.
- [ ] 관련 테스트와 전체 C# 컴파일을 실행한다.
