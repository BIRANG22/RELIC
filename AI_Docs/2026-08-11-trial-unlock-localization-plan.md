# Trial Unlock Localization Plan

1. Add an EditMode test that creates three trial items and verifies locked Effect localizers switch to unlock requirement keys.
2. Add workbook/string table checks for the three unlock requirement keys.
3. Update `TrialUnlockProgress` to expose unlock requirement keys and localized fallback text.
4. Update `ErosionSelectCarousel` to switch Effect `LocalizeStringEvent` keys between locked and unlocked state.
5. Add the three keys to `Localization.xlsx` and Unity Localization assets.
6. Verify with the data check, compile/build, and diff checks.
