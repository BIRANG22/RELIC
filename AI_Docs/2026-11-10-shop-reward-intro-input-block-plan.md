# Shop Reward Intro Input Block Plan

1. Add an EditMode regression test for a transparent, stretched, raycast-target input shield and its enable/disable lifecycle.
2. Confirm the test fails because the current panel has no intro input shield.
3. Create the shield lazily under `Relic_panel`, keep it behind the item, and exclude it from the intro child-hiding loop.
4. Enable the shield at intro start and disable it on completion, reset, and panel disable.
5. Run regression-condition checks and compile checks available without directly controlling the Unity Editor.

