# 로비 유물 구매 화면 공간 이동 효과 설계

## 목표

유물 상점에서 구매가 성공했을 때, 유물 아이콘에서 로비 UI `Lobby_Icon/Icon_04`(Storage)까지 희귀도 색상의 빛 구체가 포물선 궤적을 남기며 이동한다. 로비 모달의 블러·암전 위에서도 선명하게 보이며, 다른 UI 전송 연출에도 재사용할 수 있어야 한다.

## 조사 결과

기존 `LobbyRelicShopPresenter`는 `RawImage`를 런타임 생성하여 유물 슬롯에서 UI Equip 버튼으로 이동시키는 전용 연출을 보유한다. 이 방식은 월드 목표점과 연결되지 않고, 월드 공간 VFX로 바꾸면 상점 모달의 블러·암전에 가려진다.

## 설계

- `ScreenSpaceTransferOrbEffect` 프리팹은 `PositionPanel`의 World Space Canvas 자식이 아닌 독립 루트 Overlay Canvas로 생성한다. 로비 블러/상단 UI보다 높은 정렬 순서에서 구체와 잔상을 렌더링한다.
- `Play(Vector2 startScreen, Vector2 endScreen, Color color)` API는 UI 참조가 아닌 확정된 화면 좌표만 받는다.
- `LobbyRelicShopPresenter`는 재생 직전에 각 UI RectTransform 중심을 화면 좌표로 한 번 변환한다. 시작점은 유물 아이콘의 가장 가까운 Overlay Canvas 기준, 도착점은 `Lobby_Icon/Icon_04`의 가장 가까운 World Space Canvas와 `Camera.main` 기준으로 계산한다.
- 이후 효과는 UI, Canvas, 패널 활성 상태를 다시 참조하지 않는다. 따라서 구매 중 상점이나 `BackgroundPanel`의 활성 상태가 변해도 이동 좌표가 화면 밖으로 바뀌지 않는다.
- 화면 공간에서 제어점을 위쪽으로 올린 2차 베지어를 사용해 포물선을 만든다.
- `LobbyRelicShopPresenter`는 구매 서비스가 성공한 뒤에만 프리팹을 생성하고, 선택된 버튼의 아이콘과 `Lobby_Icon` UI 루트를 전달한다.
- 희귀도 색상은 기존 `LobbyRelicOfferButtonUI.CurrentRarityColor`를 구체와 잔상에 전달한다.

## 경계와 멀티플레이

이 컴포넌트는 구매 결과를 계산하거나 저장하지 않는 프레젠테이션 전용 코드다. 구매 서비스의 Command/State Change/Result 흐름과 동기화 스냅샷에는 변경을 가하지 않는다.
