# Core Public Skill Localization Design

## Context

`GameData.xlsx` no longer exposes the old public skill IDs as the current skill rows. The shared public skills now appear as `S_Core_61` through `S_Core_80`, while the localization tables still contain the translated rows under `S_Public_01` through `S_Public_20`.

Runtime localization resolves skill text from the current skill ID. That means a migrated skill now requests keys such as `data.skill_master.s_core_61.name`, but those keys are missing from `Localization.xlsx` and the Unity string table assets.

## Design

- Keep the old `S_Public_01` through `S_Public_20` localization keys for backward compatibility.
- Add new `S_Core_61` through `S_Core_80` localization keys for `name`, `tooltip`, and `details`.
- Copy the existing translated values from the matching legacy public skill rows:
  - `S_Public_01` maps to `S_Core_61`.
  - `S_Public_20` maps to `S_Core_80`.
- Update both sources used by the project:
  - `Assets/ExcelSource/Localization.xlsx`
  - `Assets/Language/Text Shared Data.asset` and locale table assets.

## Verification

- Add an EditMode data regression test that checks the migrated core public skill keys exist in the workbook and string table assets.
- Verify each new core row has the same localized values as its legacy public source row.
