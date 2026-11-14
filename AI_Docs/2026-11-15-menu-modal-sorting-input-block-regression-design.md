# 메뉴 모달 정렬 및 전투 입력 차단 회귀 수정 설계

## 조사 결과

- 메뉴 전용 최상위 Sorting Layer 가드는 제거됐지만, 튜토리얼 메뉴 order는 원래 값이 아니라 `32700/32701`로 유지되고 있다.
- 메뉴 승격 조건은 `IsAnyTutorialPresentationRunning()`이고 설정·기록 패널 승격 조건은 별도의 튜토리얼 판정이어서, 메뉴가 `32701`인데 모달은 `30000`에 머무를 수 있다.
- `BattleMenuEscapeInputController`는 메뉴가 열리면 씬의 모든 활성 `Selectable`을 직접 `interactable=false`로 바꾼다. 메뉴 판정이 잘못되거나 복원 상태가 끊기면 스킬 슬롯과 HUD 전체가 비활성화된다.
- `MenuPanel` 자동 탐색은 현재 씬과 `MenuRoot` 소속을 제한하지 않아 이름이 같은 다른 패널을 선택할 수 있다.
- Battle 씬 YAML의 `MenuPanel`은 비활성이고 Default Sorting Layer로 저장되어 있으며, 에디터 로그는 전투 입력 준비 완료까지 정상이다. 전투 핵심 초기화보다 메뉴 UI 상태 관리 경로가 증상과 일치한다.

## 권장 설계

1. 메뉴 모달을 열 때 실제 활성 `MenuPanel` Canvas를 조회하고, 같은 Sorting Layer에서 메뉴보다 높은 order를 적용한다.
2. 설정·기록·포기 확인·전투 설정 패널이 동일한 정렬 정책을 사용하게 한다.
3. `MenuPanel`과 배경 차단 Graphic은 Battle/DebugBattle 씬에서 명시적으로 연결하고, 자동 탐색 fallback은 컨트롤러와 같은 씬의 `MenuRoot` 자식으로 제한한다.
4. 메뉴 입력 차단은 `MenuRoot/Image`의 raycastTarget만 사용하고, 전체 `Selectable.interactable` 변경은 제거한다.
5. 활성화·비활성화 시 배경 차단 상태를 명시적으로 동기화해 이전 실행 상태가 남지 않게 한다.

## 검증 계획

- EditMode 회귀 테스트로 메뉴 기준 모달 정렬값과 Sorting Layer 상속을 검증한다.
- EditMode 회귀 테스트로 메뉴가 열려도 메뉴 밖 Button의 interactable 값이 유지되는지 검증한다.
- Battle/DebugBattle YAML의 명시적 참조와 초기 비활성 상태를 정적 검사한다.
- Assembly-CSharp 및 Assembly-CSharp-Editor를 컴파일한다.
- Unity 에디터 화면 확인은 프로젝트 규칙에 따라 미검증으로 보고한다.

## 멀티플레이 경계

Canvas 정렬과 UI raycast 차단만 변경한다. 전투 Command, 상태 변경, 결과 이벤트, 랜덤 및 저장 데이터에는 영향이 없다.
