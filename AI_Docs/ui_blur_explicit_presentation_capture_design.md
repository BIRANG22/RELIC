# UI Blur Renderer Feature Source Design

## Problem

The previous hierarchy-free blur implementation still changed arbitrary Canvas components at runtime. It scanned scene Canvas objects, changed render mode and sorting, and added Canvas or GraphicRaycaster components to panel roots. This broke existing Lobby UI because some panels already contain independent child Canvas objects and some opener scripts overwrote the same sorting values.

## Direction

`UIBlurBackgroundManager` must not own arbitrary Canvas state or render a private capture camera. A blur requester owns explicit metadata:

- `UIBlurBackground.presentationCanvases`
- optional serialized `blurredUiRoots`
- optional runtime `SetRuntimeBlurredUiRoots`

The manager uses this metadata only to understand which requester is active and which panel is the stack top. It must not enable, disable, reparent, reorder, or rewrite these Canvas components. The blur source is the texture already produced by the URP renderer feature and exposed through `UIBackgroundBlurRendererFeature.SourceTexture` / global `_UIBlurSourceTexture`.

## Render Model

Blur source:

- world camera render copied by `UIBackgroundBlurRendererFeature`
- no `Camera.Render()` UI capture path
- no temporary capture camera or capture render texture owned by the blur manager

Overlay:

- `SharedBlurCanvas` renders `UIBackgroundBlurRendererFeature.SourceTexture` through `UI/DustiumBackgroundBlur`

Sharp:

- requester panel roots remain in their prefab/scene hierarchy
- `Setting_upper`, configured as a scene Canvas above shared blur

## Lobby Configuration

- `RelicShopPanel`: root Canvas plus the three `RelicIcon` Canvas objects are registered as one presentation group.
- `CultureTankPanel`: root Canvas plus `Inven/Storage` Canvas are registered as one presentation group.
- `ErosionSelectPanel`: root Canvas is registered as its presentation group.
- `MenuPanel`: prefab root Canvas is registered as its presentation group.
- `Setting_upper`: scene Canvas is configured as always-sharp and is not managed at runtime.

## Rules

- No `SetParent` for blur presentation.
- No blur-system `SetSiblingIndex` or `SetAsLastSibling`.
- No runtime `AddComponent<Canvas>` or `AddComponent<GraphicRaycaster>` from blur presentation code.
- No scene-wide Canvas scanning or mutation by `UIBlurBackgroundManager`.
- No requester `Canvas.enabled = false` hiding.
- No `UIBlurBackgroundCaptureManager` or `CaptureBackgroundNow` production path.
- Existing EventSystem and GraphicRaycaster setup remains intact.
