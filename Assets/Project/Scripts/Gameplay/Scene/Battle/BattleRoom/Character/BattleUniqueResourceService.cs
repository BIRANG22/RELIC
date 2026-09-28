using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUniqueResourceService
{
    private const int TurnEndResourceGainAmount = 1;

    public void RecoverAllAlivePlayersAtTurnEnd()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null ||
                character.RuntimeData == null ||
                character.RuntimeData.IsDead)
            {
                continue;
            }

            ApplyUniqueResourceGainNow(character.RuntimeData, TurnEndResourceGainAmount);
        }
    }

    private void ApplyUniqueResourceGainNow(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || amount <= 0)
            return;

        CharacterMasterData masterData = GetMasterData(runtime.CharacterId);

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : 999;

        int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(runtime, amount);
        int previousResource = runtime.CurrentResource;

        BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
            runtime,
            finalAmount,
            previousResource,
            maxResource);

        runtime.CurrentResource = Mathf.Min(maxResource, runtime.CurrentResource + finalAmount);

        int gainedAmount = Mathf.Max(0, runtime.CurrentResource - previousResource);
        if (gainedAmount <= 0)
            return;

        RefreshPlayerHUDs();
        ShowUniqueResourcePopup(runtime, masterData, gainedAmount);

        Debug.Log($"[UniqueResource] TurnEnd / {runtime.CharacterId} +{gainedAmount} / {runtime.CurrentResource}/{maxResource}");
    }

    private void ShowUniqueResourcePopup(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData,
        int gainedAmount)
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

            if (character == null || character.RuntimeData != runtime)
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
