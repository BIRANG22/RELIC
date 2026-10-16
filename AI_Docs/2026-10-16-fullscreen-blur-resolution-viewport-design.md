# 전체 화면 블러 해상도 보정 제외 설계

## 조사 결과

- `UIBlurBackgroundManager`가 만드는 `SharedBlurBackground`는 원래 `SharedBlurCanvas` 전체에 스트레치된다.
- `ResolutionManager.ApplyCanvasViewports`는 활성화된 모든 루트 화면 Canvas에 `ResolutionCanvasViewportFitter`를 자동 추가한다.
- 이 Fitter는 Canvas의 직접 자식을 `Resolution Viewport` 아래로 재배치하고, 해당 루트를 레터박스 viewport 크기로 제한한다.
- 따라서 `SharedBlurBackground`가 실제 화면 전체가 아니라 기준 해상도 viewport까지만 그려져 화면 가장자리에 미세한 미적용 영역이 생긴다.
- `ResolutionCanvasFitOptOut`은 화면 픽셀 크기를 그대로 유지해야 하는 루트 Canvas를 이 보정에서 제외하기 위한 기존 표식이다.

## 권장 설계

- `SharedBlurCanvas`와 블러 복제용 `BlurReplicaRoot` 생성 시 `ResolutionCanvasFitOptOut`을 함께 부착한다.
- 블러 Canvas의 전체 화면 스트레치와 RenderTexture 크기 계산은 기존 구현을 유지한다.
- 해상도 보정 시스템의 일반 UI 정책은 변경하지 않는다.
- 회귀 테스트로 두 런타임 Canvas가 해상도 보정 대상에서 제외되는지 검증한다.
- 원인이 아니었던 카메라 패럴랙스 정지 변경은 제거한다.

## 검증 기준

- 공유 블러 Canvas와 복제 Canvas 모두 `ResolutionCanvasFitOptOut`을 가진다.
- `ResolutionManager.ShouldFitCanvasForResolution`이 두 Canvas에 `false`를 반환한다.
- 런타임 및 에디터 코드가 컴파일된다.
- 기존 블러 리소스 검증이 유지된다.

## 영향 범위

- UI 표시 및 렌더링 경계만 변경한다.
- 전투 상태, 랜덤, Command/Result 흐름과 멀티플레이 동기화에는 영향이 없다.
