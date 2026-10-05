# TutorialPanel 5페이지 가이드 설계

## 목적

데모 타이틀에서 새 튜토리얼 전투로 진입할 때 `TutorialPanel`을 한 번 표시하고, 5개 페이지의 이미지와 로컬라이즈된 설명을 좌우 버튼으로 탐색한다.

## 구조

- `TutorialPanelController`가 현재 페이지, 이미지, 설명 텍스트, 버튼 상태를 관리한다.
- 페이지 수는 5개로 고정한다.
- 이미지 5개는 프리팹 인스펙터의 `Page Images` 배열에 직접 연결한다.
- 설명은 `tutorial.battle_guide.01`부터 `tutorial.battle_guide.05`까지의 키를 사용한다.
- 첫 페이지에서 Left를 비활성화하고 마지막 페이지에서 Right를 비활성화한다.
- Check 버튼은 마지막 페이지에서만 표시하며, 누르면 패널을 닫는다.
- `Indicator_01~05`의 `Back` 오브젝트는 컨트롤러에 명시적으로 연결하고, 현재 페이지와 같은 인덱스의 `Back`만 활성화한다.
- Battle 씬은 진입 출처가 설정한 `BattleRuntimeData.IsDemoBattle`을 직접 확인한다. DemoTitle의 `TitleDemoBattleButton`이 `DemoBattlePartySetup`을 통해 이 값을 `true`로 설정하며, 기본 Title에서 Lobby를 거친 일반 전투는 `false`이므로 동일한 Battle 씬에서도 진입 경로를 구분할 수 있다.
- 패널은 방 전환 때 비활성화되는 `BattleRoom/BattleHUDCanvas`가 아니라 항상 활성 상태인 Battle 씬 최상위 UI Canvas 아래에 생성한다.
- DemoTitle에서 준비된 튜토리얼 전투에만 생성하며 Lobby를 거친 일반 전투에는 생성하지 않는다.

## 전투 및 멀티플레이 경계

페이지 UI는 전투 결과를 계산하거나 변경하지 않는다. 기존 데모 전투 구분값을 읽기만 하며 전투 Command, State, Result 흐름 및 네트워크 전투 상태에는 영향을 주지 않는다.
