# WebGL 브라우저 주도 뷰포트 구현 계획

1. `ResolutionManager`에 플랫폼별 뷰포트 정책을 판별하는 순수 API를 추가한다.
2. EditMode 테스트를 먼저 추가해 WebGL Player와 Windows Player 정책을 고정하고, 실패를 확인한다.
3. WebGL Player에서는 `Screen.SetResolution`과 브라우저 Canvas 자체의 크기 변경을 건너뛰고, 카메라 레터박스를 유지한다.
4. EditMode 테스트와 C# 빌드를 실행해 컴파일 및 회귀를 검증한다.

## 보완 작업: UI 레터박스 정렬

1. WebGL UI 콘텐츠도 카메라와 동일한 레터박스 viewport에 맞춘다는 정책을 테스트로 고정한다.
2. 브라우저 Canvas의 크기 변경 없이 Root Canvas 콘텐츠 보정만 WebGL에 다시 적용한다.
