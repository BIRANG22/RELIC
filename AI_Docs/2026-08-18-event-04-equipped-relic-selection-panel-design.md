# Event_04 장착 유물 선택 패널 설계

## 배경

`Event_04`의 3번 선택지는 장착 중인 유물 하나를 삭제하고 새로운 랜덤 유물을 얻는다. 기존 구현은 이벤트 선택지 슬롯을 재사용해 장착 유물 목록을 페이지처럼 보여주므로, 파티 전체 장착 유물을 한 화면에서 비교하고 고르기 어렵다.

## 설계

- 장착 유물 비용 선택은 `EventEquippedRelicSelectionPanelUI` 전용 모달 패널로 분리한다.
- 패널은 `EventChoiceEquippedRelicCost` 목록을 받아 모든 장착 유물을 한 번에 생성한다.
- 각 항목은 캐릭터명, 슬롯명, 유물명, 유물 아이콘을 표시한다.
- 항목 클릭 시 패널은 닫히고 선택한 `CharacterId`, `RelicSlotIndex`, `RelicId`가 기존 `EventChoiceExecutionService` 실행 컨텍스트로 전달된다.
- 취소 버튼 또는 진행 버튼은 패널을 닫고 기존 이벤트 선택지 화면으로 복귀한다.
- 패널은 씬에 수동 배치된 인스턴스가 있으면 재사용하고, 없으면 런타임에 생성한다.

## 검증

- 패널이 전달받은 장착 유물 옵션 수만큼 항목을 표시하는 테스트.
- 항목 선택 시 선택 콜백이 호출되고 패널이 닫히는 테스트.
- 취소 시 취소 콜백이 호출되고 패널이 닫히는 테스트.
- `Assembly-CSharp`, `Assembly-CSharp-Editor` 빌드 및 대상 파일 `git diff --check`.
