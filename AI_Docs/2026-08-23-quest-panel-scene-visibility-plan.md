# Quest Panel Scene Visibility Plan

1. 기존 `QuestManagerHost`, `QuestPanelPresenter`, Bootstrap 씬 연결 상태를 확인한다.
2. `QuestManagerHost`가 씬 표시 허용값을 반영하도록 테스트를 추가한다.
3. `QuestPanelVisibilityManager`의 씬 이름 규칙 해석 테스트를 추가한다.
4. `QuestManagerHost`에 씬 표시 허용 상태와 갱신 API를 구현한다.
5. `QuestPanelVisibilityManager`를 추가하고 씬 로드 이벤트에서 정책을 적용한다.
6. Bootstrap 씬의 Bootstrap 오브젝트에 `QuestPanelVisibilityManager`를 추가하고 기본 규칙을 설정한다.
7. 컴파일 및 씬 변경 범위를 검증한다.
