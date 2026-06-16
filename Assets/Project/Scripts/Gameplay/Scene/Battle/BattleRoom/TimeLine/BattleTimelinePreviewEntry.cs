using Relic.Gameplay.Data;
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
    public int PlayerCommandIndex = -1;
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
            MonsterSkillData = command.SkillData
        };
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
