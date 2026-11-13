# Combat AI Reward Presentation Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 7개 전투 AI·보상·연출 결함의 근본 원인을 수정한다.

**Architecture:** 전투 행동 결과와 프레젠테이션을 분리하고, 최종 위치·피해 확정 결과를 연출이 소비하게 한다. 보상은 seed 기반의 레어도 우선 추첨 정책을 공용화한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity YAML/CSV

**Spec:** `AI_Docs/2026-11-14-combat-ai-reward-presentation-fixes-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 사용하지 않는다.
- 전투 결과 랜덤은 `BattleRandom`을 사용한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 하지 않는다.

## Task 1: 몬스터 행동 결과와 연출

- [ ] 바로우 이동 Flip 표시 테스트를 RED로 확인하고 표시 메타데이터를 분리한다.
- [ ] 거미알 최종 칸 결과를 상태와 VFX가 공유하도록 테스트 후 수정한다.
- [ ] 아라벨라 최소 도주 및 경계 무피해 테스트 후 AI를 수정한다.
- [ ] 강습 착지 효과·사망 후 Idle 차단·전투 종료 순서 테스트 후 수정한다.
- [ ] 공격 impact 확정 후 이동과 tail 연출 병행 테스트 후 실행기를 수정한다.

## Task 2: 기억 보상과 코스트 표시

- [ ] 레어도별 후보 수와 무관한 독립 레어도 추첨 테스트를 RED로 확인한다.
- [ ] `BattleRandom` 기반 레어도 우선 추첨과 빈 등급 fallback을 구현한다.
- [ ] X/3X 수식 코스트 표시 테스트 후 보상 카드와 툴팁을 수정한다.

## Task 3: 통합 검증

- [ ] 변경별 테스트 소스를 검토한다.
- [ ] 런타임/에디터 프로젝트를 컴파일한다.
- [ ] 전체 diff와 멀티플레이 직렬화 영향을 검토한다.
