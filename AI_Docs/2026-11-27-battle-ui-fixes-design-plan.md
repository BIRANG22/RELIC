# 전투·기록서·옵션 7개 수정 설계 및 구현 계획

사용자 승인: 2026-11-27 `진행해`. 현재 작업 공간에서 기존 변경을 보존한다.

## 목표와 설계

1. 아라벨라 강습은 캐릭터 점유 칸 대신 주변 8칸 중 유효한 빈칸을 선택한다. 점유/장애물/전체 점유 크기를 검사하고 탈출 여유, 넉백 가능성, 후속 공격 가능성을 평가한다. 예약과 실행에서 같은 정책을 사용하고 실행 시 막혔으면 주변 후보를 재선정한다. 후보가 없으면 이동과 타격을 안전하게 취소한다. 연출이 착지 판정을 결정하지 않도록 먼저 결정한다.
2. 녹턴은 대상의 뒤쪽을 우선하고 이동 불가 시 앞쪽으로 이동한다. 끌어오기 예측 경로에도 동일하게 적용한다. 양쪽 불가 시 다른 대상으로 넘어간다.
3. 타임라인 행동 호버는 이름과 설명만 표시한다. 비용/행동 종류/범위/대상 수/범위 아이콘은 표시하지 않는다.
4. 기록서 몬스터 탭의 고정 문자열을 번역 키로 연결하고 언어 변경을 반영한다.
5. 실제 언어는 유지되지만 옵션 재개방 시 Korean으로 표시되는 문제를 수정한다. 표시 소유권, 로컬라이제이션 컴포넌트, 활성화 순서를 조사하고 현재 SelectedLocale에서 표시한다. 패널 열기 자체가 실제 언어를 변경하지 않게 한다.
6. 몬스터 발견 기록을 입장 체크포인트와 별개로 보존한다. 기존 전투 재개 위치/HP 등을 덮어쓰지 않고 최신 발견 ID만 병합한다. 최초 조우 저장과 재시작 로드 경로를 검증한다.
7. Battle 씬의 소모품 슬롯 네 개는 같은 정사각형 모양으로 2×2 배치한다. 이미 60×60인 루트뿐 아니라 배경/테두리/아이콘의 자식 RectTransform을 확인한다.

## 구현 단위

- [x] 이동: EliseAI, BattleActionRunner, 이동 정책 및 NocturnAI. 경계/점유/막힘/후속 이동/끌어오기 예측에 대한 회귀 테스트.
- [x] UI: OptionPanelUI, 관련 로컬라이제이션 및 기록서 표시, TimelineSkillHoverPopupUI/PreviewEntry, Battle.unity 자식 배치. 표시 재활성화 경로와 데이터 연결 확인(실제 화면 미검증).
- [x] 저장: RecordDiscoveryService, MonsterUnit, SaveSystem 필요한 경로만 변경. 체크포인트와 발견 이력 분리, 최초 조우 저장, 파일 삭제/복구 정책 검증(실제 Unity JSON 왕복 미검증).
- [x] 통합: C# 컴파일, 가능한 독립 테스트, git diff --check 및 YAML 식별자/레이아웃 검증.

## 제약과 검증

문서는 AI_Docs 안에만, 테스트는 Assets/Tests/EditMode~/ 또는 Assets/Tests/PlayMode~/ 안에만 작성한다. Unity 에디터 직접 제어와 batchmode는 사용하지 않는다. 실행 불가능한 Unity 테스트/화면 검증은 미검증으로 명시한다. 커밋/Push/PR/브랜치/worktree 작업은 하지 않는다.

전투 정책은 안정적 ID/격자 값과 결정적 순서로 판단한다. 무작위 선택이 필요하면 BattleRandom만 사용한다. UI는 전투 상태를 계산하지 않는다. 새 네트워크 프레임워크를 추가하지 않는다.

## 진행 및 판단 기록

- 조사 완료: 강습은 캐릭터 칸 착지 후 밀어내기, 녹턴 뒤쪽 전용 후보, Monster 고정 문자열, 옵션 언어의 복수 저장 키와 표시 갱신 경로, 입장 체크포인트 복사 저장을 확인했다.
- 언어 문제는 저장 실패로 단정하지 않고 현재 언어 표시의 덮어쓰기부터 검증한다.
- 옵션 프리팹의 정적 한국어 번역 writer와 동적 표시의 충돌을 제거했다. 재활성화 시 SelectedLocale에서 표시하고 패널 열기로 실제 언어를 변경하지 않는다. 구형 드롭다운도 현재 선택값을 다시 반영한다.
- 최초 몬스터 조우 ID는 `relic-monster-discoveries.json`에 기존 원자 저장/백업 함수를 사용하여 보존하고, 저장 로드/입장 스냅샷에 합집합으로 병합한다. 탐사 포기는 발견 파일을 보존하고 디버그 전체 초기화만 함께 삭제한다. 실제 사용자 저장 파일은 검증 과정에서 건드리지 않았다.
- 리뷰에서 강제이동 면역 장비 판정 누락을 보완했다. 예약 착지점이 여전히 안전하다면 그대로 실행하여 회피한 캐릭터를 추적하지 않는다. 점유/차단/탈출 불가일 때만 재선정한다.

