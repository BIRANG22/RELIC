# Character Info Text Ownership Implementation Plan

**Goal:** Restore the Lobby CharacterSettingPanel text ownership split without changing shared localization systems.

**Constraints:** Preserve existing uncommitted changes; modify only the relevant editor migration, targeted edit-mode tests, Lobby scene bindings, and this documentation. Do not commit, push, or create a PR.

### Task 1: Test runtime-owned InfoText configuration

- [ ] Add a failing edit-mode test for removing `LocalizedTMPText` from a TMP object and adding `LocalizationIgnore`.
- [ ] Run the targeted test and confirm it fails because the helper does not exist.
- [ ] Add the minimum editor helper and rerun the test.

### Task 2: Repair the scene safely

- [ ] Replace parent traversal with `FindObjectsByType<CharacterInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)`.
- [ ] Compare the `storyText` SerializedProperty reference against the target `InfoText` by identity.
- [ ] Keep `storyText` bound to `InfoText`; do not clear it or rebind it to `CharacterinfoText`.
- [ ] Remove the `LocalizedTMPText` owner from `InfoText`, add `LocalizationIgnore`, and save the Lobby scene only if changed.

### Task 3: Verify

- [ ] Run the relevant edit-mode tests through the existing Unity editor test runner.
- [ ] Compile the affected assemblies without batchmode.
- [ ] Report the manual Play Mode scenarios that still require editor interaction.
