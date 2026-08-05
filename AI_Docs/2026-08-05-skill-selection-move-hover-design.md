# 스킬 선택 중 이동 버튼 호버 프리뷰 수정 설계

## 문제

전투 예약 단계에서 선택형 스킬을 선택한 뒤 대상 그리드를 고르기 전에 이동 버튼을 호버하면, 이동 범위 프리뷰가 표시되면서 기존 스킬 그리드 선택 상태가 취소된다.

## 원인

- `BattleCharacterSkillHoverUI`는 호버 진입 시 `CancelGridSelectionWhenHoveringDifferentSkill`를 호출한다.
- `PlayerSkillReservationController.CancelSelectionWhenHoveringDifferentSkill`는 현재 선택 중인 스킬과 hover 대상 스킬 ID가 다르면 `ClearPreview()`를 호출한다.
- 이동 버튼도 스킬 데이터가 연결되어 있으므로, 선택 중인 일반 스킬과 다른 스킬로 판단되어 선택 상태가 초기화된다.
- 이동 hover 프리뷰가 `RangePreview.ShowDirectionCells`로 기존 선택 가능 그리드 표시를 덮은 뒤, hover 종료 시 현재 선택 중인 스킬의 선택 가능 그리드를 다시 그리지 않는다.

## 수정 방향

- 이동 스킬 hover는 임시 범위 확인으로만 취급하고, 이미 진행 중인 선택형 스킬 예약 상태를 취소하지 않는다.
- hover 프리뷰 종료 시 현재 선택 중인 선택형 스킬이 있으면 선택 가능 그리드를 다시 표시한다.
- 전투 결과나 예약 확정 로직은 변경하지 않고, UI 프리뷰 상태만 복구한다.

## 검증

- EditMode 테스트로 선택형 스킬 선택 중 이동 스킬 hover가 `currentSkillData`를 지우지 않는지 확인한다.
- 가능하면 hover 종료 후 선택 가능 그리드 목록이 유지되는지도 확인한다.
