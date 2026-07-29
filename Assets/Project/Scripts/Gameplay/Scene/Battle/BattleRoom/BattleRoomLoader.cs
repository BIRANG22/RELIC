using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleRoomLoader : MonoBehaviour
{
    [Header("Spawner")]
    [SerializeField] private BattleUnitSpawner unitSpawner;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;

    [Header("HUD")]
    [SerializeField] private Transform playerHudRoot;
    [SerializeField] private Transform monsterHudRoot;
    [SerializeField] private PlayerHUDSlot playerHudPrefab;
    [SerializeField] private MonsterHUDSlot monsterHudPrefab;
    [SerializeField] private float hudScale = 0.4f;

    [Header("Player HUD Position Anchors")]
    [SerializeField] private Transform[] playerHudPositionAnchors = new Transform[3];
    [SerializeField] private bool selectFirstPlayerHudByDefault = true;
    [SerializeField] private bool forcePlayerHudRootOnTop = true;
    [SerializeField] private int playerHudRootSortingOrder = 100;
    [SerializeField] private int selectedHudSortingOrder = 200;

    [Header("Monster World HUD")]
    [SerializeField] private RectTransform battleCanvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Skill List")]
    [SerializeField] private SkillListPanel skillListPanel;
    [SerializeField] private bool openSelectedCharacterSkillListWhenInputReady = true;

    [Header("Keyboard Input")]
    [SerializeField] private bool enableCharacterNumberSelectInput = true;
    [SerializeField] private bool enableSkillPanelToggleInput = true;

    [Header("Monster Turn")]
    [SerializeField] private BattleMonsterTurnPlanner monsterTurnPlanner;

    [Header("Load Control")]
    [Tooltip("BattleSceneController가 없는 테스트 씬에서만 켜세요. 전환 연출이 있는 실제 배틀씬에서는 꺼둬야 중복 스폰이 생기지 않습니다.")]
    [SerializeField] private bool loadOnEnableWithoutSceneController = false;

    [Header("Debug")]
    [SerializeField] private bool createDebugDataIfEmpty = true;
    [SerializeField] private BattleDebugDataProvider debugDataProvider;

    [Header("Turn Executor")]
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Timeline")]
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Grid Effects")]
    [SerializeField] private BattleGridEffectController gridEffectController;

    [Header("Tutorial")]
    [SerializeField] private BattleFirstTutorialController firstBattleTutorialController;

    private readonly List<MonsterUnit> spawnedMonsterUnits = new();
    private readonly List<PlayerHUDSlot> playerHudSlots = new();
    private readonly List<PlayerHUDSlot> playerHudNumberOrder = new();
    private CharacterRuntimeData selectedPlayerRuntime;
    private Coroutine openSelectedSkillListWhenReadyRoutine;
    private Coroutine loadRoutine;
    private bool isLoaded;
    private bool isLoading;
    private string loadedMapId;
    private string debugTargetMonsterId;
    private int debugTargetGridIndex = -1;

    private readonly BattlePassiveSkillService passiveSkillService = new();

    public void ConfigureDebugTargetMonster(string monsterId, int gridIndex)
    {
        debugTargetMonsterId = string.IsNullOrWhiteSpace(monsterId) ? null : monsterId.Trim();
        debugTargetGridIndex = gridIndex;
    }

    private void EnsureTimelineController()
    {
        if (timelineController != null)
            return;

        timelineController = Object.FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void EnsureFirstBattleTutorialController()
    {
        if (firstBattleTutorialController != null)
            return;

        firstBattleTutorialController = GetComponent<BattleFirstTutorialController>();

        if (firstBattleTutorialController == null)
        {
            firstBattleTutorialController =
                Object.FindFirstObjectByType<BattleFirstTutorialController>(FindObjectsInactive.Include);
        }
    }

    public void RefreshBattleHUDs()
    {
        RefreshPlayerHUDs();

        MonsterHUDSlot[] monsterHuds = FindObjectsByType<MonsterHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsterHuds.Length; i++)
        {
            if (monsterHuds[i] != null)
                monsterHuds[i].Refresh();
        }
    }

    private void OnEnable()
    {
        if (loadOnEnableWithoutSceneController)
            RequestLoadBattle();
    }

    private void Update()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        HandleSkillPanelToggleInput();
        HandleCharacterNumberSelectInput();
    }

    private void HandleSkillPanelToggleInput()
    {
        if (!enableSkillPanelToggleInput)
            return;

        if (!Input.GetKeyDown(KeyCode.Tab))
            return;

        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        if (IsTypingInputFieldSelected())
            return;

        if (turnExecutor == null)
            EnsureTurnExecutor();

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        ToggleSkillListForSelectedPlayer();
    }

    private void HandleCharacterNumberSelectInput()
    {
        if (!enableCharacterNumberSelectInput)
            return;

        if (IsTypingInputFieldSelected())
            return;

        if (turnExecutor == null)
            EnsureTurnExecutor();

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        int characterIndex = GetPressedCharacterNumberIndex();

        if (characterIndex < 0)
            return;

        SelectPlayerCharacterByNumberIndex(characterIndex);
    }

    private int GetPressedCharacterNumberIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            return 0;

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            return 1;

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            return 2;

        return -1;
    }

    private void SelectPlayerCharacterByNumberIndex(int characterIndex)
    {
        RemoveNullPlayerHudSlots();
        RemoveNullPlayerHudNumberOrder();

        if (characterIndex < 0 || characterIndex >= playerHudNumberOrder.Count)
            return;

        PlayerHUDSlot hud = playerHudNumberOrder[characterIndex];

        if (hud == null || hud.BoundRuntime == null)
            return;

        RectTransform hudRect = hud.GetComponent<RectTransform>();
        OpenSkillListForPlayer(hud.BoundRuntime, hudRect);
    }

    /// <summary>
    /// SkillButton의 OnClick에서 호출합니다.
    /// Tab 키와 동일하게 현재 선택된 캐릭터의 스킬 목록을 열거나 닫습니다.
    /// </summary>
    public void ToggleSkillListPanelFromButton()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (turnExecutor == null)
            EnsureTurnExecutor();

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        ToggleSkillListForSelectedPlayer();
    }

    private void ToggleSkillListForSelectedPlayer()
    {
        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        if (skillListPanel.IsOpen())
        {
            skillListPanel.Close();
            return;
        }

        CharacterRuntimeData runtimeData = GetSelectedOrFirstPlayerRuntime();

        if (runtimeData == null)
            return;

        OpenSkillListForPlayer(runtimeData);
    }

    private void OpenSelectedCharacterSkillListWhenInputReady()
    {
        if (!openSelectedCharacterSkillListWhenInputReady)
            return;

        if (openSelectedSkillListWhenReadyRoutine != null)
            StopCoroutine(openSelectedSkillListWhenReadyRoutine);

        openSelectedSkillListWhenReadyRoutine = StartCoroutine(OpenSelectedCharacterSkillListWhenInputReadyRoutine());
    }

    private IEnumerator OpenSelectedCharacterSkillListWhenInputReadyRoutine()
    {
        // 첫 전투방은 HUD/캐릭터 선택/스킬 패널 참조가 같은 프레임에 준비될 수 있어서
        // 한 프레임 기다린 뒤 다시 확인해야 자동 오픈이 안정적으로 동작합니다.
        yield return null;

        EnsureSkillListPanel();
        EnsureTurnExecutor();

        if (skillListPanel == null)
        {
            openSelectedSkillListWhenReadyRoutine = null;
            yield break;
        }

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
        {
            openSelectedSkillListWhenReadyRoutine = null;
            yield break;
        }

        CharacterRuntimeData runtimeData = GetSelectedOrFirstPlayerRuntime();

        if (runtimeData == null)
        {
            openSelectedSkillListWhenReadyRoutine = null;
            yield break;
        }

        OpenSkillListForPlayer(runtimeData);
        openSelectedSkillListWhenReadyRoutine = null;
    }

    private void EnsureSkillListPanel()
    {
        if (skillListPanel != null)
            return;

        skillListPanel = FindFirstObjectByType<SkillListPanel>(FindObjectsInactive.Include);
    }

    private CharacterRuntimeData GetSelectedOrFirstPlayerRuntime()
    {
        RemoveNullPlayerHudSlots();

        if (selectedPlayerRuntime != null && FindPlayerHudIndex(selectedPlayerRuntime) >= 0)
            return selectedPlayerRuntime;

        for (int i = 0; i < playerHudSlots.Count; i++)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud != null && hud.BoundRuntime != null)
                return hud.BoundRuntime;
        }

        return null;
    }

    private void SelectAdjacentPlayerCharacter(int direction)
    {
        RemoveNullPlayerHudSlots();

        if (playerHudSlots.Count <= 0)
            return;

        int currentIndex = FindPlayerHudIndex(selectedPlayerRuntime);

        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex = WrapIndex(currentIndex + direction, playerHudSlots.Count);
        PlayerHUDSlot nextHud = playerHudSlots[nextIndex];

        if (nextHud == null || nextHud.BoundRuntime == null)
            return;

        OpenSkillListForPlayer(nextHud.BoundRuntime);
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return 0;

        while (index < 0)
            index += count;

        while (index >= count)
            index -= count;

        return index;
    }

    private void RemoveNullPlayerHudSlots()
    {
        for (int i = playerHudSlots.Count - 1; i >= 0; i--)
        {
            if (playerHudSlots[i] == null)
                playerHudSlots.RemoveAt(i);
        }
    }

    private void RemoveNullPlayerHudNumberOrder()
    {
        for (int i = playerHudNumberOrder.Count - 1; i >= 0; i--)
        {
            if (playerHudNumberOrder[i] == null)
                playerHudNumberOrder.RemoveAt(i);
        }
    }

    private bool IsTypingInputFieldSelected()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return false;

        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;

        if (selectedObject.GetComponent<InputField>() != null)
            return true;

        return false;
    }

    public void RequestLoadBattle(bool forceReload = false)
    {
        if (!isActiveAndEnabled)
            return;

        if (forceReload)
        {
            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }

            ResetLoadedStateForNextBattle(true);
        }

        if (isLoaded || isLoading || loadRoutine != null)
            return;

        loadRoutine = StartCoroutine(LoadBattleWhenDataManagerReady());
    }

    public void LoadBattleFromSceneController(bool forceReload = false)
    {
        RequestLoadBattle(forceReload);
    }

    private void OnDisable()
    {
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        isLoading = false;

        if (skillListPanel != null)
            skillListPanel.Close();
    }

    private IEnumerator LoadBattleWhenDataManagerReady()
    {
        bool warned = false;

        while (DataManager.Instance == null)
        {
            if (!warned)
            {
                Debug.LogWarning("[BattleRoomLoader] DataManager가 아직 준비되지 않아 전투 로드를 대기합니다.");
                warned = true;
            }

            yield return null;
        }

        loadRoutine = null;
        LoadBattle();
    }

    public void LoadBattle()
    {
        if (isLoading)
            return;

        string currentMapId = GetCurrentMapIdSafe();

        if (isLoaded)
        {
            if (string.IsNullOrWhiteSpace(loadedMapId) || loadedMapId == currentMapId)
                return;

            ResetLoadedStateForNextBattle(true);
        }

        isLoading = true;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[BattleRoomLoader] DataManager가 아직 준비되지 않아 전투 로드를 건너뜁니다.");
            isLoading = false;
            return;
        }

        DataManager.Instance.Initialize();
        currentMapId = GetCurrentMapIdSafe();
        PrintPartyData();

        if (!DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            if (!createDebugDataIfEmpty)
            {
                Debug.LogError("[BattleRoomLoader] Party runtime data is missing.");
                isLoading = false;
                return;
            }

            if (debugDataProvider == null)
            {
                Debug.LogError("[BattleRoomLoader] DebugDataProvider is missing.");
                isLoading = false;
                return;
            }

            debugDataProvider.CreateDebugData();
            PrintPartyData();
        }

        RegisterSkillListKeepOpenRoots();

        ResetPartyCurrentGridToSpawn();
        ClearPartyBattleRoomTemporaryStatusEffects();
        ResetPartyBattleStartResources();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.ResetBattle();

        ClearSpawnedBattleObjects();

        if (monsterTurnPlanner != null)
            monsterTurnPlanner.ResetBattleStartIntroState();

        SpawnPlayersAndHUD();
        SpawnMonstersAndHUD(false);
        SpawnInitialGridEffects();
        RefreshBattleHUDs();

        EnsureTurnExecutor();

        if (turnExecutor != null)
        {
            turnExecutor.ResetBattleTurnState();
            turnExecutor.SetBattleInputReady(false);
        }

        EnsureTimelineController();

        if (timelineController != null)
            timelineController.ResetTimelineBarsForNewBattleRoom();

        SteamBattleStateSynchronizer.EnsureForBattleScene(turnExecutor, timelineController);

        loadedMapId = currentMapId;
        isLoaded = true;
        isLoading = false;

        StartCoroutine(PlanInitialMonsterTurnsAndEnableInputRoutine());
    }

    public void ResetLoadedStateForNextBattle(bool clearSpawnedObjects = true)
    {
        isLoaded = false;
        isLoading = false;
        loadedMapId = null;

        if (clearSpawnedObjects)
            ClearSpawnedBattleObjects();

        ClearHUD();
        spawnedMonsterUnits.Clear();

        EnsureTimelineController();

        if (timelineController != null)
            timelineController.ResetTimelineBarsForNewBattleRoom();
    }

    private string GetCurrentMapIdSafe()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return null;

        MapRuntimeData mapRuntime = DataManager.Instance.MapRuntimeStore.Get();
        return mapRuntime != null ? mapRuntime.CurrentMapId : null;
    }

    private void ResetPartyCurrentGridToSpawn()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            int spawnGridIndex = partyStore.GetSpawnGridIndex(i);

            if (spawnGridIndex < 0)
                continue;

            partyStore.SetCurrentGridIndex(i, spawnGridIndex);
        }
    }

    private void ClearPartyBattleRoomTemporaryStatusEffects()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
                continue;

            runtimeData.ClearBattleRoomTemporaryStatusEffects();
        }
    }

    private void ResetPartyBattleStartResources()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        var battleStartCharacters = new List<CharacterRuntimeData>();

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
                continue;

            battleStartCharacters.Add(runtimeData);
        }

        CultureTankBattleStartEffectService.ApplyToPartyAndConsume(
            DataManager.Instance.BattleRuntimeStore?.GetOrCreate(),
            battleStartCharacters);

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
                continue;

            if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData masterData))
                continue;

            BattleEquipmentEffectService.ApplyBattleStartEffects(runtimeData, masterData);
            BattlePassiveSkillService.RefreshRuntimePassiveEffects(runtimeData);
        }
    }

    public void RecoverPlayerCostsToMax()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
                continue;

            if (runtimeData.IsDead)
                continue;

            DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData masterData);

            int rechargeBonus = GetStatusStack(runtimeData.StatusEffects, "E_Charge");

            int maxCost = runtimeData.MaxCost > 0
                ? runtimeData.MaxCost
                : masterData != null ? Mathf.Max(0, masterData.MaxCost) : 0;

            int baseRecovery = runtimeData.CostRecovery > 0
                ? runtimeData.CostRecovery
                : masterData != null ? Mathf.Max(0, masterData.CostRecovery) : 0;

            int equipmentRecovery = BattleEquipmentEffectService.GetEffectiveCostRecovery(
                runtimeData,
                masterData);

            int totalRecovery = Mathf.Max(
                0,
                equipmentRecovery + rechargeBonus
            );

            runtimeData.CostRecovery = baseRecovery;

            int costBefore = runtimeData.CurrentCost;
            runtimeData.CurrentCost =
                Mathf.Min(
                    maxCost,
                    runtimeData.CurrentCost + totalRecovery
                );

            int recoveredCost = Mathf.Max(0, runtimeData.CurrentCost - costBefore);
            if (recoveredCost > 0)
            {
                BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

                for (int characterIndex = 0; characterIndex < characters.Length; characterIndex++)
                {
                    BattleCharacter character = characters[characterIndex];
                    if (character != null && character.CharacterId == characterId)
                    {
                        BattleDamageTextPopupUI.ShowCostRecovery(character.transform, recoveredCost);
                        break;
                    }
                }
            }
        }

        RefreshPlayerHUDs();

        if (skillListPanel != null)
            skillListPanel.Refresh();
    }

    private int GetStatusStack(
    List<StatusEffectRuntimeData> statuses,
    string effectId)
    {
        if (statuses == null)
            return 0;

        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i] == null)
                continue;

            if (statuses[i].EffectId == effectId)
                return statuses[i].Stack;
        }

        return 0;
    }

    private void RefreshPlayerHUDs()
    {
        for (int i = playerHudSlots.Count - 1; i >= 0; i--)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud == null)
            {
                playerHudSlots.RemoveAt(i);
                continue;
            }

            hud.Refresh();
        }
    }

    private void RegisterSkillListKeepOpenRoots()
    {
        if (skillListPanel == null)
            return;

        skillListPanel.ClearRuntimeKeepOpenClickRoots();

        if (playerHudRoot is RectTransform playerHudRootRect)
            skillListPanel.RegisterKeepOpenClickRoot(playerHudRootRect);
    }

    private void SpawnPlayersAndHUD()
    {
        playerHudSlots.Clear();
        playerHudNumberOrder.Clear();
        selectedPlayerRuntime = null;

        if (unitSpawner == null)
        {
            Debug.LogError("[BattleRoomLoader] UnitSpawner is missing.");
            return;
        }

        List<CharacterRuntimeData> playerRuntimes = unitSpawner.SpawnFromRuntimeData();

        if (playerRuntimes == null)
            return;

        for (int i = 0; i < playerRuntimes.Count; i++)
            CreatePlayerHUD(playerRuntimes[i], i);

        if (selectFirstPlayerHudByDefault && playerRuntimes.Count > 0)
            selectedPlayerRuntime = playerRuntimes[0];

        ApplyPlayerHudAnchorOrder();
        RefreshPlayerHudSelectionVisuals();
        EnsurePlayerHudLayerOrder();
    }

    private void CreatePlayerHUD(CharacterRuntimeData runtimeData, int displayIndex)
    {
        if (runtimeData == null)
            return;

        if (playerHudPrefab == null || playerHudRoot == null)
        {
            Debug.LogWarning("[BattleRoomLoader] Player HUD reference is missing.");
            return;
        }

        Transform anchor = GetPlayerHudPositionAnchor(displayIndex);

        if (anchor == null)
        {
            Debug.LogWarning($"[BattleRoomLoader] Player HUD position anchor is missing. Index: {displayIndex}");
            anchor = playerHudRoot;
        }

        PlayerHUDSlot hud = Instantiate(playerHudPrefab, anchor);
        ApplyPlayerHudToAnchor(hud, anchor);

        hud.SetKeyboardNumber(displayIndex + 1);
        hud.Bind(runtimeData);
        hud.OnClicked += OnPlayerHudClicked;
        playerHudSlots.Add(hud);
        playerHudNumberOrder.Add(hud);

        RegisterPlayerHudAsSkillListKeepOpenRoot(hud);
    }

    private void RegisterPlayerHudAsSkillListKeepOpenRoot(PlayerHUDSlot hud)
    {
        if (hud == null)
            return;

        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        RectTransform hudRect = hud.GetComponent<RectTransform>();

        if (hudRect != null)
            skillListPanel.RegisterKeepOpenClickRoot(hudRect);
    }

    private void RegisterAllPlayerHudsAsSkillListKeepOpenRoots()
    {
        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        RemoveNullPlayerHudSlots();

        for (int i = 0; i < playerHudSlots.Count; i++)
            RegisterPlayerHudAsSkillListKeepOpenRoot(playerHudSlots[i]);
    }

    private Transform GetPlayerHudPositionAnchor(int displayIndex)
    {
        if (displayIndex < 0)
            return null;

        if (playerHudPositionAnchors != null && displayIndex < playerHudPositionAnchors.Length)
        {
            Transform assignedAnchor = playerHudPositionAnchors[displayIndex];

            if (assignedAnchor != null)
                return assignedAnchor;
        }

        if (playerHudRoot != null && displayIndex < playerHudRoot.childCount)
            return playerHudRoot.GetChild(displayIndex);

        return null;
    }

    private void ApplyPlayerHudToAnchor(PlayerHUDSlot hud, Transform anchor)
    {
        if (hud == null || anchor == null)
            return;

        Transform hudTransform = hud.transform;

        if (hudTransform.parent != anchor)
            hudTransform.SetParent(anchor, false);

        RectTransform rect = hud.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * hudScale;
            hud.SetBaseScale(Vector3.one * hudScale);
        }

        LayoutElement layoutElement = hud.GetComponent<LayoutElement>();

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;

        hudTransform.SetAsLastSibling();
    }

    private void OnPlayerHudClicked(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        OpenSkillListForPlayer(runtimeData, hudRect);
    }

    public void OnPlayerCharacterClicked(CharacterRuntimeData runtimeData)
    {
        OpenSkillListForPlayer(runtimeData);
    }

    private void OpenSkillListForPlayer(CharacterRuntimeData runtimeData)
    {
        OpenSkillListForPlayer(runtimeData, null);
    }

    private void OpenSkillListForPlayer(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        if (runtimeData != null &&
            !SteamBattleStateSynchronizer.CanLocalPlayerControlCharacter(runtimeData.CharacterId))
        {
            BattleWarningUI.ShowMessage("다른 플레이어의 캐릭터입니다.");
            return;
        }

        SelectPlayerHUD(runtimeData);
        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        RegisterAllPlayerHudsAsSkillListKeepOpenRoots();

        if (hudRect != null)
            skillListPanel.RegisterKeepOpenClickRoot(hudRect);

        skillListPanel.Open(runtimeData, hudRect);
    }

    private void SelectPlayerHUD(CharacterRuntimeData runtimeData)
    {
        selectedPlayerRuntime = runtimeData;

        for (int i = playerHudSlots.Count - 1; i >= 0; i--)
        {
            if (playerHudSlots[i] == null)
                playerHudSlots.RemoveAt(i);
        }

        if (runtimeData == null || playerHudSlots.Count <= 0)
            return;

        int selectedIndex = FindPlayerHudIndex(runtimeData);

        if (selectedIndex < 0)
            return;

        RefreshPlayerHudSelectionVisuals();
    }

    private int FindPlayerHudIndex(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return -1;

        for (int i = 0; i < playerHudSlots.Count; i++)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud == null)
                continue;

            if (hud.BoundRuntime == runtimeData)
                return i;
        }

        return -1;
    }

    private void RotatePlayerHudOrder(int selectedIndex)
    {
        if (selectedIndex <= 0 || selectedIndex >= playerHudSlots.Count)
        {
            ApplyPlayerHudAnchorOrder();
            return;
        }

        List<PlayerHUDSlot> rotatedSlots = new();

        for (int i = selectedIndex; i < playerHudSlots.Count; i++)
            rotatedSlots.Add(playerHudSlots[i]);

        for (int i = 0; i < selectedIndex; i++)
            rotatedSlots.Add(playerHudSlots[i]);

        playerHudSlots.Clear();
        playerHudSlots.AddRange(rotatedSlots);

        ApplyPlayerHudAnchorOrder();
    }

    private void ApplyPlayerHudAnchorOrder()
    {
        for (int i = 0; i < playerHudSlots.Count; i++)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud == null)
                continue;

            Transform anchor = GetPlayerHudPositionAnchor(i);

            if (anchor == null)
            {
                Debug.LogWarning($"[BattleRoomLoader] Player HUD position anchor is missing. Index: {i}");
                continue;
            }

            ApplyPlayerHudToAnchor(hud, anchor);
        }
    }

    private void RefreshPlayerHudSelectionVisuals()
    {
        ApplyPlayerHudAnchorOrder();

        for (int i = 0; i < playerHudSlots.Count; i++)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud == null)
                continue;

            bool selected = hud.BoundRuntime == selectedPlayerRuntime;
            hud.SetCommandSelected(selected);
            ApplyPlayerHudCanvasSorting(hud, selected);
        }


        EnsurePlayerHudLayerOrder();
    }

    private void EnsurePlayerHudLayerOrder()
    {
        if (playerHudRoot == null || !forcePlayerHudRootOnTop)
            return;

        playerHudRoot.SetAsLastSibling();

        Canvas rootCanvas = playerHudRoot.GetComponent<Canvas>();

        if (rootCanvas == null)
            rootCanvas = playerHudRoot.gameObject.AddComponent<Canvas>();

        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = playerHudRootSortingOrder;

        if (playerHudRoot.GetComponent<GraphicRaycaster>() == null)
            playerHudRoot.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void ApplyPlayerHudCanvasSorting(PlayerHUDSlot hud, bool selected)
    {
        if (hud == null)
            return;

        Canvas canvas = hud.GetComponent<Canvas>();
        GraphicRaycaster raycaster = hud.GetComponent<GraphicRaycaster>();

        if (selected)
        {
            if (canvas == null)
                canvas = hud.gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(selectedHudSortingOrder, playerHudRootSortingOrder + 10);

            if (raycaster == null)
                hud.gameObject.AddComponent<GraphicRaycaster>();
        }
        else if (canvas != null)
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }

    private void SpawnMonstersAndHUD(bool planMonsterTurns = true)
    {
        spawnedMonsterUnits.Clear();

        if (monsterSpawner == null)
        {
            Debug.LogError("[BattleRoomLoader] MonsterSpawner is missing.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(debugTargetMonsterId))
        {
            SpawnDebugTargetMonster();
        }
        else
        {
            SpawnMapMonsters();
        }

        RefreshMonsterDisplayNames();

        if (!planMonsterTurns)
            return;

        if (monsterTurnPlanner != null)
        {
            monsterTurnPlanner.PlanMonsterTurns(spawnedMonsterUnits, true);
        }
        else
        {
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner is missing.");
        }
    }

    private void SpawnDebugTargetMonster()
    {
        SpawnedMonsterResult result = monsterSpawner.SpawnRuntimeMonster(
            debugTargetMonsterId,
            new List<int> { debugTargetGridIndex });

        if (result == null || result.RuntimeData == null || result.MonsterTransform == null)
        {
            Debug.LogError($"[BattleRoomLoader] Failed to spawn debug target: {debugTargetMonsterId}");
            return;
        }

        DebugBattleTargetRules.Configure(result.RuntimeData);

        MonsterUnit monsterUnit = result.MonsterTransform.GetComponent<MonsterUnit>();
        if (monsterUnit != null)
        {
            monsterUnit.SetAIEnabled(false);
            monsterUnit.RefreshRuntimeDisplayName();
        }

        RegisterRuntimeMonster(result);
    }

    private void SpawnMapMonsters()
    {
        MapRuntimeData mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
        {
            Debug.LogError("[BattleRoomLoader] CurrentMapId is missing.");
            return;
        }

        string mapId = mapRuntime.CurrentMapId;
        MapData mapData = DataManager.Instance.MapDatabase.Get(mapId);

        if (mapData == null || string.IsNullOrWhiteSpace(mapData.BattleMapId))
        {
            Debug.LogError($"[BattleRoomLoader] BattleMapId is missing. MapId: {mapRuntime.CurrentMapId}");
            return;
        }

        var spawns = DataManager.Instance.BattleMapDatabase.GetSpawns(mapData.BattleMapId);

        foreach (BattleMapData spawnData in spawns)
        {
            SpawnedMonsterResult result = monsterSpawner.Spawn(spawnData);

            if (result == null || result.RuntimeData == null || result.MonsterTransform == null)
                continue;

            Collider2D monsterCollider = result.MonsterTransform.GetComponentInChildren<Collider2D>();
            MonsterHUDSlot hud = CreateMonsterHUD(result.RuntimeData, result.MonsterTransform, monsterCollider);
            MonsterUnit monsterUnit = result.MonsterTransform.GetComponent<MonsterUnit>();

            if (monsterUnit != null)
            {
                monsterUnit.SetOccupiedCells(spawnData.GetOccupiedCells());
                monsterUnit.BindHUD(hud);
                spawnedMonsterUnits.Add(monsterUnit);
            }
        }
    }

    private IEnumerator PlanInitialMonsterTurnsAndEnableInputRoutine()
    {
        EnsureTurnExecutor();

        if (turnExecutor != null)
            turnExecutor.SetBattleInputReady(false);

        if (monsterTurnPlanner != null)
        {
            yield return monsterTurnPlanner.PlanMonsterTurnsAndWait(spawnedMonsterUnits, true);
        }
        else
        {
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner is missing.");
        }

        if (turnExecutor != null)
        {
            turnExecutor.SetBattleInputReady(true);
            Debug.Log("[BattleRoomLoader] Battle input ready true after initial monster plan");
        }

        OpenSelectedCharacterSkillListWhenInputReady();

        EnsureFirstBattleTutorialController();

        if (firstBattleTutorialController != null)
            firstBattleTutorialController.TryStartTutorialIfNeeded();
    }

    public void PlanNextMonsterTurns()
    {
        StartCoroutine(PlanNextMonsterTurnsRoutine());
    }

    public IEnumerator PlanNextMonsterTurnsRoutine()
    {
        EnsureTurnExecutor();

        if (turnExecutor != null)
            turnExecutor.SetBattleInputReady(false);

        if (monsterTurnPlanner == null)
        {
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner is missing.");

            if (turnExecutor != null)
                turnExecutor.SetBattleInputReady(true);

            yield break;
        }

        yield return monsterTurnPlanner.PlanMonsterTurnsAndWait(spawnedMonsterUnits);

        if (turnExecutor != null)
            turnExecutor.SetBattleInputReady(true);

        OpenSelectedCharacterSkillListWhenInputReady();
    }

    private MonsterHUDSlot CreateMonsterHUD(MonsterRuntimeData runtimeData, Transform monsterTransform, Collider2D monsterCollider)
    {
        if (runtimeData == null || monsterTransform == null)
            return null;

        if (monsterHudPrefab == null || monsterHudRoot == null)
        {
            Debug.LogWarning("[BattleRoomLoader] Monster HUD reference is missing.");
            return null;
        }

        MonsterHUDSlot hud = Instantiate(monsterHudPrefab, monsterHudRoot);
        hud.Bind(runtimeData);
        hud.SetFollowTarget(monsterTransform, monsterCollider);
        hud.Hide();

        RectTransform rect = hud.GetComponent<RectTransform>();

        if (rect != null)
            rect.localScale = Vector3.one * hudScale;

        WorldFollowHUD follow = hud.GetComponent<WorldFollowHUD>();

        if (follow != null)
        {
            follow.Bind(
                monsterTransform,
                worldCamera != null ? worldCamera : Camera.main,
                battleCanvasRect,
                uiCamera,
                monsterCollider
            );
        }

        return hud;
    }

    private void ClearSpawnedBattleObjects()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            characters[i].gameObject.SetActive(false);
            Destroy(characters[i].gameObject);
        }

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null)
                continue;

            monsters[i].gameObject.SetActive(false);
            Destroy(monsters[i].gameObject);
        }

        ClearMonsterHUDSlots();
        ClearPlayerHUDSlotsOnly();
        spawnedMonsterUnits.Clear();
        ClearGridEffects();
    }

    private void ClearHUD()
    {
        selectedPlayerRuntime = null;

        if (skillListPanel != null)
        {
            skillListPanel.Close();
            skillListPanel.ClearRuntimeKeepOpenClickRoots();
        }

        ClearPlayerHUDSlotsOnly();
        ClearMonsterHUDSlots();
    }

    private void ClearPlayerHUDSlotsOnly()
    {
        for (int i = playerHudSlots.Count - 1; i >= 0; i--)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud != null)
                Destroy(hud.gameObject);
        }

        if (playerHudRoot != null)
        {
            for (int i = 0; i < playerHudRoot.childCount; i++)
            {
                Transform anchor = playerHudRoot.GetChild(i);

                if (anchor == null)
                    continue;

                for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = anchor.GetChild(childIndex);

                    if (child != null && child.GetComponent<PlayerHUDSlot>() != null)
                        Destroy(child.gameObject);
                }
            }
        }

        playerHudSlots.Clear();
        playerHudNumberOrder.Clear();
    }

    private void ClearMonsterHUDSlots()
    {
        if (monsterHudRoot == null)
            return;

        for (int i = monsterHudRoot.childCount - 1; i >= 0; i--)
            Destroy(monsterHudRoot.GetChild(i).gameObject);
    }

    private void PrintPartyData()
    {
        var party = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            // Debug.Log($"Slot {i}: {party.GetCharacterId(i)} / Grid {party.GetGridIndex(i)}");
        }
    }

    public void RegisterRuntimeMonster(SpawnedMonsterResult result)
    {
        if (result == null || result.RuntimeData == null || result.MonsterTransform == null)
            return;

        MonsterUnit monsterUnit = result.MonsterTransform.GetComponent<MonsterUnit>();

        if (monsterUnit == null)
            return;

        Collider2D monsterCollider =
            result.MonsterTransform.GetComponentInChildren<Collider2D>();

        MonsterHUDSlot hud =
            CreateMonsterHUD(result.RuntimeData, result.MonsterTransform, monsterCollider);

        monsterUnit.BindHUD(hud);

        if (!spawnedMonsterUnits.Contains(monsterUnit))
            spawnedMonsterUnits.Add(monsterUnit);

        RefreshMonsterDisplayNames();
    }

    public void UnregisterRuntimeMonster(MonsterUnit monsterUnit)
    {
        if (monsterUnit == null)
            return;

        spawnedMonsterUnits.Remove(monsterUnit);
        RefreshMonsterDisplayNames();
    }

    private void RefreshMonsterDisplayNames()
    {
        Dictionary<string, List<MonsterUnit>> monstersById = new Dictionary<string, List<MonsterUnit>>();

        for (int i = 0; i < spawnedMonsterUnits.Count; i++)
        {
            MonsterUnit monsterUnit = spawnedMonsterUnits[i];
            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                continue;

            string monsterId = monsterUnit.RuntimeData.MonsterId;
            if (string.IsNullOrWhiteSpace(monsterId))
                monsterId = monsterUnit.RuntimeData.Name;

            if (string.IsNullOrWhiteSpace(monsterId))
                continue;

            if (!monstersById.TryGetValue(monsterId, out List<MonsterUnit> sameMonsters))
            {
                sameMonsters = new List<MonsterUnit>();
                monstersById.Add(monsterId, sameMonsters);
            }

            sameMonsters.Add(monsterUnit);
        }

        foreach (KeyValuePair<string, List<MonsterUnit>> pair in monstersById)
        {
            List<MonsterUnit> sameMonsters = pair.Value;
            if (sameMonsters == null)
                continue;

            bool useSuffix = sameMonsters.Count > 1;
            for (int i = 0; i < sameMonsters.Count; i++)
            {
                MonsterUnit monsterUnit = sameMonsters[i];
                if (monsterUnit == null || monsterUnit.RuntimeData == null)
                    continue;

                string suffix = useSuffix ? GetMonsterDisplaySuffix(i) : string.Empty;
                monsterUnit.RuntimeData.SetDisplaySuffix(suffix);
                monsterUnit.RefreshRuntimeDisplayName();
            }
        }
    }

    private static string GetMonsterDisplaySuffix(int index)
    {
        index = Mathf.Max(0, index);

        string suffix = string.Empty;
        do
        {
            int remainder = index % 26;
            suffix = (char)('A' + remainder) + suffix;
            index = (index / 26) - 1;
        }
        while (index >= 0);

        return suffix;
    }

    private void EnsureTurnExecutor()
    {
        if (turnExecutor != null)
            return;

        turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
    }

    private void SpawnInitialGridEffects()
    {
        EnsureGridEffectController();

        if (gridEffectController != null)
            gridEffectController.SpawnInitialEffects();
    }

    private void ClearGridEffects()
    {
        EnsureGridEffectController(false);

        if (gridEffectController != null)
            gridEffectController.ClearAll();
    }

    private void EnsureGridEffectController(bool createIfMissing = true)
    {
        if (gridEffectController != null)
            return;

        gridEffectController = FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null || !createIfMissing)
            return;

        gridEffectController = gameObject.AddComponent<BattleGridEffectController>();
    }
}
