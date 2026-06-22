using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class CharPick : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private List<CharBtn> charBtns = new();
    [SerializeField] private bool autoBindCharButtons = true;
    [SerializeField] private Transform charButtonRoot;

    [Header("Preview")]
    [SerializeField] private Transform previewRoot;

    [Header("Setting Panel")]
    [SerializeField] private Setting setting;

    [Header("Preview Background Animation")]
    [SerializeField] private Transform previewBackground;
    [SerializeField] private float bgShrinkDuration = 0.12f;
    [SerializeField] private float bgExpandDuration = 0.12f;

    [Header("Position")]
    [SerializeField] private float spacing = 260f;

    [Header("Scale")]
    [SerializeField] private float centerScale = 1.2f;
    [SerializeField] private float sideScale = 0.9f;
    [SerializeField] private float sideHoverScale = 1.1f;

    [Header("Drag")]
    [SerializeField] private float dragThreshold = 180f;

    [Header("Smooth")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float scaleSpeed = 12f;

    [Header("Party Confirm")]
    [SerializeField] private int firstPartyDefaultDeployCellNumber = 7;
    [SerializeField] private int maxDeployGridCount = 15;
    [SerializeField] private bool resetPendingSelectionOnEnable = true;
    [SerializeField] private bool resetPendingSelectionOnDisable = true;

    [Header("World Preview Transform")]
    [SerializeField] private Vector3 previewLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 previewLocalEulerAngles = Vector3.zero;
    [SerializeField] private float previewScale = 1f;

    private readonly List<string> pendingCharacterIds = new();
    private readonly List<string> runtimeCharacterIdsSnapshot = new();

    private int centerIndex = 0;

    private bool isDragging;
    private float dragStartX;
    private bool movedByDrag;
    private CharBtn hoveredButton;

    private GameObject currentPreview;
    private string currentPreviewCharacterId;

    private Coroutine bgAnimRoutine;
    private Vector3 previewBackgroundOriginalScale;
    private bool hasPreviewBackgroundOriginalScale;
    private bool isStarted;

    public CharBtn CurrentButton
    {
        get
        {
            if (centerIndex < 0 || centerIndex >= charBtns.Count)
                return null;

            return charBtns[centerIndex];
        }
    }

    private void OnEnable()
    {
        AutoBindCharButtonsIfNeeded();
        ClampCenterIndex();

        if (resetPendingSelectionOnEnable)
            ResetPendingSelectionFromRuntime();

        if (isStarted)
        {
            RefreshInstant();
            RefreshCenterInfo();
        }
    }

    private void Start()
    {
        isStarted = true;
        CachePreviewBackgroundScale();
        AutoBindCharButtonsIfNeeded();
        ClampCenterIndex();

        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] != null)
                charBtns[i].Init(this);
        }

        if (pendingCharacterIds.Count <= 0)
            ResetPendingSelectionFromRuntime();
        else
            RefreshAllSelectedPartyMarkers();

        RefreshInstant();

        if (charBtns.Count > 0 && charBtns[centerIndex] != null)
        {
            CreateOrUpdateRuntimeData(charBtns[centerIndex]);
            RefreshCenterInfo();
        }
    }

    private void OnDisable()
    {
        if (resetPendingSelectionOnDisable)
            ResetPendingSelectionFromRuntime();
    }

    private void Update()
    {
        RefreshSmooth();
    }

    public void PointerEnterButton(CharBtn btn)
    {
        if (btn == null)
            return;

        hoveredButton = btn;
    }

    public void PointerExitButton(CharBtn btn)
    {
        if (btn == null)
            return;

        if (hoveredButton == btn)
            hoveredButton = null;
    }

    public void ClickBtn(CharBtn btn)
    {
        if (movedByDrag)
        {
            movedByDrag = false;
            return;
        }

        int index = charBtns.IndexOf(btn);

        if (index < 0)
            return;

        if (index != centerIndex)
        {
            centerIndex = index;
            RefreshCenterInfo();
            return;
        }

        if (btn.IsLocked || !HasUsableCharacterData(btn))
        {
            RefreshCenterInfo();
            return;
        }

        ToggleButtonPartyMarker(btn);
    }

    public void ToggleButtonPartyMarker(CharBtn btn)
    {
        if (btn == null)
            return;

        if (btn.IsLocked || !HasUsableCharacterData(btn))
            return;

        if (!btn.PrepareCharacterForPartyAction(true))
            return;

        string characterId = btn.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        int registeredSlot = pendingCharacterIds.IndexOf(characterId);

        if (registeredSlot >= 0)
            pendingCharacterIds.RemoveAt(registeredSlot);
        else
        {
            int maxPartyCount = GetMaxPartyCount();

            if (pendingCharacterIds.Count >= maxPartyCount)
            {
                Debug.LogWarning("[Party] 빈 파티 슬롯이 없습니다.");
                return;
            }

            pendingCharacterIds.Add(characterId);
        }

        RefreshAllSelectedPartyMarkers();
    }

    public int FindPendingPartySlot(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return -1;

        return pendingCharacterIds.IndexOf(characterId);
    }

    public void ConfirmCurrentCharacter()
    {
        CharBtn currentButton = CurrentButton;

        if (currentButton == null)
        {
            Debug.LogWarning("[CharPick] 선택된 캐릭터가 없습니다.");
            return;
        }

        if (!currentButton.PrepareCharacterForPartyAction(true))
            return;

        if (HasPendingSelectionChanged())
        {
            ApplyPendingSelectionToRuntime();
        }
        else
        {
            string characterId = currentButton.CharacterId;
            int pendingSlot = FindPendingPartySlot(characterId);

            if (pendingSlot >= 0)
                ApplyPendingSelectionToRuntime();
            else
                SaveCharacterToEnteredPartySlot(currentButton);
        }

        RefreshPartyViews();
        ResetPendingSelectionFromRuntime();
    }

    private int GetMaxPartyCount()
    {
        if (DataManager.Instance == null)
            return 3;

        return DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue;
    }

    private void ResetPendingSelectionFromRuntime()
    {
        pendingCharacterIds.Clear();
        runtimeCharacterIdsSnapshot.Clear();

        if (DataManager.Instance != null)
        {
            PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
            int maxPartyCount = partyStore.MaxPartyCountValue;

            for (int i = 0; i < maxPartyCount; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                if (pendingCharacterIds.Contains(characterId))
                    continue;

                pendingCharacterIds.Add(characterId);
            }
        }

        for (int i = 0; i < pendingCharacterIds.Count; i++)
            runtimeCharacterIdsSnapshot.Add(pendingCharacterIds[i]);

        RefreshAllSelectedPartyMarkers();
    }

    private bool HasPendingSelectionChanged()
    {
        if (pendingCharacterIds.Count != runtimeCharacterIdsSnapshot.Count)
            return true;

        for (int i = 0; i < pendingCharacterIds.Count; i++)
        {
            if (pendingCharacterIds[i] != runtimeCharacterIdsSnapshot[i])
                return true;
        }

        return false;
    }

    private void ApplyPendingSelectionToRuntime()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharPick] DataManager instance is missing.");
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        int maxPartyCount = partyStore.MaxPartyCountValue;
        int count = Mathf.Min(pendingCharacterIds.Count, maxPartyCount);

        for (int i = 0; i < maxPartyCount; i++)
            partyStore.ClearSlot(i);

        for (int i = 0; i < count; i++)
        {
            string characterId = pendingCharacterIds[i];

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            partyStore.SetCharacter(i, characterId);

            int defaultGridIndex = GetDefaultDeployGridIndex(i);

            if (IsValidDeployGridIndex(defaultGridIndex))
                partyStore.SetSpawnGridIndex(i, defaultGridIndex);
            else
                Debug.LogWarning("[Party] 빈 배치 그리드가 없습니다.");
        }
    }

    private void SaveCharacterToEnteredPartySlot(CharBtn btn)
    {
        if (btn == null)
            return;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharPick] DataManager instance is missing.");
            return;
        }

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharPick] CharacterSelectionState instance is missing.");
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        string characterId = btn.CharacterId;
        int enteredSlot = CharacterSelectionState.Instance.CurrentPartySlotIndex;

        if (enteredSlot < 0 || enteredSlot >= partyStore.MaxPartyCountValue)
        {
            Debug.LogWarning("[Party] 선택된 파티 슬롯이 없습니다.");
            return;
        }

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (i == enteredSlot)
                continue;

            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.ClearSlot(i);
        }

        partyStore.SetCharacter(enteredSlot, characterId);

        int defaultGridIndex = FindDefaultDeployGridIndexForSlot(enteredSlot);

        if (defaultGridIndex >= 0)
            partyStore.SetSpawnGridIndex(enteredSlot, defaultGridIndex);
        else
            Debug.LogWarning("[Party] 빈 배치 그리드가 없습니다.");
    }

    private int FindDefaultDeployGridIndexForSlot(int partySlotIndex)
    {
        int preferredGridIndex = GetDefaultDeployGridIndex(partySlotIndex);

        if (IsAvailableDeployGridForSlot(preferredGridIndex, partySlotIndex))
            return preferredGridIndex;

        for (int i = 0; i < maxDeployGridCount; i++)
        {
            if (IsAvailableDeployGridForSlot(i, partySlotIndex))
                return i;
        }

        return -1;
    }

    private int GetDefaultDeployGridIndex(int partySlotIndex)
    {
        return Mathf.Max(1, firstPartyDefaultDeployCellNumber) - 1 + partySlotIndex;
    }

    private bool IsValidDeployGridIndex(int gridIndex)
    {
        return gridIndex >= 0 && gridIndex < maxDeployGridCount;
    }

    private bool IsAvailableDeployGridForSlot(int gridIndex, int partySlotIndex)
    {
        if (!IsValidDeployGridIndex(gridIndex))
            return false;

        if (DataManager.Instance == null)
            return false;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (i == partySlotIndex)
                continue;

            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return false;
        }

        return true;
    }

    private void RefreshPartyViews()
    {
        PartySlot[] partySlots = FindObjectsByType<PartySlot>(FindObjectsSortMode.None);

        for (int i = 0; i < partySlots.Length; i++)
        {
            if (partySlots[i] != null)
                partySlots[i].RefreshFromRuntime();
        }

        SpawnGridPanel[] spawnGridPanels = FindObjectsByType<SpawnGridPanel>(FindObjectsSortMode.None);

        for (int i = 0; i < spawnGridPanels.Length; i++)
        {
            if (spawnGridPanels[i] == null)
                continue;

            spawnGridPanels[i].AutoPlacePartyIfNeeded();
            spawnGridPanels[i].Refresh();
        }
    }

    private void RefreshAllSelectedPartyMarkers()
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] != null)
                charBtns[i].RefreshSelectedPartyMarker();
        }
    }

    private void CreateOrUpdateRuntimeData(CharBtn btn)
    {
        if (btn == null)
            return;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharPick] DataManager instance is missing.");
            return;
        }

        string characterId = btn.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var master))
        {
            Debug.LogWarning("[CharPick] Character master not found: " + characterId);
            return;
        }

        var runtimeStore = DataManager.Instance.CharacterRuntimeStore;

        if (runtimeStore.TryGet(characterId, out var runtime))
            return;

        runtime = new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,

            CurrentHP = master.MaxHP,
            CurrentCost = master.MaxCost,
            CurrentResource = 0,
            CurrentMoveLevel = 1,

            IsUnlocked = master.IsDefaultProvided,

            MoveSkillId = "S_Move_1",
            PassiveSkillId = master.PassiveSkill1,
            UniqueSkillId = master.UniqueSkill1,
            AbilitySkillId = master.CharacterSkill1,

            EquippedSkillIds = new string[4]
            {
                master.UniqueSkill1,
                master.CharacterSkill1,
                master.CommonSkill1,
                ""
            }
        };

        runtimeStore.AddOrUpdate(runtime);
    }

    public void BeginDrag(PointerEventData eventData)
    {
        hoveredButton = null;
        isDragging = true;
        movedByDrag = false;
        dragStartX = eventData.position.x;
    }

    public void Drag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float dragAmount = eventData.position.x - dragStartX;

        if (Mathf.Abs(dragAmount) < dragThreshold)
            return;

        if (dragAmount < 0)
            Next();
        else
            Prev();

        movedByDrag = true;
        dragStartX = eventData.position.x;

        RefreshCenterInfo();
    }

    public void EndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void Next()
    {
        if (charBtns.Count <= 0)
            return;

        centerIndex++;

        if (centerIndex >= charBtns.Count)
            centerIndex = 0;
    }

    private void Prev()
    {
        if (charBtns.Count <= 0)
            return;

        centerIndex--;

        if (centerIndex < 0)
            centerIndex = charBtns.Count - 1;
    }


    private void AutoBindCharButtonsIfNeeded()
    {
        if (!autoBindCharButtons)
            return;

        Transform root = ResolveCharButtonRoot();

        if (root == null)
            return;

        List<CharBtn> foundButtons = CollectCharButtons(root);

        if (foundButtons.Count <= 0)
            return;

        bool shouldReplace = charBtns == null || charBtns.Count != foundButtons.Count;

        if (!shouldReplace)
        {
            for (int i = 0; i < foundButtons.Count; i++)
            {
                if (charBtns[i] != foundButtons[i])
                {
                    shouldReplace = true;
                    break;
                }
            }
        }

        if (!shouldReplace)
            return;

        charBtns = foundButtons;

        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] != null)
                charBtns[i].Init(this);
        }

        ClampCenterIndex();
    }

    private List<CharBtn> CollectCharButtons(Transform root)
    {
        List<CharBtn> result = new();

        if (root == null)
            return result;

        List<Transform> candidates = new();
        CollectCharButtonTransforms(root, candidates);

        candidates.Sort(CompareCharButtonTransforms);

        for (int i = 0; i < candidates.Count; i++)
        {
            Transform candidate = candidates[i];

            if (candidate == null)
                continue;

            CharBtn btn = candidate.GetComponent<CharBtn>();

            if (btn == null && IsCharButtonName(candidate.name))
                btn = candidate.gameObject.AddComponent<CharBtn>();

            if (btn == null)
                continue;

            if (result.Contains(btn))
                continue;

            result.Add(btn);
        }

        return result;
    }

    private void CollectCharButtonTransforms(Transform root, List<Transform> result)
    {
        if (root == null || result == null)
            return;

        CharBtn rootButton = root.GetComponent<CharBtn>();

        if (rootButton != null || IsCharButtonName(root.name))
            result.Add(root);

        for (int i = 0; i < root.childCount; i++)
            CollectCharButtonTransforms(root.GetChild(i), result);
    }

    private static bool IsCharButtonName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        return objectName.StartsWith("CharBtn_", System.StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareCharButtonTransforms(Transform a, Transform b)
    {
        int aIndex = ExtractTrailingNumber(a != null ? a.name : string.Empty);
        int bIndex = ExtractTrailingNumber(b != null ? b.name : string.Empty);

        if (aIndex != bIndex)
            return aIndex.CompareTo(bIndex);

        int aSibling = a != null ? a.GetSiblingIndex() : 0;
        int bSibling = b != null ? b.GetSiblingIndex() : 0;

        return aSibling.CompareTo(bSibling);
    }

    private static int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return int.MaxValue;

        int end = value.Length - 1;

        while (end >= 0 && char.IsDigit(value[end]))
            end--;

        if (end >= value.Length - 1)
            return int.MaxValue;

        string numberText = value.Substring(end + 1);

        if (int.TryParse(numberText, out int number))
            return number;

        return int.MaxValue;
    }

    private Transform ResolveCharButtonRoot()
    {
        if (charButtonRoot != null)
            return charButtonRoot;

        if (charBtns != null)
        {
            for (int i = 0; i < charBtns.Count; i++)
            {
                if (charBtns[i] != null && charBtns[i].transform.parent != null)
                    return charBtns[i].transform.parent;
            }
        }

        return transform;
    }

    private void ClampCenterIndex()
    {
        if (charBtns == null || charBtns.Count <= 0)
        {
            centerIndex = 0;
            return;
        }

        centerIndex = Mathf.Clamp(centerIndex, 0, charBtns.Count - 1);
    }

    private void RefreshCenterInfo()
    {
        if (charBtns.Count <= 0)
            return;

        ClampCenterIndex();

        if (centerIndex < 0 || centerIndex >= charBtns.Count)
            return;

        CharBtn centerBtn = charBtns[centerIndex];

        if (centerBtn == null)
        {
            ClearCenterCharacterInfo();
            return;
        }

        string characterId = centerBtn.CharacterId;

        if (!CanShowCharacterData(centerBtn, characterId))
        {
            ClearCenterCharacterInfo();
            return;
        }

        CreateOrUpdateRuntimeData(centerBtn);
        SelectCenterCharacterState(centerBtn, characterId);
        ShowPreview(characterId);

        if (setting != null)
            setting.OpenCharacterSetting(characterId);
    }

    private bool CanShowCharacterData(CharBtn btn, string characterId)
    {
        if (btn == null)
            return false;

        if (btn.IsLocked)
            return false;

        return HasUsableCharacterData(btn);
    }

    private bool HasUsableCharacterData(CharBtn btn)
    {
        if (btn == null)
            return false;

        string characterId = btn.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        if (DataManager.Instance == null)
            return false;

        if (DataManager.Instance.CharacterDatabase == null)
            return false;

        return DataManager.Instance.CharacterDatabase.TryGet(characterId, out _);
    }

    private void ClearCenterCharacterInfo()
    {
        ShowPreview(null);

        if (CharacterSelectionState.Instance != null)
            CharacterSelectionState.Instance.SelectCharacter(CharacterType.None, null);

        if (setting != null)
            setting.Clear();
    }

    private void SelectCenterCharacterState(CharBtn btn, string characterId)
    {
        if (btn == null)
            return;

        if (CharacterSelectionState.Instance == null)
            return;

        CharacterSelectionState.Instance.SelectCharacter(btn.CharacterType, characterId);
    }

    private void ShowPreview(string characterId)
    {
        if (previewRoot == null)
            return;

        if (characterId == currentPreviewCharacterId && currentPreview != null)
            return;

        currentPreviewCharacterId = characterId;

        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharPick] DataManager instance is missing.");
            return;
        }

        if (DataManager.Instance.CharacterPrefabDatabase == null)
        {
            Debug.LogWarning("[CharPick] CharacterPrefabDatabase is missing.");
            return;
        }

        if (!DataManager.Instance.CharacterPrefabDatabase.TryGetPreviewWorldPrefab(characterId, out var prefab))
        {
            Debug.LogWarning("[CharPick] PreviewWorldPrefab not found: " + characterId);
            return;
        }

        if (prefab == null)
            return;

        currentPreview = Instantiate(prefab, previewRoot);
        currentPreview.transform.localPosition = previewLocalPosition;
        currentPreview.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);
        currentPreview.transform.localScale = Vector3.one * previewScale;

        PlayPreviewBackgroundAnim();
    }

    private void CachePreviewBackgroundScale()
    {
        if (previewBackground == null)
            return;

        previewBackgroundOriginalScale = previewBackground.localScale;
        hasPreviewBackgroundOriginalScale = true;
    }

    private void PlayPreviewBackgroundAnim()
    {
        if (previewBackground == null)
            return;

        if (!hasPreviewBackgroundOriginalScale)
            CachePreviewBackgroundScale();

        if (bgAnimRoutine != null)
            StopCoroutine(bgAnimRoutine);

        previewBackground.localScale = previewBackgroundOriginalScale;
        bgAnimRoutine = StartCoroutine(PreviewBackgroundAnimRoutine());
    }

    private IEnumerator PreviewBackgroundAnimRoutine()
    {
        Vector3 originalScale = previewBackgroundOriginalScale;

        float startX = originalScale.x;
        float y = originalScale.y;
        float z = originalScale.z;

        float timer = 0f;

        while (timer < bgShrinkDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / bgShrinkDuration);
            float x = Mathf.Lerp(startX, 0f, t);

            previewBackground.localScale = new Vector3(x, y, z);

            yield return null;
        }

        previewBackground.localScale = new Vector3(0f, y, z);

        timer = 0f;

        while (timer < bgExpandDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / bgExpandDuration);
            float x = Mathf.Lerp(0f, startX, t);

            previewBackground.localScale = new Vector3(x, y, z);

            yield return null;
        }

        previewBackground.localScale = originalScale;
        bgAnimRoutine = null;
    }

    private void RefreshInstant()
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] == null)
                continue;

            int offset = GetOffset(i);

            if (offset == -1)
                ApplyInstant(charBtns[i], new Vector2(-spacing, 0f), GetSideButtonScale(charBtns[i]), true, false);
            else if (offset == 0)
                ApplyInstant(charBtns[i], Vector2.zero, centerScale, true, true);
            else if (offset == 1)
                ApplyInstant(charBtns[i], new Vector2(spacing, 0f), GetSideButtonScale(charBtns[i]), true, false);
            else
            {
                charBtns[i].SetVisible(false);
                charBtns[i].SetCenter(false);
            }
        }
    }

    private void RefreshSmooth()
    {
        if (!isStarted)
            return;

        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] == null)
                continue;

            int offset = GetOffset(i);

            if (offset == -1)
                ApplySmooth(charBtns[i], new Vector2(-spacing, 0f), GetSideButtonScale(charBtns[i]), true, false);
            else if (offset == 0)
                ApplySmooth(charBtns[i], Vector2.zero, centerScale, true, true);
            else if (offset == 1)
                ApplySmooth(charBtns[i], new Vector2(spacing, 0f), GetSideButtonScale(charBtns[i]), true, false);
            else
            {
                charBtns[i].SetVisible(false);
                charBtns[i].SetCenter(false);
            }
        }
    }

    private float GetSideButtonScale(CharBtn btn)
    {
        if (!isDragging && hoveredButton == btn)
            return sideHoverScale;

        return sideScale;
    }

    private void ApplyInstant(CharBtn btn, Vector2 pos, float scale, bool visible, bool center)
    {
        btn.SetVisible(visible);
        btn.SetCenter(center);
        btn.Rect.anchoredPosition = pos;
        btn.Rect.localScale = Vector3.one * scale;
    }

    private void ApplySmooth(CharBtn btn, Vector2 pos, float scale, bool visible, bool center)
    {
        btn.SetVisible(visible);
        btn.SetCenter(center);

        if (!visible)
            return;

        btn.Rect.anchoredPosition = Vector2.Lerp(
            btn.Rect.anchoredPosition,
            pos,
            Time.deltaTime * moveSpeed
        );

        btn.Rect.localScale = Vector3.Lerp(
            btn.Rect.localScale,
            Vector3.one * scale,
            Time.deltaTime * scaleSpeed
        );
    }

    private int GetOffset(int index)
    {
        int count = charBtns.Count;

        if (count <= 0)
            return 999;

        if (index == centerIndex)
            return 0;

        int leftIndex = centerIndex - 1;
        if (leftIndex < 0)
            leftIndex = count - 1;

        int rightIndex = centerIndex + 1;
        if (rightIndex >= count)
            rightIndex = 0;

        if (index == leftIndex)
            return -1;

        if (index == rightIndex)
            return 1;

        return 999;
    }
}
