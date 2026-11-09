# 튜토리얼 배틀 성능 최적화 설계

## 목적

튜토리얼 전투 안내가 표시되는 동안 발생하는 프레임 저하와 반복 GC 할당을 줄인다. 전투 규칙과 튜토리얼 진행 순서는 변경하지 않는다.

## 조사 결과

- `TutorialBattleSequence.Update`가 안내 단계 동안 매 프레임 전투 UI 버튼을 전체 탐색하고 Raycast 상태를 다시 적용한다.
- `TutorialBattleSequence.LateUpdate`가 툴팁 표시 중 매 프레임 Canvas 전체 갱신과 즉시 레이아웃 재빌드를 수행한다.
- 사용되지 않는 `BattleFirstTutorialController`가 데이터 기반 튜토리얼에서도 시작 조건을 매 프레임 재검사하며 `GetForMap` 배열을 생성한다.

## 접근 방식

1. 현 구조 유지 + 변경 감지 기반 갱신(채택)
   - 단계가 변경될 때만 버튼 Raycast를 재계산한다.
   - 툴팁은 표시 시 즉시 보정하고 이후 화면 크기가 바뀔 때만 다시 보정한다.
   - 데이터 기반 튜토리얼이 있으면 구형 컨트롤러를 비활성화한다.
   - 변경 범위가 작고 기존 연출 동작을 유지할 수 있다.
2. 튜토리얼 UI 전체를 이벤트 버스로 재작성
   - 장기 구조는 명확하지만 현재 렉 수정 범위를 크게 벗어난다.
3. 일정 프레임 간격으로 폴링
   - 구현은 간단하지만 불필요한 탐색과 간헐적 스파이크가 남는다.

## 상세 설계

- 순수 정책 클래스가 단계 및 화면 크기 변경 여부를 판정한다.
- `ApplyTutorialButtonRaycasts`는 단계 변경 프레임에만 실행한다.
- `KeepTooltipInsideScreen`은 `ShowGuide` 시점의 기존 즉시 호출을 유지한다. `LateUpdate`에서는 해상도 변경 때만 호출한다.
- `TutorialDatabase.HasForMap`은 배열을 만들지 않는 조회 API를 제공한다.
- `BattleFirstTutorialController`는 현재 맵에 데이터 기반 단계가 있으면 자신을 비활성화한다.

## 비목표

- 튜토리얼 데이터, 문구, 입력 제한 규칙 변경
- 전투 판정 및 턴 실행 구조 변경
- VFX, 카메라, 사운드 연출 변경

## 멀티플레이 경계

UI 갱신 빈도와 레거시 프레젠테이션 활성 상태만 변경한다. Command, 전투 State, Result/Event에는 영향이 없다.
