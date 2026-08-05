# Battle Map UI Polish Implementation Plan

> **For agentic workers:** 승인된 현재 작업 공간에서 순서대로 직접 실행한다. 각 동작 변경은 테스트를 먼저 작성한다.

**Goal:** 맵 선·스크롤 끝 여백·파티 정보·다음 노드 아이콘 버튼을 요청된 형태로 정리한다.

**Architecture:** 맵 기하 계산과 UI 데이터 표현을 분리한다. 파티 정보는 RuntimeStore를 읽는 전용 Presenter가 렌더링하고, 다음 노드 선택은 편집 가능한 프리팹 인스턴스를 사용한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, NUnit EditMode

## Global Constraints

- 문서는 AI_Docs 내부에만 작성한다.
- 테스트는 Assets/Tests/EditMode~ 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: 선 끝점과 Content 너비

**Files:**
- Modify: `Assets/Tests/EditMode~/MapLineViewTests.cs`
- Modify: `Assets/Tests/EditMode~/BattleMapSelectionUiTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapLineView.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`

- [ ] 노드 반지름만큼 줄어든 선 길이와 Content 끝 여백 테스트를 먼저 작성한다.
- [ ] 기존 구현에서 기대값이 어긋나는 것을 확인한다.
- [ ] 선 끝점과 Content 너비 계산을 최소 수정한다.
- [ ] 컴파일과 정적 테스트 구조를 확인한다.

### Task 2: 파티 정보 Presenter

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPartyInfoPresenter.cs`
- Create: `Assets/Tests/EditMode~/BattleMapPartyInfoPresenterTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

- [ ] 슬롯의 HP·아이콘·빈 상태 표현 테스트를 먼저 작성한다.
- [ ] Presenter를 구현하고 MapPanel Open 시점에 갱신한다.
- [ ] CharacterInfo에 컴포넌트를 연결한다.

### Task 3: 아이콘 전용 다음 노드 프리팹

**Files:**
- Create: `Assets/Project/PrefabsR/Map/NextNodeChoicePrefab.prefab`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleNextNodeChoiceButton.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleNextNodeSelectionPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/BattleMapSelectionUiTests.cs`

- [ ] 프리팹 기반 3개 생성과 텍스트 제거 요구를 테스트로 고정한다.
- [ ] 선택 버튼을 아이콘 전용으로 축소한다.
- [ ] 선택 패널이 프리팹으로 슬롯을 생성하도록 변경한다.
- [ ] 씬에 프리팹 참조를 연결한다.

### Task 4: 검증

- [ ] Assembly-CSharp와 Assembly-CSharp-Editor를 빌드한다.
- [ ] YAML 참조와 변경 범위를 검사한다.
- [ ] Unity Test Runner에서 실행해야 하는 항목을 완료 보고에 명시한다.
