using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public static class CultureTankBattleStartEffectService
{
    private const string ArmorEffectId = "E_Armor";

    public static bool ApplyToPartyAndConsume(
        BattleRuntimeData battle,
        IReadOnlyList<CharacterRuntimeData> characters)
    {
        if (battle == null)
            return false;

        Normalize(battle);

        if (battle.CultureTankBattleStartEffects.Count <= 0 ||
            characters == null ||
            characters.Count <= 0)
        {
            return false;
        }

        bool appliedAny = false;

        for (int effectIndex = battle.CultureTankBattleStartEffects.Count - 1; effectIndex >= 0; effectIndex--)
        {
            CultureTankBattleStartEffectRuntimeData effect =
                battle.CultureTankBattleStartEffects[effectIndex];

            if (effect == null ||
                string.IsNullOrWhiteSpace(effect.EffectId) ||
                effect.RemainingBattleStarts <= 0)
            {
                battle.CultureTankBattleStartEffects.RemoveAt(effectIndex);
                continue;
            }

            bool appliedThisEffect = false;
            int repeatedValue = Mathf.Max(0, effect.Value) * Mathf.Max(0, effect.Count);

            if (repeatedValue > 0)
            {
                for (int characterIndex = 0; characterIndex < characters.Count; characterIndex++)
                {
                    CharacterRuntimeData character = characters[characterIndex];

                    if (character == null)
                        continue;

                    if (ApplyToCharacter(character, effect.EffectId, repeatedValue))
                        appliedThisEffect = true;
                }
            }

            if (!appliedThisEffect)
                continue;

            appliedAny = true;
            effect.RemainingBattleStarts--;

            if (effect.RemainingBattleStarts <= 0)
                battle.CultureTankBattleStartEffects.RemoveAt(effectIndex);
        }

        return appliedAny;
    }

    public static void Normalize(BattleRuntimeData battle)
    {
        if (battle == null)
            return;

        battle.CultureTankBattleStartEffects ??= new List<CultureTankBattleStartEffectRuntimeData>();

        for (int i = battle.CultureTankBattleStartEffects.Count - 1; i >= 0; i--)
        {
            CultureTankBattleStartEffectRuntimeData effect =
                CultureTankResearchService.CopyBattleStartEffect(battle.CultureTankBattleStartEffects[i]);

            if (effect == null)
                battle.CultureTankBattleStartEffects.RemoveAt(i);
            else
                battle.CultureTankBattleStartEffects[i] = effect;
        }
    }

    private static bool ApplyToCharacter(
        CharacterRuntimeData character,
        string effectId,
        int repeatedValue)
    {
        if (character == null ||
            string.IsNullOrWhiteSpace(effectId) ||
            repeatedValue <= 0)
        {
            return false;
        }

        if (effectId == ArmorEffectId)
        {
            character.CurrentShield += repeatedValue;
            return true;
        }

        character.StatusEffects ??= new List<StatusEffectRuntimeData>();

        for (int i = 0; i < character.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = character.StatusEffects[i];

            if (status == null)
                continue;

            if (status.EffectId != effectId)
                continue;

            status.Stack += repeatedValue;
            status.TurnCount = Mathf.Max(status.TurnCount, 1);
            return true;
        }

        character.StatusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId.Trim(),
            Stack = repeatedValue,
            TurnCount = 1
        });

        return true;
    }
}
