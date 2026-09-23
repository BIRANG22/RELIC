using UnityEngine;
using Relic.Gameplay.Data;

public class DataManager : Singleton<DataManager>
{
    private static ErosionIconDatabase cachedErosionIconDatabase;

    [Header("Asset Databases")]
    [SerializeField] private CharacterPrefabDatabase characterPrefabDatabase;
    [SerializeField] private SkillIconDatabase skillIconDatabase;
    [SerializeField] private RelicIconDatabase relicIconDatabase;
    [SerializeField] private MonsterPrefabDatabase monsterPrefabDatabase;
    [SerializeField] private MonsterIconDatabase monsterIconDatabase;
    [SerializeField] private ActionTypeIconDatabase actionTypeIconDatabase;
    [SerializeField] private CharacterIconDatabase characterIconDatabase;
    [SerializeField] private RuneIconDatabase runeIconDatabase;
    [SerializeField] private StatusEffectIconDatabase statusEffectIconDatabase;
    [SerializeField] private MapNodeIconDatabase mapNodeIconDatabase;
    [SerializeField] private SkillRangeIconDatabase skillRangeIconDatabase;
    [SerializeField] private ItemIconDatabase itemIconDatabase;
    [SerializeField] private GridEffectSpriteDatabase gridEffectSpriteDatabase;
    [SerializeField] private SkillAttackOverrideDatabase skillAttackOverrideDatabase;
    [SerializeField] private SkillVfxDatabase skillVfxDatabase;
    [SerializeField] private MapVisualDatabase mapVisualDatabase;
    [SerializeField] private ErosionIconDatabase erosionIconDatabase;

    private DataBootstrap dataBootstrap = new();

    public CharacterDatabase CharacterDatabase => dataBootstrap.CharacterDatabase;
    public CharacterIconDatabase CharacterIconDatabase => characterIconDatabase;
    public CharacterPrefabDatabase CharacterPrefabDatabase => characterPrefabDatabase;
    public SkillDatabase SkillDatabase => dataBootstrap.SkillDatabase;
    public SkillIconDatabase SkillIconDatabase => skillIconDatabase;
    public SkillRangeIconDatabase SkillRangeIconDatabase => skillRangeIconDatabase;
    public RelicIconDatabase RelicIconDatabase => relicIconDatabase;
    public RangeDatabase RangeDatabase => dataBootstrap.RangeDatabase;
    public RuneIconDatabase RuneIconDatabase => runeIconDatabase;
    public EffectDatabase EffectDatabase => dataBootstrap.EffectDatabase;
    public RelicDatabase RelicDatabase => dataBootstrap.RelicDatabase;
    public CompoundDatabase CompoundDatabase => dataBootstrap.CompoundDatabase;
    public StatusEffectIconDatabase StatusEffectIconDatabase => statusEffectIconDatabase;
    public ActionTypeIconDatabase ActionTypeIconDatabase => actionTypeIconDatabase;
    public MapNodeIconDatabase MapNodeIconDatabase => mapNodeIconDatabase;
    public BattleMapDatabase BattleMapDatabase => dataBootstrap.BattleMapDatabase;
    public MonsterDatabase MonsterDatabase => dataBootstrap.MonsterDatabase;
    public MonsterIconDatabase MonsterIconDatabase => monsterIconDatabase;
    public MapDatabase MapDatabase => dataBootstrap.MapDatabase;
    public EventDatabase EventDatabase => dataBootstrap.EventDatabase;
    public MonsterSkillDatabase MonsterSkillDatabase => dataBootstrap.MonsterSkillDatabase;
    public MonsterPatternInfoDatabase MonsterPatternInfoDatabase => dataBootstrap.MonsterPatternInfoDatabase;
    public RuneDatabase RuneDatabase => dataBootstrap.RuneDatabase;
    public RewardTableDatabase RewardTableDatabase => dataBootstrap.RewardTableDatabase;
    public ItemDatabase ItemDatabase => dataBootstrap.ItemDatabase;
    public ItemIconDatabase ItemIconDatabase => itemIconDatabase;
    public GridEffectDatabase GridEffectDatabase => dataBootstrap.GridEffectDatabase;
    public GridEffectSpriteDatabase GridEffectSpriteDatabase => gridEffectSpriteDatabase;
    public SkillAttackOverrideDatabase SkillAttackOverrideDatabase => skillAttackOverrideDatabase;
    public SkillVfxDatabase SkillVfxDatabase => skillVfxDatabase;
    public MapVisualDatabase MapVisualDatabase => mapVisualDatabase;
    public ErosionDatabase ErosionDatabase => dataBootstrap.ErosionDatabase;
    public ErosionIconDatabase ErosionIconDatabase => erosionIconDatabase;
    public CharacterRuntimeStore CharacterRuntimeStore { get; private set; } = new();
    public PartyRuntimeStore PartyRuntimeStore { get; private set; } = new();
    public SkillRuntimeStore SkillRuntimeStore { get; private set; } = new();
    public SkillEquipService SkillEquipService { get; private set; }
    public MapRuntimeStore MapRuntimeStore { get; private set; } = new();
    public PlayerRuntimeStore PlayerRuntimeStore { get; private set; } = new();
    public BattleRuntimeStore BattleRuntimeStore { get; private set; } = new();
    public LobbyRuntimeStore LobbyRuntimeStore { get; private set; } = new();
    protected override void Awake()
    {
        // 씬 전환 과정에서 새 DataManager가 유효한 ErosionIconDatabase를 가지고 있다면
        // 중복 오브젝트가 제거되기 전에 기존 싱글톤에 참조를 전달합니다.
        // 한 번 확인된 유효한 DB는 정적 캐시에 보관하여 이후 씬의 DataManager 참조가
        // 비어 있어도 전투/로비 UI에서 같은 DB를 계속 사용할 수 있게 합니다.
        DataManager existing = Instance;

        if (erosionIconDatabase != null)
        {
            cachedErosionIconDatabase = erosionIconDatabase;
        }
        else if (existing != null && existing.erosionIconDatabase != null)
        {
            erosionIconDatabase = existing.erosionIconDatabase;
            cachedErosionIconDatabase = existing.erosionIconDatabase;
        }
        else if (cachedErosionIconDatabase != null)
        {
            erosionIconDatabase = cachedErosionIconDatabase;
        }

        if (existing != null && existing != this && existing.erosionIconDatabase == null)
        {
            ErosionIconDatabase source = erosionIconDatabase != null
                ? erosionIconDatabase
                : cachedErosionIconDatabase;

            if (source != null)
            {
                existing.erosionIconDatabase = source;
                cachedErosionIconDatabase = source;
                source.Initialize();
            }
        }

        base.Awake();

        if (IsDuplicateInstance)
            return;

        RestoreCachedErosionIconDatabase();
    }

