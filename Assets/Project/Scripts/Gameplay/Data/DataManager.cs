using UnityEngine;
using Relic.Gameplay.Data;

public class DataManager : Singleton<DataManager>
{
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
        base.Awake();

        if (IsDuplicateInstance)
            return;
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

        SkillEquipService = new SkillEquipService(CharacterRuntimeStore);
    }
}
