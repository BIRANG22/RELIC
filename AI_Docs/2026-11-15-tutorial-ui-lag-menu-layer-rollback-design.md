# 튜토리얼 UI 렉 및 메뉴 레이어 원상복구 설계

## 조사 결과

- `TutorialFocusGuideRunner`가 `Canvas.preWillRenderCanvases`마다 비활성 오브젝트를 포함한 모든 `RectTransform`을 다시 검색한다.
- Battle 씬에는 약 1,719개, DebugBattle 씬에는 약 1,319개의 `RectTransform`이 있어 렌더 직전 전역 검색과 반복 `GetComponent` 호출이 큰 프레임 비용을 만든다.
- 같은 전역 검색은 0.25초 간격의 `Update`에서도 실행된다.
- `MenuOverlaySortingGuard`도 메뉴가 열린 동안 렌더 직전마다 모든 Canvas를 검색한다.
- `MenuOverlaySortingGuard`는 메뉴를 최상위 Sorting Layer의 order 32767로 이동하지만 설정·기록·포기 확인 패널은 기본 레이어의 order 30004 이상을 사용한다. Sorting Layer가 order보다 우선하므로 메뉴 모달이 메뉴 뒤에 가려진다.

## 권장 설계

1. `MenuOverlaySortingGuard`와 연결 코드를 제거하여 메뉴를 별도 최상위 Sorting Layer로 이동하지 않는다.
2. 동일 Sorting Layer에서 일반 튜토리얼 포커스 UI는 29800~29930, 보상 튜토리얼 포커스 UI는 최대 32630, 메뉴는 32700~32701, 메뉴 모달은 32702~32760을 사용한다.
3. 튜토리얼 툴팁 전역 검색은 포커스 시작 시 한 번만 수행하고, 이후 동적 툴팁은 활성화 시 자신을 등록한다.
4. 렌더 직전에는 등록된 Canvas 목록만 순회하여 정렬을 재적용하며, 씬 전체 오브젝트 검색을 실행하지 않는다.
5. 기존 메뉴 블러 대상과 툴팁 전면 표시 동작은 유지한다.

## 검증 계획

- EditMode 회귀 테스트로 렌더 직전 캐시 적용이 새 툴팁을 전역 검색하지 않는지 확인한다.
- EditMode 회귀 테스트로 `튜토리얼 툴팁 < 메뉴 루트 < 메뉴 패널 < 설정 패널` 순서를 확인한다.
- Assembly-CSharp와 회귀 테스트가 포함된 Assembly-CSharp-Editor를 컴파일한다.
- Unity 에디터 화면 및 Profiler 확인은 프로젝트 규칙에 따라 미검증으로 보고한다.

## 멀티플레이 경계

UI Canvas 검색과 정렬만 변경한다. 전투 Command, 상태 변경, 결과 이벤트 및 랜덤 처리에는 영향이 없다.
