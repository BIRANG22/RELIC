using NUnit.Framework;
using Relic.Gameplay.Data;

public class DiscordPresenceSnapshotBuilderTests
{
    private const long StartUnixSeconds = 1785592800L;

    [Test]
    public void Build_TitleScene_UsesMainMenuCopy()
    {
        DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
            "Title",
            null,
            new PartyRuntimeStore(),
            CreateCharacterDatabase(),
            StartUnixSeconds);

        Assert.That(snapshot.Details, Is.EqualTo("메인 메뉴"));
        Assert.That(snapshot.State, Is.EqualTo("모험 준비 중"));
        Assert.That(snapshot.StartUnixSeconds, Is.EqualTo(StartUnixSeconds));
    }

    [Test]
    public void Build_LobbyScene_UsesCharacterDisplayNamesInPartyOrder()
    {
        PartyRuntimeStore party = new();
        party.SetCharacter(0, "char_elise");
        party.SetCharacter(1, "char_biran");

        DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
            "Lobby",
            null,
            party,
            CreateCharacterDatabase(),
            StartUnixSeconds);

        Assert.That(snapshot.Details, Is.EqualTo("로비"));
        Assert.That(snapshot.State, Is.EqualTo("캐릭터: 엘리스, 비란"));
    }

    [Test]
    public void Build_LobbySceneWithActiveRun_StillShowsLobby()
    {
        MapRuntimeData map = new()
        {
            IsRunInitialized = true,
            SelectedChapterId = "chapter_previous",
            CurrentStage = "stage_previous"
        };

        DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
            "Lobby",
            map,
            new PartyRuntimeStore(),
            CreateCharacterDatabase(),
            StartUnixSeconds);

        Assert.That(snapshot.Details, Is.EqualTo("로비"));
    }

    [Test]
    public void Build_ActiveRun_UsesChapterAndStage()
    {
        MapRuntimeData map = new()
        {
            IsRunInitialized = true,
            SelectedChapterId = "chapter_01",
            CurrentStage = "stage_03",
            CurrentMapId = "battle_07"
        };

        DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
            "Battle",
            map,
            new PartyRuntimeStore(),
            CreateCharacterDatabase(),
            StartUnixSeconds);

        Assert.That(snapshot.Details, Is.EqualTo("chapter_01 · stage_03"));
        Assert.That(snapshot.State, Is.EqualTo("파티 편성 중"));
        Assert.That(snapshot.StartUnixSeconds, Is.EqualTo(StartUnixSeconds));
    }

    [Test]
    public void Build_UnknownCharacterAndMissingStage_FallsBackToStableIds()
    {
        PartyRuntimeStore party = new();
        party.SetCharacter(0, "char_unknown");
        MapRuntimeData map = new()
        {
            IsRunInitialized = true,
            SelectedChapterId = "chapter_02",
            CurrentMapId = "battle_boss"
        };

        DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
            "Battle",
            map,
            party,
            CreateCharacterDatabase(),
            StartUnixSeconds);

        Assert.That(snapshot.Details, Is.EqualTo("chapter_02 · battle_boss"));
        Assert.That(snapshot.State, Is.EqualTo("캐릭터: char_unknown"));
    }

    private static CharacterDatabase CreateCharacterDatabase()
    {
        CharacterDatabase database = new();
        database.Initialize(new[]
        {
            new CharacterMasterData { CharacterId = "char_elise", Name = "엘리스" },
            new CharacterMasterData { CharacterId = "char_biran", Name = "비란" }
        });
        return database;
    }
}
