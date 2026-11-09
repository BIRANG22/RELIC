# UI Blur Button Tint Sync Plan

1. Add an EditMode regression test with a white Image and a black CanvasRenderer runtime tint.
2. Add coverage for component-wise source color, renderer tint, and CanvasGroup alpha composition.
3. Confirm the current replica incorrectly uses only the source Image color.
4. Update the shared blur Graphic color synchronization.
5. Run regression-condition checks, diff validation, and Assembly-CSharp compilation.

