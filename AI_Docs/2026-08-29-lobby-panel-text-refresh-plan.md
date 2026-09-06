# Lobby Panel Text Refresh Plan

## Context
- ErosionSelectPanel open state can show clipped `ErosionText` line/text until the object is disabled and enabled again.
- CultureTankPanel open state can show clipped `CompoundText` line/text with the same disable/enable recovery.
- Both symptoms point to TextMeshPro mesh and Unity UI layout timing after runtime activation.

## Design
- Reuse the existing `MenuPanelTextRefresher` component.
- When ErosionSelectPanel opens, ensure the refresher exists on the panel and run immediate plus delayed refresh.
- When CultureTankPanel opens or re-enables, ensure the refresher exists on the panel root and run immediate plus delayed refresh.
- Keep this as a UI presentation refresh only. Do not change gameplay state, data, combat logic, or multiplayer synchronization.

## Verification
- Add EditMode source regression tests for both panel open flows.
- Run the targeted EditMode test assembly through MSBuild only. Unity batchmode tests are intentionally not used because the project rule says the editor is always open and batchmode tests should not be attempted.
