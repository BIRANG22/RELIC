using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;

public sealed class PlaceholderInteractionSfxTests
{
    [Test]
    public void SettingWarningMessage_RemainsRuntimeOwnedAfterLocalizedWarning()
    {
        GameObject root = new GameObject("SettingWarning");
        GameObject labelObject = new GameObject("Message");
        try
        {
            root.SetActive(false);
            labelObject.transform.SetParent(root.transform);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            LocalizedTMPText staticLocalizer = labelObject.AddComponent<LocalizedTMPText>();
            DynamicLocalizedTMPText dynamicLocalizer = labelObject.AddComponent<DynamicLocalizedTMPText>();
            LocalizeStringEvent stringEvent = labelObject.AddComponent<LocalizeStringEvent>();
            SettingWarningUI warning = root.AddComponent<SettingWarningUI>();
            typeof(SettingWarningUI)
                .GetField("messageText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(warning, label);

            warning.Show("다른 경고 문구");

            Assert.That(root.GetComponent<LocalizationAutoBindingIgnore>(), Is.Not.Null);
            Assert.That(staticLocalizer.enabled, Is.False);
            Assert.That(dynamicLocalizer.enabled, Is.False);
            Assert.That(stringEvent.enabled, Is.False);
            Assert.That(label.text, Is.EqualTo("다른 경고 문구"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SkillChangeSound_PlaysOnlyWhenSkillIdActuallyChanges()
    {
        Assert.That(SkillSettingPanel.ShouldPlaySkillChange("skill.a", "skill.b"), Is.True);
        Assert.That(SkillSettingPanel.ShouldPlaySkillChange("skill.a", "SKILL.A"), Is.False);
        Assert.That(SkillSettingPanel.ShouldPlaySkillChange("", "skill.a"), Is.True);
    }

    [Test]
    public void BattleTutorialTextSound_PlaysOnlyForANewNonEmptyLocalizationKey()
    {
        Assert.That(BattleFirstTutorialController.ShouldPlayTextSound("", "tutorial.step.1"), Is.True);
        Assert.That(BattleFirstTutorialController.ShouldPlayTextSound("tutorial.step.1", "tutorial.step.2"), Is.True);
        Assert.That(BattleFirstTutorialController.ShouldPlayTextSound("tutorial.step.1", "tutorial.step.1"), Is.False);
        Assert.That(BattleFirstTutorialController.ShouldPlayTextSound("tutorial.step.1", ""), Is.False);
    }

    [Test]
    public void BattleTutorialTextSound_UsesDefaultIdWhenSerializedValueIsEmpty()
    {
        Assert.That(
            BattleFirstTutorialController.ResolveTutorialTextSoundId(null),
            Is.EqualTo(AudioIds.Sfx.TutorialTextAdvance));
        Assert.That(
            BattleFirstTutorialController.ResolveTutorialTextSoundId("  "),
            Is.EqualTo(AudioIds.Sfx.TutorialTextAdvance));
        Assert.That(
            BattleFirstTutorialController.ResolveTutorialTextSoundId("custom.tutorial.text"),
            Is.EqualTo("custom.tutorial.text"));
    }

    [Test]
    public void BattleDialogueAdvanceSound_UsesTutorialTextAdvanceId()
    {
        Assert.That(
            TutorialBattleSequence.ResolveDialogueAdvanceSoundId(null),
            Is.EqualTo(AudioIds.Sfx.TutorialTextAdvance));
        Assert.That(
            TutorialBattleSequence.ResolveDialogueAdvanceSoundId("custom.dialogue.advance"),
            Is.EqualTo("custom.dialogue.advance"));
    }

    [Test]
    public void CharacterSelectionChangeSound_PlaysOnlyForUserInitiatedChanges()
    {
        Assert.That(CharPick.ShouldPlayCharacterChangeSound(true, "Char_01", "Char_02"), Is.True);
        Assert.That(CharPick.ShouldPlayCharacterChangeSound(false, "Char_01", "Char_02"), Is.False);
        Assert.That(CharPick.ShouldPlayCharacterChangeSound(true, "Char_01", "Char_01"), Is.False);
    }

    [Test]
    public void EquipmentAreaClick_IsConsumedEvenWhenSlotActionDoesNothing()
    {
        Assert.That(CharBtn.ShouldConsumeInfoEquipmentAreaClick(true, true), Is.True);
        Assert.That(CharBtn.ShouldConsumeInfoEquipmentAreaClick(true, false), Is.False);
        Assert.That(CharBtn.ShouldConsumeInfoEquipmentAreaClick(false, true), Is.False);
    }

    [Test]
    public void EquipmentAreaPointer_IsExcludedFromCharacterButtonPointerSounds()
    {
        GameObject infoPanel = new GameObject("Info_Panel");
        GameObject characterSelect = new GameObject("CharacterSelect");
        GameObject root = new GameObject("CharBtn");
        GameObject relic = new GameObject("Relic");
        GameObject icon = new GameObject("Icon");
        GameObject unrelated = new GameObject("Background");
        try
        {
            characterSelect.transform.SetParent(infoPanel.transform);
            root.transform.SetParent(characterSelect.transform);
            CharBtn button = root.AddComponent<CharBtn>();
            relic.transform.SetParent(root.transform);
            icon.transform.SetParent(relic.transform);
            unrelated.transform.SetParent(root.transform);

            Assert.That(button.IsInfoEquipmentAreaTransform(icon.transform), Is.True);
            Assert.That(button.IsInfoEquipmentAreaTransform(unrelated.transform), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(infoPanel);
        }
    }

    [Test]
    public void ClickSoundSuppression_DisablesOnlyClickSoundOnSupportedComponents()
    {
        GameObject root = new GameObject("SoundSuppressionTest");
        try
        {
            DBAudioSource dbAudio = root.AddComponent<DBAudioSource>();
            UISoundTrigger uiSound = root.AddComponent<UISoundTrigger>();
            ButtonAnimationCoroutine animation = root.AddComponent<ButtonAnimationCoroutine>();

            dbAudio.SetClickSoundEnabled(false);
            uiSound.SetClickSoundEnabled(false);
            animation.SetClickSoundEnabled(false);

            Assert.That(new SerializedObject(dbAudio).FindProperty("playOnClick").boolValue, Is.False);
            Assert.That(new SerializedObject(uiSound).FindProperty("playClickSound").boolValue, Is.False);
            Assert.That(new SerializedObject(animation).FindProperty("playClickSound").boolValue, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AudioIds_ExposeEveryPlaceholderInteractionSound()
    {
        Assert.That(AudioIds.Sfx.TutorialTextAdvance, Is.EqualTo("ui.tutorial.text.advance"));
        Assert.That(AudioIds.Sfx.BattleMoveFlipClick, Is.EqualTo("battle.move-flip.click"));
        Assert.That(AudioIds.Sfx.BattleSkillResetCancel, Is.EqualTo("battle.skill.reset-cancel"));
        Assert.That(AudioIds.Sfx.LobbyCharacterSelectionChange, Is.EqualTo("lobby.character.selection.change"));
        Assert.That(AudioIds.Sfx.BattleTimelineMarkerClick, Is.EqualTo("battle.timeline.marker.click"));
        Assert.That(AudioIds.Sfx.LobbyRuneEquip, Is.EqualTo("lobby.rune.equip"));
        Assert.That(AudioIds.Sfx.LobbySkillChange, Is.EqualTo("lobby.skill.change"));
        Assert.That(AudioIds.Sfx.LobbySkillMaterialCombine, Is.EqualTo("lobby.skill-material.combine"));
        Assert.That(AudioIds.Sfx.LobbySkillMaterialRemove, Is.EqualTo("lobby.skill-material.remove"));
        Assert.That(AudioIds.Sfx.LobbyCharacterRemove, Is.EqualTo("lobby.character.remove"));
        Assert.That(AudioIds.Sfx.BattleGridSelectionDrop, Is.EqualTo("battle.grid-selection.drop"));
        Assert.That(AudioIds.Sfx.LobbyEquipmentEquip, Is.EqualTo("lobby.equipment.equip"));
        Assert.That(AudioIds.Sfx.ExplorationReportCountUp, Is.EqualTo("ui.exploration-report.count-up"));
    }

    [Test]
    public void SoundDatabase_MapsEveryPlaceholderIdToSharedTemporaryClip()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>("Assets/DB/SoundDatabase.asset");
        AudioClip expectedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Project/Art/Sound/SFX_TEMP_WRONG_SOUND.mp3");
        Assert.That(database, Is.Not.Null);
        Assert.That(expectedClip, Is.Not.Null);

        var expectedIds = new HashSet<string>
        {
            AudioIds.Sfx.TutorialTextAdvance,
            AudioIds.Sfx.BattleMoveFlipClick,
            AudioIds.Sfx.BattleSkillResetCancel,
            AudioIds.Sfx.LobbyCharacterSelectionChange,
            AudioIds.Sfx.BattleTimelineMarkerClick,
            AudioIds.Sfx.LobbyRuneEquip,
            AudioIds.Sfx.LobbySkillChange,
            AudioIds.Sfx.LobbySkillMaterialCombine,
            AudioIds.Sfx.LobbySkillMaterialRemove,
            AudioIds.Sfx.LobbyCharacterRemove,
            AudioIds.Sfx.BattleGridSelectionDrop,
            AudioIds.Sfx.LobbyEquipmentEquip,
            AudioIds.Sfx.ExplorationReportCountUp,
        };

        SerializedProperty entries = new SerializedObject(database).FindProperty("sfxList");
        Assert.That(entries, Is.Not.Null);
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string id = entry.FindPropertyRelative("id").stringValue;
            if (!expectedIds.Remove(id))
                continue;

            Assert.That(
                entry.FindPropertyRelative("clip").objectReferenceValue,
                Is.SameAs(expectedClip),
                id);
        }

        Assert.That(expectedIds, Is.Empty);
    }
}
