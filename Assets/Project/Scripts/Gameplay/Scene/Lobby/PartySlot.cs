using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class PartySlot : MonoBehaviour
{
    [Header("Party")]
    [SerializeField] private int partyIndex;

    [Header("Empty Slot Object")]
    [SerializeField] private GameObject emptySlotObject;
    [SerializeField] private Image emptySlotImage;

    [Header("Selected Character Object")]
    [SerializeField] private Transform selectedCharacterRoot;
    [SerializeField] private bool selectedObjectAsLastSibling = true;
    [SerializeField] private bool stretchSelectedRectTransform = true;

    private GameObject currentSelectedObject;
    private string currentCharacterId;

    public int PartyIndex => partyIndex;
    public string CurrentCharacterId => currentCharacterId;
    public bool IsEmpty => string.IsNullOrWhiteSpace(currentCharacterId);

    private void Awake()
    {
        ApplyAutoPartyIndexFromName();
        AutoPrepareReferences();
        RefreshFromRuntime();
    }

    private void OnEnable()
    {
        RefreshFromRuntime();
    }

    private void OnDestroy()
    {
        DestroySelectedObject();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyAutoPartyIndexFromName();
    }
#endif

    public void Init(int index)
    {
        partyIndex = index;
        AutoPrepareReferences();
        RefreshFromRuntime();
    }

    public void RefreshFromRuntime()
    {
        currentCharacterId = null;

        if (DataManager.Instance != null)
            currentCharacterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        bool hasCharacter = !string.IsNullOrWhiteSpace(currentCharacterId);

        RefreshEmptySlotObject(hasCharacter);
        RefreshSelectedCharacterObject(hasCharacter);
    }

    private void RefreshEmptySlotObject(bool hasCharacter)
    {
        bool showEmpty = !hasCharacter;

        if (emptySlotObject != null && emptySlotObject != gameObject)
        {
            emptySlotObject.SetActive(showEmpty);
            return;
        }

        if (emptySlotImage != null)
            emptySlotImage.enabled = showEmpty;
    }

    private void RefreshSelectedCharacterObject(bool hasCharacter)
    {
        DestroySelectedObject();

        if (!hasCharacter)
            return;

        GameObject prefab = GetLobbySlotPrefab(currentCharacterId);

        if (prefab == null)
        {
            Debug.LogWarning("[PartySlot] Lobby slot prefab missing: " + currentCharacterId, this);
            return;
        }

        Transform parent = GetSelectedCharacterRoot();

        if (parent == null)
            parent = transform;

        currentSelectedObject = Instantiate(prefab, parent);
        currentSelectedObject.name = "LobbySlot_" + currentCharacterId;
        currentSelectedObject.SetActive(true);

        SetupSelectedObjectTransform(currentSelectedObject.transform);

        if (selectedObjectAsLastSibling)
            currentSelectedObject.transform.SetAsLastSibling();
    }

    private GameObject GetLobbySlotPrefab(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterPrefabDatabase == null)
            return null;

        if (DataManager.Instance.CharacterPrefabDatabase.TryGetLobbyPrefab(characterId, out var prefab))
            return prefab;

        return null;
    }

    private Transform GetSelectedCharacterRoot()
    {
        if (selectedCharacterRoot != null)
            return selectedCharacterRoot;

        Transform found = transform.Find("SelectedCharacterRoot");

        if (found != null)
        {
            selectedCharacterRoot = found;
            return selectedCharacterRoot;
        }

        GameObject rootObject = new GameObject("SelectedCharacterRoot", typeof(RectTransform));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.SetParent(transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        selectedCharacterRoot = rootRect;
        return selectedCharacterRoot;
    }

    private void SetupSelectedObjectTransform(Transform target)
    {
        if (target == null)
            return;

        RectTransform rect = target as RectTransform;

        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            if (stretchSelectedRectTransform)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = Vector2.zero;
            }

            return;
        }

        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    private void DestroySelectedObject()
    {
        if (currentSelectedObject == null)
            return;

        Destroy(currentSelectedObject);
        currentSelectedObject = null;
    }

    private void ApplyAutoPartyIndexFromName()
    {
        if (!TryGetPartyIndexFromObjectName(out int parsedIndex))
            return;

        partyIndex = parsedIndex;
    }

    private bool TryGetPartyIndexFromObjectName(out int result)
    {
        result = -1;

        string objectName = gameObject.name;

        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        int lastSeparatorIndex = objectName.LastIndexOf('_');

        if (lastSeparatorIndex < 0 || lastSeparatorIndex >= objectName.Length - 1)
            return false;

        string numberText = objectName.Substring(lastSeparatorIndex + 1);
        return int.TryParse(numberText, out result);
    }

    private void AutoPrepareReferences()
    {
        if (emptySlotObject == null)
        {
            Transform emptyChild = transform.Find("EmptySlotObject");

            if (emptyChild != null)
                emptySlotObject = emptyChild.gameObject;
        }

        if (emptySlotImage == null)
        {
            if (emptySlotObject != null)
                emptySlotImage = emptySlotObject.GetComponent<Image>();

            if (emptySlotImage == null)
                emptySlotImage = GetComponent<Image>();
        }

        if (selectedCharacterRoot == null)
        {
            Transform root = transform.Find("SelectedCharacterRoot");

            if (root != null)
                selectedCharacterRoot = root;
        }
    }
}