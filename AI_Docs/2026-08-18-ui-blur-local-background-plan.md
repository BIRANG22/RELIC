# UI Blur Local Background Plan

1. Add an EditMode regression test that requires `UIBlurBackground` to create its blur output under the owning background RectTransform.
2. Add an EditMode regression test that requires the capture manager to move the included UI root's parent root canvas to the temporary capture layer.
3. Change `UIBlurBackground` so the runtime blur object is a local `RawImage` surface, not a global Overlay Canvas.
4. Change `UIBlurBackgroundCaptureManager` so child UI roots can bring their root canvas into the capture camera mask while restoring it afterward.
5. Exclude `UIBlurBackground` graphics from `UIFadeInOnEnable` fade targets so the captured blur output is not made transparent.
6. Keep source-root capture and hide/restore behavior unchanged.
7. Run targeted EditMode tests through the project test runner path available in the repo.
