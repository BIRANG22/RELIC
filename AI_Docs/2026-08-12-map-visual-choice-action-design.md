# Map Visual Choice Action Design

## 목표

이벤트 선택지 결과에 따라 맵 비주얼 오브젝트가 애니메이션, VFX, 색상/스케일 변화 같은 연출을 재생할 수 있게 한다.

## 권장 구조

- 이벤트 선택지 데이터는 전투/이벤트 결과 계산을 계속 담당하고, 비주얼에는 `VisualObjectId`와 `ActionId` 신호만 보낸다.
- `MapVisualController`는 `MapId`로 생성한 비주얼 프리팹 안의 `MapVisualActor`를 등록한다.
- `MapVisualActor`는 인스펙터에 설정된 `ActionId`별 연출을 재생한다.
- 성공/실패가 나뉘는 선택지는 `SuccessVisualObjectId/SuccessVisualActionId`, `FailureVisualObjectId/FailureVisualActionId`를 따로 사용한다.

## 데이터 흐름

1. 맵 노드 진입 시 `MapVisualController.ApplyMapVisual(mapId)`가 실행된다.
2. 컨트롤러가 DB에 등록된 프리팹을 생성하고, 프리팹의 `MapVisualActor`를 `VisualObjectId`로 등록한다.
3. 선택지 클릭 시 `EventChoiceExecutionService.Execute()`가 기존 결과 계산을 수행한다.
4. 실행 결과에 비주얼 신호가 있으면 `EventRoomController`가 현재 룸의 `MapVisualController.TryPlayAction()`을 호출한다.
5. 액터가 해당 `ActionId`에 연결된 애니메이터 트리거, VFX 프리팹 생성, 색상, 스케일, 활성 상태 변화를 적용한다.

## 테스트 샘플

`Assets/Project/Data/MapVisual/Test.png`를 사용하는 테스트 프리팹을 만들고 `Map_09`에 연결한다.
샘플 액션은 `event_choice_success`이며, 선택지 결과 신호가 들어오면 이미지가 강조 색으로 바뀌고 스케일이 커지는 방식으로 확인한다.

## 멀티플레이 경계

선택지 결과 계산은 기존 `EventChoiceExecutionService`에 남긴다.
비주얼 신호는 결과 이벤트의 연출용 부가 정보이며, UI/VFX가 전투 또는 이벤트 결과를 결정하지 않는다.
