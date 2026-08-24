# Lobby Character Exp Display Plan

## Goal
- Lobby character setting `ExpText` should display experience progress inside the current level.
- Stored `CharacterRuntimeData.Exp` remains cumulative so battle reward and unlock calculations keep their current meaning.

## Root Cause
- `Setting.RefreshCharacterLevelInfo()` writes `currentRuntimeData.Exp` directly.
- Battle reward code now stores cumulative experience, so a level 2 character with total 1500 exp appears as `EXP 1500` instead of `EXP 500`.

## Design
- Add a small display helper on `Setting`:
  - Clamp level to at least 1.
  - Find cumulative exp required at that level through `BattleStageClearExperienceService.GetCumulativeExperienceForLevel`.
  - Display `max(0, cumulativeExp - levelStartExp)`.
- Use the helper only for UI text.
- Do not mutate runtime data during display refresh.

## Verification
- Add EditMode coverage for level 1, level 2, and underflow display cases.
- Run C# project builds.
- Unity batchmode tests are intentionally not run because project rules say the Unity Editor is already open and batchmode tests should not be attempted.
