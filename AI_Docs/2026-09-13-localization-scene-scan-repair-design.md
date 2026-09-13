# 로비 씬 현지화 스캔 및 바인딩 복구

## 문제

Unity 씬 YAML의 `m_text`는 한글이 `\uXXXX` 이스케이프로 저장된다. 현지화 매니저의 씬 스캐너는 이를 복원하지 않고 한글 여부를 검사해 신규 항목을 누락한다. 누락된 원문은 워크북에 키가 없으므로, 잘못된 기존 바인딩도 복구 도구가 교체할 수 없다.

## 설계

`LocalizationProjectScanner`에 Unity YAML 문자열 값을 실제 문자로 복원하는 순수 함수를 추가한다. 씬 스캐너는 `m_text` 캡처값을 이 함수로 복원한 값으로 후보 생성, 키 조회, 신규 키 생성을 수행한다.

로비의 정적 라벨은 다음 의미 기반 키로 정리한다.

- `ui.lobby.storage`: 보관함
- `ui.lobby.compound`: 연성제
- `ui.lobby.compound_recipe`: 연성식
- `ui.lobby.relic_info`: 유물 정보
- `ui.lobby.character_info`: 정보창

기존 오연결은 원문과 키의 Korean 원문이 불일치할 때 복구 도구가 위 키로 교체한다. 동적 텍스트는 기존 `LocalizationIgnore` 정책을 유지한다.

## 검증

EditMode 테스트에서 이스케이프된 YAML 텍스트의 복원, 일반 문자열 보존, 제안 키의 안정성을 검증한다. Unity 에디터 Test Runner에서 해당 테스트와 기존 현지화 EditMode 테스트를 실행한다.

## 범위 제외

조명 데이터, 외부 VFX 프리팹 누락, Unity 에디터 assertion은 현지화 바인딩과 별개다. 이번 변경은 전체 씬 스캔이 Unity 씬을 열지 않도록 유지해 assertion 재발 가능성을 늘리지 않는다.
