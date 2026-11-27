# 로비 캐릭터 씬 생성 지점 설계 및 구현 계획

## 승인된 설계
- 기존 CharacterPrefabDB.LobbyPrefab을 사용한다. 이미지 직접 생성은 이번 범위에 포함하지 않는다.
- 로비 Character 아래 캐릭터별 LobbyCharacterSpawnPoint를 배치한다. 위치/회전/크기는 지점 Transform으로 편집한다.
- 각 지점은 DB 참조, Character ID, Flip X, Sorting Layer, 앞/뒤 Order, GameObject Layer 재정의/자식 적용, 기존 이름 호버 설정을 가진다.
- 부모는 명시적 지점 목록을 순회하고 클릭 대상을 안정적인 Character ID로 연결한다. 이름 기반 캐릭터 판별에 의존하지 않는다.
- 생성은 지점당 한 번이며 다시 호출해도 중복 생성하지 않는다. 비활성 지점은 건너뛰고 나중에 활성화하면 생성한다.
- SpriteRenderer의 첫 번째/두 번째는 기존 앞/뒤 역할을 유지한다. 추가 Renderer의 프리팹 설정은 보존한다.
- 위치는 현재 작업 중인 씬의 최신 값(음수 스케일 포함)을 그대로 이관한다. 다른 씬 변경은 보존한다.
- 대안: 부모 목록에 표시 옵션 추가는 씬 지점별 편집 요구를 충족하지 않는다. 전용 프리팹만 수정하는 방식은 배치별 값을 나누기 어렵다.

## 구현 계획
적용 스킬: writing-plans → executing-plans → test-driven-development → verification-before-completion.
사용자 규칙을 우선하여 현재 작업 공간에서 진행하며 문서/진행 기록은 이 파일에만 둔다. 커밋, 브랜치, worktree 조작은 하지 않는다.

- [x] 생성 지점의 변환 상속, 사용자 표시 값, 레이어 적용 범위, 중복 생성 방지, 누락 DB/ID/Prefab, 클릭 연결과 호버 보존 테스트를 작성한다.
- [x] LobbyWorldCharacterSpawner와 새 LobbyCharacterSpawnPoint를 구현하고 부모의 목록 생성 로직을 교체한다.
- [x] DB ID/Sorting Layer/Layer 드롭다운을 제공하는 전용 Inspector를 구현한다.
- [x] Lobby.unity의 최신 배치 값을 네 개 생성 지점에 이관하고 DB/부모 연결을 검증한다.
- [x] Unity Roslyn으로 runtime/editor/test 코드를 컴파일하고 씬 구조 검증을 실행한다.

## 검증 제약 및 진행 기록
- Unity 직접 제어 및 batchmode 금지. Unity 오브젝트가 필요한 NUnit 테스트는 컴파일까지 확인하고 실행 여부는 분리해서 보고한다.
- 현재 씬에는 사용자 변경이 존재한다. 이전 대화에서 읽은 좌표 대신 수정 직전 YAML 값을 사용한다.
- 멀티플레이: 로컬 표현만 변경하며 Command/State/Event 및 동기화 데이터 변경 없음.

## 사용 방법
1. Lobby 씬의 Character 아래 HiltSpawn/KayaSpawn/HazeSpawn/InesSpawn을 선택한다.
2. Transform에서 위치/회전/크기를 조정한다. 현재 HazeSpawn의 X Scale -0.5는 기존 씬 값을 보존한 것으로, Flip X와 함께 최종 방향에 영향을 준다.
3. LobbyCharacterSpawnPoint Inspector에서 DB/Character ID, Flip X, Sorting Layer, Front/Back Order를 설정한다.
4. GameObject Layer를 별도로 변경하려면 Override GameObject Layer를 켜고 Layer와 자식 적용 여부를 설정한다. 끄면 프리팹의 Layer가 보존된다.
5. 지점 복제로 캐릭터를 추가할 때 Character 부모의 Spawn Points 목록에 새 컴포넌트를 연결한다. DB와 호버 이름도 확인한다.
6. 편집 모드에서는 생성 지점 Gizmo만 표시한다. 캐릭터 프리팹은 실행 시 생성되며 Inspector 표시 옵션은 다음 생성 시 적용된다.

## 최종 검증
- 구현 전 신규 NUnit 소스 컴파일이 LobbyCharacterSpawnPoint 미정의 오류로 실패함을 확인했다. Unity 내부 테스트 실행의 RED/GREEN을 대체하는 것으로 간주하지 않는다.
- Invoke-LobbySpawnPointChecks.ps1: Unity 설치에 포함된 Roslyn과 현재 프로젝트 응답 파일로 Assembly-CSharp, Assembly-CSharp-Editor, NUnit 테스트 어셈블리 컴파일 통과.
- Verify-LobbySpawnPoints.cjs: 수정 전 씬에 대해 생성 지점 누락으로 실패, 수정 후 네 지점 Transform/DB/스크립트/표시 값/부모 참조 검증 통과.
- 수정 직전 Temp/LobbySpawnPoints-before.unity와 비교: 부모 Transform과 생성 목록 컴포넌트 외 기존 YAML 블록 모두 동일, 신규 블록 12개(오브젝트/Transform/컴포넌트 각 4개).
- git diff --check: 변경 C# 파일 공백 오류 없음. Git의 LF→CRLF 정규화 안내만 출력됨.
- 독립 읽기 전용 코드 리뷰: 중요한 오류/회귀 없음.
- NUnit은 컴파일만 확인했다. Unity 직접 제어 및 batchmode는 하지 않았으므로 테스트 실행, 실제 화면/호버/클릭/인스펙터 조작은 미검증이다.
- 멀티플레이 상태/전투 핵심 로직 변경 없음. 커밋/Push/PR/브랜치/worktree 조작 없음.