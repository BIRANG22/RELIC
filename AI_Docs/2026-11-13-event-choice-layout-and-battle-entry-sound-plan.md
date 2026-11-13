# 이벤트 선택지 정렬 및 전투 입장 사운드 복구 구현 계획

> **For agentic workers:** 현재 작업 공간에서 계획 순서대로 구현하고 검증한다.

**Goal:** 이벤트 2개 선택지 중앙 정렬을 복구하고 전투 입장 시 잘못된 사운드 ID 조회를 제거한다.

**Architecture:** 씬 레이아웃을 직접 제어하는 코드 추가 없이 회귀를 만든 씬 설정만 복구한다. 사운드는 무효 기본값과 자동 재생 설정을 제거하며 회귀 테스트로 씬 및 코드 설정을 고정한다.

**Tech Stack:** Unity 6, C#, NUnit, Unity YAML

**Spec:** `AI_Docs/2026-11-13-event-choice-layout-and-battle-entry-sound-design.md`

## 작업

- [ ] EditMode 회귀 테스트를 작성하고 현재 상태에서 실패를 확인한다.
- [ ] `Battle.unity` 이벤트 선택지 세 슬롯의 호버 설정을 머지 전 값으로 복구한다.
- [ ] `DBAudioSource.cs`, `Battle.unity`, `DebugBattle.unity`의 잘못된 기본 사운드 ID와 Char03 자동 재생을 제거한다.
- [ ] 회귀 테스트, C# 컴파일, YAML/문자열 검사를 수행한다.

## 검토 중점

- 선택지 세 슬롯 모두 위치 이동이 0인지 확인한다.
- Battle과 DebugBattle에서 존재하지 않는 ID가 남지 않는지 확인한다.
- Char03가 씬 활성화만으로 기본 SFX를 재생하지 않는지 확인한다.
- 실제 이벤트 1 천창 VFX의 `event.01.Skylight` 연결은 유지되는지 확인한다.
- 전투 상태 및 멀티플레이 동기화 경계에 변경이 없는지 확인한다.

