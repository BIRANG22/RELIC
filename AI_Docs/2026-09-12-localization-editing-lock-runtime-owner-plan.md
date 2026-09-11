# Localization Editing Lock Runtime Owner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow the editor localization lock menu to toggle migrated `LocalizedTMPText` owners.

**Architecture:** The editor tool distinguishes the current runtime owner from legacy `LocalizeStringEvent`. It toggles the runtime owner and restores the serialized Korean source while disabled; dynamically owned TMP fields remain excluded through `ShouldManageText`.

**Tech Stack:** Unity Editor, NUnit EditMode tests, TextMeshPro, Unity Localization.

**Spec:** `AI_Docs/2026-09-12-localization-editing-lock-runtime-owner-design.md`

## Global Constraints

- Preserve existing uncommitted changes.
- Do not modify dynamic TMP fields marked with `LocalizationIgnore`.
- Do not commit, push, or create a PR.

---

### Task 1: Toggle migrated runtime owners

**Files:**
- Modify: `Assets/Editor/LocalizationEditingLockTool.cs`
- Modify: `Assets/Tests/EditMode~/LocalizationEditingLockToolTests.cs`

**Interfaces:**
- Consumes: `LocalizedTMPText.KoreanSource`, `LocalizedTMPText.Refresh()`, and `LocalizedTMPText.ShouldManageText(TMP_Text)`.
- Produces: `SetHierarchyLocalizersEnabled(GameObject, bool)` changes the active localization owner for both legacy and migrated text.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void SetHierarchyLocalizersEnabled_DisablesRuntimeLocalizerAndRestoresKoreanSource()
{
    // A migrated TMP owner disables and displays its serialized Korean source.
}

[Test]
public void SetHierarchyLocalizersEnabled_IgnoresDynamicRuntimeText()
{
    // A LocalizationIgnore hierarchy remains untouched.
}
```

- [ ] **Step 2: Verify the tests fail for the missing runtime-owner behavior**

Run the EditMode test assembly in the open Unity Editor. Expected: the migrated-owner test fails because the tool does not enumerate `LocalizedTMPText`.

- [ ] **Step 3: Implement the minimal editor-tool change**

```csharp
foreach (LocalizedTMPText localizer in root.GetComponentsInChildren<LocalizedTMPText>(true))
{
    TMP_Text text = localizer.GetComponent<TMP_Text>();
    if (!LocalizedTMPText.ShouldManageText(text))
        continue;

    localizer.enabled = enabled;
    if (!enabled)
        text.text = localizer.KoreanSource;
    else
        localizer.Refresh();
}
```

Continue to toggle legacy `LocalizeStringEvent` only when it is not accompanied by a runtime owner.

- [ ] **Step 4: Verify**

Run the targeted EditMode tests in the open Unity Editor, then compile `Assembly-CSharp-Editor.csproj` with MSBuild. Expected: tests pass and compilation succeeds.

### Task 2: Apply the editing lock to code-owned localization

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/GameLocalization.cs`
- Modify: `Assets/Editor/LocalizationEditingLockTool.cs`
- Test: `Assets/Tests/EditMode~/LocalizationEditingLockToolTests.cs`

**Interfaces:**
- Consumes: editor-only `EditorPrefs` and `GameLocalization.Get(string, string, object[])`.
- Produces: `LocalizationEditingLockState.IsEnabledForEditor` and `SetEnabledForEditor(bool)`.

- [ ] **Step 1: Add the state test**

```csharp
LocalizationEditingLockState.SetEnabledForEditor(false);
Assert.That(LocalizationEditingLockState.IsEnabledForEditor, Is.False);
```

- [ ] **Step 2: Add the editor-only state holder and set it from the menu**

```csharp
LocalizationEditingLockState.SetEnabledForEditor(enabled);
```

- [ ] **Step 3: Return the supplied source before a table lookup when disabled**

```csharp
if (!LocalizationEditingLockState.IsEnabledForEditor)
    return fallback ?? string.Empty;
```

- [ ] **Step 4: Verify**

Run the targeted EditMode tests in the open Unity Editor, then compile both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` with MSBuild.

### Task 3: Resolve key-only text from the Korean table while disabled

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/GameLocalization.cs`
- Modify: `Assets/Tests/EditMode~/LocalizationEditingLockToolTests.cs`

**Interfaces:**
- Consumes: `LocalizationSettings.AvailableLocales`, `LocalizedStringDatabase.GetLocalizedString`, and `FallbackBehavior.DontUseFallback`.
- Produces: key-only `GameLocalization.Get(key)` returns the Korean Text-table entry while the editor lock is disabled.

- [ ] **Step 1: Add the Korean-table regression test**

```csharp
LocalizationEditingLockState.SetEnabledForEditor(false);
Assert.That(GameLocalization.Get(LocalizationKeys.RelicRarity.Common), Is.EqualTo("일반 유물"));
```

- [ ] **Step 2: Resolve the Korean locale explicitly for key-only calls**

```csharp
if (!LocalizationEditingLockState.IsEnabledForEditor)
    return GetKoreanSourceForEditor(key, arguments);
```

- [ ] **Step 3: Verify**

Run the targeted EditMode tests in the open Unity Editor, then compile both Unity project assemblies.
