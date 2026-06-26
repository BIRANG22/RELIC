using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public enum SkillSlotType
{
    Passive,
    Unique,
    Ability,
    Equipped,
    Inventory
}

public struct SkillUpgradeRequest
{
    public string CharacterId;
    public string CurrentSkillId;
    public string UpgradeSkillId;
    public SkillSlotType SlotType;
    public int SlotIndex;
}
public class SkillUpgradePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillUpgradeIconItem iconPrefab;

    private readonly List<SkillUpgradeIconItem> spawnedItems = new();

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        Clear();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void Refresh()
    {
        Clear();

        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
            return;

        for (int partyIndex = 0; partyIndex < partyStore.MaxPartyCountValue; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData characterRuntime))
            {
                continue;
            }

            SpawnCharacterSkillItems(characterRuntime);
        }

        SpawnInventorySkillItems();
    }

    private void SpawnInventorySkillItems()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return;

        for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
            SpawnInventorySkillItem(runtime.SkillInventoryIds[i], i);
    }

    private void SpawnInventorySkillItem(string skillId, int inventoryIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        item.Initialize(
            null,
            skillId,
            upgradeSkillId,
            SkillSlotType.Inventory,
            inventoryIndex,
            OnSkillItemClicked
        );

        spawnedItems.Add(item);
    }

    private void SpawnCharacterSkillItems(CharacterRuntimeData characterRuntime)
    {
        if (characterRuntime == null)
            return;

        SpawnSkillItem(characterRuntime, characterRuntime.PassiveSkillId, SkillSlotType.Passive, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.UniqueSkillId, SkillSlotType.Unique, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.AbilitySkillId, SkillSlotType.Ability, -1);

        if (characterRuntime.EquippedSkillIds == null)
            return;

        for (int i = 2; i < characterRuntime.EquippedSkillIds.Length; i++)
        {
            SpawnSkillItem(characterRuntime, characterRuntime.EquippedSkillIds[i], SkillSlotType.Equipped, i);
        }
    }

    private void SpawnSkillItem(
        CharacterRuntimeData characterRuntime,
        string skillId,
        SkillSlotType slotType,
        int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return;
        }

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        item.Initialize(
            characterRuntime.CharacterId,
            skillId,
            upgradeSkillId,
            slotType,
            slotIndex,
            OnSkillItemClicked
        );

        spawnedItems.Add(item);
    }

    private void OnSkillItemClicked(SkillUpgradeRequest request)
    {
        if (DataManager.Instance == null)
            return;

        if (request.SlotType == SkillSlotType.Inventory)
        {
            UpgradeInventorySkill(request);
            return;
        }

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                request.CharacterId,
                out CharacterRuntimeData characterRuntime))
        {
            return;
        }

        switch (request.SlotType)
        {
            case SkillSlotType.Passive:
                characterRuntime.PassiveSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Unique:
                characterRuntime.UniqueSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Ability:
                characterRuntime.AbilitySkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Equipped:
                if (characterRuntime.EquippedSkillIds == null)
                    return;

                if (request.SlotIndex < 0 || request.SlotIndex >= characterRuntime.EquippedSkillIds.Length)
                    return;

                characterRuntime.EquippedSkillIds[request.SlotIndex] = request.UpgradeSkillId;
                break;
        }

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(characterRuntime);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Skill upgraded / Character:{request.CharacterId} / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        Refresh();
    }
    private void UpgradeInventorySkill(SkillUpgradeRequest request)
    {
        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return;

        if (request.SlotIndex < 0 || request.SlotIndex >= runtime.SkillInventoryIds.Count)
            return;

        runtime.SkillInventoryIds[request.SlotIndex] = request.UpgradeSkillId;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Inventory skill upgraded / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        Refresh();
    }
    private void Clear()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private bool TryGetSkillUpgradeId(string skillId, out string upgradeSkillId)
    {
        upgradeSkillId = null;

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        int lastUnderscoreIndex = skillId.LastIndexOf('_');

        if (lastUnderscoreIndex < 0)
            return false;

        string prefix = skillId.Substring(0, lastUnderscoreIndex + 1);
        string numberText = skillId.Substring(lastUnderscoreIndex + 1);

        if (!int.TryParse(numberText, out int number))
            return false;

        // 이미 강화된 스킬이면 제외
        if (number % 2 == 0)
            return false;

        int upgradeNumber = number + 1;
        string upgradedNumberText = upgradeNumber.ToString(new string('0', numberText.Length));
        upgradeSkillId = prefix + upgradedNumberText;

        if (DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return false;
        }

        return DataManager.Instance.SkillDatabase.TryGet(upgradeSkillId, out _);
    }
}
