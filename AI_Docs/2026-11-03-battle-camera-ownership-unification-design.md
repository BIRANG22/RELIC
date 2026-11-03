# Battle Camera Ownership Unification Design

## Intent

배틀 메인 카메라의 위치, 회전, 줌, 셰이크를 `BattleCameraController` 한 곳에서 소유하도록 통합한다. UI와 보스 연출은 카메라 Transform을 직접 변경하지 않고 컨트롤러에 연출 요청만 전달한다.

## Findings

- `BattleMonsterInfoCanvasUI`가 `Camera.main`을 직접 이동하며 자체 기본 Z `-17.5`와 코루틴을 가진다.
- `BattleCameraController`에는 이미 몬스터 정보 포커스와 복귀 API가 있어 기능이 중복된다.
- `SpriteTransformSequence`는 `Camera.main` 위치, 회전, orthographic size, 셰이크와 복귀를 직접 처리하며 `St1_boss.prefab`에서 활성화되어 있다.
- `CameraShakeController`는 배틀 메인 카메라에 별도로 부착되어 Transform 오프셋을 직접 적용한다.
- `BattleSceneController`의 직접 변경은 컨트롤러 부재 시 fallback이며 이번 통합 범위에서는 유지한다.
- `DissolveCamera`, `DefeatOverlayCamera`, VFX 카메라는 메인 카메라 추종/복사 대상이므로 소유권 통합 대상이 아니다.

## Goals

- 배틀 중 메인 카메라 Transform과 Projection 변경은 `BattleCameraController`를 통해서만 수행한다.
- 몬스터 정보 UI가 자체 카메라 위치와 코루틴을 갖지 않게 한다.
- 보스 시퀀스의 카메라 포즈와 복귀는 컨트롤러가 캡처하고 적용한다.
- 배틀 메인 카메라에 부착된 범용 셰이크 요청은 컨트롤러의 외부 셰이크 레이어로 전달한다.
- 기존 보스 프리팹의 카메라 모션 데이터와 호출 타이밍은 보존한다.

## Non-goals

- 로비 `PanelCameraMover`와 `CameraMouseParallaxController`를 변경하지 않는다.
- RenderTexture/VFX 전용 카메라 생성과 동기화 구조를 변경하지 않는다.
- 전투 결과, 상태 변경, 랜덤, 네트워크 데이터를 변경하지 않는다.
- `BattleSceneController`의 컨트롤러 부재 fallback은 제거하지 않는다.

## Approaches

1. UI/시퀀스마다 카메라 이동을 유지하고 충돌 방지만 추가: 소유권이 계속 분산되어 제외한다.
2. 모든 연출을 `BattleCameraController` 내부에 하드코딩: 보스 프리팹 데이터 재사용성이 떨어져 제외한다.
3. 외부는 연출 데이터를 유지하고 컨트롤러 API로 포즈/셰이크를 요청: 단일 소유권과 데이터 주도 구성을 함께 만족하므로 선택한다.

## Design

### Monster info

- `BattleMonsterInfoCanvasUI`는 선택 몬스터와 패널 방향만 컨트롤러에 전달한다.
- 열기에는 `FocusMonsterInfoWithPanelSide`, 닫기에는 `ReturnDefaultFromMonsterInfoFocus`를 사용한다.
- 자체 `Camera`, `defaultCameraPosition`, 포커스 계산 필드, 이동 코루틴을 제거한다.
- `UIDissolveReveal`은 디졸브 표현만 담당하고 카메라 요청을 제거해 중복 호출을 막는다.

### Boss cinematic

- `BattleCameraController`에 시네마틱 세션 캡처, 포즈 적용, 원상 복귀 API를 추가한다.
- `SpriteTransformSequence`는 배틀 컨트롤러가 존재할 때 해당 API만 사용한다.
- 배틀 컨트롤러가 없는 아트 테스트/비배틀 씬에서는 기존 `Camera.main` fallback을 유지한다.
- 시네마틱 시작과 종료 시 기존 전투 포커스/임팩트 코루틴을 정리해 경쟁을 막는다.

### External shake

- `CameraShakeController.Shake`는 자신이 배틀 컨트롤러의 카메라에 붙어 있으면 컨트롤러에 요청한다.
- `BattleCameraController`가 외부 셰이크 오프셋을 LateUpdate 계층에서 적용/제거한다.
- 비배틀 카메라에서는 기존 범용 셰이크 동작을 유지한다.

## Testing

- EditMode 테스트로 배틀 카메라 소유 여부 판정과 시네마틱 포즈 계산/복귀용 순수 정책을 검증한다.
- 소스 경계 테스트로 `BattleMonsterInfoCanvasUI`와 `UIDissolveReveal`의 직접 카메라 변경이 제거되었는지 검증한다.
- `Assembly-CSharp`와 `Assembly-CSharp-Editor` 컴파일을 확인한다.
- Unity 에디터에서 실제 보스 등장, 몬스터 정보 패널, 셰이크 연결감은 수동 검증 항목으로 남긴다.

## Multiplayer Boundary

클라이언트 카메라 표현 계층만 변경한다. Command, State Change, Result/Event, 전투 랜덤, 동기화 ID와 Snapshot에는 영향이 없다.
