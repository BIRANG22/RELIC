# 이벤트 선택지 번역 키 복구 설계

## 문제

이벤트 선택지 UI는 `EventId + ChoiceOrder + Field`로 구성한 정식 키를 조회한다.
예: `data.event.event_02.choice_1_name`.

현재 `Localization.xlsx`와 Unity String Table에는 과거의 원문 해시 기반 키가 대부분 남아 있고,
정식 키는 일부만 존재한다. 따라서 이벤트 선택지 표시 시 키 조회가 실패하여 선택 로케일의
미번역 표기가 노출된다.

## 권장 설계

1. 런타임 키 생성 규칙은 변경하지 않는다.
2. `GameData.xlsx`의 Event 시트에서 `EventId`, `ChoiceOrder`, 현지화 필드를 읽어 정식 키를 만든다.
3. 같은 한국어 원문을 가진 기존 해시 키 행에서 모든 로케일 번역을 복사해 정식 키 행을 만든다.
4. 정식 키가 이미 존재하면 해당 행을 유지하며, 빈 번역만 기존 행에서 보충한다.
5. 기존 해시 키는 이번 작업에서 삭제하지 않아 다른 참조에 영향을 주지 않는다.
6. `Localization.xlsx`를 Unity String Table로 다시 가져온다.

## 검증 계획

- 기존 `EventChoiceCanonicalKeys_CoverEveryLocalizedChoiceField` 테스트가 수정 전 실패하는지 확인한다.
- 이관 후 같은 테스트와 전체 EditMode 테스트를 실행한다.
- 런타임/에디터 어셈블리를 컴파일한다.
- 데모 타이틀에서 배틀 진입 후 이벤트 선택지에 정식 키 번역이 표시되는지 Unity 에디터에서 확인한다.

## 멀티플레이 영향

번역 데이터와 표시 계층만 수정한다. 전투 명령, 상태 변경, 결과 이벤트 및 랜덤 처리에는 영향이 없다.
