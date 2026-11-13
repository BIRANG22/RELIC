# Stage Map Type BGM State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 임시 BGM 상태명을 실제 Stage1 맵 타입 의미에 맞게 교체한다.

**Architecture:** 맵 선택 이벤트가 Stage/Type/전투 여부를 전달하고, 인스펙터 매핑이 적절한 BgmState를 선택한다. 미연결 상태 fallback은 기존 정책을 유지한다.

**Tech Stack:** Unity 6, C#, NUnit, Unity YAML

**Spec:** `AI_Docs/2026-11-13-stage-map-type-bgm-state-design.md`

## Task 1: 상태 및 선택 정책

- [ ] 새 상태명과 Stage/Type/Variant 매칭 테스트를 작성한다.
- [ ] enum과 디렉터 매핑 정책을 변경한다.

## Task 2: 맵 데이터 및 씬 연결

- [ ] 선택 노드의 MapData에서 Stage/Type/Variant를 전달한다.
- [ ] SoundDatabase 빈 슬롯과 Battle 씬 매핑을 새 상태로 변경한다.

## Task 3: 검증

- [ ] 런타임/에디터 프로젝트를 컴파일한다.
- [ ] CSV 타입과 씬 매핑을 정적 검사한다.
