using Relic.Gameplay.Data;
using UnityEngine;

/// <summary>씬 Transform을 배치 기준으로 사용하는 로비 전용 생성 지점.</summary>
[DisallowMultipleComponent]
public sealed class LobbyCharacterSpawnPoint : MonoBehaviour
{
    [SerializeField] private CharacterPrefabDatabase database;
    [SerializeField] private string characterId;
    [SerializeField] private bool flipX;
    [SerializeField] private int sortingLayerId;
    [SerializeField] private int frontSortingOrder = 2;
    [SerializeField] private int backSortingOrder = 1;
    [SerializeField] private bool overrideLayer;
    [SerializeField, Range(0, 31)] private int objectLayer;
    [SerializeField] private bool applyLayerToChildren = true;
    [SerializeField] private LobbyWorldObjectHoverName.HoverNameType hoverNameType;

    private GameObject spawnedInstance;
    private LobbyWorldCharacterSettingOpenObject owner;
    private bool initialized;

    public string CharacterId => characterId == null ? string.Empty : characterId.Trim();
    public bool FlipX => flipX;
    public int SortingLayerId => sortingLayerId;
    public int FrontSortingOrder => frontSortingOrder;
    public int BackSortingOrder => backSortingOrder;
    public bool OverrideLayer => overrideLayer;
    public int ObjectLayer => Mathf.Clamp(objectLayer, 0, 31);
    public bool ApplyLayerToChildren => applyLayerToChildren;
    public LobbyWorldObjectHoverName.HoverNameType HoverNameType => hoverNameType;

    public void Initialize(LobbyWorldCharacterSettingOpenObject targetOwner)
    {
        owner = targetOwner;
        initialized = true;
        Spawn();
        if (spawnedInstance != null)
            LobbyWorldCharacterSpawner.BindClicks(spawnedInstance, owner, CharacterId);
    }

    public GameObject Spawn()
    {
        if (spawnedInstance != null)
            return spawnedInstance;
        if (!isActiveAndEnabled)
            return null;
        if (database == null || string.IsNullOrWhiteSpace(CharacterId) ||
            !database.TryGetLobbyPrefab(CharacterId, out GameObject prefab))
        {
            Debug.LogWarning($"[LobbyCharacterSpawnPoint] DB/LobbyPrefab 연결을 확인하세요: {CharacterId}", this);
            return null;
        }

        spawnedInstance = LobbyWorldCharacterSpawner.Spawn(this, prefab, owner);
        return spawnedInstance;
    }

    private void OnEnable()
    {
        if (initialized)
            Spawn();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.4f);
    }
}
