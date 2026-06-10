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
                return GetCharacterIcon(OwnerId);

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
            MonsterSkillData = command.SkillData
        };
    }

    private static Sprite GetCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetMonsterIcon(string monsterId)
    {
        // MonsterIconDatabase가 아직 없으면 일단 null.
        // 나중에 만들면 여기 연결.
        return null;
    }
    private static Sprite GetTimelineActionIcon(TimelineActionType actionType)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[TimelineIcon] DataManager 없음");
            return null;
        }

        if (DataManager.Instance.ActionTypeIconDatabase == null)
        {
            Debug.LogWarning("[TimelineIcon] ActionTypeIconDatabase 없음");
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