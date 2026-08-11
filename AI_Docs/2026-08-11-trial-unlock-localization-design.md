# Trial Unlock Localization Design

## Context

`ErosionSelectPanel > Trial > Erosion_0~2 > Effect` uses `LocalizeStringEvent` for the unlocked trial effect text. When a trial is locked, `ErosionSelectCarousel` replaces the text with `TrialUnlockProgress.GetUnlockRequirementText`, which currently returns hardcoded Korean strings.

Because the locked text bypasses the `Text` table and the `LocalizeStringEvent`, it does not change when the active locale changes.

## Design

- Add three `Text` localization keys for the trial unlock requirements.
- Let `TrialUnlockProgress` expose a stable localization key for each trial requirement.
- Let `ErosionSelectCarousel` cache each Effect text's unlocked localization key.
- When locked, switch the Effect `LocalizeStringEvent` to the matching unlock requirement key.
- When unlocked, restore the original unlocked effect key.
- Keep a direct localized fallback path for Effect text objects without a `LocalizeStringEvent`.

## Keys

- `lobby.trial.unlock.stage3_clear`
- `lobby.trial.unlock.nocturne_kill`
- `lobby.trial.unlock.two_trials_clear`