## 변경 파일

프로젝트 스크립트 기준 경로는 `Assets/Project/Scripts/`이다. 중단 중 사용자가 기존 변경을 커밋했으므로 현재 git diff만으로는 이번 작업 전체 변경 목록이 표시되지 않는다. 에이전트는 커밋/Push/PR을 수행하지 않았다.

- 전투: `Gameplay/Monster/Ai/EliseAI.cs`, `NocturnAI.cs`, `ArabellaLeapLandingPolicy.cs`, `ArabellaLeapLandingResolver.cs`, `NocturnPortalLandingPolicy.cs` 및 신규 스크립트 meta, `Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- 기록 저장: `Core/SaveSystem.cs`, `RecordDiscoveryService.cs`, `Gameplay/Monster/MonsterUnit.cs`, `DebugProgressResetHotkey.cs`
- 언어/기록 UI: `Core/Bootstrap.cs`, `Core/Localization/GameLocalization.cs`, `UI/Title/OptionPanelUI.cs`, `UI/Title/LanguageDropdownUI.cs`, `UI/Title/LanguageButtonController.cs`, `RecordPanelUI.cs`
- 타임라인: `Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`, `UI/TimelineSkillHoverPopupUI.cs`, `UI/TimelineSkillHoverPopupView.cs`
- 자산: `Assets/Project/PrefabsR/Option.prefab`, `Record.prefab`, `Assets/Project/Scenes/YDM/Battle.unity`, `Assets/Language/Text Shared Data.asset` 및 ko/en/ja/es/zh-Hans/zh-Hant 번역 테이블
- 테스트: `Assets/Tests/EditMode~/ArabellaLeapLandingPolicy.Tests.ps1`, `NocturnPortalLandingPolicyTests.cs`, `SaveSystemRecoveryTests.cs`, `TimelineSkillHoverPresentationTests.cs`, `Invoke-BattleFixChecks.ps1`

## 최종 검증 결과

- Unity 6000.0.68f1 번들 Roslyn과 Bee 응답 파일을 사용하여 전체 `Assembly-CSharp` 독립 컴파일: 종료 코드 0.
- 관련 NUnit 회귀 테스트 C# 컴파일: 종료 코드 0.
- `Assets/Tests/EditMode~/Invoke-BattleFixChecks.ps1`: 녹턴 13개, 저장 파일 쓰기/복구/탐사 포기 보존/전체 초기화 6개, 타임라인 소유자 표시 7개, 총 26개 통과.
- `Assets/Tests/EditMode~/ArabellaLeapLandingPolicy.Tests.ps1`: 주변 착지, 막힌 예약, 후보 없음, 갇힘, 맵 경계, 전체 점유 크기, 결정성, 후속 공격, 구속, 예고 유지/회피 등 13개 통과.
- 기존 `RecordMonsterDiscoveryTests.Normalize_RemovesBlankAndCaseInsensitiveDuplicates` 1개 실패: 기존 서비스/DB는 Ordinal ID 비교인데 테스트가 대소문자 무시 중복 제거를 기대한다. 수정 전에도 같은 실패를 확인했으며 관련 없는 ID 의미 변경은 하지 않았다.
- Battle/Option/Record YAML 오브젝트 ID 중복 없음. 소모품 4개의 루트/배경/아이콘 정사각형 및 중앙 정렬, 몬스터 탭 6개 언어 번역 항목 확인.
- `git diff --check`: 통과.

## 미검증과 멀티플레이 영향

Unity 에디터 조작과 batchmode는 실행하지 않았다. 실제 전투 연출/재시작 후 도감 표시/옵션 재개방/해상도별 UI와 Unity JsonUtility 파일 왕복, Unity 전체 테스트 스위트 및 멀티플레이 실접속은 미검증이다.

AI는 격자 스냅샷 기반 결정적 정책으로 착지/방향을 계산한다. 기존 예약 명령과 BattleRandom 체계를 유지하며 네트워크 스키마나 프레임워크는 추가하지 않았다. 발견 이력과 UI 변경은 로컬 표시/저장에 한정한다.
