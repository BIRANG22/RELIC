# Skill Upgrade Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 강화형 Core/Ability 데이터를 단일 기본 스킬로 승격하고 강화 시스템과 홀짝 판정을 제거한다.

**Architecture:** 엑셀 원본을 권위 데이터로 유지하며 짝수 행을 연속 ID로 재번호화한다. 런타임은 단일 SkillId만 취급하고, 보상 경로는 카테고리·희귀도·소유권·허용 목록만 적용한다. 강화 UI와 저장 상태는 함께 제거한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, XLSX, Unity YAML assets, PowerShell CSV converter

**Spec:** `AI_Docs/2026-10-14-skill-upgrade-removal-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 둔다.
- Unity batchmode 테스트를 실행하지 않는다.
- 기존 세이브 마이그레이션은 구현하지 않는다.
- 커밋, Push, PR, 브랜치·worktree 변경을 수행하지 않는다.
- 전투 랜덤은 기존 `BattleRandom` 흐름을 유지한다.

## Review Focus

- 새 ID 재번호화가 아이콘·VFX·공격 오버라이드의 다른 스킬 연결을 덮어쓰지 않는가.
- Rune과 Effect 설명에 남은 구 ID가 잘못된 새 스킬을 가리키지 않는가.
- 보상 후보가 더 이상 홀짝에 의존하지 않으면서 중복 소유 스킬은 계속 제외되는가.
- 제거된 강화 MonoBehaviour 참조가 씬·프리팹에 Missing Script로 남지 않는가.
- Ability 기본 장착 ID와 캐릭터 데이터가 새 `01~10` 범위를 사용하는가.

---

### Task 1: 단일 스킬 보상 규칙 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/CharacterSkillRewardCandidateTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRewardRoller.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRarityUtility.cs`

**Interfaces:**
- Produces: `SkillRewardRoller.GetCandidates(allSkills, rarity, partyCharacterIds)`

- [ ] 새 연속 ID의 Core 후보가 홀짝과 무관하게 포함되는 실패 테스트를 작성한다.
- [ ] 현재 구현에서 실패하는지 확인한다.
- [ ] `baseOnly` 매개변수와 홀짝 필터를 제거한다.
- [ ] 관련 후보 테스트를 통과시킨다.

### Task 2: 엑셀 원본과 런타임 데이터 변환

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Modify: `Assets/DB/*.asset` 중 SkillId 참조 데이터
- Modify: SkillId를 직접 참조하는 씬·코드·테스트

**Interfaces:**
- Consumes: Core `02n -> n`, Ability `02n -> n` 매핑
- Produces: Core `01~40`, Ability `01~10` 단일 데이터 집합

- [ ] 변환 전 데이터에 대해 최종 개수·연속 ID·Level·이름 규칙이 실패하는 검증을 실행한다.
- [ ] 짝수 행만 보존해 ID, Level, 이름과 직접 참조를 변환한다.
- [ ] 엑셀에서 런타임 CSV를 다시 생성한다.
- [ ] 변환 후 데이터 및 참조 무결성 검증을 통과시킨다.

### Task 3: 모든 보상 경로의 홀짝·짝 변형 제거

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardSkillPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/RelicChoiceAreaUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/*`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/*`
- Modify: `Assets/Project/Editor/SkillRewardPoolDatabaseEditor.cs`

**Interfaces:**
- Consumes: Task 1의 단일 후보 규칙
- Produces: 대응 변형 없이 단일 ID를 차단·선택하는 보상 흐름

- [ ] 보상 중복 차단 테스트를 단일 ID 규칙으로 갱신하고 실패를 확인한다.
- [ ] `IsBaseSkillVariant`, `IsUpgradeSkillVariant`, `TryGetPairedVariantId` 호출을 제거한다.
- [ ] 후보와 중복 차단 테스트를 통과시킨다.

### Task 4: 강화 UI·서비스 비활성화와 각성 이벤트 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRarityUtility.cs`
- Modify: `Assets/ExcelSource/GameData.xlsx`의 Event 및 Map 시트

**Interfaces:**
- Produces: 강화 진입점·후보·각성 이벤트가 없는 로비·전투 런타임

- [ ] 공용 강화 정책이 항상 강화 불가·짝 없음으로 판정하는 테스트를 추가한다.
- [ ] 로비 진입 버튼을 숨기고 스킬 각성 이벤트 및 맵 항목을 제거한다.
- [ ] 직렬화 호환 껍데기가 새 스킬을 변경하지 않는지 확인한다.
- [ ] C# 프로젝트 빌드로 잔여 참조가 없음을 확인한다.

### Task 5: 최종 검증

**Files:**
- Modify: `Assets/Tests/EditMode~/`의 관련 회귀 테스트

**Interfaces:**
- Consumes: Tasks 1~4 전체 결과

- [ ] 전체 정적 데이터 검증을 실행한다.
- [ ] Assembly-CSharp 및 Assembly-CSharp-Editor를 빌드한다.
- [ ] 관련 EditMode 테스트를 Unity 에디터에서 실행 가능한 상태로 유지하고, batchmode 미실행 사실을 기록한다.
- [ ] 강화 관련 코드·데이터·씬 참조 잔존 검색 결과를 확인한다.
