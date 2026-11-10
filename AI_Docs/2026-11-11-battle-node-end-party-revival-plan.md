# Battle Node End Party Revival Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투에서 쓰러진 파티원을 승리한 노드가 끝나는 시점에 HP 1로 부활시킨다.

**Architecture:** 파티 슬롯과 캐릭터 런타임 저장소만 입력받는 순수 상태 변경 서비스를 추가한다. `BattleResultChecker`가 보상 완료 후 노드를 클리어 처리하기 직전에 서비스를 호출하여 저장 체크포인트에도 부활 상태가 포함되게 한다.

**Tech Stack:** Unity C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-11-battle-node-end-party-revival-design.md`

## Global Constraints

- 신규 데이터나 UI 보정을 추가하지 않는다.
- 패배 흐름에서는 부활시키지 않는다.
- 현재 파티의 사망 캐릭터만 HP 1로 변경한다.
- Unity 에디터를 직접 제어하지 않는다.

## Review Focus

- 생존 캐릭터 HP가 변경되지 않는지 확인한다.
- 파티 외 캐릭터가 변경되지 않는지 확인한다.
- 중복 파티 ID가 있어도 한 캐릭터를 한 번만 처리하는지 확인한다.
- null 저장소와 빈 슬롯이 안전하게 무시되는지 확인한다.
- 체크포인트 저장 전에 부활이 호출되는지 확인한다.

---

### Task 1: 파티 부활 상태 서비스

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Runtime/BattlePartyRevivalService.cs`
- Test: `Assets/Tests/EditMode~/BattlePartyRevivalServiceTests.cs`

**Interfaces:**
- Produces: `BattlePartyRevivalService.ReviveIncapacitatedParty(PartyRuntimeStore, CharacterRuntimeStore) -> int`

- [ ] 사망 파티원만 HP 1로 변경하고 생존자와 비파티원을 유지하는 실패 테스트를 작성한다.
- [ ] 구현 전 컴파일 실패를 확인한다.
- [ ] 최소 상태 변경 서비스를 구현한다.
- [ ] 테스트 소스를 포함한 컴파일 성공을 확인한다.

### Task 2: 전투 노드 완료 흐름 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`

**Interfaces:**
- Consumes: `BattlePartyRevivalService.ReviveIncapacitatedParty(...)`

- [ ] `CompleteBattleRewardFlow`에서 `MarkCurrentBattleNodeCleared`보다 먼저 부활 서비스를 호출한다.
- [ ] 패배 경로에는 호출이 없음을 검토한다.
- [ ] 전체 C# 컴파일과 diff 검사를 수행한다.
