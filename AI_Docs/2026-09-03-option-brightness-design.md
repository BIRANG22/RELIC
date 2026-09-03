# Option Brightness Design

## Goal

Add a brightness slider to the existing Option prefab without changing sound, resolution, or other option logic.

## Design

- Store `Settings.Brightness` in PlayerPrefs with default `0.5`.
- Reuse the existing volume slider pattern with a new `ChangeBrightness` component on the brightness slider.
- Apply brightness immediately through `GameBrightnessManager`.
- Convert slider value `0..1` to URP ColorAdjustments Post Exposure `-2..+2` using `(value - 0.5) * 4`.
- Keep runtime application null-safe when the slider, settings singleton, Volume, or ColorAdjustments reference is missing.
- Reapply the saved brightness after scene loads.

## Inspector

- Add one brightness slider in `Option.prefab` under the existing sound settings layout.
- Attach `ChangeBrightness` and connect its `slider` field to the Slider component.
