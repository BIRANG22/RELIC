# Character Info Text Ownership Design

## Goal

Separate the two text outputs in the Lobby character setting panel so the stat-icon hover and karma-acquisition information writes to `Info_Area/InfoText`, while character introduction writes only to `Info_Area/CharacterinfoText`.

## Confirmed scene facts

- `CharacterInfoPanel.storyText` currently references `Info_Area/InfoText`.
- `Setting.characterInfoText` already references `Info_Area/CharacterinfoText` and writes `GameDataLocalization.CharacterIntroduction(...)`.
- `CharacterInfoPanel` currently uses its `storyText` for both stat hover text and the karma-acquisition default text.

## Design

`CharacterInfoPanel.storyText` remains the generic-information output and must reference `InfoText`. The repair command finds every active and inactive `CharacterInfoPanel` in the opened Lobby scene, compares its serialized `storyText` reference by object identity, and only repairs the panel whose reference is the target `InfoText`.

The repair command removes static localization ownership from `InfoText`, marks it as runtime-owned (`LocalizationIgnore`), retains `CharacterinfoText` as the separately owned dynamic character-description target, and never uses hierarchy-parent guessing to find a panel. `Setting` continues to own character-description refresh through `GameDataLocalization.CharacterIntroduction(...)`.

`InfoText` does not use a static TMP localizer. Its hover labels, descriptions, and number formats use `GameLocalization` keys. The default karma-acquisition body uses `GameDataLocalization.CharacterRegeneration(...)`, which resolves `data.character.<characterId>.regeneration`. Missing locale values intentionally display the project's `Untranslated` value.

## Verification

Edit-mode tests will cover the runtime-localization removal helper. Scene assertions will confirm the two serialized references, the absence of a static localizer on `InfoText`, and the expected dynamic ownership flags. Manual Play Mode verification remains required for hover enter/exit, character switching, tab switching, and panel re-enable.
