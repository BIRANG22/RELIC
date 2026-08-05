using NUnit.Framework;
using UnityEngine;

public class BattleRoomMapSelectionPresenterTests
{
    [Test]
    public void CalculateCharacterPosition_ArrangesCharactersInOneHorizontalRow()
    {
        Vector3 first = BattleRoomMapSelectionPresenter.CalculateCharacterPosition(0);
        Vector3 second = BattleRoomMapSelectionPresenter.CalculateCharacterPosition(1);
        Vector3 third = BattleRoomMapSelectionPresenter.CalculateCharacterPosition(2);

        Assert.That(second.x, Is.GreaterThan(first.x));
        Assert.That(third.x, Is.GreaterThan(second.x));
        Assert.That(second.y, Is.EqualTo(first.y));
        Assert.That(third.y, Is.EqualTo(first.y));
        Assert.That(first.y, Is.EqualTo(-0.25f));
        Assert.That(first.z, Is.EqualTo(-2f));
    }

    [Test]
    public void Show_KeepsRoomActiveAndHidesRoomUiRoots()
    {
        GameObject presenterObject = new("Presenter");
        GameObject room = new("Room");
        GameObject roomUi = new("RoomUI");
        room.SetActive(false);
        roomUi.SetActive(true);

        try
        {
            BattleRoomMapSelectionPresenter presenter =
                presenterObject.AddComponent<BattleRoomMapSelectionPresenter>();

            presenter.Show(room, new Transform[0], new[] { roomUi });

            Assert.That(room.activeSelf, Is.True);
            Assert.That(roomUi.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(room);
            Object.DestroyImmediate(roomUi);
        }
    }

    [Test]
    public void Show_AutoDiscoversMarkedEventCharacterAndMovesItToMapSelectionPosition()
    {
        GameObject presenterObject = new("Presenter");
        GameObject room = new("StartRoom");
        GameObject characterObject = new("PartyCharacter");
        characterObject.transform.SetParent(room.transform);
        characterObject.transform.position = new Vector3(10f, 10f, 10f);
        characterObject.AddComponent<BattleMapSelectionCharacterMarker>();

        try
        {
            BattleRoomMapSelectionPresenter presenter =
                presenterObject.AddComponent<BattleRoomMapSelectionPresenter>();

            presenter.Show(room);

            Assert.That(characterObject.transform.position,
                Is.EqualTo(new Vector3(-5.5f, -0.25f, -2f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(room);
        }
    }


    [Test]
    public void Show_DisablesCharacterInteractionAndFacesCharacterRightUntilHide()
    {
        GameObject presenterObject = new("Presenter");
        GameObject room = new("Room");
        GameObject characterObject = new("Character");
        characterObject.transform.SetParent(room.transform);
        characterObject.AddComponent<BattleCharacter>();
        BoxCollider2D collider = characterObject.AddComponent<BoxCollider2D>();
        BattleUnitFacing facing = characterObject.AddComponent<BattleUnitFacing>();
        facing.FaceRight(false);

        try
        {
            BattleRoomMapSelectionPresenter presenter =
                presenterObject.AddComponent<BattleRoomMapSelectionPresenter>();

            presenter.Show(room, new[] { characterObject.transform }, new GameObject[0]);

            Assert.That(collider.enabled, Is.False);
            Assert.That(facing.IsFacingRight, Is.True);

            presenter.Hide();
            Assert.That(collider.enabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(room);
        }
    }
}
