# Localization Editing Lock Runtime Owner Design

## Goal

Make the Localization Editing Lock menu control the active localization owner after static TMP text has been migrated from `LocalizeStringEvent` to `LocalizedTMPText`.

## Root Cause

`StaticLocalizationMigration` removes the legacy `LocalizeStringEvent` after assigning the key and Korean source to `LocalizedTMPText`. `LocalizationEditingLockTool` currently enumerates only `LocalizeStringEvent`, so migrated text has no component for the menu to enable or disable.

## Minimal Design

- Keep controlling a legacy `LocalizeStringEvent` only when no `LocalizedTMPText` exists on that TMP object.
- Also enumerate `LocalizedTMPText` and toggle its `enabled` state.
- When disabling a runtime owner, restore its serialized `KoreanSource` to the attached TMP so the editing view is readable without localization updates.
- When enabling a runtime owner, call `Refresh()` after enabling so it applies the active locale when localization initialization is ready.
- Skip any TMP rejected by `LocalizedTMPText.ShouldManageText`, including `LocalizationIgnore` dynamic presenters. Those strings are owned by their runtime writer, not the editing-lock menu.

## Dynamic Data Localization

`CharacterinfoText` is not a static TMP binding. `Setting` writes the result of `GameDataLocalization.CharacterIntroduction` directly, and the TMP is correctly marked `LocalizationIgnore` to prevent a static component from overwriting that writer. The same `GameLocalization.Get` boundary serves code-owned text and data-owned text across the project.

The editor menu persists its enabled state in `EditorPrefs`. While disabled in the Unity Editor, `GameLocalization.Get(key, fallback)` returns the supplied source fallback before reading a string table. This restores the original source for `GameDataLocalization` callers such as character names and introductions without adding a localization component to dynamic TMP output. Player builds always treat the lock as enabled.

Calls that use only a key have no serialized fallback in source code. For those calls, including rarity, cost, type, preview effect, and dialogue labels, the disabled editor lock resolves that key directly from the Korean `Text` table with table fallback disabled. This prevents the normal English missing-translation result (`Untranslated`) from reaching the TMP writer.

## Non-goals

- Do not alter keys, translation tables, scene/prefab bindings, or runtime hover text behavior.
- Do not reintroduce `LocalizeStringEvent` on migrated TMP objects.
