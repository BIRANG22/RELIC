# 로비 화면 상태 관리 설계

## 목적

로비 씬의 화면 구성을 `Lobby`, `CharacterSelection`, `Position` 세 상태로 명시적으로 관리한다. 기존 버튼 흐름을 유지하면서 각 상태에 맞는 UI, 배경, 이펙트, 조명, 카메라 입력만 활성화한다.

## 범위

- 로비 화면 상태 3종 정의 및 전환
- 기존 캐릭터 설정 진입/복귀 버튼과 상태 전환 연결
- 기존 `TestPosition` 버튼을 로비와 거점 상태 전환에 사용
- 거점 상태에서만 `HorizontalHubCameraDrag` 실행
- `Back_Main`, `Effect_Lobby`, `Effect_Char`의 현재 셰이더 표현을 RenderTexture 기반 Canvas `RawImage`로 전환
- 기존 `Canvas` 아래에 `Background` 빈 오브젝트를 만들고 두 이미지를 자식으로 배치

## 구조

### LobbyViewState

다음 세 상태를 나타내는 enum을 사용한다.

- `Lobby`
- `CharacterSelection`
- `Position`

### LobbyViewStateController

로비 씬 화면 상태의 단일 진입점이다. 현재 상태를 보관하고 상태가 변경될 때 직렬화된 참조의 활성 상태를 일괄 적용한다.

공개 진입 메서드는 기존 Unity Button 이벤트에서 직접 연결할 수 있도록 구성한다.

- `ShowLobby()`
- `ShowCharacterSelection()`
- `TogglePosition()`
- `ShowPosition()`

상태 전환은 이전 오브젝트 활성 상태를 캡처하거나 복원하지 않는다. 같은 상태는 항상 같은 화면 구성을 만든다.

## 상태별 구성

| 대상 | Lobby | CharacterSelection | Position |
| --- | --- | --- | --- |
| `Back_Main_Source` / RawImage | 켜짐 | 켜짐 | 꺼짐 |
| `Effect_Lobby_Source` / RawImage | 켜짐 | 꺼짐 | 꺼짐 |
| `Effect_Char_Source` / RawImage | 꺼짐 | 켜짐 | 꺼짐 |
| `LobbyMainPanel` | 켜짐 | 꺼짐 | 꺼짐 |
| `CharacterSettingPanel` | 꺼짐 | 켜짐 | 꺼짐 |
| `Position` | 꺼짐 | 꺼짐 | 켜짐 |
| `PositionPanel` | 꺼짐 | 꺼짐 | 켜짐 |
| 로비 Directional Light | 켜짐 | 켜짐 | 꺼짐 |
| 거점 Directional Light | 꺼짐 | 꺼짐 | 켜짐 |
| `HorizontalHubCameraDrag` | 꺼짐 | 꺼짐 | 켜짐 |

## 기존 버튼 연결

- 캐릭터 설정 진입 버튼은 기존 패널 전환과 함께 `ShowCharacterSelection()`을 호출한다.
- 캐릭터 설정의 로비 복귀 버튼은 기존 패널 닫기 동작과 함께 `ShowLobby()`를 호출한다.
- `TestPosition` 버튼은 `TogglePosition()`을 호출한다.
- 거점에서 `TestPosition`을 다시 누르면 `Lobby`로 돌아간다.

기존 `UIPanelButton`의 범용 동작은 변경하지 않는다. 로비 씬의 관련 버튼 이벤트에 상태 컨트롤러 호출만 추가하여 다른 씬과 패널에 영향을 주지 않는다.

## 카메라 입력

`HorizontalHubCameraDrag` 컴포넌트는 거점 상태에서만 활성화한다. 비활성화될 때 진행 중인 드래그 상태와 스냅 속도를 초기화하여 다른 상태에서 카메라가 계속 움직이지 않게 한다. 다시 거점 상태로 들어오면 현재 위치를 유지한 채 입력을 받을 수 있어야 한다.

## Canvas 배경 전환

기존 `Canvas` 직계 자식의 첫 번째 배경 계층으로 `Background` RectTransform을 만든다. 화면 전체 Stretch를 사용하고 다음 순서로 RawImage 자식을 둔다.

1. `Back_Main`
2. `Effect_Lobby`
3. `Effect_Char`

원본은 Plane, SpriteRenderer, MeshRenderer, 커스텀 셰이더로 구성되어 있으므로 `Back_Main_Source`, `Effect_Lobby_Source`, `Effect_Char_Source`로 유지한다. 각 원본을 전용 레이어와 전용 카메라로 RenderTexture에 렌더링하고, 결과를 Canvas `RawImage`에 표시한다. 메인 카메라는 세 전용 레이어를 제외하여 중복 렌더링하지 않는다. RawImage의 레이캐스트는 끄고 다른 UI보다 뒤에 렌더링되도록 형제 순서를 고정한다.

## 오류 처리

- 필수 참조가 빠진 경우 NullReferenceException 대신 경고 로그를 남기고 나머지 대상을 적용한다.
- 초기 상태는 `Lobby`로 명시적으로 적용한다.
- 같은 상태를 다시 요청해도 화면 구성이 어긋나 있으면 재적용할 수 있게 한다.

## 검증

- EditMode 테스트에서 세 상태별 대상 활성화를 검증한다.
- 거점 상태에서만 카메라 드래그 컴포넌트가 활성화되는지 검증한다.
- `TogglePosition()`이 `Lobby -> Position -> Lobby`로 전환되는지 검증한다.
- 씬 YAML에서 `Background`가 Canvas 아래에 있고 `Back_Main`, `Effect_Char` RawImage가 각 RenderTexture에 연결됐는지 확인한다.
- Unity 에디터가 열려 있다는 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다. C# 프로젝트 빌드와 정적 씬 검증을 사용한다.

## 멀티플레이 경계

이번 상태는 로비 화면 표현과 카메라 입력만 제어하며 전투 상태나 동기화 대상 데이터를 변경하지 않는다. 네트워크 패키지 또는 전역 mutable 상태를 추가하지 않는다.
