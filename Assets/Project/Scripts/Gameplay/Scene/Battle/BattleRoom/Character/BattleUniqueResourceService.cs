using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUniqueResourceService
{
    public void RecoverAllAlivePlayersAtTurnEnd(TurnEndKarmaPolicy policy)
    {
        if (policy == null)
            return;

        CharacterRuntimeStore runtimeStore = DataManager.Instance?.CharacterRuntimeStore;
        if (runtimeStore == null)
        {
            policy.Clear();
            return;
        }

        foreach (System.Collections.Generic.KeyValuePair<string, int> gain in policy.GetRecordedGains())
        {
            if (!runtimeStore.TryGet(gain.Key, out CharacterRuntimeData runtime) ||
                runtime == null)
                continue;
            if (runtime.IsDead)
                continue;

            ApplyKarmaGainNow(runtime, gain.Value);
        }
        policy.Clear();
    }

    private int ApplyKarmaGainNow(CharacterRuntimeData runtime, int halfUnits)
    {
        if (runtime == null || halfUnits <= 0) return 0;
        CharacterMasterData master = GetMasterData(runtime.CharacterId);
        if (master == null || master.ResourceType != ResourceType.None) return 0;
        KarmaFixedPoint.EnsureMigrated(runtime, true);
        int previousInternal = runtime.CurrentResource;
        int equipmentDelta = BattleEquipmentEffectService.ModifyUniqueResourceGain(runtime, 1) - 1;
        int requestedInternal = halfUnits + KarmaFixedPoint.ToInternal(equipmentDelta, true);
        int gainedInternal = KarmaFixedPoint.AddHalfUnits(runtime, requestedInternal, master.MaxResource, true);
        BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
            runtime, Mathf.CeilToInt(requestedInternal / 2f), previousInternal / 2, master.MaxResource);
        if (gainedInternal <= 0) return 0;
        RefreshPlayerHUDs();
        ShowUniqueResourcePopup(runtime, master, gainedInternal / 2f);
        Debug.Log(
            $"[Karma] TurnEnd / {runtime.CharacterId} +{gainedInternal / 2f:0.#} / " +
            $"{KarmaFixedPoint.GetDisplayValue(runtime, true):0.#}/{master.MaxResource}");
        return gainedInternal;
    }

    private void ShowUniqueResourcePopup(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData,
        float gainedAmount)
    {
        if (runtime == null || masterData == null || gainedAmount <= 0)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null ||
                character.RuntimeData.CharacterId != runtime.CharacterId)
                continue;

            BattleDamageTextPopupUI.ShowUniqueResource(
                character.transform,
                GetResourceDisplayName(masterData.ResourceType),
                gainedAmount
            );
            return;
        }
    }

    private string GetResourceDisplayName(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Rage => "분노",
            ResourceType.Momentum => "기세",
            ResourceType.Aether => "에테르",
            ResourceType.Faith => "신앙",
            ResourceType.Blood => "혈기",
            _ => "카르마"
        };
    }

    private CharacterMasterData GetMasterData(string characterId)
    {
        if (DataManager.Instance == null || DataManager.Instance.CharacterDatabase == null)
            return null;

        DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData data);
        return data;
    }

    private void RefreshPlayerHUDs()
    {
        PlayerHUDSlot[] hudSlots = Object.FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }
}
