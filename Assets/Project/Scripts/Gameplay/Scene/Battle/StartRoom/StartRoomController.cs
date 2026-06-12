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

    [SerializeField] private GameObject startRoomRoot;
    [SerializeField] private GameObject mapPanel;

    private bool isDialogPlaying;
    private bool isRelicChoiceOpened;
    private bool isRelicSelected;

    public void CompleteStartRoom()
    {
        if (startRoomRoot != null)
            startRoomRoot.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(true);
    }

    private void OnEnable()
    {
        SpawnPartyAllies();

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Close();
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
                Debug.LogWarning($"[StartRoomController] LobbyPrefab ¾øÀ½: {characterId}");
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
        if (isDialogPlaying)
            return;

        if (isRelicChoiceOpened)
            return;

        if (isRelicSelected)
            return;

        isDialogPlaying = true;

        chatWindow.Open(npcDialogLines, OnDialogFinished);
    }

    private void OnDialogFinished()
    {
        isDialogPlaying = false;
        isRelicChoiceOpened = true;

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Open();
    }

    public void OnRelicChoiceFinished()
    {
        isRelicChoiceOpened = false;
        isRelicSelected = true;
    }
}