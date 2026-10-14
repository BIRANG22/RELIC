# 전투 전멸 패배 연출 구현 계획

> **실행 지침:** 현재 작업 공간에서 TDD로 순차 구현하며 커밋은 수행하지 않는다.

**목표:** 전멸 확정 후 실제 쓰러진 아군 3명을 X 순서대로 중앙 포물선 앵커에 이동시키고, 검은 안개와 `탐사 실패` 문구를 표시한 뒤 기존 탐사 결과 패널을 겹쳐 연다.

**구조:** `BattleResultChecker`는 결과 판정과 진입만 담당하고, 신규 `BattleDefeatPresentation`이 순수 연출과 완료 콜백을 담당한다. 실제 캐릭터 상태는 변경하지 않고 Transform과 렌더 정렬만 연출용으로 변경한다.

**기술:** Unity 6, C#, Coroutine, TMP, uGUI, NUnit EditMode

**설계:** `AI_Docs/2026-10-14-battle-defeat-presentation-design.md`

## 전역 제약

- 전멸 판정 및 보상 계산은 기존 전투 로직에 남긴다.
- 침식도 패배에는 캐릭터 집결 연출을 적용하지 않는다.
- Animator 상태, 방향, HP, GridIndex는 연출에서 변경하지 않는다.
- 씬 연출 오브젝트는 직렬화 참조를 사용한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.

## 검토 중점

- X가 같은 캐릭터의 안정적인 CharacterId 보조 정렬
- 캐릭터가 1~2명 또는 4명 이상인 예외 입력
- 연출 참조 누락 시 결과 패널 콜백 보장
- 결과 패널을 열기 전 캐릭터가 정리되지 않는지
- 재진입 및 씬 비활성화 시 코루틴 중복 방지

### Task 1: 캐릭터 배치 정책과 연출 프레젠터

**파일:**
- 신규: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleDefeatPresentation.cs`
- 신규 테스트: `Assets/Tests/EditMode~/BattleDefeatPresentationTests.cs`

**인터페이스:**
- `SelectCharacters(IReadOnlyList<BattleCharacter>, int)`는 X와 CharacterId로 안정 정렬한다.
- `ResolveAnchorIndices(int)`는 1명 중앙, 2명 좌·우, 3명 좌·중·우를 반환한다.
- `Play(IReadOnlyList<BattleCharacter>, Action)`은 연출 완료 후 콜백을 정확히 한 번 호출한다.

- [ ] 정렬·앵커 정책 테스트를 먼저 작성하고 컴파일 실패를 확인한다.
- [ ] 최소 정책 API를 구현해 테스트를 통과시킨다.
- [ ] 프레젠터의 안개, 이동, 텍스트, 렌더 정렬, 예외 fallback을 구현한다.
- [ ] 관련 테스트와 런타임 컴파일을 확인한다.

### Task 2: 전멸 결과 흐름 연결

**파일:**
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- 테스트: `Assets/Tests/EditMode~/BattleDefeatPresentationTests.cs`

**인터페이스:**
- 전멸 경로만 프레젠터를 호출한다.
- 완료 콜백에서 `OpenDefeat`를 호출하되 캐릭터 정리는 거점 복귀 시점으로 미룬다.
- 침식 패배는 기존 즉시 결과 흐름을 유지한다.

- [ ] 전멸/침식 분기와 정리 지연을 증명하는 실패 테스트를 작성한다.
- [ ] `BattleResultChecker`에 직렬화 프레젠터 참조와 fallback을 연결한다.
- [ ] 패배 결과 패널 진입 전 `CleanBattleRoom` 호출을 제거하고 거점 복귀 경로의 기존 정리를 확인한다.
- [ ] 관련 테스트와 런타임 컴파일을 확인한다.

### Task 3: Battle 씬 연출 구성

**파일:**
- 수정: `Assets/Project/Scenes/YDM/Battle.unity`
- 필요 시 신규: `Assets/Project/PrefabsR/BattleDefeatPresentation.prefab`

**인터페이스:**
- Battle 씬에 비활성 연출 루트, 검은 안개, 세 캐릭터 앵커, TMP 문구를 배치한다.
- `BattleResultChecker`와 `BattleDefeatPresentation`의 직렬화 참조를 연결한다.

- [ ] 씬 구성 누락을 탐지하는 정적 EditMode 테스트를 먼저 작성한다.
- [ ] 씬 또는 프리팹에 연출 오브젝트를 배치하고 참조를 연결한다.
- [ ] 씬 YAML 참조 무결성, 런타임/Editor 컴파일을 확인한다.

