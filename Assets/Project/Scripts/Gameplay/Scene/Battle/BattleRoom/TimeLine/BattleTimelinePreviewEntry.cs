using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class BattleTimelinePreviewEntry
{
    private static MonsterSkillIconDatabase cachedMonsterSkillIconDatabase;
    public int SlotIndex;
    public int OrderIndex;

    public bool IsPlayer;
    public bool IsMonster;

    public CharacterRuntimeData CharacterRuntime;
    public MonsterRuntimeData MonsterRuntime;
    public SkillMasterData PlayerSkillData;
    public MonsterSkillData MonsterSkillData;
    public PlayerReservedCommand PlayerCommand { get; private set; }
    public int PlayerCommandIndex = -1;
    public MonsterReservedCommand MonsterCommand { get; private set; }

    public string OwnerId
    {
        get
        {
            if (IsPlayer && CharacterRuntime != null)
                return CharacterRuntime.CharacterId;

            if (IsMonster && MonsterRuntime != null)
                return MonsterRuntime.MonsterId;

            return "";
        }
    }

    public Sprite OwnerIcon
    {
        get
        {
            if (IsPlayer)
                return GetCharacterTimelineIcon(OwnerId);

            if (IsMonster)
                return GetMonsterIcon(OwnerId);

            return null;
        }
    }

    public Sprite SkillIcon
    {
        get
        {
            if (IsMonster && MonsterSkillData != null)
                return GetMonsterSkillIcon(MonsterSkillData);

            if (IsPlayer && PlayerSkillData != null)
                return GetSkillIcon(PlayerSkillData.SkillId);

            return null;
        }
    }

    public Sprite SkillRangeIcon
    {
        get
        {
            if (IsMonster && MonsterSkillData != null)
                return GetSkillRangeIcon(MonsterSkillData.RangeId);

            return null;
        }
    }

    public string SkillName
    {
        get
        {
            if (IsPlayer && PlayerSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(PlayerSkillData.Name))
                    return GameDataLocalization.SkillName(PlayerSkillData);

                return PlayerSkillData.SkillId;
            }

            if (IsMonster && MonsterSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(MonsterSkillData.Name))
                    return GameDataLocalization.MonsterSkillName(MonsterSkillData);

                if (!string.IsNullOrWhiteSpace(MonsterSkillData.SkillId))
                    return MonsterSkillData.SkillId;

                return MonsterSkillData.TimelineNotation.ToString();
            }

            return "";
        }
    }

    public string SkillEffectDescription
    {
        get
        {
            if (IsPlayer && PlayerSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(PlayerSkillData.Details))
                    return FormatPlayerSkillEffectDescription(GameDataLocalization.SkillDetails(PlayerSkillData));
            }

            if (IsMonster && MonsterSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(MonsterSkillData.EffectDesc))
                    return FormatMonsterSkillEffectDescription(
                        GameDataLocalization.MonsterSkillDescription(MonsterSkillData),
                        MonsterCommand,
                        MonsterSkillData);
            }

            return "";
        }
    }

    public string SkillValueText
    {
        get
        {
            if (IsPlayer)
                return BattlePlayerSkillPreviewCalculator.GetTimelineValueText(
                    BattlePlayerSkillPreviewCalculator.CreatePreview(PlayerCommand));

            if (IsMonster)
                return GetMonsterDamageDisplayValueText(MonsterCommand);

            return "";
        }
    }

    public string MonsterRuntimeId
    {
        get
        {
            if (IsMonster && MonsterRuntime != null)
                return MonsterRuntime.RuntimeId;

            return "";
        }
    }

    public static BattleTimelinePreviewEntry CreatePlayer(
    int slotIndex,
    int orderIndex,
    PlayerReservedCommand command,
    int playerCommandIndex)
    {
        if (command == null)
            return null;

        BattleTimelinePreviewEntry entry = new BattleTimelinePreviewEntry
        {
            SlotIndex = slotIndex,
            OrderIndex = orderIndex,
            PlayerCommandIndex = playerCommandIndex,
            IsPlayer = true,
            IsMonster = false,
            CharacterRuntime = command.UserRuntime,
            PlayerSkillData = command.SkillData,
            PlayerCommand = command
        };

        return entry;
    }

    public static BattleTimelinePreviewEntry CreateMonster(
        int slotIndex,
        int orderIndex,
        MonsterReservedCommand command)
    {
        if (command == null)
            return null;

        return new BattleTimelinePreviewEntry
        {
            SlotIndex = slotIndex,
            OrderIndex = orderIndex,
            IsPlayer = false,
            IsMonster = true,
            MonsterRuntime = command.UserRuntime,
            MonsterSkillData = command.SkillData,
            MonsterCommand = command
        };
    }

    private static string GetPlayerDamageDisplayValueText(SkillMasterData skillData)
    {
        if (skillData == null)
            return string.Empty;

        List<SkillEffectEntry> entries = skillData.EffectEntries;

        if ((entries == null || entries.Count == 0) && DataManager.Instance != null)
            entries = SkillEffectParser.Parse(skillData, DataManager.Instance.EffectDatabase);

        if (entries == null)
            return string.Empty;

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry == null || !IsDamageEffect(entry.EffectId))
                continue;

            int damage = SkillValueCalculator.GetValue(entry);
            if (damage <= 0)
                damage = Mathf.Max(0, entry.ValueAmount);

            if (damage <= 0)
                return string.Empty;

            int hitCount = Mathf.Max(1, entry.CountAmount);
            return hitCount > 1 ? $"{damage}x{hitCount}" : damage.ToString();
        }

        return string.Empty;
    }

    private static string GetMonsterDamageDisplayValueText(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return string.Empty;

        string damageText = BattleDamageService.GetMonsterDamageTotalText(command);
        if (string.IsNullOrWhiteSpace(damageText))
            return string.Empty;

        int hitCount = GetDamageHitCount(command.SkillData.EffectEntries);
        return hitCount > 1 ? $"{damageText}x{hitCount}" : damageText;
    }

    private static int GetDamageHitCount(List<SkillEffectEntry> entries)
    {
        if (entries == null)
            return 1;

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry != null && IsDamageEffect(entry.EffectId))
                return Mathf.Max(1, entry.CountAmount);
        }

        return 1;
    }

    private static bool IsDamageEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce";
    }

    private static string GetDisplayValueText(List<SkillEffectEntry> effectEntries, int payAmount)
    {
        return GetDisplayValueText(effectEntries, payAmount, null);
    }

    private static string GetDisplayValueText(
        List<SkillEffectEntry> effectEntries,
        int payAmount,
        MonsterReservedCommand monsterCommand)
    {
        if (effectEntries == null || effectEntries.Count <= 0)
            return "";

        for (int i = 0; i < effectEntries.Count; i++)
        {
            SkillEffectEntry entry = effectEntries[i];

            if (entry == null)
                continue;

            if (!ShouldShowValueText(entry.EffectId))
                continue;

            if (monsterCommand != null &&
                BattleDamageService.IsMonsterDamageHitEffect(entry.EffectId))
            {
                string damageText = BattleDamageService.GetMonsterDamageTotalText(monsterCommand);

                if (!string.IsNullOrWhiteSpace(damageText))
                    return damageText;
            }

            int value = SkillValueCalculator.GetValue(entry);

            if (value <= 0)
                value = entry.ValueAmount;

            return value.ToString();
        }

        return "";
    }

    private static string GetMonsterDisplayValueText(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return "";

        string valueText = GetDisplayValueText(command.SkillData.EffectEntries, 0, command);

        if (!string.IsNullOrWhiteSpace(valueText))
            return valueText;

        if (!BattleDamageService.ShouldReserveMonsterDamage(command.SkillData))
            return "";

        return BattleDamageService.GetMonsterDamageTotalText(command);
    }

    private static bool ShouldShowValueText(string effectId)
    {
        switch (effectId)
        {
            case "E_Strike":
            case "E_Pierce":
            case "E_Poison":
            case "E_Bleed":
            case "E_Ward":
            case "E_Boost":
            case "E_Armor":
                return true;

            default:
                return false;
        }
    }

    private int GetPlayerPayAmount()
    {
        if (PlayerCommand == null || PlayerSkillData == null)
            return 0;

        switch (PlayerSkillData.ReferenceResource)
        {
            case ReferenceResource.HP:
                return PlayerCommand.HPCost;

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return PlayerCommand.Cost;

            case ReferenceResource.UniqueResource:
                return PlayerCommand.ResourceCost;

            default:
                return 0;
        }
    }

    private string FormatPlayerSkillEffectDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "";

        int payAmount = PlayerCommand != null
            ? GetPlayerPayAmount()
            : PlayerSkillData.ResourceCostValue;

        return BattlePlayerSkillPreviewCalculator.FormatDescription(
            PlayerSkillData,
            description,
            BattlePlayerSkillPreviewCalculator.CreatePreview(PlayerCommand));
    }

    private static Sprite GetCharacterTimelineIcon(string characterId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetTimelineIcon(characterId, out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetMonsterIcon(string monsterId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.MonsterIconDatabase == null)
            return null;

        if (DataManager.Instance.MonsterIconDatabase.TryGetTimelineIcon(monsterId, out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetMonsterSkillIcon(MonsterSkillData skillData)
    {
        if (skillData == null)
            return null;

        if (cachedMonsterSkillIconDatabase == null)
        {
            MonsterSkillIconDatabase[] databases = Resources.FindObjectsOfTypeAll<MonsterSkillIconDatabase>();
            if (databases != null && databases.Length > 0)
                cachedMonsterSkillIconDatabase = databases[0];
        }

        if (cachedMonsterSkillIconDatabase != null &&
            !string.IsNullOrWhiteSpace(skillData.SkillIcon) &&
            cachedMonsterSkillIconDatabase.TryGetIcon(skillData.SkillIcon, out Sprite skillIcon))
        {
            return skillIcon;
        }

        return GetTimelineActionIcon(skillData.TimelineNotation);
    }

    private static Sprite GetTimelineActionIcon(TimelineActionType actionType)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[TimelineIcon] DataManager°¡ ¾ø½?´Ï´Ù.");
            return null;
        }

        if (DataManager.Instance.ActionTypeIconDatabase == null)
        {
            Debug.LogWarning("[TimelineIcon] ActionTypeIconDatabase°¡ ¾ø½?´Ï´Ù.");
            return null;
        }

        string key = actionType.ToString();

        bool found = DataManager.Instance.ActionTypeIconDatabase.TryGetIcon(key, out Sprite icon);

        if (found)
            return icon;

        return null;
    }

    private static Sprite GetSkillIcon(string skillId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.SkillIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetSkillRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId))
            return null;

        if (DataManager.Instance == null ||
            DataManager.Instance.SkillRangeIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon))
            return icon;

        return null;
    }

    private static string FormatMonsterSkillEffectDescription(
        string description,
        MonsterReservedCommand command,
        MonsterSkillData skillData)
    {
        MonsterSkillData sourceSkillData = command != null && command.SkillData != null
            ? command.SkillData
            : skillData;

        return MonsterSkillDescriptionFormatter.Format(description, sourceSkillData);
    }



}
