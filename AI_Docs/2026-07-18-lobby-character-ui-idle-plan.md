# 로비 캐릭터 전체 Animator UI 전환 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 A/B/C Robby Animator의 전체 상태와 버튼 반응 동작을 기존 A/B/C UI 프리팹에서 동일하게 재생한다.

**Architecture:** 캐릭터별 원본 Controller와 6개 고유 Motion 클립을 기준으로 UI 전용 클립과 Controller를 만든다. 상태 구조, State Speed, Cycle Offset은 원본을 유지하고 모든 클립의 Sprite 바인딩만 `UnityEngine.UI.Image.m_Sprite`로 변환한다. UI 프리팹에는 기존 버튼 흐름이 찾는 `ButtonResponsiveSpriteAnimator`를 원본과 같은 상태명으로 연결한다.

**Tech Stack:** Unity 6, Unity YAML, AnimatorController, AnimationClip, Unity UI Image, NUnit EditMode

## 전역 제약

- 기존 A/B/C Robby 프리팹, Controller, Clip은 변경하지 않는다.
- 기존 CharacterPrefabDB의 PreviewUIPrefab 연결을 유지한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋과 PR은 별도 사용자 허락 없이 진행하지 않는다.

---

### Task 1: 전체 Animator 동등성 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/LobbyCharacterUiIdleAssetTests.cs`

**Interfaces:**
- Consumes: A/B/C 원본 및 UI Controller, 원본 및 UI Clip, UI Prefab
- Produces: 전체 상태·Motion·프레임·ButtonResponsiveSpriteAnimator 연결 검증

- [ ] 원본과 UI Controller의 상태명, Speed, Cycle Offset을 비교하는 테스트를 작성한다.
- [ ] 각 원본 Motion에 대응하는 UI Motion의 프레임·시간·반복 설정이 같고 바인딩 타입만 `Image`인지 검증한다.
- [ ] 각 UI 프리팹에 `ButtonResponsiveSpriteAnimator`가 있고 자신의 Image와 Animator를 참조하는지 검증한다.
- [ ] 현재 UI Controller가 전체 상태를 갖지 않아 검증 조건이 실패함을 YAML 정적 검사로 확인한다.

### Task 2: A/B/C 전체 UI AnimationClip 생성

**Files:**
- Keep/Modify: `Assets/Project/PrefabsR/Character/{A,B,C}/Clip/{A,B,C}_Robby_UI_Idle.anim`
- Create: 캐릭터별 Skill Idle, Rune Idle, Select→Skill, Skill→Rune, Rune→Select UI용 `.anim` 및 `.meta` 5쌍

**Interfaces:**
- Consumes: 캐릭터별 원본 6개 고유 AnimationClip
- Produces: 캐릭터별 6개 UI Image용 AnimationClip

- [ ] Select Idle UI 클립을 대응 원본 Select Idle 기준으로 재생성한다.
- [ ] 나머지 5개 고유 Motion 클립을 UI 전용 파일로 복제한다.
- [ ] 모든 PPtrCurve 및 binding constant를 UI Image 스크립트 GUID `fe87c0e1cc204ed48ad3b37840f39efc`에 바인딩한다.
- [ ] 원본과 UI 클립의 프레임, 시간, Sample Rate, Loop 설정을 정적 비교한다.

### Task 3: A/B/C UI Animator Controller 전체 복제

**Files:**
- Modify: `Assets/Project/PrefabsR/Character/A/Controller/A_Robby_UI_idle.controller`
- Modify: `Assets/Project/PrefabsR/Character/B/Controller/B_Robby_UI_idle.controller`
- Modify: `Assets/Project/PrefabsR/Character/C/Controller/C_Robby_UI_idle.controller`

**Interfaces:**
- Consumes: 원본 Controller 구조와 Task 2의 UI Motion GUID
- Produces: 원본과 동일한 9개 상태를 가진 UI Controller

- [ ] 원본 Controller YAML을 UI Controller에 복제해 상태 구조와 기본 상태를 유지한다.
- [ ] Controller 이름은 기존 UI Controller 이름으로 유지한다.
- [ ] 원본 Motion GUID 6개를 대응 UI Motion GUID로 교체한다.
- [ ] Reverse 상태가 공유 Motion, 음수 Speed, Cycle Offset을 원본과 동일하게 유지하는지 확인한다.

