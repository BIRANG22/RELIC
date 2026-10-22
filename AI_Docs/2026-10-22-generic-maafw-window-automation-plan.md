# 범용 창 선택형 MaaFramework 자동화 실행기 구현 계획

> **실행 지침:** `superpowers:executing-plans`를 사용해 작업별로 테스트 우선 구현한다.

**목표:** 실행 시 선택한 Windows 창에 MaaFramework Win32 Controller를 연결하고 프로젝트별 Pipeline을 제한 횟수만큼 실행하는 범용 QA 도구를 구현한다.

**아키텍처:** 순수 코어가 프로필 검증과 반복 실행 정책을 담당하고, WinForms 앱이 창 선택과 사용자 제어를 제공한다. MaaFramework 의존성은 별도 어댑터에 격리한다.

**기술 스택:** .NET 8, WinForms, MaaFramework C# Binding, xUnit

**설계:** `AI_Docs/2026-10-22-generic-maafw-window-automation-design.md`

## 전역 제약

- 현재 작업 공간에서 작업하며 브랜치, worktree, 커밋, Push, PR을 만들지 않는다.
- 문서는 `AI_Docs/`에만 작성한다.
- 테스트 소스는 `Assets/Tests/EditMode~/`에만 작성한다.
- 사용자가 명시적으로 선택한 HWND만 제어한다.
- 무제한 반복, 탐지 회피, 프로세스 변조 기능을 만들지 않는다.

## 검토 중점

- 프로필 상대 경로가 `..` 또는 절대 경로로 프로필 루트 밖에 나가면 거부한다.
- HWND가 실행 중 재사용되거나 창 제목·프로세스가 바뀌면 세션을 중지한다.
- 중지와 Pipeline 완료가 동시에 발생해도 새 반복을 시작하지 않는다.
- Binding 연결 중 일부 객체만 생성된 경우에도 생성된 자원은 모두 해제한다.
- 긴급 중지 후 UI가 다시 실행 가능 상태로 복구된다.

---

### 작업 1: 순수 코어와 프로필 검증

**파일:**
- 생성: `Tools/RelicAutomation/src/RelicAutomation.Core/`
- 테스트: `Assets/Tests/EditMode~/RelicAutomationExternal/`

**제공 인터페이스:** `AutomationProfile`, `ProfileValidator.Validate`, `IWindowAutomationSession`, `AutomationRunCoordinator.RunAsync`

- [ ] 프로필 검증 및 반복 실행 상태의 실패 테스트를 작성한다.
- [ ] 테스트가 필요한 타입 누락으로 실패하는지 확인한다.
- [ ] 프로필 모델, Validator, Session 추상화, 반복 실행기를 최소 구현한다.
- [ ] 코어 테스트를 통과시킨다.

### 작업 2: Windows 창 선택과 WinForms 실행기

**파일:**
- 생성: `Tools/RelicAutomation/src/RelicAutomation.App/`

**사용 인터페이스:** 작업 1의 Profile Validator와 Run Coordinator

**제공 인터페이스:** `WindowCatalog.List`, `MainForm`, `EmergencyStopHotkey`

- [ ] HWND 스냅샷 필터링과 크기 검사의 실패 테스트를 작성한다.
- [ ] Windows 창 조회 서비스와 실행기 UI를 구현한다.
- [ ] 창·프로필 미선택, 크기 불일치, 중복 실행을 UI에서 차단한다.
- [ ] 긴급 중지와 로그 표시를 구현한다.

### 작업 3: MaaFramework 어댑터

**파일:**
- 생성: `Tools/RelicAutomation/src/RelicAutomation.Maa/`

**사용 인터페이스:** 작업 1의 `IWindowAutomationSession`, 작업 2에서 선택한 HWND

**제공 인터페이스:** `MaaWindowAutomationSession`

- [ ] MaaFramework C# Binding 및 Windows x64 런타임 패키지를 추가한다.
- [ ] Resource, Win32 Controller, Tasker 생명주기 어댑터를 구현한다.
- [ ] 연결·1회 실행·중지·해제 오류를 코어 종료 사유로 변환한다.
- [ ] 솔루션 빌드와 의존성 출력을 검증한다.

### 작업 4: RELIC 예제 프로필과 검증

**파일:**
- 생성: `Tools/RelicAutomation/Profiles/RELIC/`

**사용 인터페이스:** 작업 1의 프로필 형식과 작업 3의 Pipeline 실행 계약

- [ ] 예제 `profile.json`과 Pipeline v2 골격을 추가한다.
- [ ] 실제 PNG가 없으면 실행 전에 명확히 실패하도록 검증한다.
- [ ] 코어 테스트, 앱 빌드, `git diff --check`를 실행한다.
- [ ] 실제 RELIC 창 연결이 가능한 경우 수동 스모크 테스트를 수행한다.
