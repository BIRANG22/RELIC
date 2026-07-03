using UnityEngine;
using Relic.Gameplay.Data;

public class RestRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;
    [SerializeField] private float allySpawnScale = 0.7f;

    [Header("Upgrade")]
    [SerializeField] private SkillUpgradePanel upgradePanel;

    [Header("Shop")]
    [SerializeField] private RestRoomShopPanel shopPanel;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;

    [Header("Player HUD")]
    [SerializeField] private Transform playerHudRoot;
    [SerializeField] private PlayerHUDSlot playerHudPrefab;
    [SerializeField] private Transform[] playerHudPositionAnchors = new Transform[3];
    [SerializeField] private float playerHudScale = 0.4f;
    [SerializeField] private bool autoFindPlayerHudReferences = true;

    private bool isRestUsed;
    private BattleUnitAnimator[] spawnedAllyAnimators;
    private PlayerHUDSlot[] spawnedPlayerHuds;

    private bool IsRestActionLocked => isRestUsed;

    private void Awake()
    {
        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
    }

    private void OnEnable()
    {
        isRestUsed = false;
        if (upgradePanel != null)
            upgradePanel.ResetRestRoomUpgradeLimit();

        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
        EnsurePlayerHudReferences();
        SetNextButtonVisible(false);
        SpawnPartyAllies();
        SpawnPlayerHUDs();
    }

    private void OnDisable()
    {
        ClearPlayerHUDs();
    }

    public void OnRestButtonClicked()
    {
        if (IsRestActionLocked)
            return;

        isRestUsed = true;
        if (upgradePanel != null)
            upgradePanel.Close();

        RecoverAllPartyHPToMax();
        RefreshPlayerHUDs();
        PlayHealVfxOnSpawnedAllies();
        SetNextButtonVisible(true);
    }

    public void OnUpgradeButtonClicked()
    {
        if (IsRestActionLocked)
            return;

        if (upgradePanel == null)
        {
            Debug.LogWarning("[RestRoomController] SkillUpgradePanel 없음");
            return;
        }

        isRestUsed = true;
        upgradePanel.Open();
        RefreshPlayerHUDs();
        SetNextButtonVisible(true);
    }

    public void OnTuningButtonClicked()
    {
        if (upgradePanel == null)
        {
            Debug.LogWarning("[RestRoomController] SkillUpgradePanel 없음");
            return;
        }

        upgradePanel.TuneSelectedSkill();
    }

    public void OnNextButtonClicked()
    {
        if (!isRestUsed)
            return;

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[RestRoomController] BattleSceneController not found");
    }

    private void RecoverAllPartyHPToMax()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData runtimeData))
            {
                continue;
            }

            if (!DataManager.Instance.CharacterDatabase.TryGet(
                    characterId,
                    out CharacterMasterData masterData))
            {
                continue;
            }

            runtimeData.MaxHP = masterData.MaxHP;
            runtimeData.CurrentHP = masterData.MaxHP;
        }

        Debug.Log("[RestRoomController] 모든 파티원 HP 회복 완료");
    }

    private void SpawnPartyAllies()
    {
        spawnedAllyAnimators = null;

        if (DataManager.Instance == null)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        spawnedAllyAnimators = new BattleUnitAnimator[allySpawnPoints.Length];

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point == null)
                continue;

            ClearPoint(point);

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetBattleEventWorldPrefab(characterId, out GameObject restRoomPrefab))
            {
                Debug.LogWarning($"[RestRoomController] Rest room world prefab not found: {characterId}");
                continue;
            }

            GameObject ally = Instantiate(restRoomPrefab, point);
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one * Mathf.Max(0f, allySpawnScale);

            spawnedAllyAnimators[i] = ally.GetComponentInChildren<BattleUnitAnimator>(true);
        }
    }

    private void PlayHealVfxOnSpawnedAllies()
    {
        CacheSpawnedAllyAnimatorsIfNeeded();

        if (spawnedAllyAnimators == null)
            return;

        for (int i = 0; i < spawnedAllyAnimators.Length; i++)
            spawnedAllyAnimators[i]?.PlayHeal();
    }

    private void SpawnPlayerHUDs()
    {
        ClearPlayerHUDs();
        EnsurePlayerHudReferences();

        if (DataManager.Instance == null)
            return;

        if (playerHudPrefab == null || playerHudRoot == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
            return;

        int hudCount = Mathf.Max(0, partyStore.MaxPartyCountValue);
        spawnedPlayerHuds = new PlayerHUDSlot[hudCount];

        for (int i = 0; i < hudCount; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData runtimeData))
            {
                continue;
            }

            Transform anchor = GetPlayerHudAnchor(i);

            if (anchor == null)
                anchor = playerHudRoot;

            ClearPlayerHudAnchor(anchor);

            PlayerHUDSlot hud = Instantiate(playerHudPrefab, anchor);
            ApplyPlayerHudTransform(hud, anchor);
            hud.Bind(runtimeData);
            hud.SetCommandSelected(false);

            spawnedPlayerHuds[i] = hud;
        }
    }

    private void RefreshPlayerHUDs()
    {
        if (spawnedPlayerHuds == null || spawnedPlayerHuds.Length == 0)
        {
            SpawnPlayerHUDs();
            return;
        }

        for (int i = 0; i < spawnedPlayerHuds.Length; i++)
        {
            if (spawnedPlayerHuds[i] != null)
                spawnedPlayerHuds[i].Refresh();
        }
    }

    private void ClearPlayerHUDs()
    {
        if (spawnedPlayerHuds != null)
        {
            for (int i = spawnedPlayerHuds.Length - 1; i >= 0; i--)
            {
                if (spawnedPlayerHuds[i] != null)
                    Destroy(spawnedPlayerHuds[i].gameObject);
            }
        }

        spawnedPlayerHuds = null;

        if (playerHudPositionAnchors == null)
            return;

        for (int i = 0; i < playerHudPositionAnchors.Length; i++)
            ClearPlayerHudAnchor(playerHudPositionAnchors[i]);
    }

    private void ClearPlayerHudAnchor(Transform anchor)
    {
        if (anchor == null)
            return;

        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            Transform child = anchor.GetChild(i);

            if (child != null && child.GetComponent<PlayerHUDSlot>() != null)
                Destroy(child.gameObject);
        }
    }

    private void ApplyPlayerHudTransform(PlayerHUDSlot hud, Transform anchor)
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
            rect.localScale = Vector3.one * Mathf.Max(0f, playerHudScale);
        }

        hudTransform.localPosition = Vector3.zero;
        hudTransform.localRotation = Quaternion.identity;
        hudTransform.localScale = Vector3.one * Mathf.Max(0f, playerHudScale);
        hud.SetBaseScale(Vector3.one * Mathf.Max(0f, playerHudScale));
    }

    private Transform GetPlayerHudAnchor(int index)
    {
        EnsurePlayerHudReferences();

        if (index < 0)
            return null;

        if (playerHudPositionAnchors != null && index < playerHudPositionAnchors.Length)
        {
            Transform anchor = playerHudPositionAnchors[index];

            if (anchor != null)
                return anchor;
        }

        if (playerHudRoot != null && index < playerHudRoot.childCount)
            return playerHudRoot.GetChild(index);

        return null;
    }

    private void EnsurePlayerHudReferences()
    {
        if (!autoFindPlayerHudReferences)
            return;

        if (playerHudRoot == null)
        {
            Transform rootTransform = FindSceneTransformByName("PlayerHUD_Root");

            if (rootTransform != null)
                playerHudRoot = rootTransform;
        }

        if (playerHudRoot == null)
            return;

        if (playerHudPositionAnchors == null || playerHudPositionAnchors.Length < 3)
            playerHudPositionAnchors = new Transform[3];

        for (int i = 0; i < playerHudPositionAnchors.Length; i++)
        {
            if (playerHudPositionAnchors[i] != null)
                continue;

            string anchorName = "HUD_Pos_" + (i + 1).ToString("00");
            Transform anchor = FindDirectChildByName(playerHudRoot, anchorName);

            if (anchor != null)
                playerHudPositionAnchors[i] = anchor;
            else if (i < playerHudRoot.childCount)
                playerHudPositionAnchors[i] = playerHudRoot.GetChild(i);
        }
    }

    private Transform FindDirectChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child != null && child.name == targetName)
                return child;
        }

        return null;
    }

    private void CacheSpawnedAllyAnimatorsIfNeeded()
    {
        if (spawnedAllyAnimators != null && spawnedAllyAnimators.Length > 0)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        spawnedAllyAnimators = new BattleUnitAnimator[allySpawnPoints.Length];

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point != null)
                spawnedAllyAnimators[i] = point.GetComponentInChildren<BattleUnitAnimator>(true);
        }
    }

    private void ClearPoint(Transform point)
    {
        for (int i = point.childCount - 1; i >= 0; i--)
            Destroy(point.GetChild(i).gameObject);
    }

    private void EnsureShopPanelSpawner()
    {
        if (shopPanel != null)
            return;

        shopPanel = Object.FindFirstObjectByType<RestRoomShopPanel>(FindObjectsInactive.Include);

        if (shopPanel != null)
            return;

        Transform shopPanelTransform = FindSceneTransformByName("ShopPanel");

        if (shopPanelTransform == null)
            return;

        shopPanel = shopPanelTransform.GetComponent<RestRoomShopPanel>();

        if (shopPanel == null)
            shopPanel = shopPanelTransform.gameObject.AddComponent<RestRoomShopPanel>();
    }

    private void EnsureNextButtonRoot()
    {
        if (nextButtonRoot != null)
            return;

        Transform nextButtonTransform = FindSceneTransformByName("NextButton");

        if (nextButtonTransform != null)
            nextButtonRoot = nextButtonTransform.gameObject;
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private Transform FindSceneTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                !candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            if (string.Equals(candidate.name, targetName, System.StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[RestRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }
}
