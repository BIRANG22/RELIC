using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapPartyInfoPresenter : MonoBehaviour
{
    private const int SlotCount = 3;

    public void RefreshFromRuntime()
    {
        List<CharacterRuntimeData> party = new(SlotCount);
        DataManager dataManager = DataManager.Instance;

        for (int i = 0; i < SlotCount; i++)
        {
            CharacterRuntimeData runtime = null;
            string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(i);
            if (!string.IsNullOrWhiteSpace(characterId))
                dataManager.CharacterRuntimeStore?.TryGet(characterId, out runtime);
            party.Add(runtime);
        }

        Render(party, characterId =>
        {
            if (dataManager?.CharacterIconDatabase == null)
                return null;

            return dataManager.CharacterIconDatabase.TryGetTimelineIcon(characterId, out Sprite icon)
                ? icon
                : null;
        });
    }

    public void Render(
        IReadOnlyList<CharacterRuntimeData> party,
        Func<string, Sprite> iconResolver)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slot = transform.Find($"Character{i + 1}");
            if (slot == null)
                continue;

            CharacterRuntimeData runtime = party != null && i < party.Count ? party[i] : null;
            slot.gameObject.SetActive(runtime != null);
            if (runtime == null)
                continue;

            TMP_Text hpText = slot.Find("HpInfo")?.GetComponentInChildren<TMP_Text>(true);
            if (hpText != null)
                hpText.text = $"{Mathf.Max(0, runtime.CurrentHP)}/{Mathf.Max(0, runtime.MaxHP)}";

            Image hpFill = slot.Find("HpBar/Fill")?.GetComponent<Image>();
            if (hpFill != null)
            {
                int maxHp = Mathf.Max(0, runtime.MaxHP);
                int currentHp = Mathf.Clamp(runtime.CurrentHP, 0, maxHp);
                hpFill.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            }

            Image iconImage = slot.Find("Icon")?.GetComponent<Image>();
            if (iconImage == null)
                continue;

            Sprite icon = iconResolver?.Invoke(runtime.CharacterId);
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }
    }
}