### Task 4: A/B/C UI 프리팹 버튼 반응 연결

**Files:**
- Modify: `Assets/Project/PrefabsR/Character/A/A_UI_idle.prefab`
- Modify: `Assets/Project/PrefabsR/Character/B/B_UI_idle.prefab`
- Modify: `Assets/Project/PrefabsR/Character/C/C_UI_idle.prefab`

**Interfaces:**
- Consumes: Task 3의 UI Controller와 원본 Robby 프리팹의 `ButtonResponsiveSpriteAnimator` 설정
- Produces: CharPick의 기존 버튼 호출에 반응하는 UI 프리팹

- [ ] 원본 Robby 프리팹의 `ButtonResponsiveSpriteAnimator` 직렬화 설정을 UI 프리팹에 복제한다.
- [ ] `targetImage`와 `targetAnimator`를 UI 프리팹 자신의 컴포넌트에 연결하고 `targetSpriteRenderer`는 비운다.
- [ ] UI 프리팹 초기 Sprite가 Select Idle 첫 프레임인지 확인한다.

### Task 5: 최종 검증

**Files:**
- Verify: 위에서 수정·생성한 Clip, Controller, Prefab 및 테스트

**Interfaces:**
- Consumes: Task 1~4 결과
- Produces: 정적 동등성 및 컴파일 결과

- [ ] A/B/C별 전체 상태, Motion, UI 바인딩, 프리팹 참조를 정적 검사한다.
- [ ] Runtime과 Editor 프로젝트를 순차 빌드해 exit code 0을 확인한다.
- [ ] `git diff --check`가 오류 없이 끝나는지 확인한다.
- [ ] Unity 에디터에서 Select→Skill→Rune→Select 및 역방향 전환을 수동 확인 대상으로 보고한다.

### Task 6: SpriteRenderer 투영 크기와 UI 크기 일치

**Files:**
- Modify: `Assets/Project/PrefabsR/Character/A/A_UI_idle.prefab`
- Modify: `Assets/Project/PrefabsR/Character/B/B_UI_idle.prefab`
- Modify: `Assets/Project/PrefabsR/Character/C/C_UI_idle.prefab`
- Modify: `Assets/Tests/EditMode~/LobbyCharacterUiIdleAssetTests.cs`

**Interfaces:**
- Consumes: 원본 Sprite 크기, 로비 카메라 FOV·거리, 기존 미리보기 스케일, Canvas `1920×1080` 기준 해상도
- Produces: 기존 SpriteRenderer와 같은 화면 투영 크기를 사용하는 UI 프리팹

- [ ] A/B 프리팹 RectTransform이 `2275×1280`, C가 `2502×1408`이며 `Image.preserveAspect`가 활성화되는 검증을 추가한다.
- [ ] 현재 세 프리팹이 `500×500`, Preserve Aspect 비활성이라 검증 조건을 만족하지 않음을 확인한다.
- [ ] A/B/C 프리팹의 Size Delta와 Preserve Aspect만 수정하고 anchored position, pivot, Animator 연결은 유지한다.
- [ ] 정적 에셋 검사, Runtime/Editor 순차 빌드, 작업 범위 `git diff --check`를 실행한다.

### Task 7: Background 내부 캐릭터 스폰 순서 고정

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Tests/EditMode~/LobbyCharacterUiIdleAssetTests.cs`

**Interfaces:**
- Consumes: Canvas `Background` RectTransform, `Back_Main`, `Effect_Lobby`, `Effect_Char`, `CharPick.previewRoot`
- Produces: 캐릭터 UI가 Back_Main과 Effect_Lobby 사이에 렌더링되는 씬 계층

- [ ] 씬에 `CharacterPreviewSpawnRoot`가 없고 `CharPick.previewRoot`가 CharacterSettingPanel을 가리키는 실패 조건을 확인한다.
- [ ] `Background` 아래에 화면 전체 Stretch RectTransform `CharacterPreviewSpawnRoot`를 추가한다.
- [ ] 자식 순서를 `Back_Main → CharacterPreviewSpawnRoot → Effect_Lobby → Effect_Char`로 설정한다.
- [ ] `CharPick.previewRoot`를 새 RectTransform으로 변경한다.
- [ ] 씬 FileID 중복, 부모·자식 참조, Runtime/Editor 순차 빌드와 작업 범위 `git diff --check`를 확인한다.
