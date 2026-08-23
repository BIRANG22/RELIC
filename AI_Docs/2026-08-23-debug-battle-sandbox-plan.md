# Debug Battle Sandbox Implementation Plan

## Goal

`DebugBattle` 씬을 아군 1명과 허수아비 1명을 기준으로 스킬, 룬, 유물, 컴파운드 효과를 빠르게 검증할 수 있는 공간으로 정리한다.

## Tasks

1. 테스트 추가
   - `Assets/Tests/EditMode~/DebugBattleSandboxTests.cs`
   - 기본 파티 크기 1명, 캐릭터 교체 API, 장비 ID 장착 분리, 리사이즈 최소 크기 클램프를 검증한다.

2. 디버그 파티 구성 API 정리
   - `DebugBattlePartySetup`에 기본 1인 파티 생성과 지정 캐릭터 1인 파티 생성 API를 추가한다.
   - 기존 3인 기본 파티 생성 호출부는 새 기본 정책을 따르게 한다.

3. 디버그 데이터 프로바이더 정리
   - `BattleDebugDataProvider` 기본 배열을 1명 기준으로 바꾼다.
   - 지정 캐릭터가 없으면 기본 제공 캐릭터 1명을 사용한다.

4. 디버그 창 확장
   - 창 크기와 스크롤 높이를 런타임 크기에 맞춘다.
   - 우하단 리사이즈 핸들을 추가한다.
   - 캐릭터 교체 입력, 룬/유물/컴파운드 입력 섹션을 분리한다.

5. 씬 연결 확인
   - `DebugBattle.unity`의 디버그 데이터 구성 값이 1인 테스트 구성과 맞는지 확인한다.
   - `Battle.unity`와 `Lobby.unity`는 수정하지 않는다.

6. 검증
   - MSBuild 런타임/에디터 어셈블리 빌드.
   - `git diff -- Assets/Project/Scenes/YDM/Lobby.unity Assets/Project/Scenes/YDM/Battle.unity`로 무변경 확인.
   - Unity batchmode 테스트는 실행하지 않는다.

