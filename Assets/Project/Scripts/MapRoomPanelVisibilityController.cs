using UnityEngine;

/// <summary>
/// BattleMapPanel의 활성 상태에 맞춰 MapRoom을 켜고 끕니다.
/// 이 컴포넌트는 BattleMapPanel과 같은 GameObject에 붙여 사용합니다.
/// </summary>
public class MapRoomPanelVisibilityController : MonoBehaviour
{
    [Header("Map Room")]
    [SerializeField] private GameObject mapRoom;
    [SerializeField] private string mapRoomObjectName = "MapRoom";
    [SerializeField] private bool autoFindMapRoom = true;

    private void Awake()
    {
        ResolveMapRoom();
    }

    private void OnEnable()
    {
        ResolveMapRoom();
        SetMapRoomActive(true);
    }

    private void OnDisable()
    {
        SetMapRoomActive(false);
    }

    private void ResolveMapRoom()
    {
        if (mapRoom != null || !autoFindMapRoom || string.IsNullOrWhiteSpace(mapRoomObjectName))
            return;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != mapRoomObjectName)
                continue;

            GameObject candidateObject = candidate.gameObject;
            if (!candidateObject.scene.IsValid() || !candidateObject.scene.isLoaded)
                continue;

            mapRoom = candidateObject;
            return;
        }
    }

    private void SetMapRoomActive(bool active)
    {
        if (mapRoom == null)
            ResolveMapRoom();

        if (mapRoom == null)
        {
            if (active)
            {
                Debug.LogWarning(
                    "[MapRoomPanelVisibilityController] MapRoom을 찾지 못했습니다. " +
                    "Inspector에서 Map Room을 연결하거나 오브젝트 이름을 확인하세요.",
                    this);
            }

            return;
        }

        if (mapRoom.activeSelf != active)
            mapRoom.SetActive(active);

        if (!active)
            return;

        MapRoomController controller = mapRoom.GetComponent<MapRoomController>();
        if (controller == null)
            controller = mapRoom.GetComponentInChildren<MapRoomController>(true);

        controller?.RefreshNow();
    }
}
