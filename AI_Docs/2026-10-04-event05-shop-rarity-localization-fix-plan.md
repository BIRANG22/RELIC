# Event_05 후속 선택지 및 배틀상점 레어도 현지화 수정 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Event_05 후속 3번 선택지와 배틀상점 기억·유물 레어도가 현재 로케일로 표시되게 한다.

**Architecture:** 이벤트 실행 ID는 유지하면서 번역 키만 원본 선택지 키로 정규화한다. 상점은 새 번역 데이터를 만들지 않고 기존 레어도 유틸리티와 공통 종류 키를 조합한다.

**Tech Stack:** Unity 6, C#, Unity Localization, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-04-event05-shop-rarity-localization-fix-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- batchmode 테스트는 실행하지 않는다.
- 전투 상태와 랜덤 처리에는 손대지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- `Event_05_A`와 `Event_05_B`가 모두 원본 3번 선택지 번역 키를 사용하는가.
- Event_05 외의 일반 이벤트 선택지 키 생성이 그대로 유지되는가.
- 기억 레어도가 종류를 중복해 붙이지 않는가.
- 유물 레어도가 레어도와 종류를 모두 현재 로케일로 표시하는가.
- 로케일 변경 후 기존 상품 재바인딩 흐름이 유지되는가.

---

### Task 1: Event_05 후속 선택지 번역 키

**Files:**
- Modify: `Assets/Tests/EditMode~/LocalizationDynamicTextBoundaryTests.cs`
- Modify: `Assets/Project/Scripts/Core/Localization/GameDataLocalization.cs`

**Interfaces:**
- Consumes: `EventData.EventId`, `EventData.ChoiceOrder`
- Produces: `GameDataLocalization.EventChoiceKey(EventData, string)`의 원본 키 정규화

- [ ] `Event_05_A`, `Event_05_B`의 3번 선택지가 `data.event.event_05.choice_3_*`를 반환하는 실패 테스트를 작성한다.
- [ ] 테스트가 현재 단계 키를 반환하여 실패하는지 확인한다.
- [ ] `EventChoiceKey`에 최소 정규화 로직을 구현한다.
- [ ] 테스트가 통과하는지 확인한다.

### Task 2: 배틀상점 레어도 현지화

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleDynamicLocalizationRegressionTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/GoodsIconItem.cs`

**Interfaces:**
- Consumes: `SkillRarityUtility.GetDisplayName`, `RelicRarityUtility.GetDisplayName`, `GameLocalization.Get("common.relic")`
- Produces: 현재 로케일의 상점 기억·유물 레어도 표시

- [ ] 기존 한국어 하드코딩 경로를 검출하는 실패 테스트를 작성한다.
- [ ] 테스트가 하드코딩된 레어도 반환 때문에 실패하는지 확인한다.
- [ ] 기억은 기존 완성형 표시명을 사용하고 유물은 현지화된 레어도와 종류를 조합한다.
- [ ] 관련 테스트가 통과하는지 확인한다.

### Task 3: 전체 검증

**Files:**
- Verify: 변경된 코드와 테스트

**Interfaces:**
- Consumes: Task 1, Task 2 결과
- Produces: 컴파일 및 회귀 검증 증거

- [ ] 런타임 어셈블리를 컴파일한다.
- [ ] 에디터 어셈블리를 컴파일한다.
- [ ] 테스트 실행 가능 상태를 확인하고 batchmode 없이 가능한 범위를 보고한다.
- [ ] 변경 diff를 검토한다.
