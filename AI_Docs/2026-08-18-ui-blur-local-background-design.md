# UI Blur Local Background Design

## Goal

When `Equip_panel` opens from `ShopPanel`, the opened `ShopPanel` should remain visible as a blurred background behind the equip UI instead of disappearing.

## Cause

`UIBlurBackground` currently writes the captured blur texture into a separate Screen Space Overlay canvas with a very low sorting order. After capture, roots in `blurredUiRoots` are hidden through `CanvasGroup`, so a captured `ShopPanel` can appear to vanish if the blur output is rendered behind the active UI stack.

A second capture-side issue applies to `ShopPanel`: it is a child of a Screen Space Overlay root canvas. The capture path temporarily moves included child UI to a non-UI layer, but the parent root canvas can remain on the UI layer while the capture camera excludes UI. In that case the source is hidden after capture, but the capture texture does not contain the panel.

A third presentation-side issue can happen after moving the blur output under `Equip_panel`: `UIFadeInOnEnable` can collect the generated blur graphic as a normal child `Graphic` and drive its alpha to 0 during panel fade-in. If the captured source `ShopPanel` is then hidden, the visible replacement is transparent.

## Design

Render the blur texture inside the `UIBlurBackground` object's own `RectTransform` instead of a global lowest-order canvas. The generated blur surface should be the first child of the background object so it stays behind the equip panel contents while still sharing the equip panel's canvas sorting.

When a child UI root such as `ShopPanel` is explicitly included, also move its root canvas GameObject to the temporary capture layer while preserving and restoring the original layer/render mode. This lets the camera render the included UI without making unrelated canvas renderers visible.

Exclude `UIBlurBackground` graphics from `UIFadeInOnEnable` fade targets so the already-captured replacement image remains visible immediately while the equip panel's foreground UI can still fade normally.

`blurredUiRoots` keeps its existing meaning: included roots are captured and then hidden as source UI. This keeps `ShopPanel` non-interactive while its blurred image remains visible behind `Equip_panel`.

## Multiplayer Impact

This is UI presentation only. It does not change battle state, reward resolution, random selection, or command/state/result flow.
