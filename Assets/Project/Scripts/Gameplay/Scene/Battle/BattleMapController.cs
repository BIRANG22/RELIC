using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleMapController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private MapViewSpawner mapViewSpawner;

    [Header("Rooms")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject battleRoom;
    [SerializeField] private GameObject chestRoom;
    [SerializeField] private GameObject shopRoom;
    [SerializeField] private GameObject restRoom;
    [SerializeField] private GameObject eventRoom;

    public void OpenMap()
    {
        CloseAllRooms();

        if (mapPanel != null)
            mapPanel.SetActive(true);

        List<GeneratedMapNodeData> nodes =
            DataManager.Instance.MapRuntimeStore.Get().GeneratedNodes;

        mapViewSpawner.Spawn(nodes, OnNodeClicked);
    }

    private void OnNodeClicked(GeneratedMapNodeData node)
    {
        if (node == null)
            return;

        // 현재 위치 저장
        DataManager.Instance.MapRuntimeStore.Get().CurrentMapId = node.MapId;

        if (mapPanel != null)
            mapPanel.SetActive(false);

        OpenRoomByNodeType(node.Type);
    }

    private void OpenRoomByNodeType(string nodeType)
    {
        CloseAllRooms();

        switch (nodeType)
        {
            case "Start":
                if (startRoom != null)
                    startRoom.SetActive(true);
                break;

            case "Common":
            case "Elite":
            case "Boss":
                if (battleRoom != null)
                    battleRoom.SetActive(true);
                break;

            case "Chest":
                if (chestRoom != null)
                    chestRoom.SetActive(true);
                break;

            case "Shop":
                if (shopRoom != null)
                    shopRoom.SetActive(true);
                break;

            case "Rest":
                if (restRoom != null)
                    restRoom.SetActive(true);
                break;

            case "Special":
                if (eventRoom != null)
                    eventRoom.SetActive(true);
                break;
        }
    }

    private void CloseAllRooms()
    {
        if (startRoom != null) startRoom.SetActive(false);
        if (battleRoom != null) battleRoom.SetActive(false);
        if (chestRoom != null) chestRoom.SetActive(false);
        if (shopRoom != null) shopRoom.SetActive(false);
        if (restRoom != null) restRoom.SetActive(false);
        if (eventRoom != null) eventRoom.SetActive(false);
    }
}