# Monster Skill Effect Hover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터 행동 설명의 밑줄 효과명에 호버하면 해당 효과 하나의 설명 툴팁을 표시한다.

**Architecture:** TMP 링크 좌표 판정을 담당하는 작은 UI 컴포넌트를 추가하고 `BattleMonsterInfoPanelUI`가 설명 텍스트에 바인딩한다. 효과 설명 표시는 기존 Effect DB와 `UnitStatusEffectTooltipUI`를 재사용한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity UI/EventSystem, NUnit EditMode

**Spec:** `AI_Docs/2026-11-11-monster-skill-effect-hover-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 수행하지 않는다.
- 전투 상태와 랜덤 로직은 변경하지 않는다.
- 기존 미커밋 변경을 보존하고 커밋/Push/PR은 수행하지 않는다.

## Review Focus

- 링크가 아닌 일반 설명 위에서는 툴팁이 열리지 않아야 한다.
- 잘못된 접두사·빈 Effect ID는 무시해야 한다.
- Effect DB에 없는 ID는 툴팁을 열지 않아야 한다.
- 스킬 교체 및 패널 비활성화 시 이전 툴팁이 남지 않아야 한다.
- 같은 링크를 계속 가리킬 때 툴팁 항목을 매 프레임 재생성하지 않아야 한다.

---

### Task 1: TMP 효과 링크 호버 컴포넌트

**Files:**
- Create: `Assets/Project/Scripts/UI/MonsterSkillEffectLinkTooltip.cs`
- Test: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: TMP 링크 ID `effecticon:<EffectId>`, `DataManager.EffectDatabase`, `UnitStatusEffectTooltipUI`
- Produces: `Bind(TMP_Text)`, `ClearHover()`, `TryParseEffectId(string, out string)`

- [ ] 효과 링크 ID의 정상·빈 값·잘못된 접두사 테스트를 먼저 작성한다.
- [ ] 에디터 C# 컴파일로 새 API 부재에 의한 실패를 확인한다.
- [ ] 링크 좌표 판정, DB 검증, 단일 효과 툴팁 표시·숨김을 최소 구현한다.
- [ ] 에디터 C# 컴파일로 테스트와 구현이 함께 컴파일되는지 확인한다.

### Task 2: 몬스터 정보 패널 연결

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoPanelUI.cs`
- Test: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: Task 1의 `Bind(TMP_Text)`, `ClearHover()`
- Produces: 설명 표시·초기화·비활성화 시 올바른 호버 생명주기

- [ ] 바인딩이 텍스트의 rich text와 raycast를 활성화하는 테스트를 추가하고 실패를 확인한다.
- [ ] `ResolveSkillInfoReferences` 이후 컴포넌트를 연결하고 선택 변경·Clear·OnDisable에서 호버를 정리한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드해 컴파일 오류가 없는지 확인한다.
- [ ] diff를 검토해 승인 범위 밖 변경과 기존 사용자 변경 침범이 없는지 확인한다.

### Task 3: 동적 몬스터 행동 항목 공통 적용

**Files:**
- Modify: `Assets/Project/Scripts/MonsterInfoSkillItemUI.cs`
- Test: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: Task 1의 `MonsterSkillEffectLinkTooltip`, TMP `effecticon:<EffectId>` 링크
- Produces: 모든 `MonsterSkill_*` 동적 항목의 링크 보존·밑줄·개별 효과 호버

- [ ] `E_Barrier` 및 복수 효과 링크가 밑줄과 링크 ID를 함께 보존하는 실패 테스트를 작성한다.
- [ ] 링크 제거 정규식을 링크 보존 포맷으로 교체하고 Detail 텍스트에 호버 컴포넌트를 바인딩한다.
- [ ] 재바인딩 및 비활성화 시 이전 툴팁을 정리한다.
- [ ] 런타임 및 테스트 포함 에디터 어셈블리 컴파일을 확인한다.

### Task 4: 효과 툴팁 왼쪽 위를 마우스에 정렬

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoPanelUI.cs`
- Test: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: 활성 `UnitStatusEffectTooltipItemUI`의 `RectTransform`, 현재 마우스 화면 좌표
- Produces: `AlignTopLeftToWorldPoint(RectTransform, Vector3)` 및 링크 호버 중 연속 위치 갱신

- [ ] 중앙 피벗 RectTransform의 왼쪽 위가 지정 좌표에 일치하는 실패 테스트를 작성한다.
- [ ] 활성 효과 툴팁을 찾아 마우스 화면 좌표를 부모 UI 월드 좌표로 변환한다.
- [ ] 최초 표시 및 같은 링크 호버 유지 중 왼쪽 위 좌표를 매 프레임 보정한다.
- [ ] 런타임 및 테스트 포함 에디터 어셈블리 컴파일을 확인한다.

## Self-review

- 설계의 링크 단위 호버, 단일 효과, 종료 조건, DB 검증을 두 작업이 모두 다룬다.
- 새 컴포넌트와 패널의 연결 인터페이스가 일치한다.
- 실제 포인터 위치 및 화면 배치는 Unity 에디터 화면 검증이 필요하므로 완료 보고에서 미검증으로 명시한다.
