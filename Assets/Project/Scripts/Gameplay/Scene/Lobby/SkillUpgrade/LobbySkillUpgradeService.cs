using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class LobbySkillUpgradePricePolicy
{
    public const int BasePrice = 100;
    public const int PriceIncrease = 50;

    public static int GetPrice(int successfulUpgradeCount)
    {
        long count = Math.Max(0, successfulUpgradeCount);
        return (int)Math.Min(int.MaxValue, BasePrice + count * PriceIncrease);
    }
}

public enum LobbySkillUpgradeFailure
{
    None,
    InvalidRequest,
    InsufficientBlueDustium,
    CharacterNotFound,
    SkillSlotMismatch
}

public readonly struct LobbySkillUpgradeCommand
{
    public LobbySkillUpgradeCommand(
        string characterId,
        string currentSkillId,
        string upgradeSkillId,
        SkillSlotType slotType,
        int slotIndex)
    {
        CharacterId = characterId;
        CurrentSkillId = currentSkillId;
        UpgradeSkillId = upgradeSkillId;
        SlotType = slotType;
        SlotIndex = slotIndex;
    }

    public string CharacterId { get; }
    public string CurrentSkillId { get; }
    public string UpgradeSkillId { get; }
    public SkillSlotType SlotType { get; }
    public int SlotIndex { get; }
}

public readonly struct LobbySkillUpgradeResult
{
    public LobbySkillUpgradeResult(bool succeeded, int price, LobbySkillUpgradeFailure failure)
    {
        Succeeded = succeeded;
        Price = price;
        Failure = failure;
    }

    public bool Succeeded { get; }
    public int Price { get; }
    public LobbySkillUpgradeFailure Failure { get; }
}

public sealed class LobbySkillUpgradeService
{
    private readonly CharacterRuntimeStore characterStore;

    public LobbySkillUpgradeService(CharacterRuntimeStore characterStore)
    {
        this.characterStore = characterStore;
    }

    public LobbySkillUpgradeResult Execute(
        LobbyRuntimeData lobby,
        LobbySkillUpgradeCommand command)
    {
        int price = LobbySkillUpgradePricePolicy.GetPrice(lobby?.LobbySkillUpgradeCount ?? 0);
        if (lobby == null ||
            string.IsNullOrWhiteSpace(command.CurrentSkillId) ||
            string.IsNullOrWhiteSpace(command.UpgradeSkillId))
        {
            return Fail(price, LobbySkillUpgradeFailure.InvalidRequest);
        }

        if (lobby.BlueDustium < price)
            return Fail(price, LobbySkillUpgradeFailure.InsufficientBlueDustium);

        if (!TryApplyUpgrade(lobby, command))
            return Fail(price, ResolveFailure(command));

        lobby.BlueDustium -= price;
        lobby.LobbySkillUpgradeCount++;
        LobbySkillUpgradePersistence.Record(lobby, command);
        return new LobbySkillUpgradeResult(true, price, LobbySkillUpgradeFailure.None);
    }

    private bool TryApplyUpgrade(LobbyRuntimeData lobby, LobbySkillUpgradeCommand command)
    {
        string currentId = command.CurrentSkillId.Trim();
        string upgradeId = command.UpgradeSkillId.Trim();

        if (command.SlotType == SkillSlotType.Inventory)
        {
            lobby.SkillInventoryIds ??= new List<string>();
            if (!Matches(lobby.SkillInventoryIds, command.SlotIndex, currentId))
                return false;

            lobby.SkillInventoryIds[command.SlotIndex] = upgradeId;
            return true;
        }

        if (characterStore == null ||
            string.IsNullOrWhiteSpace(command.CharacterId) ||
            !characterStore.TryGet(command.CharacterId.Trim(), out CharacterRuntimeData character) ||
            character == null)
        {
            return false;
        }

        bool changed = command.SlotType switch
        {
            SkillSlotType.Passive => Replace(ref character.PassiveSkillId, currentId, upgradeId),
            SkillSlotType.Unique => ReplaceMirrored(
                ref character.UniqueSkillId, character.EquippedSkillIds, 0, currentId, upgradeId),
            SkillSlotType.Ability => ReplaceMirrored(
                ref character.AbilitySkillId, character.EquippedSkillIds, 1, currentId, upgradeId),
            SkillSlotType.Equipped => Replace(character.EquippedSkillIds, command.SlotIndex, currentId, upgradeId),
            _ => false
        };

        if (changed)
            characterStore.AddOrUpdate(character);
        return changed;
    }

    private LobbySkillUpgradeFailure ResolveFailure(LobbySkillUpgradeCommand command)
    {
        if (command.SlotType != SkillSlotType.Inventory &&
            (characterStore == null ||
             string.IsNullOrWhiteSpace(command.CharacterId) ||
             !characterStore.TryGet(command.CharacterId.Trim(), out _)))
        {
            return LobbySkillUpgradeFailure.CharacterNotFound;
        }

        return LobbySkillUpgradeFailure.SkillSlotMismatch;
    }

    private static bool Replace(ref string value, string currentId, string upgradeId)
    {
        if (!string.Equals(value?.Trim(), currentId, StringComparison.Ordinal))
            return false;
        value = upgradeId;
        return true;
    }

    private static bool Replace(
        string[] values,
        int index,
        string currentId,
        string upgradeId)
    {
        if (values == null || index < 0 || index >= values.Length ||
            !string.Equals(values[index]?.Trim(), currentId, StringComparison.Ordinal))
        {
            return false;
        }

        values[index] = upgradeId;
        return true;
    }

