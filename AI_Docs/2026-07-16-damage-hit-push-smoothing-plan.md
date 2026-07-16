# Damage Hit Push Smoothing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공격 적중 시 공격자와 피격자의 전진 및 복귀가 눈에 보이도록 기본 연출 시간을 늘린다.

**Architecture:** 전투 결과 계산과 분리된 `BattleHitImpactFeedback` 프레젠테이션 컴포넌트의 직렬화 기본값만 변경한다. 방향, 거리, 위치 복원, 카메라 연출 로직은 그대로 유지한다.

**Tech Stack:** Unity 6, C#, Coroutine, Unity Test Framework

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트를 추가할 경우 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
- 전투 상태 및 멀티플레이 경계 로직은 변경하지 않는다.
- 커밋은 사용자에게 별도 허락을 받은 경우에만 수행한다.

---

### Task 1: 피격 이동 기본 시간 조정

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs:15-17`
- Verify: `Assembly-CSharp.csproj`

**Interfaces:**
- Consumes: `AnimateDamagePush(List<MoveEntry>)`가 사용하는 직렬화 필드
- Produces: 전진 기본 시간 `0.12f`, 복귀 기본 시간 `0.16f`

- [ ] **Step 1: 현재 동작의 원인 확인**

`damageHitPushOutDuration`이 `0.045f`, `damageHitPushReturnDuration`이 `0.09f`인지 확인한다. 60 FPS 기준 전진 구간이 약 3프레임이므로 부드러운 보간을 시각적으로 확인하기 어렵다.

- [ ] **Step 2: 최소 구현 적용**

다음 두 필드의 기본값만 변경한다.

```csharp
[SerializeField] private float damageHitPushOutDuration = 0.12f;
[SerializeField] private float damageHitPushReturnDuration = 0.16f;
```

`damageHitPushHoldDuration`, 이동 거리, 배율, `AnimationCurve`, 코루틴 흐름은 변경하지 않는다.

- [ ] **Step 3: 컴파일 검증**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
```

Expected: 빌드 오류 0개. 기존 경고는 별도로 기록한다.

- [ ] **Step 4: 에디터 수동 검증 안내**

Unity 에디터에서 공격 적중 연출을 재생하여 공격자와 피격자가 공격 진행 방향으로 부드럽게 전진하고 원래 위치로 복귀하는지 확인한다. 전투 결과, HP 변화 시점, 대상 방향은 기존과 같아야 한다.

- [ ] **Step 5: 변경 범위 확인**

```powershell
git diff --check -- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs
git diff -- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleHitImpactFeedback.cs
```

Expected: 공백 오류가 없고 두 기본 시간 값만 변경되어야 한다. 커밋은 수행하지 않는다.
