public static class SteamLobbySessionState
{
    public static bool IsActive { get; private set; }
    public static ulong LobbyId { get; private set; }
    public static ulong LocalSteamId { get; private set; }
    public static ulong HostSteamId { get; private set; }
    public static LobbyPartySnapshot PartySnapshot { get; private set; }

    public static bool IsLocalHost =>
        IsActive && LocalSteamId != 0UL && LocalSteamId == HostSteamId;

    public static void SetLobby(ulong lobbyId, ulong localSteamId, ulong hostSteamId)
    {
        if (lobbyId == 0UL || localSteamId == 0UL || hostSteamId == 0UL)
            return;

        LobbyId = lobbyId;
        LocalSteamId = localSteamId;
        HostSteamId = hostSteamId;
        IsActive = true;
    }

    public static void SetPartySnapshot(LobbyPartySnapshot snapshot)
    {
        if (snapshot == null)
            return;

        PartySnapshot = snapshot;

        if (snapshot.HostSteamId != 0UL)
            HostSteamId = snapshot.HostSteamId;
    }

    public static bool TryGetLobby(out ulong lobbyId, out ulong localSteamId, out ulong hostSteamId)
    {
        lobbyId = LobbyId;
        localSteamId = LocalSteamId;
        hostSteamId = HostSteamId;

        return IsActive && lobbyId != 0UL && localSteamId != 0UL && hostSteamId != 0UL;
    }

    public static bool IsMemberAllowedToControlCharacter(ulong memberSteamId, string characterId)
    {
        if (!IsActive)
            return true;

        if (memberSteamId == 0UL || string.IsNullOrWhiteSpace(characterId))
            return false;

        LobbyPartySnapshot snapshot = PartySnapshot;
        if (snapshot == null || snapshot.Slots == null)
            return memberSteamId == HostSteamId;

        string targetId = characterId.Trim();

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = snapshot.Slots[i];

            if (slot == null)
                continue;

            if (string.Equals(slot.CharacterId, targetId, System.StringComparison.Ordinal))
                return slot.OwnerSteamId == memberSteamId;
        }

        return false;
    }

    public static ulong GetCharacterOwnerSteamId(string characterId)
    {
        LobbyPartySnapshot snapshot = PartySnapshot;
        if (snapshot == null || snapshot.Slots == null || string.IsNullOrWhiteSpace(characterId))
            return 0UL;

        string targetId = characterId.Trim();

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = snapshot.Slots[i];

            if (slot != null &&
                string.Equals(slot.CharacterId, targetId, System.StringComparison.Ordinal))
            {
                return slot.OwnerSteamId;
            }
        }

        return 0UL;
    }

    public static void Clear()
    {
        IsActive = false;
        LobbyId = 0UL;
        LocalSteamId = 0UL;
        HostSteamId = 0UL;
        PartySnapshot = null;
    }
}
