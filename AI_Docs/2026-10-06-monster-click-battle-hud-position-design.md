# 몬스터 클릭 시 Battle HUD 위치 유지

## 목적

배틀 씬에서 몬스터를 클릭해 정보 패널을 열 때 `BattleCharacterPanel`과 `BattleSlot`이 기본(아래) 위치로 내려가는 UI 이동을 제거한다.

## 조사 결과

`MonsterUnit.OnMouseDown`은 캐릭터 선택을 해제한 뒤 `MonsterInfoSelectionChanged`를 발생시킨다. `BattleCharacterPanelUI`는 다음 프레임에 선택 상태를 다시 평가하고, 캐릭터 선택이 없다는 이유로 패널과 슬롯을 기본 위치로 이동시킨다.

## 설계

- 몬스터 정보 선택이 시작되면 이미 예약된 위치 갱신 코루틴을 취소하고, 몬스터 정보 선택이 유지되는 동안 Battle HUD 위치 갱신을 하지 않는다.
- 몬스터 정보 선택이 해제되어도 Battle HUD 위치 갱신을 예약하지 않아 현재 위치를 유지한다.
- MonsterInfoPanel은 Battle HUD가 기본 위치에 도달할 때까지 기다리지 않고 즉시 표시하며, 기존 페이드인만 적용한다.
- 캐릭터 선택, 턴 실행, 전투 종료 시의 기존 HUD 이동 규칙은 변경하지 않는다.

## 검증

- EditMode 회귀 테스트로 몬스터 정보 선택 및 해제 모두 위치 갱신을 억제하고, HUD 대기 루프 없이 즉시 표시하는지 확인한다.
- Unity 컴파일로 변경 코드와 테스트의 컴파일을 확인한다.
