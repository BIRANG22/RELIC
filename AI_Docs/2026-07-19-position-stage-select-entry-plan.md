# 거점 스테이지 선택 및 전투 진입 구현 계획

> **작업 지침:** 현재 세션에서 단계별로 실행한다. 새 테스트는 `Assets/Tests/EditMode~/`에만 작성하고 Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.

**목표:** 거점의 월드 `Play` 스프라이트로 스테이지 선택 오버레이를 열고, 중앙 테마 카드를 직접 클릭하면 기존 검증을 거쳐 즉시 전투에 진입한다.

**구조:** `PositionStageSelectController`는 거점 월드 클릭과 오버레이 표시만 담당한다. `LobbyStageButtonCarousel`은 이동과 직접 확정 클릭을 구분하고, `MapChapterSelectButton`은 선택 성공 후 기존 `BattlePlayButton`에 전투 진입을 위임한다.

**기술:** Unity 6, C#, Unity UI, SpriteRenderer/Collider2D

## 공통 제약

- 문서는 `AI_Docs` 안에만 작성한다.
- 전투 진입 검증과 런타임 전송은 기존 `BattlePlayButton`을 재사용한다.
- 새 UI 코드는 맵·파티·전투 핵심 상태를 직접 변경하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋과 PR은 별도 허락 전에는 생성하지 않는다.

---

### 작업 1: 거점 스테이지 선택 오버레이

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionStageSelectController.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionStageSelectController.cs.meta`
- 테스트: `Assets/Tests/EditMode~/PositionStageSelectControllerTests.cs`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`

**인터페이스:**

- `OpenStageSelect()`: 거점 위에 스테이지 선택 오버레이 표시
- `CloseStageSelect()`: 오버레이만 닫고 `LobbyViewState`는 유지
- `OnMouseUpAsButton()`: 월드 `Play` 클릭을 `OpenStageSelect()`로 전달

- [ ] `OpenStageSelect()`가 오버레이를 열고 `CloseStageSelect()`가 닫는 테스트를 먼저 작성한다.
- [ ] 월드 스프라이트에 클릭용 `PolygonCollider2D`가 준비되는 테스트를 먼저 작성한다.
- [ ] `PositionStageSelectController`를 최소 구현한다. `PositionPanel` 아래에 전체 화면 투명 입력 차단 오버레이를 만들고 기존 `StageSelectPanel`을 그 아래로 재배치한다.
- [ ] 패널 내부 우측 상단에 닫기 버튼을 만들고 `CloseStageSelect()`에 연결한다.
- [ ] `Position/CentralBase/Play`가 저장된 경우 컨트롤러와 `StageSelectPanel`, `PositionPanel` 참조를 씬에 연결한다.

### 작업 2: 캐러셀 이동과 테마 확정 클릭 분리

**파일:**

- 수정: `Assets/Project/Scripts/LobbyStageButtonCarousel.cs`
- 테스트: `Assets/Tests/EditMode~/LobbyStageButtonCarouselDirectClickTests.cs`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`

**인터페이스:**

- `HandleStageButtonClick(Button clickedButton)`은 비중앙 카드 클릭 시 중앙 이동 후 `true`, 이미 중앙인 카드 클릭 시 확정 처리를 계속하도록 `false` 반환
- 캐러셀 이동 시 `MapChapterSelectButton.SelectChapterForCarousel()`을 호출하지 않도록 씬의 `applyStageSelectionWhenCentered`를 비활성화

- [ ] 비중앙 카드 직접 클릭은 이동만 하고 중앙 카드 재클릭은 확정 흐름으로 넘어가는 테스트를 먼저 작성한다.
- [ ] `HandleStageButtonClick()`이 기존 중앙 인덱스와 클릭 인덱스를 비교하여 반환값을 구분하도록 변경한다.
- [ ] `StageSelectPanel` 캐러셀의 `applyStageSelectionWhenCentered`를 `false`로 변경한다.

### 작업 3: 테마 선택 성공 후 즉시 전투 진입

**파일:**

- 수정: `Assets/Project/Scripts/UI/Lobby/MapChapterSelectButton.cs`
- 테스트: `Assets/Tests/EditMode~/MapChapterSelectBattleEntryTests.cs`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`

**인터페이스:**

- 새 직렬화 옵션 `enterBattleAfterSelect`
- 새 직렬화 참조 `BattlePlayButton battlePlayButton`
- 중앙 테마 카드 직접 클릭 시 `TrySelectChapter(... closeAfterSelect: false ...)` 성공 후 `BattlePlayButton.OnClickPlay()` 호출

- [ ] 잠긴 카드 또는 선택 실패 시 전투 진입을 호출하지 않는 테스트를 먼저 작성한다.
- [ ] 선택 성공 시 패널을 닫지 않고 기존 `BattlePlayButton.OnClickPlay()`를 호출하는 테스트를 먼저 작성한다.
- [ ] `MapChapterSelectButton.OnClickSelectChapter()`에 선택 후 전투 진입 옵션을 최소 구현한다.
- [ ] 세 테마 카드에 `enterBattleAfterSelect`와 기존 `BattlePlayButton` 참조를 연결하고 `closePanelAfterSelect`는 비활성화한다.

### 작업 4: 검증

**파일:**

- 검증: `Assembly-CSharp.csproj`
- 확인: `Assets/Project/Scenes/YDM/Lobby.unity`

- [ ] 씬에 월드 `Play`, 오버레이 컨트롤러, 세 테마 카드 전투 진입 참조가 각각 정확히 연결됐는지 정적으로 확인한다.
- [ ] 새 UI 코드에 파티 슬롯 또는 전투 상태 직접 변경이 없는지 검색한다.
- [ ] `Assembly-CSharp.csproj`를 빌드하여 컴파일 오류가 없는지 확인한다.
- [ ] 플레이 모드 수동 확인 항목을 전달한다: 열기, 바깥 클릭 차단, 닫기, 캐러셀 이동만으로 미진입, 중앙 카드 직접 클릭으로 전투 진입, 검증 실패 시 패널 유지.
