# Character Level Unlock Config Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development while implementing this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 캐릭터 레벨에 따른 룬 슬롯, 전용 룬, 기억 해금을 캐릭터별 데이터로 설정하고 배틀 결과 보상패널에 이번 레벨업으로 열린 항목을 표시한다.

**Architecture:** 해금 레벨 판정은 새 공용 서비스가 담당하고, 로비 설정 패널과 배틀 결과 UI는 같은 서비스를 조회한다. `CharacterMasterData`에 배열형 컬럼을 추가해 데이터시트에서 캐릭터별 값을 지정하고, 값이 없으면 현재 로비 동작을 유지하는 기본값으로 폴백한다.

**Tech Stack:** Unity C#, TextMeshPro UI, 기존 `DataRowMapper` 배열 파싱, NUnit EditMode 테스트.

**Spec:** 사용자 요청: 로비 캐릭터 세팅의 레벨별 룬 슬롯/룬/기억 해금 하드코딩을 캐릭터별 설정으로 정리하고, 결과보상패널 해금 영역에 해금 텍스트 표시.

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터는 열려 있다고 가정하고 batchmode 테스트는 시도하지 않는다.
- UI는 전투 결과 계산을 하지 않고, 경험치 미리보기와 마스터 데이터에서 표시할 해금 텍스트만 계산한다.
- 커밋, Push, PR은 사용자 승인 없이 수행하지 않는다.

---

### Task 1: Character Unlock Service

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Character/CharacterLevelUnlockService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs`
- Test: `Assets/Tests/EditMode~/CharacterLevelUnlockServiceTests.cs`

**Interfaces:**
- Produces:
  - `CharacterLevelUnlockService.GetRuneSlotUnlockLevel(CharacterMasterData, int)`
  - `CharacterLevelUnlockService.GetRuneUnlockLevel(CharacterMasterData, RuneData, int)`
  - `CharacterLevelUnlockService.GetSkillMemoryUnlockLevel(CharacterMasterData, int, int)`
  - `CharacterLevelUnlockService.GetUnlockTexts(CharacterMasterData, RuneDatabase, SkillDatabase, int, int)`

- [ ] **Step 1: Write failing tests**

Tests cover default fallback values, per-character overrides, level-range unlock text, and non-level-up ranges returning no text.

- [ ] **Step 2: Verify RED**

Run `MSBuild .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal` and inspect compile failure caused by missing service/types.

- [ ] **Step 3: Implement minimal service and master data fields**

Add array fields on `CharacterMasterData` and static lookup methods in the service.

- [ ] **Step 4: Verify GREEN**

Run `MSBuild .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal`.

### Task 2: Lobby Panel Integration

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RuneSettingPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillSettingPanel.cs`
- Test: extend `Assets/Tests/EditMode~/CharacterLevelUnlockServiceTests.cs` for candidate-index behavior.

**Interfaces:**
- Consumes `CharacterLevelUnlockService` methods from Task 1.
- Produces 로비 룬 슬롯/룬/기억 잠금 판정이 캐릭터별 해금 배열을 사용한다.

- [ ] **Step 1: Write/extend failing test**

Verify second passive memory can be unlocked at a character-specific level rather than fixed Lv.5.

- [ ] **Step 2: Verify RED**

Build should still fail before integration or test should fail against fixed fallback.

- [ ] **Step 3: Replace hardcoded lookups**

Route `GetRuneSlotRequiredLevel`, `GetRequiredLevelForRune`, and `GetRequiredLevelForSkill` through `CharacterLevelUnlockService`.

- [ ] **Step 4: Verify GREEN**

Build and targeted tests compile.

### Task 3: Exploration Result Unlock Display

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs`
- Test: extend source/test coverage under `Assets/Tests/EditMode~/`.

**Interfaces:**
- Consumes `BattleStageClearExperiencePreview.LevelBefore/LevelAfter` and `CharacterLevelUnlockService.GetUnlockTexts`.
- Produces row `Bind` overload accepting unlock text list.

- [ ] **Step 1: Write failing test/source assertion**

Assert result panel requests unlock texts from character master data and passes them to row binding.

- [ ] **Step 2: Verify RED**

Build/test check fails before panel integration.

- [ ] **Step 3: Implement display binding**

Resolve character master by `CharacterId`, build unlock text list, and show existing `Unlocks` children only when text exists.

- [ ] **Step 4: Verify GREEN**

Build and source tests pass.

### Task 4: Data Defaults

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`

**Interfaces:**
- Consumes `CharacterMasterData` field names exactly as CSV headers.
- Produces current behavior defaults:
  - `RuneSlotUnlockLevels`: `1;1;3;5;7;10`
  - `RuneUnlockLevels`: current character runes mapped from existing `RuneData.UnlockLevel`
  - `PassiveSkillUnlockLevels`: `1;5`
  - `UniqueSkillUnlockLevels`: `1;10`
  - `CharacterSkillUnlockLevels`: `1;1`

- [ ] **Step 1: Add columns to GameData**

Update the Character sheet/source and runtime CSV with matching values for existing characters.

- [ ] **Step 2: Verify data parses**

Run compile checks and inspect CSV headers/rows.

### Task 5: Final Verification

**Files:**
- All modified files from Tasks 1-4.

- [ ] **Step 1: Build**

Run `MSBuild .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal`.

- [ ] **Step 2: Git diff review**

Inspect changed files and confirm no unrelated user work was reverted.

- [ ] **Step 3: Report**

Report changed files, implementation summary, verification results, unverified items, multiplayer impact, and no commit/push/PR.