    private void RestoreCachedErosionIconDatabase()
    {
        if (erosionIconDatabase == null && cachedErosionIconDatabase != null)
            erosionIconDatabase = cachedErosionIconDatabase;

        if (erosionIconDatabase == null)
            return;

        cachedErosionIconDatabase = erosionIconDatabase;
        erosionIconDatabase.Initialize();
    }

    public void Initialize()
    {
        dataBootstrap.SetCharacterPrefabDatabase(characterPrefabDatabase);
        dataBootstrap.SetSkillIconDatabase(skillIconDatabase);
        dataBootstrap.SetSkillRangeIconDatabase(skillRangeIconDatabase);
        dataBootstrap.SetMonsterPrefabDatabase(monsterPrefabDatabase);
        dataBootstrap.SetCharacterIconDatabase(characterIconDatabase);
        dataBootstrap.SetMapNodeIconDatabase(mapNodeIconDatabase);

        dataBootstrap.LoadAllData();

        if (mapNodeIconDatabase != null)
            mapNodeIconDatabase.Initialize();

        if (monsterIconDatabase != null)
            monsterIconDatabase.Initialize();

        if (gridEffectSpriteDatabase != null)
            gridEffectSpriteDatabase.Initialize();

        if (skillAttackOverrideDatabase != null)
            skillAttackOverrideDatabase.Initialize();

        if (skillVfxDatabase != null)
            skillVfxDatabase.Initialize();

        if (mapVisualDatabase != null)
            mapVisualDatabase.Initialize();

        if (erosionIconDatabase != null)
            erosionIconDatabase.Initialize();

        SkillEquipService = new SkillEquipService(CharacterRuntimeStore);
    }
}
