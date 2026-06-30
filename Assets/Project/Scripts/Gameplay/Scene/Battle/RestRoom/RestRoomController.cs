using UnityEngine;
using Relic.Gameplay.Data;

public class RestRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;

    [Header("Upgrade")]
    [SerializeField] private SkillUpgradePanel upgradePanel;

    [Header("Shop")]
    [SerializeField] private RestRoomShopPanel shopPanel;

    private bool isRestUsed;

    private void Awake()
    {
        EnsureShopPanelSpawner();
    }

    private void OnEnable()
    {
        isRestUsed = false;
        if (upgradePanel != null)
            upgradePanel.ResetRestRoomUpgradeLimit();

        EnsureShopPanelSpawner();
        SpawnPartyAllies();
    }

    public void OnRestButtonClicked()
    {
        if (isRestUsed)
            return;

        isRestUsed = true;

        RecoverAllPartyHPToMax();
        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[RestRoomController] BattleSceneController 없음");
    }

    public void OnUpgradeButtonClicked()
    {
        if (upgradePanel == null)
        {
            Debug.LogWarning("[RestRoomController] SkillUpgradePanel 없음");
            return;
        }

        upgradePanel.Open();
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
        if (DataManager.Instance == null)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point == null)
                continue;

            ClearPoint(point);

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetBattleEventWorldPrefab(characterId, out GameObject battleEventPrefab))
            {
                Debug.LogWarning($"[RestRoomController] Battle event world prefab not found: {characterId}");
                continue;
            }

            GameObject ally = Instantiate(battleEventPrefab, point);
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one;
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
