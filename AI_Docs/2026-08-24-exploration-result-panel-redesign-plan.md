# Exploration Result Panel Redesign Plan

## Steps

1. 소스 테스트를 추가해 결과패널이 런타임 생성 없이 씬 배치 참조를 사용하는지 검증한다.
2. `ExplorationResultPanelUI`를 구조화 바인딩 방식으로 변경한다.
3. 결과패널 캐릭터 행 전용 컴포넌트를 추가한다.
4. `Battle.unity`의 `ExplorationResultPanel` 하위에 헤더, 스테이지 정보, 요약, 캐릭터 행, 슬라이더, 버튼 오브젝트를 배치하고 직렬화 참조를 연결한다.
5. MSBuild로 컴파일을 검증한다.

## Test Scope

- 테스트 파일은 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있다고 가정하므로 batchmode 테스트는 실행하지 않는다.
- 소스 테스트와 MSBuild로 구조/컴파일을 검증한다.
