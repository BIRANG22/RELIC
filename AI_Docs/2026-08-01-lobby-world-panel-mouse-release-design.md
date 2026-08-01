# 로비 월드 패널 마우스 릴리스 입력 설계

## 목표

- `RelicShopPanel`, `ErosionSelectPanel`, `CultureTankPanel`을 마우스 버튼을 누르는 순간 열지 않는다.
- 동일한 월드 콜라이더에서 누르고 놓은 정상 클릭일 때만 패널과 카메라 이동을 실행한다.
- 카메라 드래그 도중 포인터가 클릭 오브젝트를 벗어나면 패널을 열지 않는다.

## 원인 및 변경

- 세 패널의 월드 오브젝트는 공통 `PanelCameraMover`를 사용하며, 이 컴포넌트가 `OnMouseDown`에서 패널을 열고 있다.
- `PanelCameraMover`의 콜라이더 진입점을 `OnMouseUpAsButton`으로 변경한다.
- 침식도 전용 `LobbyErosionMirrorButton`도 일반 `OnMouseUp` 대신 `OnMouseUpAsButton`을 사용한다.
- 유물상점과 배양조 전용 입력은 이미 `OnMouseUpAsButton`이므로 유지한다.

## 영향 범위

- 패널 열기, 사운드, 카메라 이동 및 입력 차단 로직은 변경하지 않는다.
- 전투 상태 및 멀티플레이 동기화 데이터에는 영향을 주지 않는다.

