using UnityEngine;
using Relic.Gameplay.Data;

public class StartRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;

    [Header("UI")]
    [SerializeField] private StartRoomChatWindow chatWindow;
    [SerializeField] private RelicChoiceAreaUI relicChoiceArea;

    [Header("Dialog")]
    [TextArea]
    [SerializeField] private string[] npcDialogLines;

    [Header("Room")]
    [SerializeField] private GameObject startRoomRoot;
    [SerializeField] private GameObject mapPanel;

    private bool isDialogPlaying;
    private bool isRelicChoiceOpened;
    private bool isRelicSelected;

    private void Awake()
    {
        if (chatWindow == null)
            chatWindow = GetComponentInChildren<StartRoomChatWindow>(true);

        if (relicChoiceArea == null)
            relicChoiceArea = GetComponentInChildren<RelicChoiceAreaUI>(true);
    }

    private void OnEnable()
    {
        isDialogPlaying = false;
        isRelicChoiceOpened = false;

        SpawnPartyAllies();

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Close();
    }

    public void CompleteStartRoom()
    {
        if (startRoomRoot != null)
            startRoomRoot.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(true);
    }

    private void SpawnPartyAllies()
    {
        if (DataManager.Instance == null)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point == null)
                continue;

            ClearPoint(point);

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetPreviewWorldPrefab(characterId, out GameObject lobbyPrefab))
            {
                Debug.LogWarning($"[StartRoomController] Lobby prefab not found: {characterId}");
                continue;
            }

            GameObject ally = Instantiate(lobbyPrefab, point);
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one;
        }
    }

    private void ClearPoint(Transform point)
    {
        for (int i = point.childCount - 1; i >= 0; i--)
            Destroy(point.GetChild(i).gameObject);
    }

    public void OnNpcClicked()
    {
        if (isDialogPlaying || isRelicChoiceOpened || isRelicSelected)
            return;

        isDialogPlaying = true;

        if (chatWindow != null)
            chatWindow.Open(npcDialogLines, OnDialogFinished);
        else
            OnDialogFinished();
    }

    private void OnDialogFinished()
    {
        if (isRelicSelected)
            return;

        isDialogPlaying = false;
        isRelicChoiceOpened = true;

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Open();
        else
            Debug.LogWarning("[StartRoomController] RelicChoiceAreaUI is not connected.");
    }

    public void OnRelicChoiceFinished()
    {
        isDialogPlaying = false;
        isRelicChoiceOpened = false;
        isRelicSelected = true;

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
        {
            sceneController.ReturnToMap();
        }
        else
        {
            Debug.LogWarning("[StartRoomController] BattleSceneController ¾øÀ½");
        }
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[StartRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }
}
