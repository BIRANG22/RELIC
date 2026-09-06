+# 런타임 데이터 및 툴팁 전체 Localization 구현 계획

> **For agentic workers:** 승인된 현재 작업 공간에서 TDD 순서로 구현한다.

**Goal:** GameData 전체 표시 데이터와 Title/Lobby/Battle 런타임 UI 문구를 5개 언어 Localization에 연결한다.

**Architecture:** 게임 데이터는 안정 ID와 한국어 fallback을 유지하고, UI 표현 계층에서 `GameLocalization`의 데이터 키 및 문장 포맷 API를 호출한다. Localization.xlsx가 번역 단일 원본이며 Unity 테이블은 단방향으로 갱신한다.

**Tech Stack:** Unity 6, Unity Localization 1.5.11, C#, TMP, OpenXML xlsx pipeline

## 전역 제약

- 로그·디버그·내부 ID는 번역 대상에서 제외한다.
- 문서와 계획은 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 둔다.
- Unity batchmode를 사용하지 않는다.
- 번역 문자열은 전투 상태와 네트워크 스냅샷에 저장하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: 런타임 Localization API

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/GameLocalization.cs`
- Test: `Assets/Tests/EditMode~/GameLocalizationTests.cs`

- [ ] 안정 ID를 snake_case 키 구성요소로 정규화하는 실패 테스트를 작성한다.
- [ ] `GetData(category, stableId, field, fallback)` 실패 테스트를 작성한다.
- [ ] 표준 `{0}` 문장을 안전하게 포맷하는 실패 테스트를 작성한다.
- [ ] 최소 구현 후 컴파일 GREEN을 확인한다.

### Task 2: GameData 표시 문자열 추출 및 번역 병합

**Files:**
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_ko.asset`
- Modify: `Assets/Language/Text_en.asset`
- Modify: `Assets/Language/Text_zh-Hans.asset`
- Modify: `Assets/Language/Text_ja.asset`
- Modify: `Assets/Language/Text_es.asset`
- Test: `Assets/Tests/EditMode~/LocalizationRuntimeDataCoverageTests.cs`

- [ ] GameData의 17개 시트에서 ID와 표시 문자열 컬럼을 추출한다.
- [ ] `data.<sheet>.<id>.<field>` 키 목록을 생성하고 중복을 검사한다.
- [ ] 5개 언어 번역을 작성하고 기존 Localization.xlsx에 병합한다.
- [ ] 빈 번역, 잘못된 인자, 중복 키가 없음을 검사한다.
- [ ] Unity 단방향 importer로 테이블을 갱신한다.

### Task 3: 공용 데이터 formatter 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillTooltipFormatter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRarityUtility.cs`
- Modify: 관련 데이터 표시 helper 및 runtime display-name 메서드
- Test: `Assets/Tests/EditMode~/RuntimeDataLocalizationTests.cs`

- [ ] 스킬 이름·설명·희귀도·효과·범위 번역 실패 테스트를 작성한다.
- [ ] 몬스터·캐릭터·룬·유물·아이템 표시명/설명 실패 테스트를 작성한다.
- [ ] 원본 데이터는 변경하지 않고 반환 시점에 번역하도록 구현한다.
- [ ] fallback과 잘못된 ID 처리를 검증한다.

### Task 4: Lobby 런타임 UI 연결

**Files:**
- Modify: `Setting.cs`, `SkillSettingPanel.cs`, `RuneSettingPanel.cs`, `InfoTooltip.cs`
- Modify: 유물 상점, 캐릭터 선택, 스테이지·침식도 관련 UI
- Test: `Assets/Tests/EditMode~/LobbyRuntimeLocalizationTests.cs`

- [ ] 직접 Name/Desc/Tooltip을 대입하는 경로를 실패 테스트로 고정한다.
- [ ] 캐릭터·스킬·룬·유물·아이템·스테이지 표시를 공용 formatter로 교체한다.
- [ ] Locale 변경 후 열린 패널 refresh를 연결한다.
- [ ] Lobby 대상 직접 원문 대입 감사 결과가 0인지 검증한다.

### Task 5: Battle 런타임 UI 연결

**Files:**
- Modify: 캐릭터/몬스터 정보, 타임라인 툴팁, 보상, 가방, 상점, 이벤트, 상태효과 UI
- Test: `Assets/Tests/EditMode~/BattleRuntimeLocalizationTests.cs`

- [ ] 전투 데이터 Name/Description 직접 대입 경로를 실패 테스트로 고정한다.
- [ ] 스킬·몬스터 패턴·상태효과·보상·이벤트 문구를 번역 API로 교체한다.
- [ ] 수치 조합 문장을 Localization 포맷 키로 교체한다.
- [ ] Battle 대상 직접 원문 대입 감사 결과가 0인지 검증한다.

### Task 6: 최종 감사 및 검증

**Files:**
- Modify: `Assets/Editor/StaticLocalizationMigration.cs` 또는 별도 감사기
- Test: `Assets/Tests/EditMode~/LocalizationRuntimeAuditTests.cs`

- [ ] 5개 언어 키 수와 빈 번역을 검사한다.
- [ ] 코드/씬/프리팹 참조 키 누락을 검사한다.
- [ ] 로그를 제외한 런타임 표시 원문 후보를 출력하고 남은 항목을 분류한다.
- [ ] 전체 MSBuild 컴파일을 수행한다.
- [ ] Unity Test Runner와 5개 Locale 수동 플레이 검증 항목을 기록한다.

