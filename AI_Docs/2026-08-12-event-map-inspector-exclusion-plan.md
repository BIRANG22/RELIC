# Event Map Inspector Exclusion Implementation Plan

**Goal:** Unity Inspector에서 특정 이벤트맵을 랜덤 후보에서 제외해 이벤트맵 테스트를 쉽게 만든다.

**Architecture:** Excel/CSV 원본 데이터는 유지하고, `BattleMapPanel`에 직렬화된 테스트용 필터를 노출한다. 맵 생성 시 필터의 비활성 EventId를 랜덤 후보 선택에서 제외하되, 수동 `MapIdOverride`는 그대로 허용한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, `BattleRandom`.

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 맵 생성 랜덤은 기존 `BattleRandom` 흐름을 유지한다.
- UI/VFX/사운드가 전투 결과를 계산하지 않도록 한다.
- 커밋, Push, PR, 브랜치 변경은 진행하지 않는다.

## Files

- Modify: `Assets/Project/Scripts/Gameplay/Data/Map/ManualBattleMapTemplate.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/MapRuntimeData.cs`
- Test: `Assets/Tests/EditMode~/ManualBattleMapTemplateTests.cs`
- Test: `Assets/Tests/EditMode~/BattleHorizontalMapLayoutTests.cs`
- Test: `Assets/Tests/EditMode~/ManualBattleMapRuntimePolicyTests.cs`

## Tasks

- [x] RED: 수동 템플릿 랜덤 Special 선택이 비활성 EventId를 건너뛰는 테스트를 추가한다.
- [x] RED: 수동 `MapIdOverride`는 비활성 EventId라도 직접 선택 가능한 테스트를 추가한다.
- [x] RED: 절차 생성 랜덤 후보가 비활성 EventId를 생성하지 않는 테스트를 추가한다.
- [x] RED: 필터 키 변경 시 기존 런타임 맵이 재생성 대상으로 판단되는 테스트를 추가한다.
- [x] GREEN: 인스펙터 직렬화용 `EventMapRandomExclusionSettings`와 엔트리 타입을 구현한다.
- [x] GREEN: `ManualBattleMapTemplate` 랜덤 후보 선택에 필터를 적용한다.
- [x] GREEN: `ProceduralMapGenerator` 랜덤 후보 선택과 fallback 후보 선택에 필터를 적용한다.
- [x] GREEN: `BattleMapPanel`에 필터를 노출하고 생성 키에 필터 해시를 포함한다.
- [x] GREEN: `MapRuntimeData`에 범용 생성 키를 저장하고 기존 수동 템플릿 키와 호환되게 한다.
- [x] VERIFY: MSBuild 런타임/에디터 빌드를 확인한다. Unity Test Runner는 batchmode 금지 규칙 때문에 실행하지 않는다.
