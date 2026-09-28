# Unique Skill Cutscene Wait Design

## 목적

`Category.Unique` 스킬이 실행될 때 `Skill_cutscene` 연출이 완전히 끝나 프리팹이 비활성화된 뒤 스킬 행동을 시작한다.

## 원인

현재 `BattleActionRunner`는 스킬 시작 콜백을 동기 `Action<PlayerReservedCommand>`로 호출한다. `BattleTurnExecutor`는 이 콜백에서 컷신을 활성화하지만 완료 신호를 반환하지 않으므로, 실행기는 같은 프레임에 스킬 범위 계산과 행동 처리를 계속한다.

## 설계

- `BattleActionRunner`의 스킬 시작 프레젠테이션 콜백을 `Func<PlayerReservedCommand, IEnumerator>`로 변경한다.
- `SkillCutsceneView.PlayAndWait`는 이미지를 설정하고 오브젝트를 재활성화한 뒤, 프리팹의 기존 `MaskChainEnableAnimator`가 오브젝트를 비활성화할 때까지 대기한다.
- `BattleTurnExecutor`는 유니크 스킬이고 이미지와 프리팹 참조가 모두 유효할 때만 대기 루틴을 반환한다. 표시할 수 없는 경우 즉시 종료하여 전투 진행을 막지 않는다.
- 로컬 실행과 네트워크 재생 경로 모두 같은 콜백을 사용한다.

## 검증

- EditMode 테스트로 컷신 뷰가 활성화된 동안 대기하고 비활성화되면 완료되는 계약을 검증한다.
- 프로젝트 스크립트 컴파일로 콜백 시그니처와 양쪽 실행 경로를 검증한다.
- Unity 에디터에서 유니크 스킬 사용 시 컷신 종료 후 행동이 시작되는지 수동 확인한다.

## 멀티플레이 경계

네트워크 커맨드, 스냅샷, 전투 결과 계산에는 손대지 않는다. 로컬과 원격 모두 확정된 커맨드의 프레젠테이션 시작 순서만 동일하게 지연하며, 컷신은 결과 값을 계산하거나 변경하지 않는다.
