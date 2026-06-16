using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class BattleTimelinePreviewEntry
{
    public int SlotIndex;
    public int OrderIndex;

    public bool IsPlayer;
    public bool IsMonster;

    public CharacterRuntimeData CharacterRuntime;
    public MonsterRuntimeData MonsterRuntime;
    public SkillMasterData PlayerSkillData;
    public MonsterSkillData MonsterSkillData;
    public PlayerReservedCommand PlayerCommand { get; private set; }
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
                return GetTimelineActionIcon(MonsterSkillData.TimelineNotation);

            if (IsPlayer && PlayerSkillData != null)
                return GetSkillIcon(PlayerSkillData.SkillId);

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
                    return PlayerSkillData.Name;

                return PlayerSkillData.SkillId;
            }

            if (IsMonster && MonsterSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(MonsterSkillData.Name))
                    return MonsterSkillData.Name;

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
                if (!string.IsNullOrWhiteSpace(PlayerSkillData.EffectDescription))
                    return PlayerSkillData.EffectDescription;

                if (!string.IsNullOrWhiteSpace(PlayerSkillData.EffectDesc))
                    return PlayerSkillData.EffectDesc;

                if (!string.IsNullOrWhiteSpace(PlayerSkillData.ToolTip))
                    return PlayerSkillData.ToolTip;

                if (!string.IsNullOrWhiteSpace(PlayerSkillData.Details))
                    return PlayerSkillData.Details;
            }

            if (IsMonster && MonsterSkillData != null)
            {
                if (!string.IsNullOrWhiteSpace(MonsterSkillData.EffectDescription))
                    return MonsterSkillData.EffectDescription;

                if (!string.IsNullOrWhiteSpace(MonsterSkillData.EffectDesc))
                    return MonsterSkillData.EffectDesc;
            }

            return "";
        }
    }

    public string SkillValueText
    {
        get
        {
            if (IsPlayer)
                return GetDisplayValueText(PlayerSkillData != null ? PlayerSkillData.EffectEntries : null, GetPlayerPayAmount());

            if (IsMonster)
                return GetDisplayValueText(MonsterSkillData != null ? MonsterSkillData.EffectEntries : null, 0);

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
        PlayerReservedCommand command)
    {
        if (command == null)
            return null;

        BattleTimelinePreviewEntry entry = new BattleTimelinePreviewEntry
        {
            SlotIndex = slotIndex,
            OrderIndex = orderIndex,
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

    private static string GetDisplayValueText(List<SkillEffectEntry> effectEntries, int payAmount)
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

            int value = SkillValueCalculator.GetValue(entry, payAmount);

            if (value <= 0)
                value = entry.ValueAmount;

            return value.ToString();
        }

        return "";
    }

    private static bool ShouldShowValueText(string effectId)
    {
        switch (effectId)
        {
            case "E_Strike":
            case "E_Pierce":
            case "E_Addicted":
            case "E_Bleeding":
            case "E_Burn":
            case "E_Thorns":
            case "E_Power":
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
            case ReferenceResource.Health:
                return PlayerCommand.HealthCost;

            case ReferenceResource.Stamina:
                return PlayerCommand.StaminaCost;

            case ReferenceResource.UniqueResource:
                return PlayerCommand.ResourceCost;

            case ReferenceResource.MovePoint:
                return PlayerCommand.MoveCost;

            default:
                return 0;
        }
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

    private static Sprite GetTimelineActionIcon(TimelineActionType actionType)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[TimelineIcon] DataManager가 없습니다.");
            return null;
        }

        if (DataManager.Instance.ActionTypeIconDatabase == null)
        {
            Debug.LogWarning("[TimelineIcon] ActionTypeIconDatabase가 없습니다.");
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
}
