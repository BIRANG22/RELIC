# Battle Monster Info Freeze Diagnostics Implementation Plan

**Goal:** 반복 개폐 정지의 입력·일시정지·선택 복원·전투 액션 경계를 동일한 진단 로그로 연결한다.

**Architecture:** 순수 포맷 함수와 런타임 상태 캡처를 제공하는 공통 진단 클래스를 사용한다. 관련 컴포넌트는 상태를 실제로 변경하는 지점에서만 공통 진단 함수를 호출한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-21-battle-monster-info-freeze-diagnostics-design.md`

## 작업

1. 공통 진단 문자열의 필수 필드를 검증하는 실패 테스트를 작성한다.
2. 공통 진단 포맷과 상태 캡처를 구현한다.
3. 몬스터 정보 패널, 메뉴 pause, 우클릭 소비자, 전투 실행과 액션 타임아웃에 계측을 연결한다.
4. 테스트 소스, Assembly-CSharp, Assembly-CSharp-Editor 컴파일 및 diff를 검증한다.

## 제한

- 전투 및 입력 동작은 변경하지 않는다.
- 프레임별 로그는 추가하지 않는다.
- Unity 에디터 직접 제어 및 batchmode 테스트는 수행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.
