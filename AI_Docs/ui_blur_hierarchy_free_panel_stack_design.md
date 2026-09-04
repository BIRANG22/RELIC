# UI Blur Hierarchy-Free Panel Stack Design

## Problem

`UIBlurBackgroundManager` moved Lobby UI siblings, panel roots, and `Setting_upper`
between runtime presentation roots with `Transform.SetParent`,
`SetSiblingIndex`, and `SetAsLastSibling`.

That happened from `UIBlurBackground.OnEnable` and `OnDisable`, while panels were
being activated or deactivated. Unity rejects hierarchy edits during that phase,
which caused repeated SetParent/SetSibling errors and also broke Canvas,
RectTransform, input, and panel rendering.

## Approved Direction

Keep the world blur and shared blur material pipeline. Remove all blur-system
Transform hierarchy movement.

The blur manager owns only:

- panel requester stack
- shared blur overlay visibility and material settings
- requester panel Canvas render mode and sorting state
- fixed sharp root Canvas render state
- camera pause

The blur manager does not own:

- panel parent/child relationships
- sibling order
- global UIPanelButton input blocking

## Rendering Model

The shared blur overlay remains a `Screen Space - Overlay` Canvas at a high
sorting order.

When one requester is open:

- non-requester Lobby UI stays in its original hierarchy and is rendered behind
  the shared blur.
- top requester panel receives an independent Overlay Canvas above shared blur.
- `Setting_upper` receives an Overlay Canvas above shared blur.

When nested requesters are open:

- only `TopRequester` receives the sharp Overlay Canvas.
- earlier requesters receive an independent `Screen Space - Camera` Canvas below
  the shared blur so they are captured into `_UIBlurSourceTexture` and appear
  blurred by the shared overlay.
- closing the top requester promotes the previous requester back to sharp by
  changing only its Canvas settings.

## Safety Rules

The blur system must not call:

- `SetParent`
- `SetSiblingIndex`
- `SetAsFirstSibling`
- `SetAsLastSibling`

on Lobby UI, panels, or panel children.

`Request` and `Release` may run during `OnEnable` and `OnDisable`, so they only
update stack state, Canvas render settings, shared blur visibility, and material
parameters.

## Lobby Scope

This pass targets the Lobby flow:

- `RelicShopPanel` open
- `MenuPanel` open above it
- `MenuPanel` close
- `RelicShopPanel` close

Battle logic and renderer feature implementation are intentionally left alone.
