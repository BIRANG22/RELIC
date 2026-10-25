# Exploration Result Hard Mode Blur Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 탐사 결과의 각 침식 아이콘 뒤에 전용 붉은 블러를 표시한다.

**Architecture:** 공용 아이템 슬롯 프리팹은 유지하고, 작은 `Image` 전용 블러 프리팹을 각 런타임 침식 슬롯에 한 번만 자식으로 배치한다. 정산패널이 블러 프리팹 참조와 생성 책임을 가지며 획득 아이템 및 캐릭터 슬롯 흐름은 변경하지 않는다.

**Tech Stack:** Unity 6, C#, uGUI, YAML prefab/scene assets, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-25-exploration-result-hard-mode-blur-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 공용 `StorageSlotUI.prefab`과 획득 아이템·캐릭터 슬롯 동작을 변경하지 않는다.
- 기존 작업 공간의 사용자 변경을 보존한다.
- 커밋, Push, PR, 브랜치 및 worktree 조작을 하지 않는다.

## Review Focus

- 같은 슬롯을 다시 바인딩해도 블러가 중복 생성되지 않아야 한다.
- 비어 있거나 숨겨진 침식 슬롯의 블러도 슬롯과 함께 숨겨져야 한다.
- 블러가 아이콘보다 뒤에 렌더되어야 한다.
- 블러는 포인터 입력을 가로채지 않아야 한다.
- 전용 블러 프리팹 참조가 없더라도 결과 패널이 예외 없이 동작해야 한다.

---

### Task 1: 붉은 블러 프리팹과 정산패널 연결

**Files:**
- Create: `Assets/Project/PrefabsR/ExplorationResultHardModeBlur.prefab`
- Create: `Assets/Project/PrefabsR/ExplorationResultHardModeBlur.prefab.meta`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `BattleBagItemSlotUI`, `Stage01/glow.png`, `hardModeSlots`
- Produces: 직렬화된 `Image hardModeBlurPrefab`, `EnsureHardModeBlur(BattleBagItemSlotUI slot)`

- [ ] 전용 프리팹 존재, 붉은 반투명 색상, `m_RaycastTarget: 0`, 씬 참조와 `EnsureHardModeBlur` 호출을 검증하는 실패 테스트를 작성한다.
- [ ] 소스/에셋 검증 스크립트로 요구 항목이 없어 실패하는지 확인한다.
- [ ] 붉은 글로우 Sprite를 사용하는 `ExplorationResultHardModeBlur.prefab`을 추가한다.
- [ ] `ExplorationResultPanelUI`에 블러 프리팹 참조와 슬롯별 단일 생성 로직을 추가하고 첫 번째 자식으로 배치한다.
- [ ] Battle 씬의 결과 패널에 전용 블러 프리팹을 연결한다.
- [ ] 소스/에셋 검증과 테스트 포함 Editor 컴파일이 통과하는지 확인한다.

### Task 2: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

**Interfaces:**
- Consumes: Task 1 결과
- Produces: 검증 기록

- [ ] 런타임 및 테스트 포함 Editor 프로젝트를 컴파일한다.
- [ ] `git diff --check`를 실행한다.
- [ ] diff에서 공용 슬롯 프리팹과 다른 슬롯 코드가 변경되지 않았는지 확인한다.
- [ ] Unity 에디터에서 실제 크기·투명도·레이어 순서 확인이 필요한 항목을 기록한다.
