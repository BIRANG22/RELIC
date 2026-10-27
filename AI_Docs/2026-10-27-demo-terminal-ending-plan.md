# Demo Terminal Ending Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 데모 마지막 노드 완료 후 지도 대신 검은 종료 메시지 화면을 표시한다.

**Architecture:** 순수 정책이 데모 여부·현재 노드 완료·유효한 다음 노드 부재를 판정한다. `BattleSceneController`는 기존 방→지도 전환의 가림 시점에서 정책 결과에 따라 지도 또는 씬 배치 종료 패널을 표시한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, NUnit EditMode

**Spec:** `AI_Docs/2026-10-27-demo-terminal-ending-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity 에디터를 직접 제어하거나 batchmode 테스트를 실행하지 않는다.
- UI는 전투 상태를 변경하지 않고 상태를 읽어 연출만 수행한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 일반 전투의 말단 노드는 종료 패널로 전환되지 않아야 한다.
- 완료되지 않은 데모 노드는 종료로 판정되지 않아야 한다.
- 유효한 다음 노드가 하나라도 있으면 지도 복귀를 유지해야 한다.
- 연결 인덱스가 존재하지만 대상 노드가 없으면 말단으로 취급해야 한다.
- 런타임 또는 현재 노드가 없을 때 안전하게 `false`를 반환해야 한다.

---

### Task 1: 데모 종료 정책

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/DemoBattleEndingPolicy.cs`
- Create: `Assets/Tests/EditMode~/DemoBattleEndingPolicyTests.cs`

**Interfaces:**
- Consumes: `BattleRuntimeData`, `MapRuntimeData`, `MapRuntimeProgressUtility`
- Produces: `DemoBattleEndingPolicy.ShouldShow(BattleRuntimeData, MapRuntimeData) : bool`

- [ ] 실패 테스트로 데모 말단·일반 말단·미완료·유효한 다음 노드·유실된 연결 대상·null 입력을 정의한다.
- [ ] Editor 테스트 어셈블리 컴파일에서 정책 부재로 실패하는지 확인한다.
- [ ] 최소 정책 코드를 구현한다.
- [ ] Editor 테스트 어셈블리 컴파일을 다시 확인한다.

### Task 2: 종료 패널과 전환 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/DemoBattleEndingPolicyTests.cs`

**Interfaces:**
- Consumes: `DemoBattleEndingPolicy.ShouldShow(...)`
- Produces: `BattleSceneController`의 데모 말단 분기와 씬 배치 `DemoEndingPanel`

- [ ] 씬 연결과 정확한 사용자 문구를 검사하는 실패 테스트를 추가한다.
- [ ] Editor 테스트 어셈블리 컴파일 또는 관련 테스트 소스 검사에서 실패를 확인한다.
- [ ] `ReturnToMap`의 기존 전환 가림 시점에 종료 분기를 추가한다.
- [ ] Battle 씬 최상위 Canvas에 비활성 종료 패널을 배치하고 직렬화 참조를 연결한다.
- [ ] 런타임 및 Editor 테스트 어셈블리 컴파일과 씬 YAML 검사를 수행한다.
