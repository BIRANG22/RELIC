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

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null || runtime.GeneratedNodes == null)
        {
            Debug.LogWarning("[BattleMapController] MapRuntimeData 또는 GeneratedNodes 없음");
            return;
        }

        Debug.Log(
            $"[BattleMapController] OpenMap / CurrentNode:{runtime.CurrentNodeIndex} / CurrentMap:{runtime.CurrentMapId}"
        );

        mapViewSpawner.Spawn(runtime.GeneratedNodes, OnNodeClicked);
    }

    private void OnNodeClicked(GeneratedMapNodeData node)
    {
        if (node == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        runtime.CurrentMapId = node.MapId;
        runtime.CurrentNodeIndex = node.NodeIndex;

        if (!runtime.VisitedMapIds.Contains(node.NodeIndex.ToString()))
            runtime.VisitedMapIds.Add(node.NodeIndex.ToString());

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[BattleMapController] Node Click / Node:{node.NodeIndex} / Map:{node.MapId} / Type:{node.Type}"
        );

        if (mapPanel != null)
            mapPanel.SetActive(false);

        OpenRoomByNodeType(node.Type);

        Debug.Log(
    $"SAVE TEST / Node:{runtime.CurrentNodeIndex}"
);
    }

    public void CompleteCurrentNode()
    {
        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[BattleMapController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
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