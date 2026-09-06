# Manual UI Blur Exception 설계

## 목표

로비 씬에서 블러 패널이 열릴 때 기본적으로 모든 UI를 블러 캡처에서 제외하고, 각 `UIBlurBackground` 인스펙터에 직접 연결한 일부 UI만 블러 결과에 포함한다.

## 문제

- 기존 구조는 `UIBlurInclude`를 씬 전체에서 자동 검색해 캡처에 포함했다.
- 캡처에 포함된 UI가 블러 텍스처로 한 번 그려지고, 원본 UI도 다시 선명하게 렌더되면 뒤쪽에 흐릿한 복사본이 겹쳐 보인다.
- UI 레이어가 `UI`가 아닌 경우에는 레이어 기반 제외만으로 일반 UI를 안정적으로 제외할 수 없다.

## 권장 구조

- `UIBlurBackground`가 블러 예외 UI 목록을 직접 소유한다.
  - 필드명: `blurredUiRoots`
  - 타입: `GameObject[]`
  - 인스펙터에서 패널별로 블러에 포함할 UI 루트를 직접 연결한다.
- `UIBlurBackgroundCaptureManager.CaptureBackgroundNow`는 예외 UI 목록을 인자로 받아 캡처한다.
- 캡처 매니저는 캡처 직전에 활성 UI 렌더러를 숨기고, 전달받은 예외 UI만 임시로 다시 보이게 한다.
- 블러 패널이 표시되는 동안 예외 원본 UI는 숨긴다. 이렇게 하면 블러 텍스처 안의 흐릿한 UI와 원본 UI가 겹쳐 보이지 않는다.
- `UIBlurInclude` 자동 검색과 presentation hide 구조는 더 이상 사용하지 않는다.

## 데이터 흐름

1. 블러 패널의 `UIBlurBackground.OnEnable`이 호출된다.
2. `UIBlurBackground`가 `blurredUiRoots`를 캡처 매니저에 전달한다.
3. 캡처 매니저가 일반 UI 그래픽을 임시로 숨긴다.
4. 연결된 예외 UI 루트만 임시로 보이게 한다.
5. 카메라 화면을 캡처해 블러 텍스처를 만든다.
6. 모든 임시 상태를 복구한다.
7. 블러 패널이 켜져 있는 동안 `UIBlurBackground`가 예외 원본 UI를 숨긴다.
8. 블러 패널이 꺼지면 예외 원본 UI를 원래 상태로 복구한다.

## 검증

- EditMode 테스트로 `UIBlurBackground`가 인스펙터 예외 목록을 캡처 매니저에 전달할 수 있는 공개/내부 API를 확인한다.
- EditMode 테스트로 `UIBlurBackgroundCaptureManager`가 예외 목록을 정규화하고, null 및 중복을 제거하는지 확인한다.
- 로비 씬에서 `UIBlurInclude` 자동 마커가 남아 있지 않은지 확인한다.
- MSBuild로 C# 컴파일을 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙에 따라 실행하지 않는다.

## 멀티플레이 영향

UI 표시와 캡처 방식만 변경한다. 전투 결과, 로비 상태, Command, State Change, Result/Event, 네트워크 동기화 데이터에는 영향을 주지 않는다.
