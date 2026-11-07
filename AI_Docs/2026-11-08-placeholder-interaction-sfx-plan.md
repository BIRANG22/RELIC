# Placeholder Interaction SFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 지정된 UI·연출 성공 지점에 동작별 임시 SFX ID를 추가하고 모두 제공된 교체용 MP3에 연결한다.

**Architecture:** 기존 `SoundDatabase`와 `AudioManager.PlaySfx(string)` 경로를 유지한다. 동작별 ID는 독립적으로 만들고, 사운드 호출은 상태 변경 성공 이후의 프레젠테이션 지점에만 둔다.

**Tech Stack:** Unity 6, C#, Unity UI/EventSystem, ScriptableObject YAML, NUnit EditMode

**Spec:** `AI_Docs/2026-11-08-placeholder-interaction-sfx-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~/` 내부에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 실패·취소 입력에는 사운드를 재생하지 않는다.
- 전투 핵심 상태, 네트워크 동기화, 랜덤 흐름은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 클릭과 드래그가 같은 장착 성공 경로로 합류할 때 중복 재생되지 않아야 한다.
- 유효하지 않은 드롭과 재료 부족 실패에는 재생되지 않아야 한다.
- 이미 사운드가 있는 튜토리얼·턴 진행·재료 등록 경로는 한 번만 재생되어야 한다.
- 빈 ID 또는 `AudioManager.Instance` 부재 시 상태 변경은 정상 진행되어야 한다.
- 탐사 보고 카운트업은 매 프레임 폭주하지 않고 의미 있는 숫자 변화에만 제한되어야 한다.

---

### Task 1: 임시 음원과 의미별 DB ID

**Files:**
- Create: `Assets/Project/Art/Sound/SFX_TEMP_WRONG_SOUND.mp3`
- Create: `Assets/Project/Art/Sound/SFX_TEMP_WRONG_SOUND.mp3.meta`
- Modify: `Assets/DB/SoundDatabase.asset`
- Modify: `Assets/Project/Scripts/Core/AudioIds.cs`
- Test: `Assets/Tests/EditMode~/PlaceholderInteractionSfxTests.cs`

- [ ] MP3와 메타 파일을 프로젝트에 추가한다.
- [ ] 이미지 목록의 동작별 `AudioIds.Sfx` 상수와 SoundDatabase 항목을 추가한다.
- [ ] 모든 신규 DB 항목이 같은 임시 클립 GUID를 참조하는 EditMode 테스트를 작성한다.
- [ ] 테스트 어셈블리를 컴파일해 직렬화 및 상수 오류를 확인한다.

### Task 2: 기존 호출부 재연결

**Files:**
- Modify: `Assets/Project/Scripts/LobbyTutorialController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankPanelPresenter.cs`
- Modify: 관련 씬·프리팹 YAML
- Test: `Assets/Tests/EditMode~/PlaceholderInteractionSfxTests.cs`

- [ ] 기존 직렬화 SFX 필드의 기본값과 YAML 연결을 의미별 ID로 변경한다.
- [ ] 기존 호출과 새 호출이 중복되지 않는지 정적 검증 테스트를 추가한다.
- [ ] C# 컴파일을 실행한다.

### Task 3: 선택·버튼·편성 성공 피드백

**Files:**
- Modify: 이동·플립, 스킬 리셋·취소, 캐릭터 편성 전환·제거 담당 스크립트
- Test: `Assets/Tests/EditMode~/PlaceholderInteractionSfxTests.cs`

- [ ] 각 성공 지점을 확정하고 직렬화 SFX ID 및 단일 재생 호출을 추가한다.
- [ ] 비활성 버튼과 실패 경로에서 호출되지 않는 검증을 추가한다.
- [ ] C# 컴파일을 실행한다.

### Task 4: 드래그앤드롭·장착·스킬 변경 피드백

**Files:**
- Modify: 룬, 스킬, 유물·연성제, 그리드 선택 드래그 담당 스크립트
- Modify: `Assets/Project/Scripts/LobbyInfoPanelUI.cs`
- Modify: 관련 프리팹 YAML
- Test: `Assets/Tests/EditMode~/PlaceholderInteractionSfxTests.cs`

- [ ] 입력 시작이 아닌 실제 성공 결과에서 한 번 재생하도록 공통 성공 경로를 사용한다.
- [ ] 유물·연성제 슬롯의 기존 클릭 사운드를 제거한다.
- [ ] 클릭 장착과 드롭 장착 모두 같은 성공 사운드를 한 번만 재생하는 검증을 추가한다.
- [ ] C# 컴파일을 실행한다.

### Task 5: 탐사 보고 카운트업 피드백

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Test: `Assets/Tests/EditMode~/PlaceholderInteractionSfxTests.cs`

- [ ] 카운트업 중 표시 정수가 실제로 바뀌는 시점에 제한하여 사운드를 재생한다.
- [ ] 즉시 표시·복원 경로에서는 불필요한 반복 재생을 막는다.
- [ ] EditMode 테스트와 C# 컴파일을 실행한다.

### Task 6: 최종 정적 검증

**Files:**
- Verify: 모든 변경 코드, YAML, MP3 메타, 테스트

- [ ] 신규 ID가 DB에 모두 존재하고 클립 참조가 유효한지 검색한다.
- [ ] 기존 클릭 사운드가 유물·연성제 장착 전에 남아 있지 않은지 확인한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 에디터 화면과 실제 청취가 필요한 항목을 미검증으로 기록한다.
