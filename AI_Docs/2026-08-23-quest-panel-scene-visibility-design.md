# Quest Panel Scene Visibility Design

## Goal

Bootstrap에서 유지되는 공용 QuestPanel을 씬별로 표시하거나 숨길 수 있게 한다.

## Design

- `QuestManagerHost`는 퀘스트 진행 상태와 씬 표시 허용값을 합쳐 최종 패널 표시 여부를 결정한다.
- 새 `QuestPanelVisibilityManager`는 Bootstrap 씬에 배치되며, 현재 씬 이름을 기준으로 `QuestManagerHost`에 표시 허용값을 전달한다.
- 표시 규칙은 Bootstrap 인스펙터의 씬 이름/표시 여부 목록으로 설정한다.
- 규칙이 없는 씬은 기본값을 따른다. 기본값은 숨김으로 둔다.
- 초기 규칙은 `Lobby`, `Battle` 표시로 둔다.

## Scope

- Bootstrap 씬과 퀘스트 관련 스크립트만 수정한다.
- Lobby/Battle 씬은 수정하지 않는다.
- 전투 결과나 퀘스트 진행 저장 로직은 변경하지 않는다.

## Verification

- EditMode 테스트로 씬 표시 정책과 `QuestManagerHost`의 최종 표시 결정을 검증한다.
- MSBuild로 런타임/에디터 어셈블리 컴파일을 검증한다.
