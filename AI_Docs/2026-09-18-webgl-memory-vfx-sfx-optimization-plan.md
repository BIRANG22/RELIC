# WebGL 메모리 및 VFX/SFX 빌드 최적화 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** WebGL의 최대 힙을 4GB로 늘리고, 게임 코드가 직접 `Resources.Load`하지 않는 Vefects 데모 리소스가 WebGL Player에 자동 포함되는 것을 막아 초기 메모리 사용량을 낮춘다.

**Architecture:** Unity의 `Resources` 폴더는 참조 여부와 무관하게 Player에 포함된다. 따라서 Vefects 데모 하위의 `Resources` 폴더 이름을 `Resources_ExcludedFromPlayer`로 변경해 자동 포함 규칙에서 제외한다. Unity가 일반 에셋 폴더로 안정적으로 임포트하며, 파일과 GUID를 보존하는 이동이므로 씬/프리팹의 직접 참조는 유지된다. 실제 경로 기반 `Resources.Load`가 없는 것을 코드 검색으로 확인한 폴더만 대상으로 한다.

**Tech Stack:** Unity ProjectSettings, Unity AssetDatabase/serialized asset references, NUnit EditMode tests.

**Spec:** `AI_Docs/2026-09-18-webgl-memory-vfx-sfx-optimization-design.md`

## Global Constraints

- 원본 VFX/SFX 파일을 삭제하지 않는다.
- `Assets/Project/Download/vfx/Vefects` 아래의 데모 `Resources` 폴더 6개만 이동한다.
- 실제 게임의 `Assets/Resources` 및 `Assets/Project/Resources`는 건드리지 않는다.
- 대형 WAV는 실제 Player 포함 여부와 사용 지점이 검증되기 전에는 임포트 설정을 일괄 변경하지 않는다.
- 전투 상태·네트워크 동기화 로직은 변경하지 않는다.

## Task 1: 설정 회귀 테스트 작성

**Files:**
- Create: `Assets/Tests/EditMode~/WebGlBuildFootprintOptimizationTests.cs`

- [x] `ProjectSettings/ProjectSettings.asset`에서 WebGL 초기/최대 메모리 값을 읽는 EditMode 테스트를 만들었다.
- [x] 최대 메모리 4096MB, 초기 메모리 512MB를 요구한다.
- [x] Vefects 대상 경로에 `Resources` 폴더가 남지 않고 `Resources_ExcludedFromPlayer` 폴더가 존재해야 한다는 테스트를 만들었다.
- [x] 구현 전 테스트가 실패함을 확인했다.

## Task 2: WebGL 메모리 설정 적용

**Files:**
- Modify: `ProjectSettings/ProjectSettings.asset`

- [x] `webGLInitialMemorySize`를 512로 변경했다.
- [x] `webGLMaximumMemorySize`를 4096으로 변경했다.
- [x] 다른 WebGL 메모리 성장 정책과 압축 정책은 변경하지 않았다.

## Task 3: 미사용 Vefects 데모 Resources 제외

**Files:**
- Move: `Assets/Project/Download/vfx/Vefects/Anime VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`
- Move: `Assets/Project/Download/vfx/Vefects/Combat Flipbook VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`
- Move: `Assets/Project/Download/vfx/Vefects/Flipbook VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`
- Move: `Assets/Project/Download/vfx/Vefects/Pixel Craft VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`
- Move: `Assets/Project/Download/vfx/Vefects/Stylized VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`
- Move: `Assets/Project/Download/vfx/Vefects/Wind VFX URP/Demo/Resources` → `Resources_ExcludedFromPlayer`

- [x] 각 원본·대상 경로와 하위 `.meta` 존재를 확인했다.
- [x] Unity GUID를 보존하는 폴더 이동을 수행했다.
- [x] 게임 코드에 해당 데모 리소스의 경로 기반 `Resources.Load`가 없음을 재검색했다.
- [x] TextMesh Pro Examples의 `Resources`는 별도 영향 분석 전에는 유지했다.

## Task 4: 검증

**Files:**
- Verify: `Assets/Tests/EditMode~/WebGlBuildFootprintOptimizationTests.cs`
- Verify: `ProjectSettings/ProjectSettings.asset`

- [x] 새 EditMode 테스트의 조건을 정적 검사로 확인했다.
- [x] 기존 WebGL Vefects 셰이더 호환성 테스트의 조건을 정적 검사했다.
- [x] `git diff --check`를 실행했다.
- [ ] Unity 에디터에서 리임포트가 끝난 뒤 Build & Run을 실행하고, 새 `Editor.log`에서 `abort(\"OOM\")` 및 빌드 실패를 확인한다. 이 단계는 Unity UI 실행 시간이 길어 현 세션에서 완료하지 못할 수 있다.

## Expected Outcome

- WebGL 런타임의 최대 힙이 4GB가 된다.
- Vefects 데모 `Resources` 6개(원본 파일 합계 약 122MB)가 무조건 Player에 포함되는 경로가 제거된다.
- 직접 참조된 VFX는 GUID 유지로 인해 계속 참조되고, 이름 기반 `Resources.Load` 대상은 변경하지 않는다.
