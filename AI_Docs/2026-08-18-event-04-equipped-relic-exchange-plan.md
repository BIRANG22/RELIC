# Event_04 장착 유물 등가 교환 구현 계획

1. `EventChoiceExecutionServiceTests`에 Event_04 비용 조건 및 장착 유물 비용 회귀 테스트를 추가한다.
2. `EventChoiceExecutionService`에 선택된 장착 유물 비용 데이터와 비용 제거 델리게이트를 추가한다.
3. `CanSelect`에서 `선택 유물` 비용은 장착 유물 보유 여부만 검사하게 분리한다.
4. `ApplyCost`를 성공/실패 반환형으로 바꾸고, 선택된 장착 유물 비용을 실행 전 처리한다.
5. `EventRoomController`에서 `선택 유물` 비용 선택지를 클릭하면 장착 유물 목록을 이벤트 선택 슬롯으로 표시한다.
6. 장착 유물을 고르면 선택 비용 컨텍스트를 포함해 기존 선택 실행 흐름으로 진입한다.
7. 빌드 및 정적 검증을 수행한다.
