# Shop Reward Intro Input Block Design

## Problem

When a relic is purchased from the rest-room shop, the equip panel hides every direct child except `item` during the opening presentation. The panel root has no `Graphic`, so changing only its `CanvasGroup.blocksRaycasts` value cannot intercept pointer events. The normal background raycast targets are hidden with the other children, allowing hover/click events to reach shop buttons behind the panel only during the intro.

## Design

- The relic reward panel owns the intro lifetime, so it also owns a transparent full-panel `Image` used only as a temporary input shield.
- Keep the shield behind the animated item, but exclude it from the direct-child hide/restore list.
- During the item intro, keep the panel non-interactable, its raycast blocking enabled, and the shield active.
- When the intro completes or is interrupted, disable the shield and restore normal interaction.
- Preserve the existing close-fade behavior; this change only applies to the opening intro.
- The skill reward panel already keeps its root raycast blocking enabled and independently locks card interaction until each reveal completes, so it requires no behavior change.

## Expected behavior

- While the purchased relic presentation is playing, the transparent shield consumes pointer raycasts so neither reward controls nor shop controls behind the panel can hover or click.
- When the presentation completes, reward-panel interaction returns normally.
- Closing and reopening the panel still restores its normal CanvasGroup state.

