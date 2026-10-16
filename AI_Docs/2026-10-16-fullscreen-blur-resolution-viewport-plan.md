# 전체 화면 블러 해상도 보정 제외 구현 계획

1. 기존 오진으로 추가한 패럴랙스 변경과 관련 문서·테스트를 제거한다.
2. `UIBlurBackgroundManager`의 런타임 Canvas 생성 동작을 검증하는 EditMode 회귀 테스트를 작성한다.
3. 구현 전 테스트가 `ResolutionCanvasFitOptOut` 누락으로 실패하는지 확인한다.
4. 두 블러 Canvas 생성 시 `ResolutionCanvasFitOptOut`을 추가한다.
5. 회귀 테스트와 C# 컴파일을 다시 실행한다.
6. 변경 범위와 멀티플레이 영향 여부를 검토한다.
