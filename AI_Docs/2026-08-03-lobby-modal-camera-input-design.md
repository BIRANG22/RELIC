# 로비 위치 패널 카메라 및 입력 차단 설계

## 문제

- `PanelCameraMover`의 카메라 이동은 월드 콜라이더의 `OnMouseUpAsButton()` 호출에 의존한다.
- `LobbyPanelTransitionButton.Execute()` 또는 Presenter가 패널을 직접 활성화하면 카메라 이동이 시작되지 않는다.
- `PanelCameraMover.DetectPanelState()`는 패널 닫힘만 감지하고 열림은 감지하지 않는다.
- `LobbyPanelTransitionButton.Execute()`는 이미 열린 위치 모달을 검사하지 않아 UI Button의 직접 호출로 다른 패널을 열 수 있다.
- 패널 루트에는 Canvas 전체를 덮는 활성 Raycast 차단 Graphic이 없다.

## 권장 설계

`PanelCameraMover`를 대상 패널의 카메라 연출과 모달 입력 차단 수명 주기의 공통 소유자로 사용한다.

1. 대상 패널이 외부 코드에서 비활성→활성으로 변경되면 `DetectPanelState()`가 이를 감지하여 기존 `OpenPanel()` 카메라 이동을 한 번 시작한다.
2. 패널이 열릴 때 같은 Canvas 부모 아래에 투명한 전체 크기 `Image`를 만들고 대상 패널 바로 뒤에 배치한다. 이 Image는 `raycastTarget`을 활성화하여 뒤 UI 입력을 소비한다.
3. `LobbyPositionModalInputBlocker.IsBlocked`는 기존 owner뿐 아니라 활성 `PanelCameraMover` 대상 패널도 확인한다. Mover가 owner를 차지하지 않으므로 `CultureTank` 같은 열린 패널 내부 상호작용의 `IsBlockedByAnother` 판정과 충돌하지 않는다.
4. 패널이 닫히거나 Mover가 비활성화·파괴되면 차단막을 정리한다.
5. `LobbyPanelTransitionButton.Execute()`는 `PanelCameraMover`가 관리하는 다른 활성 패널이 있으면 새 패널 열기를 거부한다. `panelToOpen`이 없는 닫기 동작은 허용한다.

## 범위

- 대상 패널: `ErosionSelectPanel`, `RelicShopPanel`, `CultureTankPanel`
- 전투 상태나 전투 결과 로직은 변경하지 않는다.
- 씬 오브젝트의 수동 UnityEvent 연결은 추가하지 않는다.

## 검증

- 외부 `SetActive(true)` 후 카메라가 목표 위치로 이동하는지 확인한다.
- 패널 열림 시 Canvas 전체 차단막이 패널 바로 뒤에 생성되는지 확인한다.
- 패널 닫힘 시 차단막이 비활성화되는지 확인한다.
- 패널 활성 동안 기존 월드 입력 차단 상태가 유지되고 닫힘과 함께 해제되는지 확인한다.
- 카메라 복귀 중 재오픈하거나 Mover를 다시 활성화해도 줌과 차단막이 복구되는지 확인한다.
- 한 위치 패널이 열린 동안 다른 `LobbyPanelTransitionButton.Execute()`가 대상 패널을 열지 않는지 확인한다.
