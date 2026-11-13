# 이벤트 선택지 정렬 및 전투 입장 사운드 복구 설계

## 목적

- 이벤트 선택지가 2개일 때 두 슬롯의 중간이 화면 중앙에 오도록 기존 동작을 복구한다.
- 전투방 입장 시 존재하지 않는 `event..09.Skylight`를 재생하려는 경고와 의도하지 않은 자동 재생을 제거한다.

## 원인

- `9698a14f5`에서 Battle 씬의 이벤트 선택지 슬롯 3개에 `hoverButtonMoveOffset.y = 5`가 들어갔다. 슬롯 자체가 `HorizontalLayoutGroup`의 자식이어서 `ButtonAnimationCoroutine`이 저장된 3칸 좌표를 다시 쓰며 2개 중앙 정렬을 덮어쓴다.
- `DBAudioSource`의 기본 `soundId`가 존재하지 않는 `event..09.Skylight`이고, Battle/DebugBattle의 Char01~03에 직렬화돼 있다. 이 중 Char03만 `playOnEnable = true`라 전투 씬 진입 시 조회된다.

## 접근 방식

1. 선택지 컨테이너를 코드에서 강제로 재배치: 현상은 막지만 최신 머지에서 생긴 잘못된 슬롯 애니메이션 설정을 남긴다.
2. 선택지 슬롯의 머지 전 설정 복구: 레이아웃이 소유하는 RectTransform의 위치 애니메이션을 끄고 기존 확대 호버를 복구한다. 변경 범위가 가장 작아 이 방식을 사용한다.
3. 사운드 ID를 `event.01.Skylight`로 교정: 경고는 사라지지만 캐릭터 슬롯 활성화 때 이벤트 천창음을 재생하므로 의도와 맞지 않는다.
4. 잘못된 기본 ID를 비우고 Char03 자동 재생을 끄기: 의도하지 않은 전투 입장 SFX를 제거하고 신규 컴포넌트 재발도 막는다. 이 방식을 사용한다.

## 변경 범위

- Battle 씬 이벤트 선택지 3개의 호버 이동값을 0, 호버 배율을 1.1로 복구한다.
- Battle/DebugBattle의 Char01~03 `DBAudioSource.soundId` 오타를 빈 값으로 정리하고 Char03 `playOnEnable`을 끈다.
- `DBAudioSource`의 신규 컴포넌트 기본 `soundId`를 빈 값으로 변경한다.
- 씬 YAML과 기본값을 검사하는 EditMode 회귀 테스트를 추가한다.

## 비목표

- 다른 버튼 호버 연출, 이벤트 전용 사운드, SoundDatabase 항목은 변경하지 않는다.
- 전투 로직이나 상태 변경 흐름은 변경하지 않는다.

