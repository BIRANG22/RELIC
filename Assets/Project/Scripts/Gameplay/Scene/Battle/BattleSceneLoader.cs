using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleSceneLoader : MonoBehaviour
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
    [SerializeField] private float monsterHudX = 50f;
    private IEnumerator Start()
    {
        yield return null;

        if (DataManager.Instance == null)
        {
            Debug.LogError("[BattleSceneLoader] DataManager가 없습니다.");
            yield break;
        }

        DataManager.Instance.Initialize();

        yield return null;

        PrintPartyData();

        if (!DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            if (!createDebugDataIfEmpty)
            {
                Debug.LogError("[BattleSceneLoader] 파티 데이터가 없습니다.");
                yield break;
            }

            if (debugDataProvider == null)
            {
                Debug.LogError("[BattleSceneLoader] DebugDataProvider가 없습니다.");
                yield break;
            }

            debugDataProvider.CreateDebugData();
            PrintPartyData();
        }

        yield return null;

        SpawnPlayersAndHUD();
        SpawnMonstersAndHUD();
    }

    private void SpawnPlayersAndHUD()
    {
        List<CharacterRuntimeData> playerRuntimes =
            unitSpawner.SpawnFromRuntimeData();

        if (playerRuntimes == null)
            return;

        for (int i = 0; i < playerRuntimes.Count; i++)
        {
            CreatePlayerHUD(playerRuntimes[i], i);
        }
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
    }

    private void SpawnMonstersAndHUD()
    {
        var mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
        {
            Debug.LogError("[BattleSceneLoader] CurrentMapId가 없습니다.");
            return;
        }

        var mapData = DataManager.Instance.MapDatabase.Get(mapRuntime.CurrentMapId);

        if (mapData == null || string.IsNullOrWhiteSpace(mapData.BattleMapId))
        {
            Debug.LogError($"[BattleSceneLoader] BattleMapId가 없습니다. MapId: {mapRuntime.CurrentMapId}");
            return;
        }

        var spawns = DataManager.Instance.BattleMapDatabase.GetSpawns(mapData.BattleMapId);

        int index = 0;

        foreach (var spawnData in spawns)
        {
            MonsterRuntimeData monsterRuntime = monsterSpawner.Spawn(spawnData);

            if (monsterRuntime != null)
            {
                CreateMonsterHUD(monsterRuntime, index);
                index++;
            }
        }
    }

    private void CreateMonsterHUD(MonsterRuntimeData runtimeData, int index)
    {
        if (runtimeData == null)
            return;

        if (monsterHudPrefab == null || monsterHudRoot == null)
        {
            Debug.LogWarning("[BattleSceneLoader] Monster HUD 참조가 없습니다.");
            return;
        }

        MonsterHUDSlot hud = Instantiate(monsterHudPrefab, monsterHudRoot);

        RectTransform rect = hud.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localScale = Vector3.one * hudScale;
            rect.anchoredPosition = new Vector2(
                monsterHudX,
                hudStartY + index * hudYGap
            );
        }

        hud.Bind(runtimeData);
    }

    private void PrintPartyData()
    {
        var party = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            Debug.Log($"Slot {i}: {party.GetCharacterId(i)} / Grid {party.GetGridIndex(i)}");
        }
    }
}