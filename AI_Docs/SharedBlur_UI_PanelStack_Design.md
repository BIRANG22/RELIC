# Shared Blur UI Panel Stack Design

## 조사 결과

- 기존 월드 블러는 `UIBackgroundBlurRendererFeature`가 `_UIBlurSourceTexture`를 갱신하고 `DustiumBackgroundBlur.shader`가 그 텍스처를 샘플링하는 구조다.
- `UIBlurBackgroundManager`는 기존에 `HashSet<UIBlurBackground>`와 `activeRequester`로 요청자를 관리해 중첩 패널의 순서를 안정적으로 표현할 수 없었다.
- `UIBlurBackground.EnsureForPanel(panelRoot)` 호출 흐름은 로비 유물 상점, 로비 일부 패널, 전투 보상 장착 패널에서 이미 사용 중이다.
- `UIBlurBackgroundCaptureManager`에는 과거 `Camera.Render()` 기반 캡처 코드가 남아 있지만, 현재 요구사항에서는 Renderer Feature 기반 Shared Blur를 유지해야 하므로 사용하지 않는다.
- 로비 씬에는 `RelicShopPanel`, `Setting_upper`, `SettingButton`이 있으며 `SettingButton`은 `Setting_upper` 아래에 있다.
- 전투 씬에는 `BattleRewardPanelUI`, `BattleHUDCanvas`, `MenuRoot`가 존재한다.

## 구현 설계

- `UIBlurBackgroundManager` 요청자 저장소를 ordered `List<UIBlurBackground>`로 변경한다.
- 요청 시 기존 항목을 제거한 뒤 뒤에 다시 추가해 중복 요청을 방지하고, 마지막 유효 요청자를 Top으로 사용한다.
- Disable/Destroy/Scene 전환 때 요청자를 제거하고 null 또는 비활성 요청자는 refresh 시 자동 정리한다.
- Top 패널은 부모를 옮기지 않고 패널 root의 Canvas sorting order만 `SharedBlurCanvas`보다 높게 임시 적용해 선명하게 렌더한다.
- Top에서 내려간 패널은 저장된 Canvas 상태로 복원되어 blur source에 다시 포함될 수 있게 한다.
- 일반 UI 및 이전 패널은 blur 활성 중 root canvas를 `Screen Space - Camera`로 임시 라우팅해 Renderer Feature가 복사하는 카메라 결과에 들어가게 한다.
- `Setting_upper`, `SettingButton`, `MenuRoot`는 부모를 옮기지 않고 Canvas sorting order만 높여 항상 선명하게 유지한다.
- 모든 Canvas 상태 변경은 원래 상태를 저장하고 blur 종료 시 복원한다. `Canvas`/`GraphicRaycaster`를 매번 삭제하지 않으므로 dependency warning을 만들지 않는다.

## 보존 범위

- `UIBackgroundBlurRendererFeature`의 capture timing과 `_UIBlurSourceTexture` 구조는 변경하지 않는다.
- `DustiumBackgroundBlur.shader`의 Radius, Darken, Saturation, Contrast 계산은 변경하지 않는다.
- 월드 SpriteRenderer, 캐릭터, VFX 블러 경로는 변경하지 않는다.

## 검증 기준

- A -> B -> C 순서로 패널이 열리면 C만 sharp이고 A/B는 blur source로 복원된다.
- C -> B -> A 순서로 닫으면 매번 마지막 유효 요청자만 sharp가 된다.
- 중간 요청자가 Disable/Destroy되어도 Top 요청자가 유지된다.
- `Setting_upper`, `SettingButton`, `MenuRoot`는 blur 활성 중 Shared Blur보다 높은 sorting order를 받고, blur 종료 시 원래 Canvas 상태로 돌아간다.
