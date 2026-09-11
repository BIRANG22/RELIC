# Compound Hover Runtime Diagnostics Design

## Goal

Collect runtime evidence that distinguishes incorrect compound/item data delivery from a later TMP writer overwriting the displayed name.

## Scope

- Observe only the Compound reference tooltip path.
- Record the stable source ID, localized display name, hovered icon identity, tooltip TMP identity, and the value before and after `TMP_Text.text` assignment.
- Preserve all functional behavior, serialized data, localization lookup behavior, and existing uncommitted changes.

## Design

`CompoundRecipeSlotUI` already owns the stable ID while binding each compound or material entry. It will pass that ID into `CompoundReferenceIconHover.Initialize` alongside the existing localized display name. The hover component will retain the ID only for diagnostic output and emit it at initialization and pointer-enter boundaries.

`CompoundReferenceNameTooltip.Show` will log the source ID, incoming display name, actual target TMP, and its value immediately before and after assignment. Existing `TEXT_CHANGED_EVENT` observation remains responsible for detecting later text changes. This yields one shared TMP InstanceID across A/B/C and makes the last writer distinguishable.

## Non-goals

- No localization logic, tooltip behavior, UI layout, serialization, or scene changes.
- No fix for the first-value-sticky issue before Play Mode evidence identifies a root cause.
