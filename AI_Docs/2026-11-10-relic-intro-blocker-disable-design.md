# 유물 보상 입력 차단기 비활성화 오류 수정 설계

## 목적

유물 추출 후 `Relic_panel`이 비활성화될 때 `__IntroInputBlocker`의 sibling 위치를 변경해 발생하는 Unity 오류를 제거한다. 유물 추출 결과와 기존 인트로 입력 차단 동작은 유지한다.

## 원인

`CloseWithoutEquip()`이 패널을 비활성화하면 `OnDisable()`에서 `ResetItemIntro()`가 호출된다. 이 경로가 `SetIntroInputBlocked(false)`를 호출하지만, 현재 구현은 차단 해제 시에도 `SetAsFirstSibling()`을 실행한다. Unity는 부모 GameObject가 활성화 또는 비활성화되는 동안 자식의 sibling 위치 변경을 허용하지 않는다.

## 선택한 설계

`SetAsFirstSibling()`은 차단기를 활성화하는 `blocked == true` 경로에서만 실행한다. 차단 해제 경로는 `SetActive(false)`만 수행한다. 차단기 생성과 전체 패널 크기, 투명 raycast 대상 설정은 변경하지 않는다.

## 고려한 대안

- `OnDisable()`에서 입력 차단기 정리를 생략하면 다른 비활성화 경로의 상태 복구가 약해지므로 제외한다.
- 차단기를 매번 파괴하면 불필요한 생성·파괴가 반복되고 현재 재사용 구조를 바꾸므로 제외한다.

## 검증

- 실제 GameObject를 사용한 EditMode 회귀 테스트에서 차단기를 만든 뒤 부모 패널을 비활성화하고 예상하지 않은 오류 로그가 없는지 확인한다.
- 런타임 및 테스트 어셈블리를 컴파일하고 `git diff --check`를 실행한다.
- 프로젝트 규칙에 따라 Unity 에디터 직접 제어와 batchmode 테스트는 수행하지 않는다.
