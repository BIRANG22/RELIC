# Resolution Aspect and Fullscreen Design

## Goal

사용자가 창을 임의 크기로 조절해도 선택한 해상도의 종횡비를 유지하고, 옵션의 해상도 드롭다운 옆 토글로 전체화면과 창 모드를 전환한다.

## Design

- `ResolutionManager`는 선택 해상도를 논리 해상도로 유지한다. 실제 창 크기가 가로 또는 세로 방향으로만 변하면 카메라와 UI 콘텐츠를 같은 비율로 스케일하고 남은 공간은 기존 레터박스로 채운다.
- 전체화면 여부를 PlayerPrefs에 저장한다. 해상도를 변경할 때도 현재 전체화면 상태를 보존하며, 시작 시 해상도와 전체화면 설정을 함께 복원한다.
- `OptionPanelUI`는 `ResolutionContent` 내부의 `FullscreenToggle`을 직렬화하고, 표시될 때 저장 상태를 동기화한다. 토글 변경은 `ResolutionManager`로만 전달한다.
- `Option.prefab`은 해상도 드롭다운과 같은 행의 우측에 `FullscreenToggle`을 배치한다.

## Verification

- EditMode: 전체화면 상태 API와 저장 키, Option 프리팹의 직렬화된 토글 참조를 검증한다.
- EditMode: 넓거나 높은 창에서 선택 해상도 비율을 유지하는 기존 레터박스 계산을 검증한다.
- Unity Editor: 창의 가로/세로를 각각 조절하고, 전체화면 토글 전환 및 재시작 후 상태 복원을 확인한다.
