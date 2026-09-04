# Shared Blur UI Fixed Root Rework

## 조사 결과

- 기존 월드 블러는 `UIBackgroundBlurRendererFeature`가 `_UIBlurSourceTexture`를 갱신하고 `DustiumBackgroundBlur.shader`가 그 텍스처를 샘플링하는 구조다.
- 이번 문제는 Renderer Feature나 셰이더가 아니라 UI 배치 계층 문제다.
- `UIBlurBackground.PanelRoot`가 블러 컴포넌트가 패널 루트에 붙은 경우에도 부모 오브젝트를 패널 루트로 반환해, `MenuPanel`을 열 때 같은 부모 아래의 로비 UI가 패널처럼 취급될 수 있었다.
- 이전 동적 Canvas 라우팅 방식은 패널 루트와 일반 UI를 계속 재설정해 `SettingButton` 클릭 차단, 내부 UI 누락, 기존 UI가 사라진 뒤 블러되는 문제를 만들 수 있었다.
- `UIBlurBackgroundCaptureManager`에는 `Camera.Render()` 기반 캡처 코드가 남아 있지만, 이번 로비 Shared Blur 흐름에서는 사용하지 않는다.

## 구현 설계

- `UIBlurBackgroundManager` 요청자 저장소를 ordered `List<UIBlurBackground>`로 관리한다.
- 런타임에 고정 루트 `BlurredUIRoot`, `SharpUIRoot`를 한 번 준비한다.
- `BlurredUIRoot`는 `Screen Space - Camera` Canvas로 만들고, Renderer Feature가 복사하는 카메라 결과에 포함되게 한다.
- `SharedBlurBackground`는 기존처럼 `_UIBlurSourceTexture`를 샘플링하는 overlay Canvas로 유지한다.
- `SharpUIRoot`는 `SharedBlurBackground`보다 높은 overlay Canvas로 만들고, 현재 top 패널과 `Setting_upper`만 배치한다.
- 패널이 열리면 현재 로비 UI와 이전 패널을 `BlurredUIRoot`로 옮기고, 마지막으로 top 패널과 `Setting_upper`를 `SharpUIRoot`로 옮긴다.
- 패널이 모두 닫히면 이동했던 Transform의 부모, sibling index, RectTransform 값을 원래대로 복구한다.
- 패널 루트나 하위 UI에 Canvas/GraphicRaycaster를 매번 추가하거나 RenderMode/sortingOrder를 반복 변경하지 않는다.

## 검증 기준

- A -> B 순서로 패널이 열리면 B만 `SharpUIRoot`이고 A는 `BlurredUIRoot`에 있다.
- B가 닫히면 A가 다시 top 패널이 된다.
- 마지막 패널이 닫히면 이동했던 UI가 원래 부모와 sibling index로 돌아간다.
- `SharedBlurBackground`는 raycast를 막지 않는다.
- `Setting_upper`만 `SharpUIRoot`에 있고, `Setting_under`를 포함한 나머지 Setting UI는 `BlurredUIRoot`에 남아 blur source에 포함된다.
