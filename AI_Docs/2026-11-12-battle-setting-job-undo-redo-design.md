# 배틀 설정 패널 직업 아이콘 및 Undo/Redo 버튼 설계

## 의도

배틀 씬의 `Setting_panel`에서 파티 캐릭터별 직업 아이콘을 표시하고, 새로 배치된 취소/복원 버튼을 기존 예약 행동 Undo/Redo와 동일하게 동작시킨다.

## 조사 결과

- `Setting_panel/Char_content/Char1~3/Job/Image`는 존재하지만 `BattleSettingPanelUI.CharacterView`가 참조하지 않아 항상 비어 있다.
- 캐릭터별 직업 아이콘은 `CharacterIconDatabase`의 `Mark`에 등록되어 있고 실제 리소스는 `Assets/Project/Art/Image/UI/Icon/job`에 있다.
- `BattleCharacterPanelUI`는 제거된 `ResetButton`만 탐색하고 전체 예약 초기화를 연결한다.
- 현재 씬의 `CancelButton`과 `RestoreButton`에는 클릭 리스너가 없으며, 기존 우클릭/Alt+우클릭은 각각 `BattleTimelineController.UndoLastPlayerReservation()`과 `RedoLastPlayerReservation()`을 호출한다.

## 선택한 접근

- `BattleSettingPanelUI`가 각 캐릭터 뷰의 `Job/Image`를 명시적으로 찾아 `CharacterIconDatabase.TryGetMark(CharacterId)`로 표시한다.
- `BattleCharacterPanelUI`가 `CancelButton`과 `RestoreButton`을 명시적으로 찾아 각각 기존 Undo/Redo 공개 API에 연결한다.
- 타임라인 히스토리 로직은 변경하지 않고 UI 입력만 기존 API에 합류시킨다.

## 고려한 대안

- 씬 YAML에 캐릭터별 고정 스프라이트를 직접 지정하는 방식은 파티 구성이 바뀌면 잘못된 아이콘을 표시하므로 제외한다.
- 버튼 전용 Undo/Redo 로직을 새로 만드는 방식은 마우스 입력과 동작이 갈라질 수 있어 제외한다.
- `Resources.Load`로 폴더의 이미지를 이름 기반 조회하는 방식은 기존 `CharacterIconDatabase`를 우회하고 빌드 의존성을 늘리므로 제외한다.

## 범위와 제약

- 수정 대상은 배틀 설정 패널 표시와 배틀 캐릭터 패널 버튼 연결이다.
- 전투 예약 상태, 히스토리 구조, 랜덤, 저장 데이터는 변경하지 않는다.
- 씬 오브젝트 이름 탐색은 기존 호환 패턴을 유지하되 새 버튼은 serialized reference를 우선한다.
- Unity 에디터 직접 제어와 batchmode 테스트는 수행하지 않는다.

## 검증

- EditMode 테스트로 직업 아이콘 선택과 취소/복원 버튼 동작을 검증한다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 컴파일한다.
- `Battle.unity` YAML에서 새 버튼 참조가 올바르게 직렬화됐는지 확인한다.

