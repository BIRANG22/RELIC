using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class BattleRoomMapSelectionPresenterTests
{
    [Test]
    public void CloseAllRooms_KeepsSharedRoomRootActive()
    {
        GameObject controllerObject = new("BattleSceneController");
        GameObject roomRoot = new("RoomRoot");
        GameObject sharedRoot = new("SharedRoomRoot");
        GameObject battleRoom = new("BattleRoom");
        sharedRoot.transform.SetParent(roomRoot.transform);
        battleRoom.transform.SetParent(roomRoot.transform);

        try
        {
            BattleSceneController controller = controllerObject.AddComponent<BattleSceneController>();
            SetPrivateField(controller, "roomRoot", roomRoot.transform);
            SetPrivateField(controller, "sharedRoomRoot", sharedRoot);

            typeof(BattleSceneController).GetMethod(
                "CloseAllRooms",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);

            Assert.That(sharedRoot.activeSelf, Is.True);
            Assert.That(battleRoom.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(roomRoot);
        }
    }

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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    [Test]
    public void OpenRoom_BattleRoomHidesSharedPartyPresentationRoot()
    {
        GameObject controllerObject = new("BattleSceneController");
        GameObject roomRoot = new("RoomRoot");
        GameObject sharedRoot = new("SharedRoomRoot");
        GameObject allyRoot = new("AllyRoot");
        GameObject battleRoom = new("BattleRoom");
        sharedRoot.transform.SetParent(roomRoot.transform);
        allyRoot.transform.SetParent(sharedRoot.transform);
        battleRoom.transform.SetParent(roomRoot.transform);
        allyRoot.SetActive(true);

        try
        {
            BattleSceneController controller = controllerObject.AddComponent<BattleSceneController>();
            SetPrivateField(controller, "roomRoot", roomRoot.transform);
            SetPrivateField(controller, "sharedRoomRoot", sharedRoot);
            SetPrivateField(controller, "sharedPartyPresentationRoot", allyRoot);
            SetPrivateField(controller, "battleRoom", battleRoom);

            typeof(BattleSceneController).GetMethod(
                "OpenRoom",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
                controller,
                new object[] { battleRoom, "BattleRoom" });

            Assert.That(sharedRoot.activeSelf, Is.True);
            Assert.That(allyRoot.activeSelf, Is.False);
            Assert.That(battleRoom.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(roomRoot);
        }
    }

    [Test]
    public void ActivateMapRoomForMap_ShowsSharedPartyPresentationRoot()
    {
        GameObject controllerObject = new("BattleSceneController");
        GameObject roomRoot = new("RoomRoot");
        GameObject sharedRoot = new("SharedRoomRoot");
        GameObject allyRoot = new("AllyRoot");
        GameObject battleRoom = new("BattleRoom");
        sharedRoot.transform.SetParent(roomRoot.transform);
        allyRoot.transform.SetParent(sharedRoot.transform);
        battleRoom.transform.SetParent(roomRoot.transform);
        allyRoot.SetActive(false);
        battleRoom.SetActive(true);

        try
        {
            BattleSceneController controller = controllerObject.AddComponent<BattleSceneController>();
            SetPrivateField(controller, "roomRoot", roomRoot.transform);
            SetPrivateField(controller, "sharedRoomRoot", sharedRoot);
            SetPrivateField(controller, "sharedPartyPresentationRoot", allyRoot);
            SetPrivateField(controller, "battleRoom", battleRoom);

            typeof(BattleSceneController).GetMethod(
                "ActivateMapRoomForMap",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);

            Assert.That(sharedRoot.activeSelf, Is.True);
            Assert.That(allyRoot.activeSelf, Is.True);
            Assert.That(battleRoom.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(roomRoot);
        }
    }

    [UnityTest]
    public IEnumerator BattleCharacterPanel_BindMovesToReservationWhenSelectionAlreadyExists()
    {
        GameObject timelineObject = new("BattleTimelineController");
        GameObject executorObject = new("BattleTurnExecutor");
        GameObject panelObject = new("BattleCharacterPanel", typeof(RectTransform));

        try
        {
            BattleTimelineController timeline = timelineObject.AddComponent<BattleTimelineController>();
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();
            BattleCharacterPanelUI panel = panelObject.AddComponent<BattleCharacterPanelUI>();
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_01",
                MaxHP = 10,
                CurrentHP = 10,
                MaxCost = 3,
                CurrentCost = 3,
                CostRecovery = 1
            };

            panelRect.anchoredPosition = new Vector2(0f, 150f);
            SetPrivateField(timeline, "selectedCharacter", runtime);
            SetPrivateField(executor, "isMonsterPlanReady", true);
            SetPrivateField(executor, "isPlayerInputReady", true);
            SetPrivateField(panel, "battleTimelineController", timeline);
            SetPrivateField(panel, "turnExecutor", executor);
            SetPrivateField(panel, "executionPositionY", 150f);
            SetPrivateField(panel, "reservationPositionY", 540f);
            SetPrivateField(panel, "panelMoveDuration", 0f);

            panel.Bind(runtime);
            yield return null;

            Assert.That(panelRect.anchoredPosition.y, Is.EqualTo(540f));
        }
        finally
        {
            Object.DestroyImmediate(timelineObject);
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(panelObject);
        }
    }

    private static TValue GetPrivateField<TValue>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        return (TValue)field.GetValue(target);
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
    public void Show_DoesNotAutoReuseBattleCharactersForMapSelection()
    {
        GameObject presenterObject = new("Presenter");
        GameObject room = new("BattleRoom");
        GameObject characterObject = new("BattleCharacter");
        characterObject.transform.SetParent(room.transform);
        characterObject.transform.position = new Vector3(10f, 10f, 10f);
        characterObject.AddComponent<BattleCharacter>();

        try
        {
            BattleRoomMapSelectionPresenter presenter =
                presenterObject.AddComponent<BattleRoomMapSelectionPresenter>();

            presenter.Show(room);

            Assert.That(characterObject.transform.position,
                Is.EqualTo(new Vector3(10f, 10f, 10f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(room);
        }
    }

    [Test]
    public void PrepareRoomForMapSelection_DoesNotReactivateBattleRoomForEventRoomMapReturn()
    {
        GameObject controllerObject = new("BattleSceneController");
        GameObject eventRoom = new("EventRoom");
        GameObject battleRoom = new("BattleRoom");
        eventRoom.SetActive(true);
        battleRoom.SetActive(false);

        try
        {
            BattleSceneController controller = controllerObject.AddComponent<BattleSceneController>();
            SetPrivateField(controller, "eventRoom", eventRoom);
            SetPrivateField(controller, "battleRoom", battleRoom);

            typeof(BattleSceneController).GetMethod(
                "PrepareRoomForMapSelection",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
                controller,
                new object[] { eventRoom });

            Assert.That(eventRoom.activeSelf, Is.False);
            Assert.That(battleRoom.activeSelf, Is.False);
            Assert.That(
                battleRoom.GetComponentsInChildren<BattleCharacter>(true),
                Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(eventRoom);
            Object.DestroyImmediate(battleRoom);
        }
    }

    [Test]
    public void BattleRoomCleaner_PrepareForMapSelection_HidesBattleCharactersImmediately()
    {
        GameObject cleanerObject = new("BattleRoomCleaner");
        GameObject characterObject = new("BattleCharacter");
        characterObject.AddComponent<BattleCharacter>();

        try
        {
            BattleRoomCleaner cleaner = cleanerObject.AddComponent<BattleRoomCleaner>();

            cleaner.PrepareForMapSelection();

            Assert.That(characterObject == null || !characterObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(cleanerObject);
            Object.DestroyImmediate(characterObject);
        }
    }

    [Test]
    public void BattleRoomLoader_EnsureBattleCharacterPanelPrefersPanelUnderOwnBattleHudCanvas()
    {
        GameObject mapPanelObject = new("MapPanel", typeof(RectTransform));
        GameObject battleRoomObject = new("BattleRoom");
        GameObject hudCanvasObject = new("BattleHUDCanvas", typeof(RectTransform));
        GameObject battlePanelObject = new("BattleCharacterPanel", typeof(RectTransform));

        try
        {
            BattleCharacterPanelUI wrongPanel =
                mapPanelObject.AddComponent<BattleCharacterPanelUI>();
            BattleRoomLoader loader = battleRoomObject.AddComponent<BattleRoomLoader>();

            hudCanvasObject.transform.SetParent(battleRoomObject.transform, false);
            battlePanelObject.transform.SetParent(hudCanvasObject.transform, false);
            BattleCharacterPanelUI battleRoomPanel =
                battlePanelObject.AddComponent<BattleCharacterPanelUI>();

            SetPrivateField(loader, "battleCharacterPanel", wrongPanel);

            typeof(BattleRoomLoader).GetMethod(
                "EnsureBattleCharacterPanel",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(loader, null);

            Assert.That(GetPrivateField<BattleCharacterPanelUI>(loader, "battleCharacterPanel"),
                Is.SameAs(battleRoomPanel));
            Assert.That(battleRoomPanel.transform.parent, Is.SameAs(hudCanvasObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(mapPanelObject);
            Object.DestroyImmediate(battleRoomObject);
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