    private static bool ReplaceMirrored(
        ref string primaryValue,
        string[] equippedSkillIds,
        int mirroredIndex,
        string currentId,
        string upgradeId)
    {
        if (!Replace(ref primaryValue, currentId, upgradeId))
            return false;

        if (equippedSkillIds != null && mirroredIndex >= 0 && mirroredIndex < equippedSkillIds.Length)
            equippedSkillIds[mirroredIndex] = upgradeId;

        return true;
    }

    private static bool Matches(IReadOnlyList<string> values, int index, string currentId)
    {
        return values != null &&
               index >= 0 &&
               index < values.Count &&
               string.Equals(values[index]?.Trim(), currentId, StringComparison.Ordinal);
    }

    private static LobbySkillUpgradeResult Fail(int price, LobbySkillUpgradeFailure failure)
    {
        return new LobbySkillUpgradeResult(false, price, failure);
    }
}

public sealed class LobbySkillUpgradeSelection
{
    private SkillUpgradeRequest request;

    public bool HasSelection { get; private set; }

    public void Select(SkillUpgradeRequest value)
    {
        request = value;
        HasSelection = true;
    }

    public void Clear()
    {
        request = default;
        HasSelection = false;
    }

    public LobbySkillUpgradeResult Execute(
        LobbyRuntimeData lobby,
        LobbySkillUpgradeService service)
    {
        if (!HasSelection || service == null)
        {
            return new LobbySkillUpgradeResult(
                false,
                LobbySkillUpgradePricePolicy.GetPrice(lobby?.LobbySkillUpgradeCount ?? 0),
                LobbySkillUpgradeFailure.InvalidRequest);
        }

        LobbySkillUpgradeCommand command = new(
            request.CharacterId,
            request.CurrentSkillId,
            request.UpgradeSkillId,
            request.SlotType,
            request.SlotIndex);
        LobbySkillUpgradeResult result = service.Execute(lobby, command);
        if (result.Succeeded)
            Clear();
        return result;
    }
}

public static class LobbySkillUpgradePersistence
{
    public static void Record(LobbyRuntimeData lobby, LobbySkillUpgradeCommand command)
    {
        if (lobby == null || command.SlotType == SkillSlotType.Inventory ||
            string.IsNullOrWhiteSpace(command.CharacterId) ||
            string.IsNullOrWhiteSpace(command.UpgradeSkillId))
            return;

        lobby.CharacterSkillUpgrades ??= new List<LobbySkillUpgradeRecordData>();
        LobbySkillUpgradeRecordData record = lobby.CharacterSkillUpgrades.Find(item =>
            item != null &&
            string.Equals(item.CharacterId, command.CharacterId, StringComparison.Ordinal) &&
            item.SlotType == (int)command.SlotType &&
            item.SlotIndex == command.SlotIndex);

        if (record == null)
        {
            record = new LobbySkillUpgradeRecordData
            {
                CharacterId = command.CharacterId.Trim(),
                SlotType = (int)command.SlotType,
                SlotIndex = command.SlotIndex
            };
            lobby.CharacterSkillUpgrades.Add(record);
        }

        record.SkillId = command.UpgradeSkillId.Trim();
    }

    public static bool ApplyAll(LobbyRuntimeData lobby, CharacterRuntimeStore characters)
    {
        if (lobby?.CharacterSkillUpgrades == null || characters == null)
            return false;

        bool changed = false;
        for (int i = 0; i < lobby.CharacterSkillUpgrades.Count; i++)
        {
            LobbySkillUpgradeRecordData record = lobby.CharacterSkillUpgrades[i];
            if (record == null || string.IsNullOrWhiteSpace(record.CharacterId) ||
                string.IsNullOrWhiteSpace(record.SkillId) ||
                !characters.TryGet(record.CharacterId, out CharacterRuntimeData character) ||
                character == null)
                continue;

            string skillId = record.SkillId.Trim();
            bool recordChanged = false;
            switch ((SkillSlotType)record.SlotType)
            {
                case SkillSlotType.Unique:
                    recordChanged |= SetIfDifferent(ref character.UniqueSkillId, skillId);
                    recordChanged |= SetArrayIfDifferent(character.EquippedSkillIds, 0, skillId);
                    break;
                case SkillSlotType.Ability:
                    recordChanged |= SetIfDifferent(ref character.AbilitySkillId, skillId);
                    recordChanged |= SetArrayIfDifferent(character.EquippedSkillIds, 1, skillId);
                    break;
                case SkillSlotType.Passive:
                    recordChanged |= SetIfDifferent(ref character.PassiveSkillId, skillId);
                    break;
                case SkillSlotType.Equipped:
                    recordChanged |= SetArrayIfDifferent(character.EquippedSkillIds, record.SlotIndex, skillId);
                    break;
            }

            if (recordChanged)
            {
                changed = true;
                characters.AddOrUpdate(character);
            }
        }

        return changed;
    }

    private static bool SetIfDifferent(ref string value, string skillId)
    {
        if (string.Equals(value, skillId, StringComparison.Ordinal))
            return false;
        value = skillId;
        return true;
    }

    private static bool SetArrayIfDifferent(string[] values, int index, string skillId)
    {
        if (values == null || index < 0 || index >= values.Length ||
            string.Equals(values[index], skillId, StringComparison.Ordinal))
            return false;
        values[index] = skillId;
        return true;
    }
}
