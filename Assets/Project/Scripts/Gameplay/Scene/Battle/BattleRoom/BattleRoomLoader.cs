using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

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

    [Header("Debug")]
    [SerializeField] private bool createDebugDataIfEmpty = true;
    [SerializeField] private BattleDebugDataProvider debugDataProvider;

    [Header("HUD Layout")]
    [SerializeField] private float hudScale = 0.4f;
    [SerializeField] private float hudStartY = 80f;
    [SerializeField] private float hudYGap = -40f;
    [SerializeField] private float playerHudX = 60f;

    [Header("Monster World HUD")]
    [SerializeField] private RectTransform battleCanvasRect;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Skill List")]
    [SerializeField] private SkillListPanel skillListPanel;

    [Header("Monster Turn")]
    [SerializeField] private BattleMonsterTurnPlanner monsterTurnPlanner;

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
            Debug.LogError("[BattleSceneLoader] DataManager가 없습니다.");
            return;
        }

        DataManager.Instance.Initialize();

        PrintPartyData();

        if (!DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            if (!createDebugDataIfEmpty)
            {
                Debug.LogError("[BattleSceneLoader] 파티 데이터가 없습니다.");
                return;
            }

            if (debugDataProvider == null)
            {
                Debug.LogError("[BattleSceneLoader] DebugDataProvider가 없습니다.");
                return;
            }

            debugDataProvider.CreateDebugData();
            PrintPartyData();
        }

        SpawnPlayersAndHUD();
        SpawnMonstersAndHUD();
    }

    private void SpawnPlayersAndHUD()
    {
        if (unitSpawner == null)
        {
            Debug.LogError("[BattleSceneLoader] UnitSpawner가 없습니다.");
            return;
        }

        List<CharacterRuntimeData> playerRuntimes =
            unitSpawner.SpawnFromRuntimeData();

        if (playerRuntimes == null)
            return;

        for (int i = 0; i < playerRuntimes.Count; i++)
            CreatePlayerHUD(playerRuntimes[i], i);
    }

    private void CreatePlayerHUD(CharacterRuntimeData runtimeData, int index)
    {
        if (runtimeData == null)
            return;

        if (playerHudPrefab == null || playerHudRoot == null)
        {
            Debug.LogWarning("[BattleSceneLoader] Player HUD 참조가 없습니다.");
            return;
        }

        PlayerHUDSlot hud = Instantiate(playerHudPrefab, playerHudRoot);

        RectTransform rect = hud.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localScale = Vector3.one * hudScale;
            rect.anchoredPosition = new Vector2(
                playerHudX,
                hudStartY + index * hudYGap
            );
        }

        hud.Bind(runtimeData);

        hud.OnClicked += OnPlayerHudClicked;
    }

    private void OnPlayerHudClicked(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        if (skillListPanel != null)
            skillListPanel.Open(runtimeData, hudRect);
    }

    private void SpawnMonstersAndHUD()
    {
        if (monsterSpawner == null)
        {
            Debug.LogError("[BattleRoomLoader] MonsterSpawner가 없습니다.");
            return;
        }

        MapRuntimeData mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
        {
            Debug.LogError("[BattleRoomLoader] CurrentMapId가 없습니다.");
            return;
        }

        string mapId = "Map_01";

        MapData mapData = DataManager.Instance.MapDatabase.Get(mapId);

        if (mapData == null || string.IsNullOrWhiteSpace(mapData.BattleMapId))
        {
            Debug.LogError($"[BattleRoomLoader] BattleMapId가 없습니다. MapId: {mapRuntime.CurrentMapId}");
            return;
        }

        var spawns = DataManager.Instance.BattleMapDatabase.GetSpawns(mapData.BattleMapId);

        List<Relic.Gameplay.Monster.MonsterUnit> spawnedMonsterUnits = new();

        foreach (var spawnData in spawns)
        {
            SpawnedMonsterResult result = monsterSpawner.Spawn(spawnData);

            if (result == null || result.RuntimeData == null || result.MonsterTransform == null)
                continue;

            MonsterHUDSlot hud = CreateMonsterHUD(
                result.RuntimeData,
                result.MonsterTransform
            );

            Relic.Gameplay.Monster.MonsterUnit monsterUnit =
                result.MonsterTransform.GetComponent<Relic.Gameplay.Monster.MonsterUnit>();

            if (monsterUnit != null)
            {
                monsterUnit.BindHUD(hud);
                spawnedMonsterUnits.Add(monsterUnit);
            }
        }

        if (monsterTurnPlanner != null)
            monsterTurnPlanner.PlanMonsterTurns(spawnedMonsterUnits);
        else
            Debug.LogWarning("[BattleRoomLoader] MonsterTurnPlanner가 연결되지 않았습니다.");
    }

    private MonsterHUDSlot CreateMonsterHUD(
        MonsterRuntimeData runtimeData,
        Transform monsterTransform)
    {
        if (runtimeData == null || monsterTransform == null)
            return null;

        if (monsterHudPrefab == null || monsterHudRoot == null)
        {
            Debug.LogWarning("[BattleSceneLoader] Monster HUD 참조가 없습니다.");
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
        if (playerHudRoot != null)
        {
            for (int i = playerHudRoot.childCount - 1; i >= 0; i--)
                Destroy(playerHudRoot.GetChild(i).gameObject);
        }

        if (monsterHudRoot != null)
        {
            for (int i = monsterHudRoot.childCount - 1; i >= 0; i--)
                Destroy(monsterHudRoot.GetChild(i).gameObject);
        }
    }

    private void PrintPartyData()
    {
        var party = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            //Debug.Log($"Slot {i}: {party.GetCharacterId(i)} / Grid {party.GetGridIndex(i)}");
        }
    }
}