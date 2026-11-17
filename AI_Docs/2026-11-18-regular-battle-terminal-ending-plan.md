# Regular Battle Terminal Ending Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기본 전투 최종 승리 정산과 로비 이동 사이에 기존 데모 종료 문구와 확인 단계를 삽입한다.

**Architecture:** 순수 정책이 기본 전투·승리·현재 노드 완료·유효한 다음 노드 부재를 판정한다. `ExplorationResultPanelUI`는 첫 확인에서 기존 `DemoEndingPanel`을 표시하고 같은 확인 버튼을 패널 위로 옮기며, 두 번째 확인에서 기존 `ReturnToBase` 처리를 실행한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, NUnit EditMode

**Spec:** `AI_Docs/2026-11-18-regular-battle-terminal-ending-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트는 수행하지 않는다.
- UI는 전투 결과를 계산하거나 변경하지 않고 확정된 런타임 상태만 읽는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 마지막 기본 전투 승리만 종료 텍스트 단계를 거쳐야 한다.
- 다음 노드가 남은 기본 전투는 종료 텍스트 없이 기존 흐름을 유지해야 한다.
- 데모 및 튜토리얼 전투는 각자의 기존 종료 흐름을 유지해야 한다.
- 패배 정산은 종료 텍스트 없이 기존 로비 이동을 유지해야 한다.
- 종료 텍스트 첫 표시에서는 저장·전투 정리·로비 상태 전환이 시작되지 않아야 한다.

---

### Task 1: 기본 전투 최종 정산 정책

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/RegularBattleTerminalEndingPolicy.cs`
- Create: `Assets/Tests/EditMode~/RegularBattleTerminalEndingPolicyTests.cs`

**Interfaces:**
- Consumes: `BattleRuntimeData`, `MapRuntimeData`, `MapRuntimeProgressUtility`
- Produces: `RegularBattleTerminalEndingPolicy.ShouldShow(BattleRuntimeData battleRuntime, MapRuntimeData mapRuntime, bool isDefeat) : bool`

- [x] 기본 전투 최종 승리, 다음 노드 존재, 데모, 튜토리얼, 패배, 미완료 노드, null 런타임을 다루는 실패 테스트를 작성한다.
- [x] 런타임 및 Editor 테스트 어셈블리 컴파일에서 정책 부재로 실패하는지 확인한다.
- [x] `ShouldShow`가 기본 전투 승리이면서 현재 노드가 완료됐고 선택 가능한 다음 노드가 없을 때만 `true`를 반환하도록 최소 구현한다.
- [x] 런타임 및 Editor 테스트 어셈블리 컴파일로 정책과 테스트가 통과하는지 확인한다.

### Task 2: 정산 확인과 종료 패널 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Modify: `Assets/Tests/EditMode~/RegularBattleTerminalEndingPolicyTests.cs`

**Interfaces:**
- Consumes: `RegularBattleTerminalEndingPolicy.ShouldShow(...)`, 기존 `DemoEndingPanel`, 기존 `returnToBaseButton`
- Produces: 첫 확인의 종료 화면 전환과 두 번째 확인의 기존 `ReturnToBase` 실행

- [x] 첫 확인만 종료 단계를 열고 두 번째 확인은 기존 로비 이동을 허용하는 실패 테스트를 추가한다.
- [x] `ExplorationResultPanelUI`에 직렬화된 `terminalEndingPanel` 참조와 세션별 표시 상태를 추가한다.
- [x] `OpenInternal`에서 표시 상태와 확인 버튼 부모·RectTransform 정보를 초기화한다.
- [x] 확인 콜백을 동기 진입 메서드와 기존 비동기 로비 이동 메서드로 분리하고, 첫 확인에서 정책이 참이면 종료 패널 활성화·최상위 정렬·확인 버튼 재배치를 수행한 뒤 반환한다.
- [x] 두 번째 확인 및 정책 제외 대상에서는 기존 로비 정리·저장·상태 이동 코드를 변경 없이 실행한다.
- [x] `Battle.unity`와 `DebugBattle.unity`의 `ExplorationResultPanelUI`에 기존 `DemoEndingPanel` 참조를 연결한다.
- [x] 런타임·Editor 테스트 어셈블리 컴파일과 씬 YAML 검사를 수행한다.
