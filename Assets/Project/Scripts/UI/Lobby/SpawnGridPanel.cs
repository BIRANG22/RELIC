using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Relic.Gameplay.Data;

public class SpawnGridPanel : MonoBehaviour
{
    [SerializeField] private SpawnGridCell[] cells;

    [Header("Party Slot Order Icon Objects")]
    [FormerlySerializedAs("partySlotOrderIcons")]
    [SerializeField] private GameObject[] partySlotOrderIconObjects;

    [Header("Default Party Deploy Cells")]
    [SerializeField] private bool usePartySlotDefaultDeployCells = true;
    [SerializeField] private int firstPartyDefaultDeployCellNumber = 7;

    [Header("Warning")]
    [SerializeField] private string noPartyCharacterMessage = "캐릭터를 먼저 선택하세요.";
    [SerializeField] private string noSelectedDeployedCharacterMessage = "포지션을 변경할 캐릭터를 선택하세요.";

    private int selectedPartySlotIndex = -1;
    private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].Init(this, i);
            }
        }
    }

    private void Start()
    {
        AutoPlacePartyIfNeeded();
        Refresh();
    }

    private void OnEnable()
    {
        AutoPlacePartyIfNeeded();
        Refresh();
    }

    private void Update()
    {
        if (selectedPartySlotIndex < 0)
            return;

        if (!WasPointerPressedThisFrame())
            return;

        if (IsPointerOverThisDeployPanel())
            return;

        ClearSelection();
    }

    public void ClearSelection()
    {
        if (selectedPartySlotIndex < 0)
            return;

        selectedPartySlotIndex = -1;
        Refresh();
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    private bool IsPointerOverThisDeployPanel()
    {
        if (EventSystem.current == null)
            return false;

        Vector2 pointerPosition;

        if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        pointerRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, pointerRaycastResults);

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject hitObject = pointerRaycastResults[i].gameObject;

            if (hitObject == null)
                continue;

            Transform hitTransform = hitObject.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    public void AutoPlacePartyIfNeeded()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int partyIndex = 0; partyIndex < partyStore.MaxPartyCountValue; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            int currentGridIndex = partyStore.GetSpawnGridIndex(partyIndex);

            if (currentGridIndex >= 0)
                continue;

            int emptyGridIndex = FindDefaultEmptyGridForPartySlot(partyIndex);

            if (emptyGridIndex < 0)
            {
                Debug.LogWarning("[SpawnGridPanel] 빈 배치 그리드가 없습니다.");
                return;
            }

            partyStore.SetSpawnGridIndex(partyIndex, emptyGridIndex);
        }
    }

    public void OnClickCell(int gridIndex)
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        int clickedPartySlotIndex = FindPartySlotByGridIndex(gridIndex);

        if (clickedPartySlotIndex >= 0)
        {
            selectedPartySlotIndex = clickedPartySlotIndex;
            Refresh();
            return;
        }

        if (selectedPartySlotIndex < 0)
        {
            ShowNoSelectedCharacterWarning();
            return;
        }

        bool success = partyStore.SetSpawnGridIndex(selectedPartySlotIndex, gridIndex);

        if (!success)
            return;

        selectedPartySlotIndex = -1;
        Refresh();
    }


    private void ShowNoSelectedCharacterWarning()
    {
        PartyRuntimeStore partyStore = DataManager.Instance != null
            ? DataManager.Instance.PartyRuntimeStore
            : null;

        bool hasPartyCharacter = partyStore != null && partyStore.HasAnyCharacter;
        string defaultMessage = hasPartyCharacter
            ? "포지션을 변경할 캐릭터를 선택하세요."
            : "캐릭터를 먼저 선택하세요.";

        string configuredMessage = hasPartyCharacter
            ? noSelectedDeployedCharacterMessage
            : noPartyCharacterMessage;

        string message = string.IsNullOrWhiteSpace(configuredMessage)
            ? defaultMessage
            : configuredMessage;

        if (SettingWarningUI.ShowMessage(message))
            return;

        Debug.LogWarning($"[SpawnGridPanel] {message}");
    }

    public bool TryGetPartySlotOrderIconObject(int partySlotIndex, out GameObject iconObject)
    {
        iconObject = null;

        if (partySlotOrderIconObjects == null)
            return false;

        if (partySlotIndex < 0 || partySlotIndex >= partySlotOrderIconObjects.Length)
            return false;

        iconObject = partySlotOrderIconObjects[partySlotIndex];
        return iconObject != null;
    }

    private int FindDefaultEmptyGridForPartySlot(int partySlotIndex)
    {
        if (usePartySlotDefaultDeployCells && DataManager.Instance != null)
        {
            int preferredGridIndex = GetDefaultDeployGridIndex(partySlotIndex);

            if (IsValidGridIndex(preferredGridIndex) && !DataManager.Instance.PartyRuntimeStore.IsGridUsed(preferredGridIndex))
                return preferredGridIndex;
        }

        return FindFirstEmptyGrid();
    }

    private int GetDefaultDeployGridIndex(int partySlotIndex)
    {
        return Mathf.Max(1, firstPartyDefaultDeployCellNumber) - 1 + partySlotIndex;
    }

    private bool IsValidGridIndex(int gridIndex)
    {
        return cells != null && gridIndex >= 0 && gridIndex < cells.Length;
    }

    private int FindFirstEmptyGrid()
    {
        if (cells == null)
            return -1;

        if (DataManager.Instance == null)
            return -1;

        for (int i = 0; i < cells.Length; i++)
        {
            if (DataManager.Instance.PartyRuntimeStore.IsGridUsed(i))
                continue;

            return i;
        }

        return -1;
    }

    private int FindPartySlotByGridIndex(int gridIndex)
    {
        if (DataManager.Instance == null)
            return -1;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return i;
        }

        return -1;
    }

    public bool IsSelectedGrid(int gridIndex)
    {
        if (selectedPartySlotIndex < 0)
            return false;

        if (DataManager.Instance == null)
            return false;

        return DataManager.Instance.PartyRuntimeStore.GetSpawnGridIndex(selectedPartySlotIndex) == gridIndex;
    }

    public void Refresh()
    {
        if (cells == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Refresh();
        }
    }
}
