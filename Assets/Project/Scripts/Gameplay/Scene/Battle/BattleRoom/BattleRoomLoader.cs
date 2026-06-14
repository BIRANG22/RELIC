using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
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

    [Header("Debug")]
    [SerializeField] private bool createDebugDataIfEmpty = true;
    [SerializeField] private BattleDebugDataProvider debugDataProvider;

    private readonly List<MonsterUnit> spawnedMonsterUnits = new();
    private readonly List<PlayerHUDSlot> playerHudSlots = new();
    private CharacterRuntimeData selectedPlayerRuntime;
    private bool isLoaded;

    private void OnEnable()
    {
        LoadBattle();
    }

    private void OnDisable()
    {
        isLoaded = false;
        ClearHUD();
    }

    public void LoadBattle()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        if (DataManager.Instance == null)
        {
            Debug.LogError("[BattleRoomLoader] DataManager is missing.");
            return;
        }

        DataManager.Instance.Initialize();
        PrintPartyData();

        if (!DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            if (!createDebugDataIfEmpty)
            {
                Debug.LogError("[BattleRoomLoader] Party runtime data is missing.");
                return;
            }

            if (debugDataProvider == null)
            {
                Debug.LogError("[BattleRoomLoader] DebugDataProvider is missing.");
                return;
            }

            debugDataProvider.CreateDebugData();
            PrintPartyData();
        }

        RegisterSkillListKeepOpenRoots();

        ResetPartyCurrentGridToSpawn();

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.ResetBattle();

        SpawnPlayersAndHUD();
        SpawnMonstersAndHUD();
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

        string mapId = "Map_01";
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

            MonsterHUDSlot hud = CreateMonsterHUD(result.RuntimeData, result.MonsterTransform);
            MonsterUnit monsterUnit = result.MonsterTransform.GetComponent<MonsterUnit>();

            if (monsterUnit != null)
            {
                monsterUnit.SetOccupiedCells(spawnData.GetOccupiedCells());
                monsterUnit.BindHUD(hud);
                spawnedMonsterUnits.Add(monsterUnit);
            }
        }

        if (monsterTurnPlanner != null)
            monsterTurnPlanner.PlanMonsterTurns(spawnedMonsterUnits);
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

    private MonsterHUDSlot CreateMonsterHUD(MonsterRuntimeData runtimeData, Transform monsterTransform)
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
                uiCamera
            );
        }

        return hud;
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
}
