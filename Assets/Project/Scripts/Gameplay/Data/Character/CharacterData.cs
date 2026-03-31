using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData_", menuName = "Game/Character/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string characterId;
    [SerializeField] private string displayName;
    [SerializeField] private CharacterClassType classType;

    [Header("Presentation")]
    [SerializeField] private Sprite portrait;
    [SerializeField] private GameObject worldPrefab;

    [Header("Base Stats")]
    [SerializeField] private CharacterStatBlock baseStats = new CharacterStatBlock();

    [Header("Default Loadout")]
    [SerializeField] private List<CharacterSkillEntry> defaultSkills = new();
    [SerializeField] private List<CharacterItemEntry> defaultItems = new();

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public CharacterClassType ClassType => classType;
    public Sprite Portrait => portrait;
    public GameObject WorldPrefab => worldPrefab;
    public CharacterStatBlock BaseStats => baseStats;
    public IReadOnlyList<CharacterSkillEntry> DefaultSkills => defaultSkills;
    public IReadOnlyList<CharacterItemEntry> DefaultItems => defaultItems;
}