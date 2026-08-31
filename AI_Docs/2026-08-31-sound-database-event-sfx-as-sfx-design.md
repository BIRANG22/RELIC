# Sound Database Event SFX as SFX Design

## Context

`SoundDatabase` needs a separate `Event Sfx List` in the inspector so event sounds can be organized apart from common UI/gameplay SFX. Runtime playback and script references should still treat those entries as ordinary SFX.

## Decision

- Keep `eventSfxList` as a separate serialized list in `SoundDatabase`.
- Register both `sfxList` and `eventSfxList` into the same SFX lookup used by `TryGetSfx` and `AudioManager.PlaySfx`.
- Keep `SoundCategory` limited to `Bgm` and `Sfx`.
- Make `[SoundId(SoundCategory.Sfx)]` show IDs from both `Sfx List` and `Event Sfx List`.
- Do not keep a separate `AudioIds.EventSfx` class. Script constants can stay under `AudioIds.Sfx`, or a direct string can be used when the ID is only a serialized default.

## Multiplayer Boundary

This change only affects audio lookup and editor dropdown presentation. It does not change battle result calculation or synchronized state.
