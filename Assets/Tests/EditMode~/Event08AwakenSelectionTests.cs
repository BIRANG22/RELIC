using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class Event08AwakenSelectionTests
{
    [Test]
    public void CanSelectChoice_AllowsAwakenWhenUpgradeableEquippedMemoryExists()
    {
        EventChoiceExecutionContext context = new()
        {
            HasUpgradeableEquippedSkill = () => true
        };
        EventData choice = CreateAwakenChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.That(canSelect, Is.True);
        Assert.That(reason, Is.Empty);
    }

    [Test]
    public void CanSelectChoice_BlocksAwakenWhenOnlyInventoryMemoryExists()
    {
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = new BattleRuntimeData
            {
                SkillInventoryIds = new List<string> { "Skill_Base_001" }
            },
            HasUpgradeableEquippedSkill = () => false
        };
        EventData choice = CreateAwakenChoice();

        bool canSelect = EventChoiceExecutionService.CanSelect(choice, context, out string reason);

        Assert.That(canSelect, Is.False);
        Assert.That(reason, Does.Contain("장착"));
    }

    [Test]
    public void ExecuteChoice_AwakenUsesSelectedEquippedMemoryTarget()
    {
        EventChoiceSkillAwakenTarget target = new(
            "C_001",
            EventChoiceSkillSlotKind.Equipped,
            2,
            "Skill_Base_001",
            "Skill_Base_002");
        EventChoiceSkillAwakenTarget receivedTarget = default;
        EventChoiceExecutionContext context = new()
        {
            SelectedSkillAwakenTarget = target,
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 0f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget received, out string message) =>
            {
                receivedTarget = received;
                message = "기억 강화: Skill_Base_002";
                return true;
            }
        };
        EventData choice = CreateAwakenChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(receivedTarget.CharacterId, Is.EqualTo("C_001"));
            Assert.That(receivedTarget.SlotKind, Is.EqualTo(EventChoiceSkillSlotKind.Equipped));
            Assert.That(receivedTarget.SlotIndex, Is.EqualTo(2));
            Assert.That(receivedTarget.SkillId, Is.EqualTo("Skill_Base_001"));
            Assert.That(receivedTarget.UpgradeSkillId, Is.EqualTo("Skill_Base_002"));
            Assert.That(result.ResultMessage, Does.Contain("Skill_Base_002"));
        });
    }

    [Test]
    public void ExecuteChoice_AwakenSuccessTracksSelectedMemoryForFailureRollback()
    {
        EventChoiceSkillAwakenTarget target = new(
            "C_001",
            EventChoiceSkillSlotKind.Equipped,
            2,
            "Skill_Base_001",
            "Skill_Base_002");
        EventChoiceSessionState sessionState = new();
        EventChoiceExecutionContext context = new()
        {
            SessionState = sessionState,
            SelectedSkillAwakenTarget = target,
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 0f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget _, out string message) =>
            {
                message = "기억 강화: Skill_Base_002";
                return true;
            }
        };
        EventData choice = CreateAwakenChoice();
        choice.NextEventId = "Event_08_A";

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.NextEventId, Is.EqualTo("Event_08_A"));
            Assert.That(sessionState.AwakenedSkillTargets, Has.Count.EqualTo(1));
            Assert.That(sessionState.AwakenedSkillTargets[0].CharacterId, Is.EqualTo("C_001"));
            Assert.That(sessionState.AwakenedSkillTargets[0].SlotKind, Is.EqualTo(EventChoiceSkillSlotKind.Equipped));
            Assert.That(sessionState.AwakenedSkillTargets[0].SlotIndex, Is.EqualTo(2));
            Assert.That(sessionState.AwakenedSkillTargets[0].SkillId, Is.EqualTo("Skill_Base_001"));
            Assert.That(sessionState.AwakenedSkillTargets[0].UpgradeSkillId, Is.EqualTo("Skill_Base_002"));
        });
    }

    [Test]
    public void ExecuteChoice_AwakenFailureClearsNextEventAndRollsBackTrackedAwakenedMemories()
    {
        EventChoiceSkillAwakenTarget firstTarget = new(
            "C_001",
            EventChoiceSkillSlotKind.Ability,
            -1,
            "Skill_A_001",
            "Skill_A_002");
        EventChoiceSkillAwakenTarget secondTarget = new(
            "C_002",
            EventChoiceSkillSlotKind.Equipped,
            3,
            "Skill_B_001",
            "Skill_B_002");
        EventChoiceSessionState sessionState = new();
        sessionState.AwakenedSkillTargets.Add(firstTarget);
        sessionState.AwakenedSkillTargets.Add(secondTarget);

        bool upgradeCalled = false;
        List<EventChoiceSkillAwakenTarget> rollbackTargets = new();
        EventChoiceExecutionContext context = new()
        {
            SessionState = sessionState,
            SelectedSkillAwakenTarget = secondTarget,
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 1f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget _, out string message) =>
            {
                upgradeCalled = true;
                message = string.Empty;
                return true;
            },
            RollbackAwakenedSkills = (IReadOnlyList<EventChoiceSkillAwakenTarget> targets, out string message) =>
            {
                rollbackTargets.AddRange(targets);
                message = "이번 이벤트로 얻은 기억 2개를 잃었습니다.";
                return true;
            }
        };
        EventData choice = CreateAwakenChoice();
        choice.SuccessRate = "0%";
        choice.NextEventId = "Event_08_A";

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.NextEventId, Is.Empty);
            Assert.That(upgradeCalled, Is.False);
            Assert.That(rollbackTargets, Has.Count.EqualTo(2));
            Assert.That(rollbackTargets[0].CharacterId, Is.EqualTo("C_001"));
            Assert.That(rollbackTargets[1].CharacterId, Is.EqualTo("C_002"));
            Assert.That(sessionState.AwakenedSkillTargets, Is.Empty);
            Assert.That(result.ResultMessage, Does.Contain("잃었습니다"));
        });
    }

    [Test]
    public void ExecuteChoice_AwakenWithoutSelectedMemoryTargetIsRejected()
    {
        bool upgradeCalled = false;
        EventChoiceExecutionContext context = new()
        {
            HasUpgradeableEquippedSkill = () => true,
            RollChanceValue = () => 0f,
            UpgradeSelectedSkill = (EventChoiceSkillAwakenTarget _, out string message) =>
            {
                upgradeCalled = true;
                message = string.Empty;
                return true;
            }
        };
        EventData choice = CreateAwakenChoice();

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(upgradeCalled, Is.False);
            Assert.That(result.ResultMessage, Does.Contain("기억"));
        });
    }

    [Test]
    public void SkillAwakenPanel_OpenCreatesVisibleOptionForEveryCandidate()
    {
        GameObject root = CreateSceneBoundPanel(out EventSkillAwakenSelectionPanelUI panel);

        try
        {
            List<EventSkillAwakenSelectionPanelEntry> entries = new()
            {
                CreateEntry("C_001", EventChoiceSkillSlotKind.Ability, -1, "Skill_A_001", "Skill_A_002"),
                CreateEntry("C_002", EventChoiceSkillSlotKind.Equipped, 3, "Skill_B_001", "Skill_B_002")
            };

            bool opened = panel.Open(entries, _ => true, () => { });

            Assert.That(opened, Is.True);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.VisibleOptionCount, Is.EqualTo(entries.Count));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SkillAwakenPanel_TrySelectInvokesCallbackAndClosesPanel()
    {
        GameObject root = CreateSceneBoundPanel(out EventSkillAwakenSelectionPanelUI panel);

        try
        {
            EventSkillAwakenSelectionPanelEntry entry =
                CreateEntry("C_001", EventChoiceSkillSlotKind.Equipped, 2, "Skill_A_001", "Skill_A_002");
            EventChoiceSkillAwakenTarget selected = default;

            bool opened = panel.Open(
                new[] { entry },
                target =>
                {
                    selected = target;
                    return true;
                },
                () => { });

            bool didSelect = panel.TrySelect(entry.Target);

            Assert.That(opened, Is.True);
            Assert.That(didSelect, Is.True);
            Assert.That(selected.CharacterId, Is.EqualTo("C_001"));
            Assert.That(selected.SlotIndex, Is.EqualTo(2));
            Assert.That(selected.UpgradeSkillId, Is.EqualTo("Skill_A_002"));
            Assert.That(panel.IsOpen, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SkillAwakenPanel_OpenFailsWithoutSceneBoundPanelReferences()
    {
        GameObject root = new("AwakenPanel");

        try
        {
            EventSkillAwakenSelectionPanelUI panel = root.AddComponent<EventSkillAwakenSelectionPanelUI>();
            int initialChildCount = root.transform.childCount;

            bool opened = panel.Open(
                new[] { CreateEntry("C_001", EventChoiceSkillSlotKind.Ability, -1, "Skill_A_001", "Skill_A_002") },
                _ => true,
                () => { });

            Assert.That(opened, Is.False);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(root.transform.childCount, Is.EqualTo(initialChildCount));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static EventData CreateAwakenChoice()
    {
        return new EventData
        {
            ChoiceType = "Chance",
            SelectCondition = "미각성 기억 1개 이상",
            ResultType = "Awaken",
            ResultTarget = "선택 기억",
            ResultValue = "각성",
            SuccessRate = "100%"
        };
    }

    private static EventSkillAwakenSelectionPanelEntry CreateEntry(
        string characterId,
        EventChoiceSkillSlotKind slotKind,
        int slotIndex,
        string skillId,
        string upgradeSkillId)
    {
        EventChoiceSkillAwakenTarget target = new(
            characterId,
            slotKind,
            slotIndex,
            skillId,
            upgradeSkillId);
        return new EventSkillAwakenSelectionPanelEntry(
            target,
            characterId,
            slotKind.ToString(),
            skillId,
            upgradeSkillId);
    }

    private static GameObject CreateSceneBoundPanel(
        out EventSkillAwakenSelectionPanelUI panel)
    {
        GameObject root = new("AwakenPanel", typeof(RectTransform));
        GameObject content = new("Content", typeof(RectTransform));
        content.transform.SetParent(root.transform, false);

        GameObject template = new(
            "OptionTemplate",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        template.transform.SetParent(content.transform, false);
        template.SetActive(false);

        CreateImage(template.transform, "Icon");
        CreateText(template.transform, "SkillNameText");
        CreateText(template.transform, "CharacterNameText");
        CreateText(template.transform, "SlotNameText");
        CreateText(template.transform, "UpgradeNameText");

        GameObject emptyText = CreateText(root.transform, "EmptyText");
        GameObject cancel = new("CancelButton", typeof(RectTransform), typeof(Button));
        cancel.transform.SetParent(root.transform, false);

        panel = root.AddComponent<EventSkillAwakenSelectionPanelUI>();
        SetPrivateField(panel, "panelRoot", root);
        SetPrivateField(panel, "contentRoot", content.GetComponent<RectTransform>());
        SetPrivateField(panel, "emptyText", emptyText.GetComponent<TMP_Text>());
        SetPrivateField(panel, "cancelButton", cancel.GetComponent<Button>());
        SetPrivateField(panel, "optionTemplate", template);
        root.SetActive(false);
        return root;
    }

    private static GameObject CreateText(Transform parent, string name)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject;
    }

    private static GameObject CreateImage(Transform parent, string name)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
