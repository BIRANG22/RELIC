# StartRoom Active Relic Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** StartRoom이 숫자 범위 없이 `Relic_A_` ID를 가진 모든 액티브 유물을 자동으로 선택 후보에 포함하게 한다.

**Architecture:** 후보 판별을 순수 정적 유틸리티로 분리해 DB 데이터만으로 테스트한다. `RelicChoiceAreaUI`는 `RelicDatabase.GetAll()` 결과를 유틸리티에 전달하고, 기존 보유 유물 제외·셔플·표시 흐름은 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity YAML scene serialization

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 안에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 요청 범위 밖의 전투 및 랜덤 구조는 변경하지 않는다.
- 커밋과 PR은 별도 허락 없이 진행하지 않는다.

---

### Task 1: 액티브 유물 후보 판별 테스트 및 유틸리티

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/StartRoomRelicSelectionUtility.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/StartRoomRelicSelectionUtility.cs.meta`
- Create: `Assets/Tests/EditMode~/StartRoomRelicSelectionUtilityTests.cs`
- Create: `Assets/Tests/EditMode~/StartRoomRelicSelectionUtilityTests.cs.meta`

**Interfaces:**
- Consumes: `IReadOnlyList<RelicData>`
- Produces: `StartRoomRelicSelectionUtility.CollectActiveRelicIds(IReadOnlyList<RelicData>) : List<string>`

- [ ] **Step 1: 실패 테스트 작성**

`Relic_A_01`, `Relic_A_15`, `Relic_A_99`는 포함하고 `Relic_P_01`, 빈 ID, null 데이터는 제외하는 NUnit 테스트를 작성한다. 번호 연속성과 상한에 의존하지 않는 것을 함께 검증한다.

- [ ] **Step 2: RED 검증**

Unity batchmode 대신 프로덕션 유틸리티 파일이 아직 없다는 정적 검증을 실행한다.

Run: `Test-Path Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/StartRoomRelicSelectionUtility.cs`

Expected: `False`

- [ ] **Step 3: 최소 구현 작성**

```csharp
using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class StartRoomRelicSelectionUtility
{
    private const string ActiveRelicIdPrefix = "Relic_A_";

    public static List<string> CollectActiveRelicIds(IReadOnlyList<RelicData> relics)
    {
        List<string> result = new();
        if (relics == null)
            return result;

        for (int i = 0; i < relics.Count; i++)
        {
            string id = relics[i]?.FragmentId?.Trim();
            if (!string.IsNullOrWhiteSpace(id) &&
                id.StartsWith(ActiveRelicIdPrefix, StringComparison.Ordinal))
            {
                result.Add(id);
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: 컴파일 검증**

Run: `MSBuild.exe Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal`

Expected: exit code 0

### Task 2: RelicChoiceAreaUI의 범위 의존 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/RelicChoiceAreaUI.cs`

**Interfaces:**
- Consumes: `StartRoomRelicSelectionUtility.CollectActiveRelicIds(...)`
- Produces: 기존 `PickRandomRelicIds()`의 액티브 유물 전용 후보 목록

- [ ] **Step 1: 후보 생성 연결**

`useRelicNumberRange`, `minRelicNumber`, `maxRelicNumber`, `relicIdPrefix`, `AddRelicsFromNumberRange`, `AddAllRelics`를 제거한다. `PickRandomRelicIds()`에서 다음처럼 후보를 생성한다.

```csharp
List<string> candidates = StartRoomRelicSelectionUtility.CollectActiveRelicIds(
    DataManager.Instance.RelicDatabase.GetAll());
```

- [ ] **Step 2: 정적 검증**

Run: `rg -n "useRelicNumberRange|minRelicNumber|maxRelicNumber|relicIdPrefix|AddRelicsFromNumberRange" Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/RelicChoiceAreaUI.cs`

Expected: 검색 결과 없음

### Task 3: 씬 직렬화와 기존 테스트 정리

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Modify: `Assets/Tests/EditMode~/StartRoomActiveRelicConfigurationTests.cs`

**Interfaces:**
- Consumes: 범위 필드가 제거된 `RelicChoiceAreaUI`
- Produces: 두 씬에 불필요한 범위 직렬화 값이 없는 상태

- [ ] **Step 1: 씬 설정 제거**

두 씬의 `RelicChoiceAreaUI` 컴포넌트에서 아래 네 줄을 모두 제거한다.

```yaml
  useRelicNumberRange: 1
  minRelicNumber: 11
  maxRelicNumber: 15
  relicIdPrefix: Relic_
```

- [ ] **Step 2: 씬 구성 테스트 갱신**

기존 범위 `11~15`를 요구하는 테스트를 제거하고, Battle/DebugBattle 씬 모두 범위 필드를 포함하지 않는지 검증한다.

```csharp
[TestCase("Assets/Project/Scenes/YDM/Battle.unity")]
[TestCase("Assets/Project/Scenes/YDM/DebugBattle.unity")]
public void StartRoomRelicChoicesDoNotSerializeNumberRanges(string scenePath)
{
    string sceneText = File.ReadAllText(scenePath);
    Assert.That(sceneText, Does.Not.Contain("useRelicNumberRange:"));
    Assert.That(sceneText, Does.Not.Contain("minRelicNumber:"));
    Assert.That(sceneText, Does.Not.Contain("maxRelicNumber:"));
    Assert.That(sceneText, Does.Not.Contain("relicIdPrefix:"));
}
```

- [ ] **Step 3: 전체 검증**

Run: `MSBuild.exe Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal`

Run: `MSBuild.exe Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal`

Run: `git diff --check`

Expected: 모두 exit code 0. Unity batchmode 테스트는 실행하지 않는다.

## 실행 및 커밋 정책

사용자가 이미 현재 세션에서 구현 진행을 승인했으므로 계획 승인 후 실행한다. 커밋 단계는 프로젝트 규칙에 따라 포함하지 않으며 별도 요청 전에는 커밋하지 않는다.
