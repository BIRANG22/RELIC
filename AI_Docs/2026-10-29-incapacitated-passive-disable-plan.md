# 전투불능 캐릭터 패시브 비활성화 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투불능 캐릭터의 패시브가 갱신 시 다시 적용되지 않도록 회귀 테스트와 최소 수정으로 보장한다.

**Architecture:** `BattlePassiveSkillService`의 공통 런타임 갱신 경계에서 `CharacterRuntimeData.IsDead`를 검사한다. 기존 패시브 상태 제거 후 전투불능이면 스킬 패시브와 장비 패시브 추가 효과를 모두 건너뛴다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-29-incapacitated-passive-disable-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 하지 않는다.
- 전투 상태 판정은 `CharacterRuntimeData.IsDead`를 사용한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- `runtime == null`이면 패시브를 적용하지 않는다.
- 패시브 데이터가 없으면 패시브를 적용하지 않는다.
- 생존 캐릭터는 카르마가 0이어도 기존대로 패시브 적용 대상이다.
- 전투불능 캐릭터의 기존 패시브 상태는 갱신 시 제거된다.
- 전투불능 캐릭터의 비패시브 상태는 갱신으로 제거되지 않는다.

---

### Task 1: 전투불능 패시브 회귀 테스트와 최소 수정

**Files:**
- Modify: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Skill/BattlePassiveSkillService.cs`

**Interfaces:**
- Consumes: `CharacterRuntimeData.IsDead`, `BattlePassiveSkillService.ShouldApplyPassive(SkillMasterData, CharacterRuntimeData)`
- Produces: 전투불능 런타임을 거부하는 패시브 활성 조건과 갱신 시 패시브 상태 정리 동작

- [x] **Step 1: 실패 회귀 테스트 작성**

`ShouldApplyPassive_ReturnsFalseWhenCharacterIsDead`와 `RefreshRuntimePassiveEffects_DeadCharacter_ClearsOnlyPassiveStatuses` 테스트를 추가한다.

- [x] **Step 2: RED 검증**

프로젝트 규칙상 batchmode를 실행하지 않는다. 변경 전 구현에서 첫 테스트의 기대값과 실제값이 각각 `false`, `true`임을 코드 경로로 확인하고 C# 테스트 프로젝트 컴파일로 테스트 자체의 유효성을 확인한다.

- [x] **Step 3: 최소 구현**

`ShouldApplyPassive()`에 `!runtime.IsDead` 조건을 추가하고, `RefreshRuntimePassiveEffects()`가 기존 패시브 상태를 지운 뒤 전투불능이면 장비 패시브 추가 효과를 호출하지 않고 반환하도록 한다.

- [x] **Step 4: 검증**

런타임 및 Editor 테스트 프로젝트를 MSBuild로 컴파일한다. Unity 에디터 실행이 필요한 EditMode 테스트 결과는 미검증 항목으로 보고한다.

- [x] **Step 5: 변경 검토**

승인 범위를 벗어난 수정이 없는지 `git diff`로 확인한다. 커밋은 수행하지 않는다.
