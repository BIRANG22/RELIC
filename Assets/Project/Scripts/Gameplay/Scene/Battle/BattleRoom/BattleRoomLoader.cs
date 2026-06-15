using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Monster Turn")]
    [SerializeField] private BattleMonsterTurnPlanner monsterTurnPlanner;

    [Header("Load Control")]
    [Tooltip("BattleSceneController가 없는 테스트 씬에서만 켜세요. 전환 연출이 있는 실제 배틀씬에서는 꺼둬야 중복 스폰이 생기지 않습니다.")]
    [SerializeField] private bool loadOnEnableWithoutSceneController = false;

    [Header("Debug")]
    [SerializeField] private bool createDebugDataIfEmpty = true;
    [SerializeField] private BattleDebugDataProvider debugDataProvider;

    private readonly List<MonsterUnit> spawnedMonsterUnits = new();
    private readonly List<PlayerHUDSlot> playerHudSlots = new();
    private CharacterRuntimeData selectedPlayerRuntime;
    private Coroutine loadRoutine;
    private bool isLoaded;
    private bool isLoading;
    private string loadedMapId;

    private void OnEnable()
    {
        if (loadOnEnableWithoutSceneController)
            RequestLoadBattle();
    }

    public void RequestLoadBattle()
    {
        if (!isActiveAndEnabled)
            return;

        if (isLoaded || isLoading || loadRoutine != null)
            return;

        loadRoutine = StartCoroutine(LoadBattleWhenDataManagerReady());
    }

    public void LoadBattleFromSceneController()
    {
        RequestLoadBattle();
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
        ResetPartyBattleStartResources();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.ResetBattle();

        ClearSpawnedBattleObjects();

        if (monsterTurnPlanner != null)
            monsterTurnPlanner.ResetBattleStartIntroState();

        SpawnPlayersAndHUD();
        SpawnMonstersAndHUD();

        loadedMapId = currentMapId;
        isLoaded = true;
        isLoading = false;
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

            Debug.Log(
                $"[BattleRoomLoader] Reset CurrentGrid / Slot:{i} / Character:{characterId} / Grid:{spawnGridIndex}"
            );
        }
    }

    private void ResetPartyBattleStartResources()
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

            if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData masterData))
                continue;

            runtimeData.CurrentStamina = Mathf.Max(0, masterData.MaxStamina);
            runtimeData.CurrentResource = 0;
            runtimeData.ClearReservedCosts();
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

            if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData masterData))
                continue;

            runtimeData.CurrentStamina = Mathf.Max(0, masterData.MaxStamina);
            runtimeData.ReservedStaminaCost = 0;
        }

        RefreshPlayerHUDs();

        if (skillListPanel != null)
            skillListPanel.Refresh();
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

        hud.Bind(runtimeData);
        hud.OnClicked += OnPlayerHudClicked;
        playerHudSlots.Add(hud);
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
        OpenSkillListForPlayer(runtimeData);
    }

    public void OnPlayerCharacterClicked(CharacterRuntimeData runtimeData)
    {
        OpenSkillListForPlayer(runtimeData);
    }

    private void OpenSkillListForPlayer(CharacterRuntimeData runtimeData)
    {
        SelectPlayerHUD(runtimeData);

        if (skillListPanel != null)
            skillListPanel.Open(runtimeData);
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

        RotatePlayerHudOrder(selectedIndex);
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
        for (int i = 0; i < playerHudSlots.Count; i++)
        {
            PlayerHUDSlot hud = playerHudSlots[i];

            if (hud == null)
                continue;

            bool selected = hud.BoundRuntime == selectedPlayerRuntime;
            hud.SetCommandSelected(selected);
            ApplyPlayerHudCanvasSorting(hud, selected);
        }

        ApplyPlayerHudAnchorOrder();
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

    private void SpawnMonstersAndHUD()
    {
        spawnedMonsterUnits.Clear();

        if (monsterSpawner == null)
        {
            Debug.LogError("[BattleRoomLoader] MonsterSpawner is missing.");
            return;
        }

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

        if (monsterTurnPlanner != null)
            monsterTurnPlanner.PlanMonsterTurns(spawnedMonsterUnits, true);
        else
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner is missing.");
    }

    public void PlanNextMonsterTurns()
    {
        if (monsterTurnPlanner == null)
        {
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner is missing.");
            return;
        }

        monsterTurnPlanner.PlanMonsterTurns(spawnedMonsterUnits);
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
    }

    public void UnregisterRuntimeMonster(MonsterUnit monsterUnit)
    {
        if (monsterUnit == null)
            return;

        spawnedMonsterUnits.Remove(monsterUnit);
    }
}
