# Tutorial, Skill, Rune Localization Implementation Plan

> **For agentic workers:** Execute each task with a test-first cycle. This repository requires all project documents to reside in `AI_Docs`; no commit is authorized.

**Goal:** Move the requested Tutorial and SkillSettingPanel runtime display copy to key/data-owned localization, migrate Rune Installation static text safely, and validate RelicShop rarity keys.

**Architecture:** UI/system labels use `LocalizationKeys` and `GameLocalization.Get/Format`; game-data display uses `GameDataLocalization` before value-token and RichText processing. `LocalizationSyncCommand` remains the single workbook-to-table flow, extended with scoped Tutorial/Skill/Rune validation menu commands. Locale-sensitive presenters re-render their currently selected stable data without resetting progress.

**Tech Stack:** Unity C#, Unity Localization String Tables, existing XLSX writer/importer, NUnit EditMode tests.

**Spec:** `AI_Docs/2026-09-10_Localization_System_Restructure_Design.md`

## Global Constraints

- Preserve all pre-existing uncommitted changes; do not commit, push, create a PR, branch, or worktree.
- Runtime user-facing Korean literals and direct GameData display output are removed only in the requested Tutorial/SkillSettingPanel paths.
- Documentation remains in `AI_Docs`; tests remain in `Assets/Tests/EditMode~/`.
- Do not run Unity batchmode; user-triggered editor actions are exposed as MenuItems.
- Localization affects presentation only and has no multiplayer/battle-state impact.

---

### Task 1: Tutorial key ownership and locale refresh

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/LocalizationKeys.cs`
- Modify: `Assets/Project/Scripts/LobbyTutorialController.cs`
- Modify: `Assets/Editor/LocalizationSyncCommand.cs`
- Test: `Assets/Tests/EditMode~/TutorialLocalizationTests.cs`

- [ ] Write EditMode source/behavior tests for the eleven `LocalizationKeys.Tutorial` constants, key-only dialogue arrays, and locale-change subscription lifecycle.
- [ ] Run the focused test and observe failure because Tutorial constants and event lifecycle are absent.
- [ ] Add Tutorial constants; route serialized defaults through them; subscribe/unsubscribe safely; rerender only the current non-empty dialogue mode.
- [ ] Add Tutorial workbook entries and the `Tools/Localization/Sync Tutorial Localization` command using the existing writer/importer/validation flow.
- [ ] Run the focused test and inspect the Workbook/table validation implementation.

### Task 2: SkillSettingPanel dynamic localization

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/LocalizationKeys.cs`
- Modify: `Assets/Project/Scripts/Core/Localization/GameDataLocalization.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillSettingPanel.cs`
- Modify: `Assets/Editor/LocalizationSyncCommand.cs`
- Test: `Assets/Tests/EditMode~/SkillSettingPanelLocalizationTests.cs`

- [ ] Write failing source/contract tests for key-based rarity/resource/range/effect labels, format templates, locale refresh, and data-localized effect names.
- [ ] Run the focused test and observe the direct Korean/raw-name paths fail the assertions.
- [ ] Add `SkillInfo` keys, resolve labels through `GameLocalization`, route effect names through `GameDataLocalization.EffectName`, and make detail templates available before token/RichText replacement.
- [ ] Mark dynamic Skill/Character TMP targets with `LocalizationIgnore`, retain selected skill state, and rerender it on locale changes.
- [ ] Extend scoped sync entries/validation for all new UI keys, then run the focused test.

### Task 3: Rune static Installation migration and RelicShop validation

**Files:**
- Modify: `Assets/Editor/LocalizationSyncCommand.cs`
- Modify: `Assets/Editor/StaticLocalizationMigration.cs` or a focused Editor migration helper
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity` only through Unity Editor migration logic
- Test: `Assets/Tests/EditMode~/RuneInstallationLocalizationTests.cs`

- [ ] Write failing tests for the Installation key, deterministic scene ownership/migration targeting, and RelicShop rarity-key validation inclusion.
- [ ] Run the focused test and observe missing command/key ownership.
- [ ] Add the static Installation localization entry and a `Tools/Localization/Sync Rune Installation Localization` command; use one Editor migration traversal to bind all 25 scene targets only if no prefab source exists.
- [ ] Include `RelicRarity` keys in scoped validation without changing the already-localized presenter.
- [ ] Run focused tests, compile assemblies through the established non-batch workflow, and report the exact Editor menu commands the user must invoke for workbook/table/scene serialization.
