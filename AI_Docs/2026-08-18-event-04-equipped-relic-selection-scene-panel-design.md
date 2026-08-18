# Event_04 장착 유물 삭제 패널 씬 배치 전환 설계

## 목표

Event_04의 3번 선택지에서 장착 유물 삭제 목록을 런타임 생성 패널이 아닌 씬에 배치된 패널로 표시한다.

## 현재 상태

- `EventRoomController`가 장착 유물 비용 선택 시 `EventEquippedRelicSelectionPanelUI`를 찾지 못하면 런타임으로 패널 오브젝트를 만든다.
- 패널 컴포넌트가 `EventRoomController.cs` 안에 같이 있어 씬에 직접 배치하기 어렵다.
- 삭제 자체는 `EventChoiceExecutionService`와 선택된 `CharacterId`, `RelicSlotIndex`, `RelicId`를 통해 기존 상태 변경 흐름에서 처리된다.

## 설계

- `EventEquippedRelicSelectionPanelUI`와 표시 엔트리 타입을 별도 스크립트 파일로 분리한다.
- 패널 루트, 콘텐츠 루트, 닫기 버튼, 빈 목록 텍스트, 항목 템플릿은 씬에 배치하고 serialized 참조로 연결한다.
- 패널 UI는 씬에 있는 템플릿을 복제해 목록 항목만 생성한다. 패널 구조 자체는 생성하지 않는다.
- `EventRoomController`는 패널을 찾기만 하고, 없으면 오류 메시지를 표시한 뒤 기존 이벤트 선택지로 복귀한다.
- 선택 결과는 계속 `OnEquippedRelicCostSelected`를 거쳐 기존 이벤트 실행 서비스로 전달한다.

## 멀티플레이 경계

패널은 선택 UI만 담당하고 전투 상태를 직접 수정하지 않는다. 실제 장착 유물 삭제와 보상 지급은 기존 이벤트 선택 실행 경로를 유지한다.
