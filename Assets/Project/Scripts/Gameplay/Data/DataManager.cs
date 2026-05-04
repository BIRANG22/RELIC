using UnityEngine;
using Relic.Gameplay.Data;

public class DataManager : Singleton<DataManager>
{
    private DataBootstrap dataBootstrap = new();

    public CharacterDatabase CharacterDatabase => dataBootstrap.CharacterDatabase;

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
        dataBootstrap.LoadAllData();

        SkillEquipService = new SkillEquipService(CharacterRuntimeStore);

        Debug.Log("[DataManager] Initialize Complete");
    }
}