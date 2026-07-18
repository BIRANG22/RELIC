# 로비 캐릭터 전체 Animator UI 전환 설계

## 목표

캐릭터 선택 화면의 `A_UI_idle`, `B_UI_idle`, `C_UI_idle` 프리팹이 기존 `A/B/C_Robby_idle`과 동일한 Select, Skill, Rune 상태 및 전환 애니메이션을 Canvas `Image`로 재생하게 한다.

## 원본 유지

- 기존 `A/B/C_Robby_idle` 프리팹, Animator Controller, AnimationClip은 변경하지 않는다.
- `CharacterPrefabDB`에 이미 연결된 `PreviewUIPrefab`을 그대로 사용한다.
- `CharPick`의 캐릭터 선택 및 룬·스킬 버튼 흐름을 그대로 사용한다.

## UI용 Animator 구성

캐릭터별 기존 `Robby_idle` Controller의 상태 구조와 상태명을 UI용 Controller에 동일하게 구성한다.

- Select Idle
- Skill Idle
- Rune Idle
- Select → Skill
- Select → Skill Reverse
- Skill → Rune
- Skill → Rune Reverse
- Rune → Select
- Rune → Select Reverse

각 UI용 AnimationClip은 대응하는 원본 클립과 다음 항목이 같아야 한다.

- Sprite 프레임 참조와 순서
- Keyframe 시간
- Sample Rate와 길이
- Loop 설정
- Reverse 상태의 Speed 및 Cycle Offset 설정

다른 점은 애니메이션 바인딩뿐이다.

- 원본: `SpriteRenderer.m_Sprite`
- UI: `UnityEngine.UI.Image.m_Sprite`

## UI 프리팹 구성

각 `A/B/C_UI_idle` 프리팹은 기존 `RectTransform`, `CanvasRenderer`, `Image`, `Animator`를 유지한다. Animator에는 전체 상태를 가진 대응 UI Controller를 연결한다.

룬·스킬 버튼 이동에 반응하도록 기존 `Robby_idle` 프리팹과 동일한 `ButtonResponsiveSpriteAnimator`를 UI 프리팹에도 연결한다.

- `targetImage`: 프리팹 자신의 `Image`
- `targetSpriteRenderer`: 비어 있음
- `targetAnimator`: 프리팹 자신의 `Animator`
- 상태명: 캐릭터별 원본 Controller 상태명과 일치
- `useAnimatorStates`: 활성화
- `routeAssetCallsToActiveInstance`: 활성화

## UI 표시 크기

기존 SpriteRenderer 미리보기의 로비 카메라 투영 크기를 Canvas 기준 해상도 `1920×1080`에 환산한다.

- 원본 Sprite 프레임: `1920×1080`
- Sprite Pixels Per Unit: `100`
- 기존 미리보기 부모 스케일: `1.1`
- C 캐릭터 원본 프리팹 추가 스케일: `1.1`
- 로비 카메라: FOV `28`, 카메라와 Sprite 사이 거리 약 `20.11`
- A/B UI RectTransform: `2275×1280`
- C UI RectTransform: `2502×1408`
- `Image.preserveAspect`: 활성화
- 기존 UI anchored position과 pivot은 유지

이 크기는 원본 SpriteRenderer의 전체 16:9 프레임이 화면에 투영되던 크기와 일치시키기 위한 값이다. 캐릭터 애니메이션 프레임마다 동일한 RectTransform을 사용하므로 상태 전환 중 크기가 변하지 않는다.

## 실행 흐름

1. `CharPick`이 `CharacterPrefabDB.TryGetPreviewUIPrefab`으로 기존 UI 프리팹을 생성한다.
2. 생성된 프리팹의 `ButtonResponsiveSpriteAnimator`를 `CharPick`이 찾는다.
3. 룬·스킬 버튼 이동 시 기존 호출 흐름이 대응 Animator 상태를 재생한다.
4. UI용 클립이 원본과 같은 Sprite 프레임을 `Image.m_Sprite`에 적용한다.

## Canvas 렌더링 순서

로비 Canvas의 `Background` 아래에 화면 전체 Stretch `CharacterPreviewSpawnRoot` RectTransform을 추가한다. 자식 순서는 다음과 같이 고정한다.

1. `Back_Main`
2. `CharacterPreviewSpawnRoot`
3. `Effect_Lobby`
4. `Effect_Char`

`CharPick.previewRoot`는 `CharacterSettingPanel` 대신 `CharacterPreviewSpawnRoot`를 참조한다. 따라서 런타임에 생성되는 A/B/C UI 프리팹은 항상 `Back_Main` 위, `Effect_Lobby` 아래에 렌더링된다. 캐릭터 프리팹의 anchored position, size, pivot과 Animator 설정은 변경하지 않는다.

## 검증

- 캐릭터별 UI Controller의 상태명, State Speed, Cycle Offset이 원본과 일치하는지 비교한다.
- 각 UI Controller의 모든 Motion이 UI용 클립을 참조하는지 확인한다.
- UI용 클립의 프레임과 타이밍이 대응 원본 클립과 일치하는지 확인한다.
- 모든 UI용 클립이 `Image.m_Sprite` 바인딩인지 확인한다.
- UI 프리팹의 `ButtonResponsiveSpriteAnimator` 참조와 상태명을 확인한다.
- A/B UI 프리팹 크기가 `2275×1280`, C가 `2502×1408`이며 Preserve Aspect가 활성화됐는지 확인한다.
- `Background` 자식 순서가 `Back_Main → CharacterPreviewSpawnRoot → Effect_Lobby → Effect_Char`인지 확인한다.
- `CharPick.previewRoot`가 `CharacterPreviewSpawnRoot` RectTransform을 참조하는지 확인한다.
- Runtime 및 Editor 프로젝트 빌드를 실행한다.
- Unity 에디터가 열린 상태이므로 batchmode 테스트는 실행하지 않는다.

## 영향 범위

변경은 로비 캐릭터 선택 화면의 표현과 버튼 반응 애니메이션에만 적용한다. 전투 캐릭터, 전투 상태, 캐릭터 ID, 선택 결과 및 멀티플레이 경계에는 영향을 주지 않는다.
