# Discord Rich Presence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Discord Social SDK를 RELIC에 설치하고 맵·캐릭터·플레이 시간을 무로그인 Rich Presence로 표시한다.

**Architecture:** SDK 호출은 전역 `DiscordRichPresenceService`에 격리하고, 표시 데이터 생성은 순수 `DiscordPresenceSnapshotBuilder`로 분리한다. 서비스는 씬 변경과 5초 주기로 기존 런타임 저장소를 읽되 게임 또는 Steam 상태를 변경하지 않는다.

**Tech Stack:** Unity 6000.0.68f1, C# 9, Discord Social SDK Unity Plugin 1.10.18247, Unity Test Framework 1.6.0

## Global Constraints

- 문서는 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- Discord 실패가 게임 또는 Steam 멀티플레이를 중단시키지 않는다.
- OAuth 토큰과 Client Secret을 사용하거나 저장하지 않는다.

---

### Task 1: SDK 패키지 설치

**Files:**
- Create: `Packages/com.discord.partnersdk/**`

**Interfaces:**
- Produces: `Discord.Sdk` 어셈블리와 Windows x86_64 Release 네이티브 플러그인

- [ ] ZIP의 `com.discord.partnersdk` 패키지를 프로젝트 `Packages`에 임베디드 패키지로 설치한다.
- [ ] `package.json` 버전이 `1.10.18247`인지 확인한다.
- [ ] Unity 프로젝트 파일 재생성 후 `Discord.Sdk` 참조 가능 여부를 확인한다.

### Task 2: 표시 스냅샷 테스트와 구현

**Files:**
- Create: `Assets/Tests/EditMode~/DiscordPresenceSnapshotBuilderTests.cs`
- Create: `Assets/Project/Scripts/Platform/Discord/DiscordPresenceSnapshot.cs`
- Create: `Assets/Project/Scripts/Platform/Discord/DiscordPresenceSnapshotBuilder.cs`

**Interfaces:**
- Consumes: 씬 이름, `MapRuntimeData`, `PartyRuntimeStore`, `CharacterDatabase`, 시작 Unix timestamp
- Produces: `DiscordPresenceSnapshot Build(...)`

- [ ] 타이틀 상태가 `메인 메뉴 / 모험 준비 중`인지 확인하는 실패 테스트를 작성한다.
- [ ] 로비의 편성 CharacterId가 CharacterDatabase 이름으로 변환되는 실패 테스트를 작성한다.
- [ ] 진행 중 맵의 챕터·스테이지와 시작 timestamp가 유지되는 실패 테스트를 작성한다.
- [ ] 데이터 누락 시 CharacterId와 일반 상태로 fallback하는 실패 테스트를 작성한다.
- [ ] 순수 스냅샷 모델과 빌더를 최소 구현한다.
- [ ] 프로젝트 테스트 어셈블리 컴파일로 테스트 코드와 구현 코드가 함께 컴파일되는지 확인한다.

### Task 3: Discord 서비스 테스트와 구현

**Files:**
- Create: `Assets/Tests/EditMode~/DiscordPresencePolicyTests.cs`
- Create: `Assets/Project/Scripts/Platform/Discord/DiscordPresenceStatus.cs`
- Create: `Assets/Project/Scripts/Platform/Discord/DiscordPresencePolicy.cs`
- Create: `Assets/Project/Scripts/Platform/Discord/DiscordRichPresenceService.cs`

**Interfaces:**
- Consumes: `DiscordPresenceSnapshotBuilder.Build(...)`, Discord `Client.UpdateRichPresence`
- Produces: 자동 생성되는 지속 서비스, `DiscordPresenceStatus Status`, 5초 갱신 정책

- [ ] Application ID 검증과 재시도 가능한 실패 분류 테스트를 먼저 작성한다.
- [ ] 상태 enum과 SDK 비의존 정책을 최소 구현한다.
- [ ] Application ID `1533104947875549325`를 설정하고 Direct Rich Presence를 갱신하는 서비스를 구현한다.
- [ ] 씬 변경 및 5초 주기 갱신, 종료 정리, 예외 격리를 구현한다.
- [ ] 로그에 초기화, 성공, Discord 미실행, 예외 원인이 구분되는지 확인한다.

### Task 4: 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`

**Interfaces:**
- Consumes: 설치된 패키지와 신규 런타임/테스트 코드
- Produces: 컴파일 결과와 수동 확인 체크리스트

- [ ] Unity 에디터가 생성한 프로젝트 파일에서 Discord SDK 참조를 확인한다.
- [ ] Runtime 및 Editor C# 프로젝트를 Restore 없이 빌드한다.
- [ ] 컴파일 오류가 있으면 원인을 수정하고 같은 검증을 반복한다.
- [ ] Discord 데스크톱 실행 상태에서 Play Mode 수동 확인 절차를 정리한다.
- [ ] Git diff와 신규 파일 범위를 검토하고 요청 밖 변경이 없는지 확인한다.

## Self-review

- 요구 범위는 SDK 설치, 연결 상태, 게임명, 맵, 캐릭터, 경과 시간으로 모두 Task 1~4에 대응한다.
- 초대/Join Secret, 친구 목록, OAuth UI는 현재 범위에서 제외했다.
- 코드 타입과 생산·소비 인터페이스의 명칭을 일치시켰다.
- 커밋 단계는 사용자 승인이 없으므로 포함하지 않았다.
