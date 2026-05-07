using UnityEngine;
using Relic.Gameplay.Data;

public class DataManager : Singleton<DataManager>
{
    [Header("Asset Databases")]
    [SerializeField] private CharacterPrefabDatabase characterPrefabDatabase;
    [SerializeField] private SkillIconDatabase skillIconDatabase;
    [SerializeField] private MonsterPrefabDatabase monsterPrefabDatabase;

    private DataBootstrap dataBootstrap = new();

    public CharacterDatabase CharacterDatabase => dataBootstrap.CharacterDatabase;
    public SkillDatabase SkillDatabase => dataBootstrap.SkillDatabase;
    public CharacterRuntimeStore CharacterRuntimeStore { get; private set; } = new();
    public PartyRuntimeStore PartyRuntimeStore { get; private set; } = new();
    public SkillRuntimeStore SkillRuntimeStore { get; private set; } = new();
    public SkillEquipService SkillEquipService { get; private set; }
    
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
        dataBootstrap.LoadAllData();

        SkillEquipService = new SkillEquipService(CharacterRuntimeStore);

        Debug.Log("[DataManager] Initialize Complete");
    }
}