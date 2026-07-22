# DebugBattle Target Monster Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DebugBattle에 HP 999, 무행동, 생존 시 턴 종료 완전 회복 특성을 가진 테스트 몬스터 한 마리를 생성한다.

**Architecture:** 기존 몬스터 프리팹과 스폰/HUD 경로를 재사용하되 런타임 ID로 디버그 표적을 식별한다. 턴 계획 단계에서 표적을 제외하고 플레이어 턴 반환 이벤트에서 HP만 복구한다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit EditMode 테스트

## Global Constraints

- 디버그 동작은 DebugBattle 씬에만 적용한다.
- 실제 몬스터 DB와 저장 데이터는 수정하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
- 커밋과 PR은 별도 사용자 허락 없이 진행하지 않는다.

---

### Task 1: 디버그 표적 런타임 규칙

**Files:**
- Create: `Assets/Project/Scripts/Debug/DebugBattleTargetRules.cs`
- Create: `Assets/Tests/EditMode~/DebugBattleTargetRulesTests.cs`

**Interfaces:**
- Produces: `DebugBattleTargetRules.Configure(MonsterRuntimeData data)`, `TryRestoreFullHp(MonsterRuntimeData data)` 및 `IsDebugTarget(MonsterRuntimeData data)`

- [ ] HP 999 구성, 생존 시 회복, 사망 시 미회복을 검증하는 테스트를 먼저 작성한다.
- [ ] 승인된 테스트 실행 예외에 따라 정적 검토 후 최소 런타임 규칙을 구현한다.
- [ ] Assembly-CSharp 빌드로 컴파일을 확인한다.

### Task 2: DebugBattle 전용 스폰과 무행동 연결

**Files:**
- Modify: `Assets/Project/Scripts/Debug/DebugBattleSceneBootstrap.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Monster/MonsterUnit.cs`

**Interfaces:**
- Consumes: `DebugBattleTargetRules`와 기존 몬스터 스폰/HUD 경로
- Produces: DebugBattle 전용 단일 표적 스폰 및 빈 몬스터 행동 계획

- [ ] DebugBattle 로더가 저장 맵 스폰 대신 기존 프리팹 기반 표적 한 마리를 생성하도록 제한된 진입점을 추가한다.
- [ ] 스폰된 디버그 표적의 AI만 비활성화한다.
- [ ] 턴 반환 이벤트에서 생존 표적 HP를 999로 복구하고 HUD를 갱신한다.
- [ ] 일반 Battle 씬의 기존 스폰 및 AI 흐름이 조건문 밖에서 유지되는지 검토한다.

### Task 3: 최종 검증

**Files:**
- Verify: 위 변경 파일과 DebugBattle 씬 참조

**Interfaces:**
- Produces: 컴파일 및 수동 검증 가능한 DebugBattle

- [ ] Assembly-CSharp와 Assembly-CSharp-Editor 빌드를 실행한다.
- [ ] `git diff --check`와 디버그 씬 한정 조건을 확인한다.
- [ ] Unity에서 HP, 무행동, 턴 종료 회복, 999 피해 사망을 확인할 체크리스트를 보고한다.
