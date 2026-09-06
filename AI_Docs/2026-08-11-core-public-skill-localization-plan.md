# Core Public Skill Localization Plan

1. Add a failing EditMode test for missing `S_Core_61` through `S_Core_80` localization keys.
2. Confirm the test fails against the current workbook/string tables.
3. Copy `S_Public_01` through `S_Public_20` translated values into `S_Core_61` through `S_Core_80` rows in `Localization.xlsx`.
4. Add matching shared table entries and locale values to the Unity Localization assets.
5. Re-run the data check and compile/build verification.
