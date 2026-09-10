# Localization System Restructure Design

## Goal

Static UI, runtime UI/system copy, and game-data copy each have one unambiguous localization owner. Existing binding corruption must be detected and only deterministically repairable cases changed.

## Source of Truth and Boundaries

- `Assets/ExcelSource/Localization.xlsx` remains the source of truth for UI/system and game-data translation rows; Unity String Tables are imported outputs.
- `LocalizedTMPText` owns static TMP display only and persists its immutable Korean source plus key.
- Dynamic TMP is explicitly excluded from static migration/auto-localization and is rendered by its presenter from a key or stable game-data ID.
- Game-data presentation always follows `ID -> GameDataLocalization -> localized template -> token replacement -> TMP`.
- Runtime localization never derives permanent source metadata from a currently displayed TMP value.

## Binding Resolution

The workbook map is `normalized Korean source -> candidate key list`, never a first-key dictionary. A static binding is valid only when its persisted `KoreanSource` equals the workbook Korean value for its key after normalization. A repair changes a key only when the source has one candidate. Zero candidates produce `MissingKey`; multiple candidates produce `AmbiguousKey`/`ManualConflict` and retain the asset unchanged.

## Runtime Policy

`RuntimeTMPTextAutoLocalizer` will only consider text objects that have no `LocalizedTMPText`, no dynamic-ignore marker, and a uniquely resolved Korean source. It will not scan or mutate existing scene/prefab bindings, and its own refresh path is guarded against TMP text-change re-entry.

## Editor Validation and Reporting

The manager will load prefabs/scenes through Unity Editor APIs, including inactive objects. It reports asset path, complete hierarchy path, TMP type/component index, persisted source, key, table Korean, and status. The status model includes Valid, Fixed, MissingKey, SourceMismatch, AmbiguousKey, MissingComponent, UnexpectedComponent, DynamicTextWithStaticLocalization, DuplicateBinding, and ManualConflict. A C# scanner will separately detect player-facing Korean literals while excluding comments, logging, attributes, Editor/debug/test paths and known developer-only code.

## Key API and Data

`LocalizationKeys` holds only UI/system keys. `GameLocalization` receives key-only Get/Format overloads, while compatibility overloads remain temporarily for legacy code. New UI/system Korean copy is entered into the existing workbook and imported; game-data keys stay generated from data category/stable ID/field and are never enumerated in `LocalizationKeys`.

## Dynamic Refresh

Dynamic presenters subscribe/unsubscribe to locale changes and rerender their currently held stable ID/data. They do not rely on disable/enable lifecycle ordering. The same presentation method is used for initial bind, data selection change, and locale change.

## Verification

Before and after metrics will cover runtime user-facing Korean, raw game-data UI output, source mismatch, ambiguous bindings, missing UI/system keys, and static/dynamic conflicts. EditMode unit tests cover candidate resolution, binding validation, static/dynamic exclusion, key-only formatting and game-data formatting order. Assembly-CSharp and Assembly-CSharp-Editor compile validation will be run without Unity batchmode.
