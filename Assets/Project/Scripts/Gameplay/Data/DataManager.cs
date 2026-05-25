using UnityEngine;
using Relic.Gameplay.Data;

public class DataManager : Singleton<DataManager>
{
    [Header("Asset Databases")]
    [SerializeField] private CharacterPrefabDatabase characterPrefabDatabase;
    [SerializeField] private SkillIconDatabase skillIconDatabase;
    [SerializeField] private MonsterPrefabDatabase monsterPrefabDatabase;
    [SerializeField] private ActionTypeIconDatabase actionTypeIconDatabase;
    [SerializeField] private CharacterIconDatabase characterIconDatabase;
    [SerializeField] private RuneIconDatabase runeIconDatabase;
    
    private DataBootstrap dataBootstrap = new();

    public CharacterDatabase CharacterDatabase => dataBootstrap.CharacterDatabase;
    public CharacterIconDatabase CharacterIconDatabase => characterIconDatabase;
    public CharacterPrefabDatabase CharacterPrefabDatabase => characterPrefabDatabase;
    public SkillDatabase SkillDatabase => dataBootstrap.SkillDatabase;
    public SkillIconDatabase SkillIconDatabase => skillIconDatabase;
    public RangeDatabase RangeDatabase => dataBootstrap.RangeDatabase;
    public RuneIconDatabase RuneIconDatabase => runeIconDatabase;
    public EffectDatabase EffectDatabase => dataBootstrap.EffectDatabase;
    public ActionTypeIconDatabase ActionTypeIconDatabase => actionTypeIconDatabase;
    public BattleMapDatabase BattleMapDatabase => dataBootstrap.BattleMapDatabase;
    public MonsterDatabase MonsterDatabase => dataBootstrap.MonsterDatabase;
    public MapDatabase MapDatabase => dataBootstrap.MapDatabase;
    public SkillEnhanceDatabase SkillEnhanceDatabase => dataBootstrap.SkillEnhanceDatabase;
    public MonsterSkillDatabase MonsterSkillDatabase => dataBootstrap.MonsterSkillDatabase;
    public RuneDatabase RuneDatabase => dataBootstrap.RuneDatabase;
    public CharacterRuntimeStore CharacterRuntimeStore { get; private set; } = new();
    public PartyRuntimeStore PartyRuntimeStore { get; private set; } = new();
    public SkillRuntimeStore SkillRuntimeStore { get; private set; } = new();
    public SkillEquipService SkillEquipService { get; private set; }
    public MapRuntimeStore MapRuntimeStore { get; private set; } = new();
    
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
        dataBootstrap.SetMonsterPrefabDatabase(monsterPrefabDatabase);
        dataBootstrap.SetCharacterIconDatabase(characterIconDatabase);
        dataBootstrap.LoadAllData();

        SkillEquipService = new SkillEquipService(CharacterRuntimeStore);

        Debug.Log("[DataManager] Initialize Complete");
    }
}