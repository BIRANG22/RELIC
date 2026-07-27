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

    [Header("Party Confirm")]
    [SerializeField] private int firstPartyDefaultDeployCellNumber = 7;
    [SerializeField] private int maxDeployGridCount = 15;
    [SerializeField] private bool resetPendingSelectionOnEnable = true;
    [SerializeField] private bool resetPendingSelectionOnDisable = true;

    private readonly List<string> pendingCharacterIds = new();
    private readonly List<string> runtimeCharacterIdsSnapshot = new();

    private int centerIndex = 0;

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
            RefreshFixedButtons();
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

        RefreshFixedButtons();

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
        // 캐릭터 버튼은 배치된 위치를 그대로 사용한다.
        // 이전의 좌우 이동, 중앙 정렬, 중앙 확대 갱신은 더 이상 실행하지 않는다.
    }

    public void PointerEnterButton(CharBtn btn)
    {
        // 고정형 버튼 배치에서는 CharPick이 호버 위치나 크기를 제어하지 않는다.
    }

    public void PointerExitButton(CharBtn btn)
    {
        // 고정형 버튼 배치에서는 CharPick이 호버 위치나 크기를 제어하지 않는다.
    }

    public void ClickBtn(CharBtn btn)
    {
        ClickBtn(btn, true);
    }

    public void ClickBtn(CharBtn btn, bool playPartyActionSound)
    {
        int index = charBtns.IndexOf(btn);

        if (index < 0)
            return;

        // 클릭하기 전부터 정보를 보고 있던 캐릭터인지 먼저 기록한다.
        // 다른 캐릭터 버튼을 눌러 정보를 전환한 경우에는 이미 편성된 캐릭터라도 해제하지 않는다.
        bool wasCurrentInfoCharacter = centerIndex == index;

        // 버튼을 누르면 해당 캐릭터의 정보와 프리뷰를 즉시 갱신한다.
        centerIndex = index;
        RefreshCenterInfo();

        if (btn.IsLocked || !HasUsableCharacterData(btn))
            return;

        string characterId = btn.CharacterId;
        bool isAlreadyInParty = FindPendingPartySlot(characterId) >= 0;

        // 아직 편성되지 않은 캐릭터는 한 번 클릭하면 바로 편성한다.
        // 이미 편성된 다른 캐릭터를 클릭한 경우에는 정보만 보여주고 편성을 유지한다.
        // 현재 정보를 보고 있는 편성 캐릭터를 다시 클릭한 경우에만 편성을 해제한다.
        if (isAlreadyInParty && !wasCurrentInfoCharacter)
            return;

        ToggleButtonPartyMarker(btn, playPartyActionSound);
    }

    public void ToggleButtonPartyMarker(CharBtn btn)
    {
        ToggleButtonPartyMarker(btn, true);
    }

    public void ToggleButtonPartyMarker(CharBtn btn, bool withClickSound)
    {
        if (btn == null)
            return;

        if (btn.IsLocked || !HasUsableCharacterData(btn))
            return;

        if (!btn.PrepareCharacterForPartyAction(withClickSound))
            return;

        string characterId = btn.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        SteamLobbyPartySynchronizer synchronizer = SteamLobbyPartySynchronizer.Instance;

        if (synchronizer != null && synchronizer.IsNetworkPartyActive)
        {
            synchronizer.RequestAutomaticCharacterToggle(characterId);
            return;
        }

        EnsurePendingSlotCount();

        int registeredSlot = pendingCharacterIds.IndexOf(characterId);

        if (registeredSlot >= 0)
        {
            // 선택 해제 시 해당 슬롯만 비운다.
            // 뒤 슬롯의 캐릭터를 앞으로 당기지 않는다.
            pendingCharacterIds[registeredSlot] = string.Empty;
        }
        else
        {
            int emptySlot = FindFirstEmptyPendingSlot();

            if (emptySlot < 0)
            {
                Debug.LogWarning("[Party] 빈 파티 슬롯이 없습니다.");
                return;
            }

            pendingCharacterIds[emptySlot] = characterId;
        }

        RefreshAllSelectedPartyMarkers();

        // 캐릭터 버튼을 누르는 즉시 실제 파티 데이터에 반영한다.
        // LobbyMainPanel이 현재 비활성화되어 있어도 PartySlot이 다시 활성화될 때
        // PartyRuntimeStore의 최신 편성 정보를 읽어 정상적으로 표시할 수 있다.
        ApplyPendingSelectionToRuntime();
        RefreshPartyViews();
        SyncRuntimeSnapshotFromPendingSelection();
    }

    public int FindPendingPartySlot(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return -1;

        return pendingCharacterIds.IndexOf(characterId);
    }

    /// <summary>
    /// 기존 SelectButton 연결을 위한 호환 메서드입니다.
    /// 현재는 캐릭터 버튼을 누르는 즉시 파티 편성이 저장되므로
    /// SelectButton을 사용하거나 씬에 배치할 필요가 없습니다.
    /// </summary>
    public void ConfirmCurrentCharacter()
    {
        ConfirmCurrentCharacter(true);
    }

    /// <summary>
    /// 기존 UnityEvent 연결이 남아 있어도 오류가 발생하지 않도록 유지합니다.
    /// 파티 데이터는 이미 즉시 반영되어 있으므로 추가 편성이나 슬롯 이동은 하지 않습니다.
    /// </summary>
    public void ConfirmCurrentCharacter(bool withClickSound)
    {
        RefreshPartyViews();
        RefreshAllSelectedPartyMarkers();
    }

    public void RefreshFromPartyRuntime()
    {
        ResetPendingSelectionFromRuntime();
        RefreshPartyViews();
        RefreshAllSelectedPartyMarkers();
    }

    public void ShowCurrentPreviewNormal()
    {
        if (TryGetCurrentPreviewAnimator(out ButtonResponsiveSpriteAnimator animator))
            animator.ShowNormal();
    }

    public void ShowCurrentPreviewSkill()
    {
        if (TryGetCurrentPreviewAnimator(out ButtonResponsiveSpriteAnimator animator))
            animator.ShowSkill();
    }

    public void ShowCurrentPreviewRune()
    {
        if (TryGetCurrentPreviewAnimator(out ButtonResponsiveSpriteAnimator animator))
            animator.ShowRune();
    }

    private int GetMaxPartyCount()
    {
        if (DataManager.Instance == null)
            return 3;

        return DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue;
    }


    private void EnsurePendingSlotCount()
    {
        int maxPartyCount = GetMaxPartyCount();

        while (pendingCharacterIds.Count < maxPartyCount)
            pendingCharacterIds.Add(string.Empty);

        if (pendingCharacterIds.Count > maxPartyCount)
            pendingCharacterIds.RemoveRange(maxPartyCount, pendingCharacterIds.Count - maxPartyCount);
    }

    private int FindFirstEmptyPendingSlot()
    {
        EnsurePendingSlotCount();

        for (int i = 0; i < pendingCharacterIds.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(pendingCharacterIds[i]))
                return i;
        }

        return -1;
    }

    private void ResetPendingSelectionFromRuntime()
    {
        pendingCharacterIds.Clear();
        runtimeCharacterIdsSnapshot.Clear();

        int maxPartyCount = GetMaxPartyCount();

        for (int i = 0; i < maxPartyCount; i++)
            pendingCharacterIds.Add(string.Empty);

        if (DataManager.Instance != null)
        {
            PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

            for (int i = 0; i < maxPartyCount; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                // 런타임 파티의 슬롯 번호를 그대로 유지한다.
                pendingCharacterIds[i] = characterId;
            }
        }

        SyncRuntimeSnapshotFromPendingSelection();
        RefreshAllSelectedPartyMarkers();
    }

    private void SyncRuntimeSnapshotFromPendingSelection()
    {
        runtimeCharacterIdsSnapshot.Clear();

        for (int i = 0; i < pendingCharacterIds.Count; i++)
            runtimeCharacterIdsSnapshot.Add(pendingCharacterIds[i]);
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

        EnsurePendingSlotCount();

        for (int i = 0; i < maxPartyCount; i++)
        {
            string characterId = i < pendingCharacterIds.Count
                ? pendingCharacterIds[i]
                : string.Empty;

            if (string.IsNullOrWhiteSpace(characterId))
            {
                partyStore.ClearSlot(i);
                continue;
            }

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
        {
            CharacterStartingRelicUtility.EnsureStartingRelicEquippedIfEmpty(
                runtime,
                master,
                DataManager.Instance.RelicDatabase);
            return;
        }

        runtime = new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,

            CurrentHP = master.MaxHP,
            CurrentCost = master.MaxCost,
            CurrentResource = 0,
            CurrentMoveLevel = 0,

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
            },

            EquippedRuneIds = new string[12],
            EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master)
        };

        CharacterStartingRelicUtility.InitializeActiveRelicUses(
            runtime,
            DataManager.Instance.RelicDatabase);

        runtimeStore.AddOrUpdate(runtime);
    }

    public void BeginDrag(PointerEventData eventData)
    {
        // 고정형 캐릭터 버튼 목록에서는 드래그로 캐릭터를 넘기지 않는다.
    }

    public void Drag(PointerEventData eventData)
    {
        // 고정형 캐릭터 버튼 목록에서는 드래그로 캐릭터를 넘기지 않는다.
    }

    public void EndDrag(PointerEventData eventData)
    {
        // 고정형 캐릭터 버튼 목록에서는 드래그로 캐릭터를 넘기지 않는다.
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
        RefreshViewedCharacterButtons();

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

    private void RefreshViewedCharacterButtons(bool immediate = false)
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            CharBtn button = charBtns[i];

            if (button == null)
                continue;

            button.SetViewedCharacter(i == centerIndex, immediate);
        }
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

        if (!DataManager.Instance.CharacterPrefabDatabase.TryGetPreviewUIPrefab(characterId, out var prefab))
        {
            Debug.LogWarning("[CharPick] PreviewUIPrefab not found: " + characterId);
            return;
        }

        if (prefab == null)
            return;

        currentPreview = Instantiate(prefab, previewRoot, false);
        currentPreview.name = "Preview_" + characterId;

        PlayPreviewBackgroundAnim();
    }

    private bool TryGetCurrentPreviewAnimator(out ButtonResponsiveSpriteAnimator animator)
    {
        animator = null;

        if (currentPreview == null)
            return false;

        animator = currentPreview.GetComponentInChildren<ButtonResponsiveSpriteAnimator>(true);
        return animator != null;
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

    private void RefreshFixedButtons()
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            CharBtn btn = charBtns[i];

            if (btn == null)
                continue;

            // 씬에서 설정한 위치와 크기를 변경하지 않고 모든 버튼을 표시한다.
            btn.SetVisible(true);
            btn.SetCenter(false);
        }
    }


}
