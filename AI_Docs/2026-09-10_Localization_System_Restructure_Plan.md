# Localization System Restructure Implementation Plan

**Goal:** Make localization ownership deterministic for static UI, dynamic UI/system text, and game data while providing safe validation and repair.

**Architecture:** Workbook source resolution is shared by migration, repair, scanning, and runtime auto-localization. Static bindings persist source/key; dynamic output receives localized display text from presenter code. Editor validation is read-first and only writes uniquely resolvable repairs.

**Spec:** `AI_Docs/2026-09-10_Localization_System_Restructure_Design.md`

## Global Constraints

- Preserve existing uncommitted changes; do not commit, push, create PRs, branches, or worktrees.
- Write project documents only under `AI_Docs` and tests only under `Assets/Tests/EditMode~/` or `Assets/Tests/PlayMode~/`.
- Do not run Unity batchmode.
- UI/VFX do not change combat state; localization consumes display data only.

### Task 1: Deterministic binding resolution

**Files:** `LocalizationTextBindingRepairTool.cs`, `StaticLocalizationMigration.cs`, `RuntimeTMPTextAutoLocalizer.cs`, EditMode localization tests.

- [ ] Add failing tests for duplicate Korean source candidates and source/key validation.
- [ ] Verify the tests fail because the code retains the first key.
- [ ] Replace first-key maps with normalized candidate lists and return explicit resolution statuses.
- [ ] Restrict migration/repair to unique candidates and retain ambiguous bindings unchanged.
- [ ] Run the focused EditMode tests.

### Task 2: Static/dynamic ownership

**Files:** `LocalizedTMPText.cs`, new dynamic marker, auto-localizer, migration/repair/lock tools, tests.

- [ ] Add failing tests proving ignored/dynamic TMP cannot be managed or reconfigured by static tools.
- [ ] Add a serialized marker and central ownership predicates.
- [ ] Make auto-localization skip existing bindings, marker targets, and non-unique sources; add re-entry protection.
- [ ] Ensure enable/disable and validation use the same ownership predicate.
- [ ] Run focused EditMode tests.

### Task 3: Key-only runtime API and game-data formatting

**Files:** `GameLocalization.cs`, new `LocalizationKeys.cs`, `GameDataLocalization.cs`, data display call sites, workbook, tests.

- [ ] Add failing tests for key-only fallback behavior and localized-before-token formatting.
- [ ] Add key-only Get/Format APIs and key constants without literal translations.
- [ ] Change all game-data presentation entry points to localized templates followed by token replacement.
- [ ] Replace user-facing runtime Korean/fallback literals with key constants; append missing Korean workbook entries and import.
- [ ] Run focused tests and source scanner.

### Task 4: Dynamic presenter refresh

**Files:** affected Skill/Rune/Record/Relic presenters, dynamic marker placement, tests.

- [ ] Add failing tests for selected data rendering through GameDataLocalization and locale-change rerender contracts where unit-testable.
- [ ] Store current stable ID/data at dynamic presenters and route initial selection, selection changes, and locale changes through one refresh method.
- [ ] Remove direct raw game-data assignments in examined display paths.
- [ ] Run focused tests and inspect configured prefabs/scenes using Editor tools.

### Task 5: Validation, metrics, and regression controls

**Files:** `LocalizationManagerWindow.cs`, `LocalizationProjectScanner.cs`, repair tool, tests.

- [ ] Add failing tests for literal scanner exclusions and status aggregation.
- [ ] Replace YAML-only scene scanning with Editor API inspection and stable hierarchy/component identities.
- [ ] Add hardcoded-Korean, `LocalizationKeys` missing-key, binding-status, and pre/post metric reports.
- [ ] Run full scan, deterministic repair, workbook import, and focused EditMode tests.

### Task 6: Compile and final audit

- [ ] Run Assembly-CSharp and Assembly-CSharp-Editor compile validation through the existing project workflow.
- [ ] Re-run all requested metrics and record unresolved ManualConflict entries with exact asset/hierarchy/reason.
- [ ] Confirm no commit/push/PR was made.
