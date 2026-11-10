# Skill Effect Hover Source Design

## Intent

Skill acquisition cards must show the same linked effect explanations as the in-battle skill tooltip. In particular, `독성발현` must explain `E_Poison`, not its internal executor `E_PoisonEqualTargetPoison`.

## Approach

Use effect links produced from the localized skill description as the single display source. Add a small parser to `SkillDescriptionFormatter`, then use it from both battle and reward panels. Do not infer player-facing terminology from execution effect entries.

## Constraints

- Preserve link order and remove duplicate effect IDs case-insensitively.
- Ignore internal effects that are not present in the authored description.
- Keep `UnitStatusEffectTooltipUI` unchanged.

