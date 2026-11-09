# UI Blur Button Tint Sync Design

## Problem

The lobby blur replica copies `Graphic.color` but not the runtime color multiplier stored in `CanvasRenderer`. Unity `Button` Color Tint writes its state color to the target graphic's `CanvasRenderer`, so white source images tinted black in the normal button state become white in the blur replica.

## Design

- Treat the rendered graphic color as the component-wise product of `Graphic.color` and `CanvasRenderer.GetColor()`.
- Continue multiplying the resulting alpha by the effective parent `CanvasGroup` alpha.
- Apply the behavior in the shared `GraphicPair.Sync()` path so Image, RawImage, and TMP replicas receive the same runtime tint synchronization.
- Preserve all existing material, sprite, layout, and visibility synchronization behavior.

