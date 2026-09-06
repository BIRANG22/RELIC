# Lobby Camera Mouse Parallax Design

## Goal

로비 `Main Camera`에도 배틀씬에서 조정한 마우스 기반 카메라 패럴랙스 값을 동일하게 적용한다.
마우스가 화면 중심에서 벗어나면 카메라가 XY 위치와 XY 회전으로 아주 살짝 따라 움직여 로비 화면에 약한 입체감을 더한다.

## Findings

- 배틀씬의 현재 저장 값은 다음과 같다.
  - `enableMouseParallax: 1`
  - `mouseParallaxPositionAmount: {x: 0.08, y: 0.03}`
  - `mouseParallaxRotationAmount: {x: 3, y: 1.5}`
  - `mouseParallaxSmoothSpeed: 8`
  - `mouseParallaxCameraMotionMultiplier: 0.35`
- 로비 `Main Camera`는 `Assets/Project/Scenes/YDM/Lobby.unity`에 직접 배치되어 있다.
- `HorizontalHubCameraDrag`는 현재 소스에는 없고 씬 이벤트에 null 타겟 흔적으로만 남아 있다.
- `PanelCameraMover`는 로비 패널을 열 때 카메라 또는 카메라 Rig Transform을 직접 이동한다.

## Recommended Design

- 새 공용 연출 컴포넌트 `CameraMouseParallaxController`를 만든다.
- 로비 `Main Camera`에 이 컴포넌트를 부착하고 배틀씬과 같은 값을 직렬화한다.
- 매 프레임 시작에는 직전 프레임 패럴랙스를 제거하고, `LateUpdate`에서 최종 카메라 Transform에 다시 더한다.
- 카메라 기본 Transform이 이전 프레임보다 움직였으면 `mouseParallaxCameraMotionMultiplier`로 강도를 낮춰 패널 카메라 이동을 방해하지 않는다.

## Testing

- EditMode 테스트에서 마우스 정규화, 위치 오프셋, 회전 오프셋, 이동 중 강도 배율 선택을 검증한다.
- Unity 에디터는 열려 있다고 가정하므로 batchmode 테스트는 실행하지 않는다.
- 컴파일 검증은 `Assembly-CSharp-Editor.csproj` MSBuild로 수행한다.

## Multiplayer Boundary

로비 카메라 전용 클라이언트 연출이며 전투 상태, 결과 계산, 네트워크 동기화 데이터에는 영향을 주지 않는다.
