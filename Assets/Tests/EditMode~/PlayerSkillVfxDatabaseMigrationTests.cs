using System;
using System.IO;
using NUnit.Framework;

public class PlayerSkillVfxDatabaseMigrationTests
{
    private const string SkillVfxDatabasePath = "Assets/DB/SkillVfxDatabase.asset";

    [TestCase("S_Ability_01", "e60bee9487d55684db191b1b1c1eb04d")]
    [TestCase("S_Ability_02", "e60bee9487d55684db191b1b1c1eb04d")]
    [TestCase("S_Ability_03", "cd098c4abf4c0094b9c18e2d5c90dfe0")]
    [TestCase("S_Ability_04", "cd098c4abf4c0094b9c18e2d5c90dfe0")]
    [TestCase("S_Ability_05", "d9e677191abd37b4ea44b0c2a095d363")]
    [TestCase("S_Ability_06", "d9e677191abd37b4ea44b0c2a095d363")]
    [TestCase("S_Ability_07", "ad2188fbf0e2d7745911825db1f2644e")]
    [TestCase("S_Ability_08", "ad2188fbf0e2d7745911825db1f2644e")]
    [TestCase("S_Ability_09", "7d62d8b59c052e74f841fe794b1df8a6")]
    [TestCase("S_Ability_10", "7d62d8b59c052e74f841fe794b1df8a6")]
    [TestCase("S_Ability_11", "29f13300e2979c949905f4b38ac44c24")]
    [TestCase("S_Ability_12", "29f13300e2979c949905f4b38ac44c24")]
    [TestCase("S_Ability_13", "8940258aeab2fc4478a1717538600122")]
    [TestCase("S_Ability_14", "8940258aeab2fc4478a1717538600122")]
    [TestCase("S_Ability_17", "69ccb2bf4a2fae641bfda27160ed0dd5")]
    [TestCase("S_Ability_18", "69ccb2bf4a2fae641bfda27160ed0dd5")]
    [TestCase("S_Core_63", "84666d4664e176142abb5f53fecd97cc")]
    [TestCase("S_Core_67", "e73296d74c8fe6f41ae13018828efcf0")]
    [TestCase("S_Core_77", "d6ecd58b7ed664f459ec6d2d06b7ffeb")]
    [TestCase("S_Core_78", "d6ecd58b7ed664f459ec6d2d06b7ffeb")]
    [TestCase("S_Core_79", "a168d5406d649fb42a013779f4db911b")]
    [TestCase("S_Core_80", "a168d5406d649fb42a013779f4db911b")]
    public void SkillVfxDatabase_ContainsPlayerSkillVfx(string skillId, string prefabGuid)
    {
        string yaml = File.ReadAllText(SkillVfxDatabasePath);
        string entry = GetSkillEntry(yaml, skillId);

        Assert.That(entry, Does.Contain($"guid: {prefabGuid}"));
    }

    [TestCase("Assets/Project/PrefabsR/Character/A/A_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/B/B_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/C/C_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/C/Cha_03_idle_0.prefab")]
    public void PlayerSkillPresentations_DoNotKeepSkillVfxPrefabReferences(string prefabPath)
    {
        string yaml = File.ReadAllText(prefabPath);

        AssertPlayerSkillSlotHasNoPrefab(yaml, "attack1");
        AssertPlayerSkillSlotHasNoPrefab(yaml, "attack2");
        AssertPlayerSkillSlotHasNoPrefab(yaml, "attack3");
        AssertPlayerSkillSlotHasNoPrefab(yaml, "skill");
    }

    [Test]
    public void SkillAttackOverrideDatabase_TrimsAbility03SkillId()
    {
        string yaml = File.ReadAllText("Assets/DB/SkillAttackOverrideDatabase.asset");

        Assert.That(yaml, Does.Contain("SkillId: S_Ability_03"));
        Assert.That(yaml, Does.Not.Contain("SkillId: 'S_Ability_03 '"));
    }

    private static string GetSkillEntry(string yaml, string skillId)
    {
        string marker = $"- SkillId: {skillId}";
        int start = yaml.IndexOf(marker, StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing SkillVfxDatabase entry: {skillId}");

        int next = yaml.IndexOf("\n  - SkillId:", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? yaml.Substring(start) : yaml.Substring(start, next - start);
    }

    private static void AssertPlayerSkillSlotHasNoPrefab(string yaml, string slotName)
    {
        string marker = $"    {slotName}:";
        int start = yaml.IndexOf(marker, StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing playerSkillPresentations.{slotName}");

        int prefabStart = yaml.IndexOf("prefab:", start, StringComparison.Ordinal);
        Assert.That(prefabStart, Is.GreaterThanOrEqualTo(0), $"Missing playerSkillPresentations.{slotName}.vfx.prefab");

        int prefabLineEnd = yaml.IndexOf('\n', prefabStart);
        string prefabLine = prefabLineEnd < 0 ? yaml.Substring(prefabStart) : yaml.Substring(prefabStart, prefabLineEnd - prefabStart);

        Assert.That(prefabLine, Does.Contain("prefab: {fileID: 0}"));
        Assert.That(prefabLine, Does.Not.Contain("type: 3}"));
    }
}
