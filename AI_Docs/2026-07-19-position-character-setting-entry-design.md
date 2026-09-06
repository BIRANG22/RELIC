# 거점 캐릭터 세팅 진입 설계

## 목적

거점의 `Position` 자식 오브젝트 `Cha1`, `Cha2`, `Cha3`을 클릭하면 캐릭터 세팅 상태로 이동한다.

## 동작

- `Cha1`, `Cha2`, `Cha3`은 모두 동일한 진입 동작을 수행한다.
- 클릭 시 `LobbyViewStateController.ShowCharacterSelection()`을 호출한다.
- 거점 캐릭터 버튼은 파티 슬롯 인덱스나 캐릭터 ID를 저장·선택하지 않는다.
- 기존 `PartySlotButton`의 `CharacterSelectionState.SelectPartySlot()` 흐름은 사용하지 않는다.
- 캐릭터 세팅 상태에서 복귀할 때는 `Lobby`가 아니라 `Position` 상태로 돌아온다.

## 구조

`PositionCharacterSettingButton` 컴포넌트를 추가한다.

- 책임: 클릭 입력을 받아 캐릭터 세팅 화면으로 전환
- 참조: `LobbyViewStateController`
- 전투·파티 런타임 데이터는 변경하지 않는다.
- 컨트롤러 참조가 없으면 경고만 출력하고 상태를 변경하지 않는다.

`Cha1`, `Cha2`, `Cha3`에는 동일한 컴포넌트를 연결한다. 각 오브젝트의 `Button.onClick`은 컴포넌트의 공개 실행 함수를 호출한다.

## 기존 흐름 변경

캐릭터 세팅 화면의 복귀 동작은 `LobbyViewStateController.ShowPosition()`을 호출하도록 변경한다. 현재 로비 화면의 다른 기능은 이번 작업에서 제거하거나 옮기지 않는다.

## 검증

- `Cha1`, `Cha2`, `Cha3` 각각을 클릭했을 때 캐릭터 세팅 상태가 열린다.
- 어느 버튼을 클릭해도 `CurrentPartySlotIndex`가 변경되지 않는다.
- 캐릭터 세팅 화면에서 복귀하면 거점 상태가 열린다.
- 전투 및 파티 런타임 데이터에는 변화가 없다.

## 멀티플레이 경계

이번 변경은 로비 화면 전환만 담당한다. 캐릭터, 파티, 전투 상태를 직접 수정하지 않으므로 멀티플레이 동기화 대상에는 영향을 주지 않는다.
