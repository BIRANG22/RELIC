# 배양조 행 아이템 아이콘 설계

## 목표

- `CultureTankRow_1~3` 각각에 선택 재료와 연구 결과를 표시하는 전용 `ItemIcon` 이미지를 둔다.
- 연구 시작 직후에는 선택한 아이템의 기본 아이콘을 표시한다.
- 연구 완료 후에는 같은 아이템 ID에 등록된 `ResearchResultIcon`을 표시한다.
- 빈 배양조는 아이콘을 숨기고, 저장 데이터를 다시 읽어도 상태에 맞는 아이콘을 복원한다.

## 설계

- 행 표현은 `LobbyCultureTankPanelPresenter`가 담당하며 연구 상태를 직접 변경하지 않는다.
- 각 행은 자식 `ItemIcon`을 탐색하고, 없으면 런타임에 전용 `Image` 오브젝트를 생성한다.
- `Empty`/`MissingData` 상태에서는 아이콘을 숨긴다.
- `Running` 상태에서는 `ItemIconDatabase.TryGetIcon(ItemId)`를 사용한다.
- `Completed` 상태에서는 `TryGetResearchResultIcon(ItemId)`을 우선하고, 미등록 시 기본 아이콘으로 대체한다.
- 결과 이름인 `ItemData.ResearchResult`와 실제 결과 스프라이트의 연결은 기존 `ItemIconDatabase.ResearchResultIcon` 등록 구조를 유지한다.

## 멀티플레이 경계

- 동기화되는 `CultureTankResearchRuntimeData.ItemId`와 완료 상태만 읽는다.
- UI는 연구 상태나 결과를 계산·변경하지 않고, 호스트가 확정한 상태의 아이콘만 표시한다.

## 완료 피드백 안전 처리

- 비활성 월드 배양조도 패널 데이터 공급 대상으로 검색될 수 있다.
- 비활성 `MonoBehaviour`에서는 코루틴을 시작할 수 없으므로 완료 보상을 받은 뒤 로컬 월드 텍스트 연출은 생략한다.
- 보상 적용, 저장, 공용 안내 메시지, 호스트 상태 발행은 기존 순서를 유지한다.
