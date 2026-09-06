# Event_06 상점 패널 프리팹화 설계

## 목표

Event_06의 1번 선택지(`OpenPanel / 상점`)를 선택하면 RestRoom 상점과 같은 상품 구매 패널을 EventRoom 안에서 열 수 있게 한다. 기존 RestRoom 상점 패널도 같은 프리팹 인스턴스를 사용하도록 바꾼다.

## 조사 결과

- `GameDataRuntime.csv`의 Event_06 1번 선택지는 이미 `ResultType=OpenPanel`, `ResultTarget=상점`으로 구성되어 있다.
- `EventRoomController`는 `OpenShop` 실행 경로를 가지고 있지만, 현재는 씬 전체에서 첫 번째 `RestRoomShopPanel`을 찾아 `Open()`을 호출한다.
- RestRoom의 `ShopPanel`은 `Battle.unity` 씬에 직접 배치되어 있고 프리팹 인스턴스가 아니다.
- 기존 RestRoom 상점은 `Npc_shop`의 `UIPanelButton.MovePanel()`로 `ShopPanel`의 anchored position을 `y=1100`에서 `y=0`으로 이동시키는 방식이다.
- 기존 `RestRoomShopPanel.Open()`은 활성화만 수행하므로 EventRoom에서 직접 호출하면 패널 위치가 열림 위치로 보장되지 않는다.

## 권장 구조

- `RestRoomShopPanel`이 자체적으로 열림/닫힘 위치를 적용할 수 있게 보강한다.
  - 열림 위치: `(0, 0)`
  - 닫힘 위치: `(0, 1100)`
  - 기본 닫기 동작은 오브젝트 비활성화가 아니라 닫힘 위치 이동으로 처리한다.
- `EventRoomController`에 EventRoom 전용 `RestRoomShopPanel` 직렬화 필드를 추가한다.
  - 필드가 지정되어 있으면 그 패널을 연다.
  - 누락 시 EventRoom 하위에서 먼저 찾고, 마지막으로 기존 전체 씬 검색을 fallback으로 둔다.
- 기존 `ShopPanel` UI 구조를 기반으로 `Assets/Project/PrefabsR/RestRoom/ShopPanel.prefab`을 만든다.
  - 상품 생성은 기존 `Goods.prefab`을 계속 참조한다.
  - 뒤로가기 버튼은 패널 내부의 `RestRoomShopPanel.Close()`를 호출하도록 바꿔 EventRoom에서도 독립적으로 닫힌다.
- `Assets/Project/Scenes/YDM/Battle.unity`에 ShopPanel 프리팹 인스턴스를 RestRoom과 EventRoom 각각 하나씩 배치한다.
  - RestRoomController는 RestRoom 인스턴스를 참조한다.
  - EventRoomController는 EventRoom 인스턴스를 참조한다.

## 구현 계획

1. EditMode 테스트를 추가해 ShopPanel 프리팹 존재, 프리팹 컴포넌트 구성, Battle 씬의 RestRoom/EventRoom 참조를 검증한다.
2. `RestRoomShopPanel`에 위치 기반 `Open()`/`Close()` 동작을 추가한다.
3. `EventRoomController`가 EventRoom 전용 상점 패널을 우선 사용하도록 변경한다.
4. 기존 씬 배치 `ShopPanel` 구조에서 프리팹을 생성하고, Battle 씬에 RestRoom/EventRoom용 배치를 반영한다.
5. MSBuild로 런타임/에디터 컴파일을 검증한다.

## 멀티플레이 경계

이번 변경은 Event_06 선택 결과가 상점 UI를 여는 경로와 씬/프리팹 배치에 한정한다. 상품 구매 시 재화 차감과 보상 장착은 기존 `RestRoomShopPanel`/`RestRoomShopService` 흐름을 그대로 사용하며, 새 전투 판정 로직이나 랜덤 처리는 추가하지 않는다.
