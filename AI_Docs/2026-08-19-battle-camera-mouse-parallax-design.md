# Battle Camera Mouse Parallax Design

## Goal

배틀 씬 카메라에만 마우스 위치를 따라가는 아주 작은 연출용 패럴랙스를 추가한다.
씬 중앙에서는 변화가 없고, 마우스가 화면 가장자리로 갈수록 XY 위치 이동과 XY 회전이 부드럽게 더해져 약한 3D 입체감을 준다.

## Findings

- `BattleCameraController`가 배틀 카메라의 줌, 피격 임팩트, 몬스터 정보 포커스, 기본 복귀를 직접 제어한다.
- `BattleVfxCameraSync`는 메인 카메라의 최종 Transform과 Projection을 VFX 카메라에 복사한다.
- 별도 컴포넌트가 카메라 Transform을 또 조작하면 기존 줌/임팩트 코루틴과 순서 충돌이 생길 수 있다.

## Recommended Design

- `BattleCameraController` 내부에 마우스 패럴랙스 연출 레이어를 추가한다.
- 매 프레임 시작에는 직전 프레임에 더했던 패럴랙스 오프셋을 제거하고 기존 카메라 로직을 실행한다.
- `LateUpdate`에서 최종 카메라 위치/회전에 작은 패럴랙스 오프셋을 더한다.
- 마우스 위치는 화면 중심 기준 `-1..1` 범위로 정규화하고 화면 밖 값은 클램프한다.
- 강한 배틀 줌, 몬스터 정보 포커스, 피격 임팩트 중에는 패럴랙스 강도를 낮춰 기존 연출을 방해하지 않는다.

## Default Tuning

- 위치 이동: X `0.08`, Y `0.05`
- 회전: X `1.0`도, Y `1.0`도
- 스무딩: `8`
- 카메라 연출 중 강도 배율: `0.35`

## Testing

- EditMode 테스트로 화면 중심/구석 정규화와 패럴랙스 위치/회전 계산을 검증한다.
- Unity 에디터는 열려 있다고 가정하므로 batchmode 테스트는 실행하지 않는다.
- 컴파일 검증은 `Assembly-CSharp-Editor.csproj` MSBuild로 확인한다.

## Multiplayer Boundary

이 기능은 카메라 Transform에만 영향을 주는 클라이언트 연출이다.
전투 상태, 판정, 랜덤, Result/Event 계산에는 관여하지 않는다.
