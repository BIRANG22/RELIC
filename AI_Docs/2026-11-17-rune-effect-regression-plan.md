# 룬 효과 회귀 수정 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 보고된 13개 룬 효과를 실제 전투 결과와 툴팁에 동일하게 적용한다.

**Architecture:** 장비 효과 서비스가 룬 DB 효과를 명령·런타임 상태에 적용하고, 예약 명령에는 첫 예약 및 전투 생존 적 수처럼 실행에 필요한 결정적 입력을 보관한다. 전투 종료와 턴 종료는 상태 변경 후 결과 이벤트를 전달한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, CSV/XLSX GameData

**Spec:** `AI_Docs/2026-11-17-rune-effect-regression-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 및 에디터 직접 제어를 사용하지 않는다.
- UI/VFX와 전투 결과 계산을 분리한다.
- 커밋, Push, PR, 브랜치, worktree 작업은 수행하지 않는다.

## Review Focus

- 룬과 유물 보정이 함께 있을 때 룬 → 유물 순서를 유지한다.
- 첫 예약 기억 앞에 이동이 있어도 Rune_58과 Rune_61이 의도대로 동작한다.
- Rune_61 무료 상태가 미리보기 재계산으로 조기 소비되지 않는다.
- Rune_56은 광역 공격 대상 수가 아니라 생존 적 전체 수만 본다.
- 전투 종료 회복이 한 번만 적용되고 저장 이전에 확정된다.

---

### Task 1: 공용 룬 수치 보정

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Test: `Assets/Tests/EditMode~/RuneEffectRegressionTests.cs`

- [ ] Rune_11~15의 공격·회복·방어도·중독·출혈 증가 실패 테스트를 작성한다.
- [ ] 테스트 컴파일 실패 또는 기대값 실패를 확인한다.
- [ ] 룬 보정을 공용 효과 값 계산 경로에 연결한다.
- [ ] 테스트 소스를 포함한 컴파일을 통과시킨다.

### Task 2: 전용 룬 조건과 상태

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerReservedCommand.cs`
- Modify: 예약 명령 구성 및 실행 호출부
- Test: `Assets/Tests/EditMode~/RuneEffectRegressionTests.cs`

- [ ] Rune_52, 53, 56~58, 61의 조건별 실패 테스트를 작성한다.
- [ ] 첫 예약 공격·첫 비이동 기억·생존 적 수를 명령/런타임 상태로 판정한다.
- [ ] Rune_52 턴 종료 강타, Rune_53 장막 매핑을 수정한다.
- [ ] 조건별 테스트 소스 컴파일을 통과시킨다.

### Task 3: 전투 종료와 툴팁 일치

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`
- Modify: `Assets/Project/Scripts/BattlePlayerSkillPreviewCalculator.cs`
- Test: `Assets/Tests/EditMode~/RuneEffectRegressionTests.cs`

- [ ] Rune_04 적용 순서와 Rune_51 툴팁 보정 테스트를 작성한다.
- [ ] 상태 변경 후 결과 이벤트 순서로 수정한다.
- [ ] 저체력 피해 배율을 툴팁 계산에 연결한다.
- [ ] 전체 테스트 소스 컴파일과 정적 검사를 통과시킨다.

### Task 4: 데이터 동기화와 최종 검증

**Files:**
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Modify: `Assets/ExcelSource/GameData.xlsx`

- [ ] Rune_52 설명을 턴 종료 조건으로 동기화한다.
- [ ] CSV와 Excel 룬 행을 다시 읽어 일치를 확인한다.
- [ ] `Assembly-CSharp-Editor` 재빌드와 `git diff --check`를 실행한다.

