# Bootstrap Shared UI Blur 설계

## 목표

기존 Camera.Render 기반 UI 블러 캡처를 제거하고, URP Renderer Feature가 월드 렌더 결과를
`_UIBlurSourceTexture`로 제공하며 Bootstrap의 단일 Shared Blur UI가 이를 블러 처리한다.

## 구성

`Bootstrap/SharedBlurRoot`는 씬 전환 뒤에도 유지된다. 루트는 `UIBlurBackgroundManager`,
`SharedBlurCanvas`, `SharedBlurBackground`를 하나씩만 가진다. 매니저는 활성 요청자를
`HashSet<UIBlurBackground>`으로 관리하고 첫 요청 시 Canvas를 켜며 마지막 요청 해제 시 끈다.
파괴되거나 비활성화된 요청자는 정리한다.

`UIBlurBackground`는 기존 인스펙터의 Radius/Darken/Saturation/Contrast를 유지하고,
OnEnable/OnDisable에서 공용 매니저에 요청/해제만 수행한다. `EnsureForPanel`은 기존 호출부
호환을 위해 유지한다. 캡처 포함 UI, runtimeBlurredUiRoots, LobbyQuestMessenger 예외와 UI
숨김은 새 구조에서 의미가 없으므로 제거한다.

## 렌더링

`UIBackgroundBlurRendererFeature`는 RTHandle 하나를 재사용하고 `AfterRenderingTransparents`
시점에 카메라 Color 결과를 복사해 `_UIBlurSourceTexture`로 전역 등록한다. Game/Scene 카메라만
처리하고, 해상도와 dynamic scale 변경 시 재할당하며 Dispose에서 해제한다. Screen Space Overlay
UI는 카메라 결과에 포함되지 않으므로 일반 Popup UI가 blur source에 들어가지 않는다.

Feature는 실제 사용 가능한 `PC_Renderer.asset`와 `2D Renderer Data.asset` 모두에 연결한다.
SharedBlurCanvas는 Screen Space Overlay, override sorting, 기존 일반 Popup보다 낮은 순서로 두어
월드보다 위이고 Popup보다 아래에 표시한다. sceneLoaded에서 현재 활성 UI Camera를 찾아 Canvas에
재연결하되 Overlay 정렬에는 의존하지 않는다.

## 정리 대상

`UIBlurBackgroundCaptureManager`, `UIBlurInclude` 및 Camera.Render, 임시 RT, 캡처 카메라,
CanvasRenderer 숨김, 레이어/Canvas 상태 변경, 캡처용 CameraMouseParallax pause 로직을 삭제한다.
이 기능에만 의존하는 기존 EditMode 테스트는 공용 요청 관리와 캡처 API 부재를 검증하는 테스트로 교체한다.

## 검증

EditMode 테스트로 중복 Request/Release, 파괴된 요청자 정리, 단일 공용 Canvas 동작을 검증한다.
컴파일 후 Unity Editor에서 Lobby/Battle, 중첩 패널, 씬 전환, 2D Light/VFX, 해상도 변경을 확인한다.
