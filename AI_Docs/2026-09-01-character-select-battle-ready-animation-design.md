# 캐릭터 선택 Battle Ready 애니메이션 설계

## 목표

배틀씬 예약 단계에서 캐릭터가 선택되어 `HighlightSprite` 선택 표시가 켜지는 타이밍에, 같은 캐릭터 프리팹의 `SpriteRoot` 아래 Animator가 `battle_ready` 애니메이션을 재생한다.

## 설계

기존 선택 흐름은 `BattleTimelineController.SelectCharacter`가 선택 캐릭터 ID를 정하고 `BattleCharacter.SetSelectionScaleFeedback(true)`를 호출한다. Highlight 표시도 이 함수에서 관리되므로 ready 애니메이션도 같은 경계에 붙인다.

- 선택 상태가 `true`가 되면 `SpriteRoot` 자식 Animator를 찾는다.
- Animator에 `battle_ready` 상태가 있으면 0번 레이어에서 처음부터 재생한다.
- 같은 캐릭터를 다시 선택해도 선택 피드백 호출이 들어오면 처음부터 재생한다.
- 선택 해제 시에는 애니메이션을 강제로 변경하지 않는다.
- Animator 또는 상태가 없으면 전투 흐름을 막지 않고 무시한다.

## 범위

`BattleCharacter`에 선택 ready 애니메이션 전용 캐시와 재생 메서드를 추가한다. 프리팹, 타임라인 예약 데이터, 전투 결과 계산, 네트워크 구조는 변경하지 않는다.

## 검증

- 선택 시 사용하는 Animator state 이름이 `battle_ready`인지 테스트한다.
- `SpriteRoot` 아래 Animator를 우선 찾는지 테스트한다.
- 선택 해제 시 재생 대상이 없는 경로가 안전한지 컴파일로 확인한다.
