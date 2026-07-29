using System;
using Relic.Gameplay.Data;

public enum BattleNetworkCommandType
{
    None,
    SelectTimelineSlot,
    ClearTimelineSlotSelection,
    ReservePlayerCommand,
    RemovePlayerCommand,
    SetReady,
    EquipRelic,
    UnequipRelic,
    EquipSkill,
    UnequipSkill
}

public enum BattleNetworkRejectReason
{
    None,
    UnknownMember,
    NotCharacterOwner,
    InvalidSlot,
    SlotViewedByOtherMember,
    InvalidCommand,
    RejectedByService
}

[Serializable]
public sealed class BattleNetworkCommand
{
    public int version = BattleNetworkSerialization.ProtocolVersion;
    public string requestId;
    public string requesterSteamId;
    public int commandType;
    public int slotIndex = -1;
    public int commandIndex = -1;
    public string characterId;
    public string itemId;
    public string skillId;
    public int direction;
    public int selectedGridIndex = -1;
    public int moveOffsetX;
    public int moveOffsetY;
    public int plannedMoveDistance;
    public int moveDistancePerCost = 1;
    public int[] rangeGridIndices;
    public int[] targetGridIndices;
    public bool ready;
    public long knownRevision;
}

[Serializable]
public sealed class BattleNetworkCommandResponse
{
    public int version = BattleNetworkSerialization.ProtocolVersion;
    public string requestId;
    public string requesterSteamId;
    public bool accepted;
    public int rejectReason;
    public long resultRevision;
    public BattleNetworkSnapshot snapshot;
}

[Serializable]
public sealed class BattleNetworkSnapshot
{
    public int version = BattleNetworkSerialization.ProtocolVersion;
    public string hostSteamId;
    public long revision;
    public bool isExecuting;
    public MapRuntimeData map;
    public BattleRuntimeData battle;
    public string[] startRelicChoiceIds;
    public BattleNetworkPartySlotSnapshot[] partySlots;
    public CharacterRuntimeData[] characters;
    public BattleNetworkMonsterSnapshot[] monsters;
    public BattleNetworkTimelineSlotSnapshot[] timelineSlots;
    public BattleNetworkMemberTimelineSelection[] viewedSlots;
    public BattleNetworkMemberReadyState[] readyStates;
}

[Serializable]
public sealed class BattleNetworkExecutionSnapshot
{
    public int version = BattleNetworkSerialization.ProtocolVersion;
    public string hostSteamId;
    public long revision;
    public BattleNetworkExecutionBatchSnapshot[] batches;
}

[Serializable]
public sealed class BattleNetworkStartRelicChoicesMessage
{
    public int version = BattleNetworkSerialization.ProtocolVersion;
    public string hostSteamId;
    public string[] relicIds;
}

[Serializable]
public sealed class BattleNetworkExecutionBatchSnapshot
{
    public int timelineSlotIndex = -1;
    public BattleNetworkPlayerCommandSnapshot[] playerCommands;
    public BattleNetworkMonsterCommandSnapshot[] monsterCommands;
}

[Serializable]
public sealed class BattleNetworkPartySlotSnapshot
{
    public int slotIndex = -1;
    public string ownerSteamId;
    public string characterId;
    public int spawnGridIndex = -1;
    public int currentGridIndex = -1;
}

[Serializable]
public sealed class BattleNetworkMonsterSnapshot
{
    public MonsterRuntimeData runtime;
    public int[] occupiedGridIndices;
}

[Serializable]
public sealed class BattleNetworkTimelineSlotSnapshot
{
    public int slotIndex = -1;
    public BattleNetworkPlayerCommandSnapshot[] playerCommands;
    public BattleNetworkMonsterCommandSnapshot[] monsterCommands;
}

[Serializable]
public sealed class BattleNetworkPlayerCommandSnapshot
{
    public string characterId;
    public string skillId;
    public int direction;
    public int selectedGridIndex = -1;
    public int moveOffsetX;
    public int moveOffsetY;
    public int plannedMoveDistance;
    public int moveDistancePerCost = 1;
    public int[] rangeGridIndices;
    public int[] targetGridIndices;
}

[Serializable]
public sealed class BattleNetworkMonsterCommandSnapshot
{
    public string runtimeId;
    public string skillId;
    public int moveOffsetX;
    public int moveOffsetY;
    public int reservedDamage = -1;
    public int actionIndex;
    public int rangeOriginGridIndex = -1;
    public bool hasForcedDirection;
    public int forcedDirection;
    public bool isPortalMove;
    public int[] rangeGridIndices;
    public int[] targetGridIndices;
}

[Serializable]
public sealed class BattleNetworkMemberTimelineSelection
{
    public string memberSteamId;
    public int slotIndex = -1;
}

[Serializable]
public sealed class BattleNetworkMemberReadyState
{
    public string memberSteamId;
    public bool ready;
}
