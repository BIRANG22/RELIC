# 스킬 자원 제한 및 플립 드래그 수정 설계

## 목적

- 예약 후 남은 자원이 부족한 스킬은 드래그를 시작하지 못하게 한다.
- 플립 버튼을 타임라인 슬롯으로 드래그하면 해당 슬롯에 방향 전환 행동을 예약한다.
- 클릭과 드래그가 동일한 전투 예약 검증 및 명령 확정 경로를 사용하게 한다.

## 범위

- `BattleCharacterPanelUI`가 스킬 드래그 시작 전에 미리보기 자원 사용 가능 여부를 검사한다.
- 플립 버튼에도 드래그 소스를 연결하고 `TimelineReservationDragContext`에 플립 여부를 명시한다.
- `BattleTimelineController`가 드롭 대상 슬롯을 선택한 뒤 기존 `TryReserveFacingFlip` 경로로 플립 명령을 생성·확정한다.
- 자원 부족 판정과 플립 컨텍스트를 EditMode 테스트로 보호한다.

## 비범위

- 기존 타임라인 행동 재배치 규칙 변경
- 플립 비용·행동 수용량 변경
- 네트워크 프로토콜 또는 프레임워크 추가
- 프리팹·씬 구조 변경

## 접근안

1. 버튼의 `interactable`만 변경: 툴팁과 클릭 경고까지 막을 수 있고 EventSystem 드래그 핸들러 차단을 보장하지 않아 제외한다.
2. 플립을 이동 스킬처럼 위장: 비용과 경로 선택 의미가 섞여 제외한다.
3. 선택안: 드래그 시작 함수에서 자원을 검사하고, 컨텍스트에 플립 행동을 별도로 표시하여 컨트롤러의 명령 예약 경로로 전달한다.

## 데이터 흐름

`버튼 드래그 → 드래그 시작 검증 → TimelineReservationDragContext → 슬롯 드롭 → BattleTimelineController → PlayerReservedCommand 생성/확정`

플립 드래그는 안정적인 `CharacterRuntimeData.CharacterId`를 가진 런타임과 이동 스킬 데이터를 전달하며, 실제 상태 변경은 컨트롤러가 담당한다.

## 실패 처리

- 자원이 부족하거나 입력이 잠겼으면 드래그 컨텍스트와 고스트를 만들지 않는다.
- 플립 드롭 대상 슬롯 선택 또는 기존 예약 검증이 실패하면 명령을 추가하지 않는다.
- 기존 경고와 예약 결과 갱신은 컨트롤러 경로를 재사용한다.

## 멀티플레이 경계

UI는 플립 결과를 직접 계산하거나 방향 상태를 직접 변경하지 않는다. 기존 `PlayerReservedCommand` 생성과 `ConfirmPlayerCommand` 흐름을 사용하므로 `Command -> State Change -> Result/Event` 경계를 유지한다.
